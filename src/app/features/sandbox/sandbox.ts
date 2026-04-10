import { JsonPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, firstValueFrom } from 'rxjs';

import {
  SandboxAuth,
  SandboxDocumentRecognitionRequest,
  SandboxDocumentRecognitionResponse,
  SandboxLoginResponse,
} from './services/sandbox-auth';

interface SandboxLoginForm {
  codigoPersona: FormControl<number | null>;
  password: FormControl<string>;
}

interface SandboxDocumentRecognitionForm {
  tipoDocumentoEsperado: FormControl<string>;
  tipoMime: FormControl<string>;
  archivo: FormControl<File | null>;
}

@Component({
  selector: 'app-sandbox',
  imports: [ReactiveFormsModule, JsonPipe],
  templateUrl: './sandbox.html',
  styleUrl: './sandbox.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Sandbox {
  private static readonly MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
  private static readonly MIME_BY_EXTENSION: Record<string, string> = {
    pdf: 'application/pdf',
    jpg: 'image/jpeg',
    jpeg: 'image/jpeg',
    png: 'image/png',
    tif: 'image/tiff',
    tiff: 'image/tiff',
    bmp: 'image/bmp',
    webp: 'image/webp',
    heic: 'image/heic',
  };

  private readonly auth = inject(SandboxAuth);

  protected readonly isSubmitting = signal(false);
  protected readonly isAuthenticated = signal(false);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly isRecognizing = signal(false);
  protected readonly recognitionError = signal<string | null>(null);
  protected readonly recognitionResponse = signal<SandboxDocumentRecognitionResponse | null>(null);
  protected readonly selectedFileName = signal<string | null>(null);
  protected readonly requiresReview = computed<boolean | null>(() => {
    const value = this.recognitionResponse()?.data?.requiereRevision;
    return typeof value === 'boolean' ? value : null;
  });
  protected readonly detectedFields = computed<
    Array<{ key: string; label: string; value: string }>
  >(() => {
    const fields = this.recognitionResponse()?.data?.campos;
    if (!fields || typeof fields !== 'object') {
      return [];
    }

    return Object.entries(fields)
      .filter(([, value]) => this.hasDisplayValue(value))
      .map(([key, value]) => ({
        key,
        label: this.formatFieldLabel(key),
        value: this.toDisplayValue(value),
      }));
  });

  protected readonly form = new FormGroup<SandboxLoginForm>({
    codigoPersona: new FormControl<number | null>(null, {
      validators: [Validators.required],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  protected readonly recognitionForm = new FormGroup<SandboxDocumentRecognitionForm>({
    tipoDocumentoEsperado: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100), Validators.pattern(/.*\S.*/)],
    }),
    tipoMime: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(100),
        Validators.pattern(/^[\w.+-]+\/[\w.+-]+$/),
      ],
    }),
    archivo: new FormControl<File | null>(null, {
      validators: [Validators.required],
    }),
  });

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { codigoPersona, password } = this.form.getRawValue();
    if (codigoPersona === null) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    this.auth
      .login({ codigoPersona, password })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (response: SandboxLoginResponse) => {
          this.isAuthenticated.set(true);
          this.successMessage.set(
            response.message ||
              'Autenticación exitosa. Los tokens han sido establecidos como cookies seguras.'
          );
          this.form.controls.password.reset('');
        },
        error: error => {
          this.isAuthenticated.set(false);
          this.successMessage.set(null);
          this.error.set(this.getErrorMessage(error));
        },
      });
  }

  protected onBackToLogin(): void {
    this.isAuthenticated.set(false);
    this.successMessage.set(null);
    this.error.set(null);
    this.form.controls.password.reset('');
    this.recognitionForm.reset({
      tipoDocumentoEsperado: '',
      tipoMime: '',
      archivo: null,
    });
    this.selectedFileName.set(null);
    this.recognitionError.set(null);
    this.recognitionResponse.set(null);
    this.isRecognizing.set(false);
  }

  protected onDocumentSelected(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const selectedFile = input?.files?.item(0) ?? null;

    this.recognitionForm.controls.archivo.setValue(selectedFile);
    this.recognitionForm.controls.archivo.markAsTouched();
    this.selectedFileName.set(selectedFile?.name ?? null);

    if (!selectedFile) {
      return;
    }

    const inferredMimeType = this.inferMimeType(selectedFile);
    if (inferredMimeType && !this.recognitionForm.controls.tipoMime.value.trim()) {
      this.recognitionForm.controls.tipoMime.setValue(inferredMimeType);
    }

    const inferredDocumentType = this.inferExpectedDocumentType(selectedFile);
    if (inferredDocumentType && !this.recognitionForm.controls.tipoDocumentoEsperado.value.trim()) {
      this.recognitionForm.controls.tipoDocumentoEsperado.setValue(inferredDocumentType);
    }
  }

  protected async onRecognizeDocument(): Promise<void> {
    if (this.recognitionForm.invalid) {
      this.recognitionForm.markAllAsTouched();
      return;
    }

    const { tipoDocumentoEsperado, tipoMime, archivo } = this.recognitionForm.getRawValue();
    if (!archivo) {
      this.recognitionForm.controls.archivo.setErrors({ required: true });
      return;
    }

    if (archivo.size > Sandbox.MAX_FILE_SIZE_BYTES) {
      this.recognitionForm.controls.archivo.setErrors({ maxFileSize: true });
      return;
    }

    this.isRecognizing.set(true);
    this.recognitionError.set(null);
    this.recognitionResponse.set(null);

    try {
      const fileBase64 = await this.readFileAsBase64(archivo);
      const payload: SandboxDocumentRecognitionRequest = {
        tipoDocumentoEsperado: tipoDocumentoEsperado.trim(),
        tipoMime: tipoMime.trim(),
        archivoAdjunto: {
          nombreArchivo: archivo.name,
          archivo: fileBase64,
        },
      };

      const response = await firstValueFrom(this.auth.recognizeDocument(payload));
      this.recognitionResponse.set(response);
    } catch (error) {
      this.recognitionError.set(this.getRecognitionErrorMessage(error));
    } finally {
      this.isRecognizing.set(false);
    }
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      return `Error ${error.status || 'de red'} al autenticar.`;
    }

    return 'No se pudo iniciar sesión.';
  }

  private getRecognitionErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      return `Error ${error.status || 'de red'} al reconocer el documento.`;
    }

    return 'No se pudo reconocer el documento.';
  }

  private readFileAsBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = () => {
        const result = reader.result;
        if (typeof result !== 'string') {
          reject(new Error('No se pudo leer el archivo seleccionado.'));
          return;
        }

        const [, base64] = result.split(',', 2);
        if (!base64) {
          reject(new Error('No se pudo codificar el archivo en base64.'));
          return;
        }

        resolve(base64);
      };

      reader.onerror = () => {
        reject(reader.error ?? new Error('No se pudo procesar el archivo seleccionado.'));
      };

      reader.readAsDataURL(file);
    });
  }

  private inferMimeType(file: File): string | null {
    if (file.type && /^[\w.+-]+\/[\w.+-]+$/.test(file.type)) {
      return file.type;
    }

    const extension = this.getFileExtension(file.name);
    return extension ? (Sandbox.MIME_BY_EXTENSION[extension] ?? null) : null;
  }

  private inferExpectedDocumentType(file: File): string | null {
    const normalizedName = file.name.trim().toLowerCase();

    if (/pasaporte|passport/.test(normalizedName)) {
      return 'PASAPORTE';
    }

    if (/cedula|c[ée]dula|\bdni\b|documento/.test(normalizedName)) {
      return 'CI';
    }

    if (/licencia|license/.test(normalizedName)) {
      return 'LICENCIA';
    }

    if (/carnet|carn[ée]/.test(normalizedName)) {
      return 'CARNET';
    }

    if (/credencial/.test(normalizedName)) {
      return 'CREDENCIAL';
    }

    return null;
  }

  private getFileExtension(fileName: string): string | null {
    const dotIndex = fileName.lastIndexOf('.');
    if (dotIndex === -1 || dotIndex === fileName.length - 1) {
      return null;
    }

    return fileName.slice(dotIndex + 1).toLowerCase();
  }

  private hasDisplayValue(value: unknown): boolean {
    if (value === null || value === undefined) {
      return false;
    }

    if (typeof value === 'string') {
      return value.trim().length > 0;
    }

    return true;
  }

  private toDisplayValue(value: unknown): string {
    if (value === null || value === undefined) {
      return '';
    }

    if (typeof value === 'string') {
      return value;
    }

    if (typeof value === 'number' || typeof value === 'boolean') {
      return String(value);
    }

    return JSON.stringify(value);
  }

  private formatFieldLabel(fieldKey: string): string {
    const withSpaces = fieldKey
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/[_-]+/g, ' ')
      .trim();

    if (!withSpaces) {
      return fieldKey;
    }

    return withSpaces.charAt(0).toUpperCase() + withSpaces.slice(1);
  }
}

