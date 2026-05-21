import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { Career, Comienzo } from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import {
  CEDULA_DOCUMENT_NUMBER_VALIDATORS,
  createCareerForm,
  createIdentityForm,
  createPersonalForm,
  emailsMatch,
  NON_CEDULA_DOCUMENT_NUMBER_VALIDATORS,
} from '../forms/auth-forms';
import {
  resolveStateCodeFromBirthplace,
  toRecognizedFormPatch,
} from '../mappers/document-recognition.mapper';
import { AuthRequestError } from '../models/auth-error';
import {
  DocumentRecognitionFileError,
  DocumentRecognitionRequestError,
} from '../models/document-recognition-error';
import { AcademicLevel, REGISTER_STEP_VIEW_MODELS, RegisterStep } from '../models/register-step';
import { Auth } from '../services/auth';
import { DocumentRecognition } from '../services/document-recognition';
import { RegisterDocumentStore } from '../store/register-document.store';

@Injectable({
  providedIn: 'root',
})
export class RegisterFlowFacade {
  private readonly auth = inject(Auth);
  private readonly catalogs = inject(Catalogs);
  private readonly documentRecognition = inject(DocumentRecognition);
  private readonly registerDocumentStore = inject(RegisterDocumentStore);
  private readonly snackbar = inject(SnackbarHandler);

