import { AsyncPipe } from '@angular/common';
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

import { Catalogs } from '../../../catalogs/services/catalogs';
import { LocationSelect, LocationValue } from '../../components/location-select/location-select';
import { AuthForm } from '../../components/auth-form/auth-form';
import { AuthRequestError } from '../../models/auth-error';
import { AuthRegisterPersonalData } from '../../models/auth.interface';
import {
  DocumentRecognitionFileError,
  DocumentRecognitionRequestError,
} from '../../models/document-recognition-error';
import { DocumentRecognitionFields } from '../../models/document-recognition.interface';
import { Auth } from '../../services/auth';
import { DocumentRecognition } from '../../services/document-recognition';
import { RegisterDocumentStore } from '../../store/register-document.store';

type RegisterStep = 'identity' | 'personal';

interface IdentityForm {
  documentType: FormControl<string>;
  documentNumber: FormControl<string>;
}

interface PersonalForm {
  primerNombre: FormControl<string>;
  segundoNombre: FormControl<string>;
  primerApellido: FormControl<string>;
  segundoApellido: FormControl<string>;
  fechaNacimiento: FormControl<string>;
  sexo: FormControl<string>;
  location: FormControl<LocationValue>;
  direccion: FormControl<string>;
  telefono1: FormControl<string>;
  mail: FormControl<string>;
  verificacionMail: FormControl<string>;
}

@Component({
  selector: 'app-register',
  imports: [
    AsyncPipe,
    AuthForm,
    LocationSelect,
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
  private readonly catalogs = inject(Catalogs);
  private readonly documentRecognition = inject(DocumentRecognition);
  private readonly registerDocumentStore = inject(RegisterDocumentStore);

  protected readonly documentTypes$ = this.catalogs.getDocumentTypes();
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
    primerNombre: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    segundoNombre: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    primerApellido: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    segundoApellido: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    fechaNacimiento: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    sexo: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    location: new FormControl<LocationValue>(
      { codigoPais: null, codigoEstado: null, codigoCiudad: null },
      { nonNullable: true, validators: [Validators.required] }
    ),
    direccion: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    telefono1: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    mail: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    verificacionMail: new FormControl('', {
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
        personal: this.buildPersonalData(),
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.successMessage.set('Registro enviado correctamente.');
        },
        error: error => {
          this.error.set(this.getErrorMessage(error));
        },
      });
  }

  private emailsMatch(): boolean {
    const { mail, verificacionMail } = this.personalForm.getRawValue();
    return mail.trim().toLowerCase() === verificacionMail.trim().toLowerCase();
  }

  private buildPersonalData(): AuthRegisterPersonalData {
    const { location, ...rest } = this.personalForm.getRawValue();
    return {
      ...rest,
      codigoPais: location.codigoPais,
      codigoEstado: location.codigoEstado,
      codigoCiudad: location.codigoCiudad,
    };
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
      primerNombre: this.getStringValue(fields.primerNombre),
      segundoNombre: this.getStringValue(fields.segundoNombre),
      primerApellido: this.getStringValue(fields.primerApellido),
      segundoApellido: this.getStringValue(fields.segundoApellido),
      fechaNacimiento: this.getStringValue(fields.fechaNacimiento),
      sexo: this.getStringValue(fields.sexo),
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

