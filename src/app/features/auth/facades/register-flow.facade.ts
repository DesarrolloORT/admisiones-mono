import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { isNormalizedApiError } from '@desarrolloort/ngx-utils';
import { firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { Career, Comienzo } from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import {
  createCareerForm,
  createIdentityForm,
  createPersonalForm,
  syncDocumentNumberValidators,
} from '../forms/auth-forms';
import { toAuthRegisterPersonalData } from '../mappers/registration.mapper';
import { AuthIdentityData } from '../models/auth.interface';
import {
  CEDULA_DOCUMENT_TYPE,
  cleanDocumentNumber,
  getDocumentNumberLabel,
  isCedulaDocumentType,
} from '../models/document-number';
import { DocumentRecognitionFileError } from '../models/document-recognition-error';
import {
  getRegisterPersonalMode,
  isRegisterContinuableFlow,
  RegisterFlowKind,
  resolveRegisterFlow,
} from '../models/register-flow';
import { AcademicLevel, REGISTER_STEP_VIEW_MODELS, RegisterStep } from '../models/register-step';
import { DocumentPrefillResult, DocumentPrefillService } from '../services/document-prefill';
import { RegistrationService } from '../services/registration';

@Injectable({
  providedIn: 'root',
})
export class RegisterFlowFacade {
  private readonly catalogs = inject(Catalogs);
  private readonly documentPrefill = inject(DocumentPrefillService);
  private readonly registration = inject(RegistrationService);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarHandler);

  public readonly documentTypes$ = this.catalogs.getDocumentTypes();
  public readonly identityForm = createIdentityForm();
  public readonly personalForm = createPersonalForm();
  public readonly careerForm = createCareerForm();
  public readonly step = signal<RegisterStep>('identity');
  public readonly selectedFileName = signal<string | null>(null);
  public readonly isSubmitting = signal(false);
  public readonly isRecognizingDocument = signal(false);
  public readonly error = signal<string | null>(null);
  public readonly recognitionError = signal<string | null>(null);
  public readonly recognitionSuccessMessage = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);
  public readonly registrationFlow = signal<RegisterFlowKind | null>(null);
  public readonly careers = signal<Career[]>([]);
  public readonly comienzos = signal<Comienzo[]>([]);
  private readonly selectedAcademicLevel = signal<number | null>(null);

  private readonly _documentTypeValue = toSignal(
    this.identityForm.controls.documentType.valueChanges,
    { initialValue: this.identityForm.controls.documentType.value }
  );

  public readonly isCedulaInput = computed(() => isCedulaDocumentType(this._documentTypeValue()));

  public readonly documentNumberLabel = computed(() =>
    getDocumentNumberLabel(this._documentTypeValue())
  );

  public readonly isPersonalStep = computed(() => this.step() === 'personal');
  public readonly isCareerStep = computed(() => this.step() === 'career');
  public readonly personalMode = computed(() => getRegisterPersonalMode(this.registrationFlow()));
  public readonly stepViewModel = computed(() => REGISTER_STEP_VIEW_MODELS[this.step()]);
  public readonly filteredCareers = computed(() => {
    const nivel = this.selectedAcademicLevel();

    if (nivel === null) {
      return [];
    }

    return this.careers().filter(c => c.idNivelProducto === nivel);
  });
  public readonly academicLevels = computed<AcademicLevel[]>(() => {
    const seen = new Map<number, string>();

    for (const career of this.careers()) {
      if (!seen.has(career.idNivelProducto)) {
        seen.set(career.idNivelProducto, career.nombreNivelProducto);
      }
    }

    return Array.from(seen, ([id, nombre]) => ({ id, nombre }));
  });

  constructor() {
    effect(() => {
      syncDocumentNumberValidators(
        this.identityForm.controls.documentNumber,
        this._documentTypeValue()
      );
    });
  }

  public async onDocumentSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement | null;
    const selectedFile = input?.files?.item(0) ?? null;

    this.selectedFileName.set(selectedFile?.name ?? null);
    this.recognitionError.set(null);
    this.recognitionSuccessMessage.set(null);
    this.error.set(null);
    this.registrationFlow.set(null);
    this.clearRecognizedFields();

    if (!selectedFile) {
      return;
    }

    await this.preloadDocumentData(selectedFile);
  }

  public async continueToPersonalData(): Promise<void> {
    if (this.isRecognizingDocument()) {
      this.showError('Esperá a que termine la precarga del documento.');
      return;
    }

    syncDocumentNumberValidators(
      this.identityForm.controls.documentNumber,
      this._documentTypeValue()
    );

    if (this.identityForm.invalid) {
      this.identityForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.registrationFlow.set(null);
    this.isSubmitting.set(true);

    const identity = this.getCleanIdentityValues();
    try {
      const result = await firstValueFrom(this.registration.evaluateDocument(identity));
      const flow = resolveRegisterFlow(identity.documentType, result);

      this.registrationFlow.set(flow);

      if (!flow) {
        this.showError('No se pudo determinar el flujo de registro para este documento.');
        return;
      }

      if (flow === 'user-exists' || flow === 'application-exists') {
        this.showGoToLoginSnackbar(result.message, identity.documentType, identity.documentNumber);
        return;
      }

      this.step.set('personal');
    } catch (error) {
      this.setError(this.getApiErrorMessage(error, 'No se pudo completar el registro.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  public backToIdentity(): void {
    this.error.set(null);
    this.successMessage.set(null);
    this.registrationFlow.set(null);
    this.step.set('identity');
  }

  public backToPersonal(): void {
    this.error.set(null);
    this.successMessage.set(null);
    this.step.set('personal');
  }

  public submitPersonalData(): void {
    const flow = this.registrationFlow();

    switch (flow) {
      case 'existing-person':
        this.submitVerification();
        return;
      case 'new-person':
      case 'new-application':
        this.submitFullPersonalData();
        return;
      case 'user-exists':
      case 'application-exists':
      case null:
        this.showError('Primero evaluá el documento para continuar.');
        return;
    }
  }

  private submitFullPersonalData(): void {
    if (this.personalForm.invalid) {
      this.personalForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.loadCareers();
    this.step.set('career');
  }

  private submitVerification(): void {
    const { primerApellido, mail } = this.personalForm.controls;

    primerApellido.markAsTouched();
    mail.markAsTouched();

    if (primerApellido.invalid || mail.invalid) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    this.registration
      .verifyExistingPersonIdentity({
        identity: this.getCleanIdentityValues(),
        primerApellido: primerApellido.value,
        mail: mail.value,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: result => {
          if (result.success) {
            this.loadCareers();
            this.step.set('career');
          } else {
            this.snackbar.error('No se pudo verificar la identidad.');
          }
        },
        error: error => {
          const message = this.getApiErrorMessage(error, 'No se pudo completar el registro.');
          this.snackbar.error(message);
        },
      });
  }

  public onPropuestaChange(): void {
    const level = this.careerForm.controls.propuestaAcademica.value;
    this.selectedAcademicLevel.set(level);
    this.careerForm.controls.carrera.reset(null);
    this.careerForm.controls.comienzo.reset(null);
    this.comienzos.set([]);
  }

  public onCarreraChange(): void {
    this.careerForm.controls.comienzo.reset(null);
    this.comienzos.set([]);
    const idCarrera = this.careerForm.controls.carrera.value;

    if (idCarrera !== null) {
      this.loadComienzos(idCarrera);
    }
  }

  public submitCareerData(): void {
    const flow = this.registrationFlow();

    if (!isRegisterContinuableFlow(flow)) {
      this.showError('Primero evaluá el documento para continuar.');
      return;
    }

    if (this.careerForm.invalid) {
      this.careerForm.markAllAsTouched();
      return;
    }

    const selection = this.getCareerSelection();

    if (!selection) {
      this.careerForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    this.registration
      .confirmCareerInterest({
        flow,
        identity: this.getCleanIdentityValues(),
        personal:
          flow === 'existing-person'
            ? null
            : toAuthRegisterPersonalData(this.personalForm.getRawValue()),
        selection,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.showSuccess(
            'Cuenta creada correctamente. Revisá tu correo para obtener la contraseña.'
          );
        },
        error: error => {
          this.setError(this.getApiErrorMessage(error, 'No se pudo completar el registro.'));
        },
      });
  }

  private getCareerSelection(): { idProducto: number; idProceso: number } | null {
    const { carrera, comienzo } = this.careerForm.getRawValue();

    if (carrera === null || comienzo === null) {
      return null;
    }

    return {
      idProducto: carrera,
      idProceso: comienzo,
    };
  }

  private getCleanIdentityValues(): AuthIdentityData {
    const { documentType, documentNumber } = this.identityForm.getRawValue();

    return {
      documentType,
      documentNumber: cleanDocumentNumber(documentType, documentNumber),
    };
  }

  private loadCareers(): void {
    this.catalogs.getCareers().subscribe({
      next: careers => this.careers.set(careers),
      error: error =>
        this.setError(this.getApiErrorMessage(error, 'No se pudieron cargar las carreras.')),
    });
  }

  private loadComienzos(idCarrera: number): void {
    this.catalogs.getComienzos(idCarrera).subscribe({
      next: comienzos => this.comienzos.set(comienzos),
      error: error =>
        this.setError(this.getApiErrorMessage(error, 'No se pudieron cargar los comienzos.')),
    });
  }

  private async preloadDocumentData(file: File): Promise<void> {
    this.isRecognizingDocument.set(true);

    try {
      const result = await this.documentPrefill.preload(file);
      this.applyRecognizedFields(result);
      this.showRecognitionSuccess('Datos precargados. Revisalos antes de continuar.');
    } catch (error) {
      this.handleDocumentRecognitionError(error);
    } finally {
      this.isRecognizingDocument.set(false);
    }
  }

  private applyRecognizedFields(result: DocumentPrefillResult): void {
    if (!result.patch) {
      return;
    }

    this.identityForm.patchValue(result.patch.identity);
    this.personalForm.patchValue(result.patch.personal);

    if (result.location) {
      this.personalForm.controls.location.setValue(result.location);
    }
  }

  private clearRecognizedFields(): void {
    this.identityForm.patchValue({ documentType: CEDULA_DOCUMENT_TYPE, documentNumber: '' });
    this.personalForm.patchValue({
      primerNombre: '',
      segundoNombre: '',
      primerApellido: '',
      segundoApellido: '',
      fechaNacimiento: '',
      sexo: '',
    });
    this.personalForm.controls.location.setValue({
      codigoPais: null,
      codigoEstado: null,
      codigoCiudad: null,
    });
  }

  private setError(message: string): void {
    this.error.set(message);
  }

  private getApiErrorMessage(error: unknown, fallback: string): string {
    return isNormalizedApiError(error) ? error.message : fallback;
  }

  private showError(message: string): void {
    this.error.set(message);
    this.snackbar.error(message);
  }

  private showGoToLoginSnackbar(
    backendMessage: string | null,
    documentType: string,
    documentNumber: string
  ): void {
    this.snackbar.show({
      message: backendMessage ?? 'Ya existe un registro con este documento.',
      variant: 'warning',
      actionLabel: 'Iniciar sesión',
      duration: 10000,
      action: () => {
        void this.router.navigate(['/iniciar-sesion'], {
          queryParams: { tipoDoc: documentType, doc: documentNumber },
        });
      },
    });
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    this.snackbar.success(message);
  }

  private showRecognitionError(message: string): void {
    this.recognitionError.set(message);
    this.snackbar.error(message);
  }

  private setRecognitionError(message: string): void {
    this.recognitionError.set(message);
  }

  private showRecognitionSuccess(message: string): void {
    this.recognitionSuccessMessage.set(message);
    this.snackbar.success(message);
  }

  private handleDocumentRecognitionError(error: unknown): void {
    if (error instanceof DocumentRecognitionFileError) {
      this.showRecognitionError(this.getDocumentRecognitionFileErrorMessage(error));
      return;
    }

    this.setRecognitionError(this.getApiErrorMessage(error, 'No se pudo precargar el documento.'));
  }

  private getDocumentRecognitionFileErrorMessage(error: DocumentRecognitionFileError): string {
    if (error.code === 'maxFileSize') {
      return 'El archivo supera el límite de 10 MB.';
    }

    if (error.code === 'invalidMimeType') {
      return 'El archivo seleccionado no tiene un tipo válido.';
    }

    return 'No se pudo leer el archivo seleccionado.';
  }
}