  public readonly documentTypes$ = this.catalogs.getDocumentTypes();
  public readonly identityForm = createIdentityForm();
  public readonly personalForm = createPersonalForm();
  public readonly careerForm = createCareerForm();
  public readonly step = signal<RegisterStep>('identity');
  public readonly selectedFileName = this.registerDocumentStore.selectedFileName;
  public readonly isSubmitting = signal(false);
  public readonly isRecognizingDocument = signal(false);
  public readonly error = signal<string | null>(null);
  public readonly recognitionError = signal<string | null>(null);
  public readonly recognitionSuccessMessage = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);
  public readonly requiresVerification = signal(false);
  public readonly careers = signal<Career[]>([]);
  public readonly comienzos = signal<Comienzo[]>([]);
  private readonly selectedAcademicLevel = signal<number | null>(null);

  private readonly _documentTypeValue = toSignal(
    this.identityForm.controls.documentType.valueChanges,
    { initialValue: this.identityForm.controls.documentType.value }
  );

  public readonly isCedulaInput = computed(() => this._documentTypeValue() === 'CI');

  public readonly isPersonalStep = computed(() => this.step() === 'personal');
  public readonly isCareerStep = computed(() => this.step() === 'career');
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
      const validators = this.isCedulaInput()
        ? CEDULA_DOCUMENT_NUMBER_VALIDATORS
        : NON_CEDULA_DOCUMENT_NUMBER_VALIDATORS;

      this.identityForm.controls.documentNumber.setValidators(validators);
      this.identityForm.controls.documentNumber.updateValueAndValidity({ emitEvent: false });
    });
  }

  public async onDocumentSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement | null;
    const selectedFile = input?.files?.item(0) ?? null;

    this.registerDocumentStore.setSelectedFile(selectedFile);
    this.registerDocumentStore.clearRecognition();
    this.recognitionError.set(null);
    this.recognitionSuccessMessage.set(null);
    this.error.set(null);
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

    if (this.identityForm.invalid) {
      this.identityForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    const { documentType, documentNumber } = this.getCleanIdentityValues();
    try {
      const result = await firstValueFrom(this.auth.evaluateDocument(documentType, documentNumber));

      if (result.usuarioExistente) {
        this.showError('Ya existe un usuario registrado con este documento.');
        return;
      }

      if (result.solicitudAltaExistente) {
        this.showError('Ya existe una solicitud de alta pendiente para este documento.');
        return;
      }

      this.requiresVerification.set(result.requiereVerificacion);
      this.step.set('personal');
    } catch (error) {
      this.showError(this.getErrorMessage(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  public backToIdentity(): void {
    this.error.set(null);
    this.successMessage.set(null);
    this.step.set('identity');
  }

  public backToPersonal(): void {
    this.error.set(null);
    this.successMessage.set(null);
    this.step.set('personal');
  }

  public submitPersonalData(): void {
    if (this.requiresVerification()) {
      this.submitVerification();
      return;
    }

    if (this.personalForm.invalid) {
      this.personalForm.markAllAsTouched();
      return;
    }

    if (!emailsMatch(this.personalForm)) {
      this.showError('Los e-mails ingresados no coinciden.');
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    const { documentType, documentNumber } = this.getCleanIdentityValues();
    const { primerApellido, mail, verificacionMail } = this.personalForm.getRawValue();

    this.auth
      .verifyIdentity({
        tipoDocumento: documentType,
        documento: documentNumber,
        primerApellido,
        mail,
        verificacionMail,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.loadCareers();
          this.step.set('career');
        },
        error: error => {
          this.showError(this.getErrorMessage(error));
        },
      });
  }

  private submitVerification(): void {
    const { primerApellido, mail, verificacionMail } = this.personalForm.controls;

    primerApellido.markAsTouched();
    mail.markAsTouched();
    verificacionMail.markAsTouched();

    if (primerApellido.invalid || mail.invalid || verificacionMail.invalid) {
      return;
    }

    if (!emailsMatch(this.personalForm)) {
      this.showError('Los e-mails ingresados no coinciden.');
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    const { documentType, documentNumber } = this.getCleanIdentityValues();

    this.auth
      .verifyIdentity({
        tipoDocumento: documentType,
        documento: documentNumber,
        primerApellido: primerApellido.value,
        mail: mail.value,
        verificacionMail: verificacionMail.value,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: result => {
          if (result.success) {
            this.loadCareers();
            this.step.set('career');
          } else {
            this.showError('No se pudo verificar la identidad.');
          }
        },
        error: error => {
          this.showError(this.getErrorMessage(error));
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
    if (this.careerForm.invalid) {
      this.careerForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    const { documentType, documentNumber } = this.getCleanIdentityValues();
    const { carrera, comienzo } = this.careerForm.getRawValue();

    this.auth
      .confirmExistingPerson({
        tipoDocumento: documentType,
        documento: documentNumber,
        idProducto: carrera ?? undefined,
        idProceso: comienzo ?? undefined,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.showSuccess(
            'Cuenta creada correctamente. Revisá tu correo para obtener la contraseña.'
          );
        },
        error: error => {
          this.showError(this.getErrorMessage(error));
        },
      });
  }

  private getCleanIdentityValues(): { documentType: string; documentNumber: string } {
    const { documentType, documentNumber } = this.identityForm.getRawValue();

    return {
      documentType,
      documentNumber: documentType === 'CI' ? documentNumber.replace(/\D/g, '') : documentNumber,
    };
  }

  private loadCareers(): void {
    this.catalogs.getCareers().subscribe({
      next: careers => this.careers.set(careers),
      error: () => this.showError('No se pudieron cargar las carreras.'),
    });
  }

  private loadComienzos(idCarrera: number): void {
    this.catalogs.getComienzos(idCarrera).subscribe({
      next: comienzos => this.comienzos.set(comienzos),
      error: () => this.showError('No se pudieron cargar los comienzos.'),
    });
  }

  private async preloadDocumentData(file: File): Promise<void> {
    this.isRecognizingDocument.set(true);

    try {
      const payload = await this.documentRecognition.createRequestFromFile(file);
      const response = await firstValueFrom(this.documentRecognition.recognizeDocument(payload));

      this.registerDocumentStore.setRecognitionResponse(response);
      await this.applyRecognizedFields(response.data?.campos);
      this.showRecognitionSuccess('Datos precargados. Revisalos antes de continuar.');
    } catch (error) {
      this.showRecognitionError(this.getDocumentRecognitionErrorMessage(error));
    } finally {
      this.isRecognizingDocument.set(false);
    }
  }

  private async applyRecognizedFields(
    fields: Parameters<typeof toRecognizedFormPatch>[0]
  ): Promise<void> {
    const patch = toRecognizedFormPatch(fields);

    if (!patch) {
      return;
    }

    this.identityForm.patchValue(patch.identity);
    this.personalForm.patchValue(patch.personal);

    if (patch.countryCode !== null) {
      const stateCode = await this.resolveStateCodeFromBirthplace(
        patch.countryCode,
        patch.birthplace
      );
      this.personalForm.controls.location.setValue({
        codigoPais: patch.countryCode,
        codigoEstado: stateCode,
        codigoCiudad: null,
      });
    }
  }

  private async resolveStateCodeFromBirthplace(
    countryCode: number,
    birthplace: string | null | undefined
  ): Promise<number | null> {
    try {
      const locations = await firstValueFrom(this.catalogs.getCountryLocations());
      return resolveStateCodeFromBirthplace(locations, countryCode, birthplace);
    } catch {
      return null;
    }
  }

  private clearRecognizedFields(): void {
    this.identityForm.patchValue({ documentType: 'CI', documentNumber: '' });
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

  private getErrorMessage(error: unknown): string {
    if (error instanceof AuthRequestError) {
      return `No se pudo completar el registro. Error ${error.status || 'de red'}.`;
    }

    return 'No se pudo completar el registro.';
  }

  private showError(message: string): void {
    this.error.set(message);
    this.snackbar.error(message);
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    this.snackbar.success(message);
  }

  private showRecognitionError(message: string): void {
    this.recognitionError.set(message);
    this.snackbar.error(message);
  }

  private showRecognitionSuccess(message: string): void {
    this.recognitionSuccessMessage.set(message);
    this.snackbar.success(message);
  }

  private getDocumentRecognitionErrorMessage(error: unknown): string {
    if (error instanceof DocumentRecognitionFileError) {
      if (error.code === 'maxFileSize') {
        return 'El archivo supera el límite de 10 MB.';
      }

      if (error.code === 'invalidMimeType') {
        return 'El archivo seleccionado no tiene un tipo válido.';
      }

      return 'No se pudo leer el archivo seleccionado.';
    }

    if (error instanceof DocumentRecognitionRequestError) {
      return `No se pudo precargar el documento. Error ${error.status || 'de red'}.`;
    }

    return 'No se pudo precargar el documento.';
  }
}
