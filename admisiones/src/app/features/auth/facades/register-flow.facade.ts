import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AsyncValidatorFn } from '@angular/forms';
import { Router } from '@angular/router';
import type { OrtPhoneInputValue } from '@desarrolloort/components';
import { firstValueFrom, of } from 'rxjs';
import { catchError, filter, finalize, map, take } from 'rxjs/operators';
import { getApiErrorMessage } from 'src/app/shared/errors/api-error-message';

import { MAX_IMAGE_SIZE_BYTES } from '../../../shared/files/image-upload';
import { toBackendPhone } from '../../../shared/forms/phone';
import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { AccountApi } from '../api/account.api';
import { AuthApi } from '../api/auth.api';
import {
  createIdentityForm,
  createPersonalForm,
  syncDocumentNumberValidators,
} from '../forms/auth-forms';
import {
  toAuthRegisterPersonalData,
  toRegisterPayload,
  toVerifyIdentityPayload,
} from '../mappers/registration.mapper';
import { AuthIdentityData } from '../models/auth.interface';
import {
  cleanDocumentNumber,
  formatDocumentForBackend,
  getDocumentNumberLabel,
  NATIONAL_ID_DOCUMENT_TYPE,
} from '../models/document-number';
import { DocumentRecognitionFileError } from '../models/document-recognition-error';
import {
  getRegisterPersonalMode,
  isRegisterContinuableFlow,
  RegisterFlowKind,
  RegistrationOutcome,
  resolveRegisterFlow,
  resolveRegistrationEnding,
} from '../models/register-flow';
import { REGISTER_STEP_VIEW_MODELS, RegisterStep } from '../models/register-step';
import { DocumentPrefillResult, DocumentPrefillService } from '../services/document-prefill';

export class RegisterFlowFacade {
  private readonly account = inject(AccountApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthApi);
  private readonly documentPrefill = inject(DocumentPrefillService);
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
    this.personalForm.controls.primaryPhone.addAsyncValidators(this.phoneValidator());

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
      const result = await firstValueFrom(
        this.auth.evaluateDocument({
          documentType: identity.documentType,
          documentNumber: formatDocumentForBackend(identity.documentType, identity.documentNumber),
        })
      );
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

      if (flow === 'user-exists') {
        this.showGoToLoginSnackbar(result.message ?? undefined);
        return;
      }

      if (flow === 'application-exists') {
        this.showPendingApplicationSnackbar(result.message ?? undefined);
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

    const phone = this.personalForm.controls.primaryPhone;

    if (phone.pending) {
      phone.statusChanges
        .pipe(
          filter(status => status !== 'PENDING'),
          take(1),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe(() => this.submitFullRegistration(flow));

      // La validacion en curso pudo dispararse sin emitEvent (el control es updateOn:
      // 'blur'), asi que se relanza para garantizar la notificacion.
      phone.updateValueAndValidity();
      return;
    }

    if (this.personalForm.invalid) {
      this.personalForm.markAllAsTouched();
      return;
    }

    this.isCompleted.set(false);
    this.isSubmitting.set(true);

    const payload = toRegisterPayload({
      identity: this.getCleanIdentityValues(),
      personal: toAuthRegisterPersonalData(this.personalForm.getRawValue()),
    });

    (flow === 'new-person'
      ? this.auth.register(payload, flowId)
      : this.auth.confirmApplicationRequest(payload, flowId)
    )
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: result => {
          this.navigateToEmailConfirmation(result);
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

    const { firstSurname, email } = this.personalForm.controls;

    firstSurname.markAsTouched();
    email.markAsTouched();

    if (firstSurname.invalid || email.invalid) {
      return;
    }

    this.isCompleted.set(false);
    this.isSubmitting.set(true);

    this.auth
      .verifyIdentity(
        toVerifyIdentityPayload({
          identity: this.getCleanIdentityValues(),
          firstSurname: firstSurname.value,
          email: email.value,
        }),
        flowId
      )
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: result => {
          this.navigateToEmailConfirmation({ pendingReview: false, mailSent: result.mailSent });
        },
        error: error => {
          const message = getApiErrorMessage(error, 'No se pudo completar el registro.');
          this.snackbar.error(message);
        },
      });
  }

  private phoneValidator(): AsyncValidatorFn {
    return control => {
      const value = control.value as OrtPhoneInputValue | null;

      if (!value?.number.trim()) {
        return of(null);
      }

      return this.account.validatePhone(toBackendPhone(value)).pipe(
        map(isValid => (isValid ? null : { phone: true })),
        catchError((error: unknown) => {
          this.snackbar.error(getApiErrorMessage(error, 'No se pudo validar el celular.'));
          return of({ phoneValidation: true });
        })
      );
    };
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
    this.identityForm.patchValue({ documentType: NATIONAL_ID_DOCUMENT_TYPE, documentNumber: '' });
    this.personalForm.patchValue({
      firstName: '',
      middleName: '',
      firstSurname: '',
      secondSurname: '',
      birthDate: '',
      sex: '',
    });
    this.personalForm.controls.location.setValue({
      countryCode: null,
      stateCode: null,
      cityCode: null,
    });
  }

  private clearRegistrationFlow(): void {
    this.registrationFlow.set(null);
    this.registrationFlowId.set(null);
  }

  private showError(message: string): void {
    this.snackbar.error(message);
  }

  private showGoToLoginSnackbar(message = 'No se pudo continuar con el registro.'): void {
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

  private showPendingApplicationSnackbar(message = 'No se pudo continuar con el registro.'): void {
    this.snackbar.show({
      message,
      variant: 'warning',
      duration: 10000,
    });
  }

  private showMissingActivationEmailSnackbar(): void {
    this.snackbar.show({
      message: 'No pudimos enviarte el correo de activación.',
      hint: 'Tu cuenta quedó creada: usá "Recuperar acceso" para definir tu contraseña.',
      variant: 'warning',
      actionLabel: 'Recuperar acceso',
      duration: 10000,
      action: () => {
        this.router.navigateByUrl('/recuperar-acceso');
      },
    });
  }

  private navigateToEmailConfirmation(outcome: RegistrationOutcome): void {
    const { route, missingActivationEmail } = resolveRegistrationEnding(outcome);

    this.isCompleted.set(true);
    this.router.navigateByUrl(route);

    if (missingActivationEmail) {
      this.showMissingActivationEmailSnackbar();
    }
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
      const maxSizeMb = MAX_IMAGE_SIZE_BYTES / (1024 * 1024);
      return `El archivo supera el límite de ${maxSizeMb} MB.`;
    }

    if (error.code === 'invalidMimeType') {
      return 'El archivo seleccionado no tiene un tipo válido.';
    }

    return 'No se pudo leer el archivo seleccionado.';
  }
}
