import { computed, inject, Injectable, signal } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ValidationUtils } from '@desarrolloort/ngx-utils';
import { finalize } from 'rxjs/operators';
import {
  buildFormErrorSummary,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';
import {
  buildOrtPasswordRequirements,
  ORT_PASSWORD_ERROR_MESSAGES,
  ORT_PASSWORD_VALIDATORS,
} from 'src/app/shared/forms/password-validation';

import { ChangePasswordService } from '../../services/change-password';

export interface ChangePasswordForm {
  currentPassword: FormControl<string>;
  password: FormControl<string>;
  confirmPassword: FormControl<string>;
}

interface PasswordRequirement {
  label: string;
  met: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class ChangePasswordFacade {
  private readonly changePasswordService = inject(ChangePasswordService);
  private readonly router = inject(Router);

  public readonly form = new FormGroup<ChangePasswordForm>(
    {
      currentPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
      password: new FormControl('', {
        nonNullable: true,
        validators: ORT_PASSWORD_VALIDATORS,
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    {
      validators: [ValidationUtils.confirmPasswordValidator('password', 'confirmPassword')],
    }
  );

  private readonly _isSubmitting = signal(false);
  public readonly isSubmitting = this._isSubmitting.asReadonly();

  private readonly _error = signal<string | null>(null);
  public readonly error = this._error.asReadonly();
  public readonly submitted = signal(false);

  private readonly _currentPasswordVisible = signal(false);
  private readonly _passwordVisible = signal(false);
  private readonly _confirmPasswordVisible = signal(false);

  public readonly currentPasswordInputType = computed(() =>
    this._currentPasswordVisible() ? 'text' : 'password'
  );
  public readonly currentPasswordIcon = computed(() =>
    this._currentPasswordVisible() ? 'visibility_off' : 'visibility'
  );
  public readonly currentPasswordToggleLabel = computed(() =>
    this._currentPasswordVisible() ? 'Ocultar contraseña actual' : 'Mostrar contraseña actual'
  );
  public readonly passwordInputType = computed(() =>
    this._passwordVisible() ? 'text' : 'password'
  );
  public readonly passwordIcon = computed(() =>
    this._passwordVisible() ? 'visibility_off' : 'visibility'
  );
  public readonly passwordToggleLabel = computed(() =>
    this._passwordVisible() ? 'Ocultar nueva contraseña' : 'Mostrar nueva contraseña'
  );
  public readonly confirmPasswordInputType = computed(() =>
    this._confirmPasswordVisible() ? 'text' : 'password'
  );
  public readonly confirmPasswordIcon = computed(() =>
    this._confirmPasswordVisible() ? 'visibility_off' : 'visibility'
  );
  public readonly confirmPasswordToggleLabel = computed(() =>
    this._confirmPasswordVisible()
      ? 'Ocultar confirmación de contraseña'
      : 'Mostrar confirmación de contraseña'
  );
  public readonly errorSummary = computed(() =>
    this.submitted()
      ? buildFormErrorSummary(
          this.form,
          [
            {
              controlName: 'currentPassword',
              fieldId: 'current-password',
              label: 'Contraseña actual',
            },
            {
              controlName: 'password',
              fieldId: 'new-password',
              label: 'Nueva contraseña',
              messages: {
                ...ORT_PASSWORD_ERROR_MESSAGES,
              },
            },
            {
              controlName: 'confirmPassword',
              fieldId: 'confirm-new-password',
              label: 'Confirmar contraseña',
              messages: {
                confirmPasswordMismatch: 'Las contraseñas no coinciden.',
              },
            },
          ],
          ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
        )
      : []
  );

  private readonly _password = signal('');

  public readonly requirements = computed<PasswordRequirement[]>(() => {
    this._password();
    return buildOrtPasswordRequirements(this.form.controls.password);
  });

  constructor() {
    this.form.controls.password.valueChanges.subscribe(v => this._password.set(v));
  }

  public toggleCurrentPasswordVisibility(): void {
    this._currentPasswordVisible.update(v => !v);
  }

  public togglePasswordVisibility(): void {
    this._passwordVisible.update(v => !v);
  }

  public toggleConfirmPasswordVisibility(): void {
    this._confirmPasswordVisible.update(v => !v);
  }

  public submit(): void {
    this.submitted.set(true);
    this.form.markAllAsTouched();
    if (this.form.invalid || this._isSubmitting()) {
      return;
    }

    this._isSubmitting.set(true);
    this._error.set(null);

    this.changePasswordService
      .changePassword({
        currentPassword: this.form.controls.currentPassword.value,
        password: this.form.controls.password.value,
      })
      .pipe(finalize(() => this._isSubmitting.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/inicio']),
        error: () =>
          this._error.set('No se pudo cambiar la contraseña. Verificá que la actual sea correcta.'),
      });
  }
}
