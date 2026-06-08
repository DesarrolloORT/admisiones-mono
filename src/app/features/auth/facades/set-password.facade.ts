import { computed, inject, Injectable, signal } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ValidationUtils } from '@desarrolloort/ngx-utils';
import { finalize } from 'rxjs/operators';

import {
  buildFormErrorSummary,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from '../../../shared/forms/form-error-summary';
import {
  buildOrtPasswordRequirements,
  buildOrtPasswordStrength,
  ORT_PASSWORD_ERROR_MESSAGES,
  ORT_PASSWORD_VALIDATORS,
  OrtPasswordStrength,
} from '../../../shared/forms/password-validation';
import { PasswordActivationService } from '../services/password-activation';

export interface SetPasswordForm {
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
export class SetPasswordFacade {
  private readonly passwordActivation = inject(PasswordActivationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  public readonly isRecovery = this.route.snapshot.queryParamMap.get('flow') === 'recovery';

  public readonly title = this.isRecovery ? 'Recuperar acceso' : 'Creá tu contraseña';
  public readonly description = this.isRecovery
    ? 'Ingresá tus datos para verificar tu identidad. Te enviaremos por correo electrónico un link para actualizar tu contraseña.'
    : 'Para activar tu cuenta, definí una contraseña segura.';
  public readonly heroTitle = this.isRecovery
    ? 'Recuperá el acceso a tu cuenta.'
    : 'Activá tu cuenta.';
  public readonly submitLabel = this.isRecovery ? 'Actualizar contraseña' : 'Activar cuenta';
  public readonly submittingLabel = this.isRecovery ? 'Actualizando...' : 'Activando...';

  public readonly form = new FormGroup<SetPasswordForm>(
    {
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

  private readonly _tokenError = signal<string | null>(null);
  public readonly tokenError = this._tokenError.asReadonly();

  private readonly _passwordVisible = signal(false);
  private readonly _confirmPasswordVisible = signal(false);

  public readonly passwordInputType = computed(() =>
    this._passwordVisible() ? 'text' : 'password'
  );
  public readonly passwordIcon = computed(() =>
    this._passwordVisible() ? 'visibility_off' : 'visibility'
  );
  public readonly passwordToggleLabel = computed(() =>
    this._passwordVisible() ? 'Ocultar contraseña' : 'Mostrar contraseña'
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
              controlName: 'password',
              fieldId: 'crear-password',
              label: 'Contraseña',
              messages: {
                ...ORT_PASSWORD_ERROR_MESSAGES,
              },
            },
            {
              controlName: 'confirmPassword',
              fieldId: 'crear-password-confirm',
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

  public readonly strength = computed<OrtPasswordStrength | null>(() =>
    buildOrtPasswordStrength(this._password())
  );

  constructor() {
    this.form.controls.password.valueChanges.subscribe(v => this._password.set(v));
    this.activateToken();
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

    this.passwordActivation
      .completePassword(this.form.controls.password.value)
      .pipe(finalize(() => this._isSubmitting.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/iniciar-sesion']),
        error: () => this._error.set('No se pudo crear la contraseña. Intentá de nuevo.'),
      });
  }

  private activateToken(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this._tokenError.set('El enlace no es válido. Verificá que copiaste la URL completa.');
      return;
    }

    this.passwordActivation.activateLink(token).subscribe({
      error: () => this._tokenError.set('El enlace expiró o ya fue utilizado. Solicitá uno nuevo.'),
    });
  }
}
