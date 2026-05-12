import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import { firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { AuthForm } from '../../components/auth-form/auth-form';
import { AuthRegisterPersonalData } from '../../models/auth.interface';
import { AuthRequestError } from '../../models/auth-error';
import { DocumentRecognitionFields } from '../../models/document-recognition.interface';
import {
  DocumentRecognitionFileError,
  DocumentRecognitionRequestError,
} from '../../models/document-recognition-error';
import { Auth } from '../../services/auth';
import { DocumentRecognition } from '../../services/document-recognition';
import { RegisterDocumentStore } from '../../store/register-document.store';

type RegisterStep = 'identity' | 'personal';

interface IdentityForm {
  documentType: FormControl<string>;
  documentNumber: FormControl<string>;
}

interface PersonalForm {
  firstName: FormControl<string>;
  secondName: FormControl<string>;
  firstLastName: FormControl<string>;
  secondLastName: FormControl<string>;
  birthDate: FormControl<string>;
  sex: FormControl<string>;
  country: FormControl<string>;
  address: FormControl<string>;
  phone: FormControl<string>;
  email: FormControl<string>;
  confirmEmail: FormControl<string>;
}

@Component({
  selector: 'app-register',
  imports: [
    AuthForm,
    OrtFormFieldModule,
    OrtInputModule,
    OrtSelectModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  private readonly auth = inject(Auth);
  private readonly documentRecognition = inject(DocumentRecognition);
  private readonly registerDocumentStore = inject(RegisterDocumentStore);

  protected readonly step = signal<RegisterStep>('identity');
  protected readonly selectedFileName = this.registerDocumentStore.selectedFileName;
  protected readonly isSubmitting = signal(false);
  protected readonly isRecognizingDocument = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly recognitionError = signal<string | null>(null);
  protected readonly recognitionSuccessMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly isPersonalStep = computed(() => this.step() === 'personal');
  protected readonly pageTitle = computed(() =>
    this.isPersonalStep() ? 'Datos personales' : 'Crear cuenta'
  );
  protected readonly pageDescription = computed(() =>
    this.isPersonalStep() ? 'Paso 1 de 2' : 'El registro te llevará solo unos minutos.'
  );
  protected readonly heroIcon = computed(() =>
    this.isPersonalStep() ? 'public' : 'settings_suggest'
  );
  protected readonly heroTitle = computed(() =>
    this.isPersonalStep() ? 'Proyección global.' : 'Tecnología aplicada en aulas.'
  );
  protected readonly heroDescription = computed(() =>
    this.isPersonalStep()
      ? 'Validá tu talento con una formación alineada a estándares internacionales.'
      : 'Aprendizaje práctico en laboratorios de vanguardia desde el primer día.'
  );
  protected readonly stepLabel = computed(() => (this.isPersonalStep() ? 'Paso 1 de 2' : null));
  protected readonly stepTitle = computed(() =>
    this.isPersonalStep() ? 'Datos personales' : null
  );
  protected readonly cardSize = computed(() => (this.isPersonalStep() ? 'long' : 'default'));

  protected readonly identityForm = new FormGroup<IdentityForm>({
    documentType: new FormControl('CI', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    documentNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[0-9A-Za-z]+$/)],
    }),
  });

  protected readonly personalForm = new FormGroup<PersonalForm>({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    secondName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    firstLastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    secondLastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    birthDate: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    sex: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    country: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    phone: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    confirmEmail: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
  });

  protected async onDocumentSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement | null;
    const selectedFile = input?.files?.item(0) ?? null;

    this.registerDocumentStore.setSelectedFile(selectedFile);
    this.registerDocumentStore.clearRecognition();
    this.recognitionError.set(null);
    this.recognitionSuccessMessage.set(null);
    this.error.set(null);

    if (!selectedFile) {
      return;
    }

    await this.preloadDocumentData(selectedFile);
  }

  protected continueToPersonalData(): void {
    if (this.isRecognizingDocument()) {
      this.error.set('Esperá a que termine la precarga del documento.');
      return;
    }

    if (this.identityForm.invalid) {
      this.identityForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.step.set('personal');
  }

  protected backToIdentity(): void {
    this.error.set(null);
    this.successMessage.set(null);
    this.step.set('identity');
  }

  protected submitPersonalData(): void {
    if (this.personalForm.invalid) {
      this.personalForm.markAllAsTouched();
      return;
    }

    if (!this.emailsMatch()) {
      this.error.set('Los e-mails ingresados no coinciden.');
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    this.auth
      .register({
        identity: this.identityForm.getRawValue(),
        personal: this.personalForm.getRawValue() satisfies AuthRegisterPersonalData,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: response => {
          this.successMessage.set(response.message ?? 'Registro enviado correctamente.');
        },
        error: error => {
          this.error.set(this.getErrorMessage(error));
        },
      });
  }

  private emailsMatch(): boolean {
    const { email, confirmEmail } = this.personalForm.getRawValue();
    return email.trim().toLowerCase() === confirmEmail.trim().toLowerCase();
  }

  private async preloadDocumentData(file: File): Promise<void> {
    this.isRecognizingDocument.set(true);

    try {
      const payload = await this.documentRecognition.createRequestFromFile(file);
      const response = await firstValueFrom(this.documentRecognition.recognizeDocument(payload));

      this.registerDocumentStore.setRecognitionResponse(response);
      this.applyRecognizedFields(response.data?.campos);
      this.recognitionSuccessMessage.set('Datos precargados. Revisalos antes de continuar.');
    } catch (error) {
      this.recognitionError.set(this.getDocumentRecognitionErrorMessage(error));
    } finally {
      this.isRecognizingDocument.set(false);
    }
  }

  private applyRecognizedFields(fields: DocumentRecognitionFields | undefined): void {
    if (!fields) {
      return;
    }

    const identity = {
      documentType: this.getStringValue(fields.tipoDocumento),
      documentNumber: this.getStringValue(fields.numeroDocumento),
    };
    const personal = {
      firstName: this.getStringValue(fields.primerNombre),
      secondName: this.getStringValue(fields.segundoNombre),
      firstLastName: this.getStringValue(fields.primerApellido),
      secondLastName: this.getStringValue(fields.segundoApellido),
      birthDate: this.getStringValue(fields.fechaNacimiento),
      sex: this.getStringValue(fields.sexo),
      country: this.getStringValue(fields.nacionalidad),
    };

    this.identityForm.patchValue(this.withoutEmptyValues(identity));
    this.personalForm.patchValue(this.withoutEmptyValues(personal));
  }

  private withoutEmptyValues<T extends Record<string, string | null>>(
    value: T
  ): Partial<Record<keyof T, string>> {
    const result: Partial<Record<keyof T, string>> = {};

    (Object.keys(value) as Array<keyof T>).forEach(key => {
      const fieldValue = value[key];
      if (fieldValue) {
        result[key] = fieldValue;
      }
    });

    return result;
  }

  private getStringValue(value: unknown): string | null {
    if (typeof value !== 'string') {
      return null;
    }

    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof AuthRequestError) {
      return `No se pudo completar el registro. Error ${error.status || 'de red'}.`;
    }

    return 'No se pudo completar el registro.';
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
