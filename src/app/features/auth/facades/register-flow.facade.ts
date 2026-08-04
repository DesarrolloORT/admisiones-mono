import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import {
  createIdentityForm,
  createPersonalForm,
  syncDocumentNumberValidators,
} from '../forms/auth-forms';
import { toAuthRegisterPersonalData } from '../mappers/registration.mapper';
import { getApiErrorMessage } from '../models/api-error-message';
import { AuthIdentityData } from '../models/auth.interface';
import {
  CEDULA_DOCUMENT_TYPE,
  cleanDocumentNumber,
  getDocumentNumberLabel,
} from '../models/document-number';
import { DocumentRecognitionFileError } from '../models/document-recognition-error';
import {
  getRegisterPersonalMode,
  isRegisterContinuableFlow,
  RegisterFlowKind,
  resolveRegisterFlow,
} from '../models/register-flow';
import { REGISTER_STEP_VIEW_MODELS, RegisterStep } from '../models/register-step';
import { DocumentPrefillResult, DocumentPrefillService } from '../services/document-prefill';
import { RegistrationService } from '../services/registration';

export class RegisterFlowFacade {
  private readonly destroyRef = inject(DestroyRef);
  private readonly documentPrefill = inject(DocumentPrefillService);
  private readonly registration = inject(RegistrationService);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarHandler);

  public readonly identityForm = createIdentityForm();
  public readonly personalForm = createPersonalForm();
  public readonly step = signal<RegisterStep>('identity');
  public readonly selectedFileName = signal<string | null>(null);
  public readonly isSubmitting = signal(false);
  public readonly isRecognizingDocument = signal(false);
  public readonly isCompleted = signal(false);
  public readonly registrationFlow = signal<RegisterFlowKind | null>(null);
  public readonly registrationFlowId = signal<string | null>(null);

  private readonly _documentTypeValue = toSignal(
    this.identityForm.controls.documentType.valueChanges,
    { initialValue: this.identityForm.controls.documentType.value }
  );

  public readonly documentNumberLabel = computed(() =>
    getDocumentNumberLabel(this._documentTypeValue())
  );

  public readonly isPersonalStep = computed(() => this.step() === 'personal');
  public readonly personalMode = computed(() => getRegisterPersonalMode(this.registrationFlow()));
  public readonly stepViewModel = computed(() => {
    const viewModel = REGISTER_STEP_VIEW_MODELS[this.step()];

    return this.isPersonalStep() && this.personalMode() === 'verification'
      ? {
          ...viewModel,
          title: 'Verificación de identidad',
          stepTitle: 'Verificación de identidad',
        }
      : viewModel;
  });

  constructor() {
    effect(() => {
      syncDocumentNumberValidators(
        this.identityForm.controls.documentNumber,
        this._documentTypeValue()
      );
    });
  }

  public async onDocumentSelected(selectedFile: File | null): Promise<void> {
    this.selectedFileName.set(selectedFile?.name ?? null);
    this.clearRegistrationFlow();
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

    this.isCompleted.set(false);
    this.clearRegistrationFlow();
    this.isSubmitting.set(true);

    const identity = this.getCleanIdentityValues();
    try {
      const result = await firstValueFrom(this.registration.evaluateDocument(identity));
      const flow = resolveRegisterFlow(identity.documentType, result);

      this.registrationFlow.set(flow);
      this.registrationFlowId.set(result.flowId);

      if (!flow) {
        this.showError('No se pudo determinar el flujo de registro para este documento.');
        return;
      }

      if (isRegisterContinuableFlow(flow) && !result.flowId) {
        this.showError('No se pudo iniciar el flujo de registro. Intentá nuevamente.');
        return;
      }

      if (flow === 'user-exists' || flow === 'application-exists') {
        this.showGoToLoginSnackbar(result.message ?? undefined);
        return;
      }

      this.step.set('personal');
    } catch (error) {
      this.showError(getApiErrorMessage(error, 'No se pudo completar el registro.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  public backToIdentity(): void {
    this.isCompleted.set(false);
    this.clearRegistrationFlow();
    this.step.set('identity');
  }

  public navigateToLogin(): void {
    this.router.navigateByUrl('/iniciar-sesion');
  }

  public submitPersonalData(): void {
    const flow = this.registrationFlow();

    switch (flow) {
      case 'existing-person':
        this.submitVerification();
        return;
      case 'new-person':
      case 'new-application':
        this.submitFullRegistration(flow);
        return;
      case 'user-exists':
      case 'application-exists':
      case null:
        this.showError('Primero evaluá el documento para continuar.');
        return;
    }
  }

  private submitFullRegistration(flow: 'new-person' | 'new-application'): void {
    const flowId = this.registrationFlowId();

    if (!flowId) {
      this.showError('Primero evaluá el documento para continuar.');
      return;
    }

    if (this.personalForm.invalid) {
      this.personalForm.markAllAsTouched();
      return;
    }

    this.isCompleted.set(false);
    this.isSubmitting.set(true);

    this.registration
      .confirmRegistration({
        flow,
        flowId,
        identity: this.getCleanIdentityValues(),
        personal: toAuthRegisterPersonalData(this.personalForm.getRawValue()),
      })
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.navigateToEmailConfirmation();
        },
        error: error => {
          this.showError(getApiErrorMessage(error, 'No se pudo completar el registro.'));
        },
      });
  }

  private submitVerification(): void {
    const flowId = this.registrationFlowId();

    if (!flowId) {
      this.showError('Primero evaluá el documento para continuar.');
      return;
    }

    const { primerApellido, mail } = this.personalForm.controls;

    primerApellido.markAsTouched();
    mail.markAsTouched();

    if (primerApellido.invalid || mail.invalid) {
      return;
    }

    this.isCompleted.set(false);
    this.isSubmitting.set(true);

    this.registration
      .verifyExistingPersonIdentity({
        flowId,
        identity: this.getCleanIdentityValues(),
        primerApellido: primerApellido.value,
        mail: mail.value,
      })
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: result => {
          if (result.success) {
            this.navigateToEmailConfirmation();
          } else {
            this.snackbar.error('No se pudo verificar la identidad.');
          }
        },
        error: error => {
          const message = getApiErrorMessage(error, 'No se pudo completar el registro.');
          this.snackbar.error(message);
        },
      });
  }

  private getCleanIdentityValues(): AuthIdentityData {
    const { documentType, documentNumber } = this.identityForm.getRawValue();

    return {
      documentType,
      documentNumber: cleanDocumentNumber(documentType, documentNumber),
    };
  }

  private async preloadDocumentData(file: File): Promise<void> {
    this.isRecognizingDocument.set(true);

    try {
      const result = await this.documentPrefill.preload(file);
      this.applyRecognizedFields(result);
      this.snackbar.success('Datos precargados. Revisalos antes de continuar.');
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

  private clearRegistrationFlow(): void {
    this.registrationFlow.set(null);
    this.registrationFlowId.set(null);
  }

  private showError(message: string): void {
    this.snackbar.error(message);
  }

  private showGoToLoginSnackbar(message = 'Ya existe un registro con este documento.'): void {
    this.snackbar.show({
      message,
      variant: 'warning',
      actionLabel: 'Iniciar sesión',
      duration: 10000,
      action: () => {
        this.router.navigateByUrl('/iniciar-sesion');
      },
    });
  }

  private navigateToEmailConfirmation(): void {
    this.isCompleted.set(true);
    this.router.navigateByUrl('/confirmacion-correo/registro');
  }

  private handleDocumentRecognitionError(error: unknown): void {
    if (error instanceof DocumentRecognitionFileError) {
      this.showError(this.getDocumentRecognitionFileErrorMessage(error));
      return;
    }

    this.showError(getApiErrorMessage(error, 'No se pudo precargar el documento.'));
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
