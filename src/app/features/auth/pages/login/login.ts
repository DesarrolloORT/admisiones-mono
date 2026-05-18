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
import { finalize } from 'rxjs/operators';

import { Catalogs } from '../../../catalogs/services/catalogs';
import { AuthForm } from '../../components/auth-form/auth-form';
import { AuthRequestError } from '../../models/auth-error';
import { Auth } from '../../services/auth';

interface LoginForm {
  documentType: FormControl<string>;
  documentNumber: FormControl<string>;
  password: FormControl<string>;
}

@Component({
  selector: 'app-login',
  imports: [
    AsyncPipe,
    AuthForm,
    OrtFormFieldModule,
    OrtInputModule,
    OrtSelectModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  private readonly auth = inject(Auth);
  private readonly catalogs = inject(Catalogs);

  protected readonly documentTypes$ = this.catalogs.getDocumentTypes();
  protected readonly isSubmitting = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly passwordInputType = computed(() =>
    this.showPassword() ? 'text' : 'password'
  );
  protected readonly passwordIcon = computed(() =>
    this.showPassword() ? 'visibility_off' : 'visibility'
  );

  protected readonly form = new FormGroup<LoginForm>({
    documentType: new FormControl('CI', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    documentNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[0-9A-Za-z]+$/)],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  protected togglePasswordVisibility(): void {
    this.showPassword.update(value => !value);
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    const { documentType, documentNumber, password } = this.form.getRawValue();

    this.auth
      .login({ documentType, documentNumber, password })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.successMessage.set('Sesión iniciada correctamente.');
          this.form.controls.password.reset('');
        },
        error: error => {
          this.error.set(this.getErrorMessage(error));
        },
      });
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof AuthRequestError) {
      return `No se pudo iniciar sesión. Error ${error.status || 'de red'}.`;
    }

    return 'No se pudo iniciar sesión.';
  }
}
