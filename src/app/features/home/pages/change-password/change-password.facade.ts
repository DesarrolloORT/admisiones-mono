import { computed, inject, Injectable, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import type { OrtErrorItem } from '@desarrolloort/components';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { postPersonaCambiarContrasenaEndpoint } from 'src/app/shared/api/generated/endpoints/persona.endpoints';

export interface ChangePasswordForm {
  currentPassword: FormControl<string>;
  password: FormControl<string>;
  confirmPassword: FormControl<string>;
}

interface PasswordRequirement {
  label: string;
  met: boolean;
}

@Injectable()
export class ChangePasswordFacade {
  private readonly api = inject(ApiHttpClient);
  private readonly router = inject(Router);

  public readonly form = new FormGroup<ChangePasswordForm>(
    {
      currentPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
      password: new FormControl('', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.minLength(12),
          Validators.maxLength(20),
          Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$%@_.!-])/),
        ],
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    { validators: [passwordMatchValidator] }
  );

  private readonly _isSubmitting = signal(false);
  public readonly isSubmitting = this._isSubmitting.asReadonly();

  private readonly _error = signal<string | null>(null);
  public readonly error = this._error.asReadonly();

  private readonly _currentPasswordVisible = signal(false);
  private readonly _passwordVisible = signal(false);
  private readonly _confirmPasswordVisible = signal(false);

  public readonly currentPasswordInputType = computed(() =>
    this._currentPasswordVisible() ? 'text' : 'password'
  );
  public readonly currentPasswordIcon = computed(() =>
    this._currentPasswordVisible() ? 'visibility' : 'visibility_off'
  );
  public readonly passwordInputType = computed(() =>
    this._passwordVisible() ? 'text' : 'password'
  );
  public readonly passwordIcon = computed(() =>
    this._passwordVisible() ? 'visibility' : 'visibility_off'
  );
  public readonly confirmPasswordInputType = computed(() =>
    this._confirmPasswordVisible() ? 'text' : 'password'
  );
  public readonly confirmPasswordIcon = computed(() =>
    this._confirmPasswordVisible() ? 'visibility' : 'visibility_off'
  );

  public readonly currentPasswordToggleLabel = computed(() =>
    this._currentPasswordVisible() ? 'Ocultar contraseña actual' : 'Mostrar contraseña actual'
  );
  public readonly passwordToggleLabel = computed(() =>
    this._passwordVisible() ? 'Ocultar nueva contraseña' : 'Mostrar nueva contraseña'
  );
  public readonly confirmPasswordToggleLabel = computed(() =>
    this._confirmPasswordVisible()
      ? 'Ocultar confirmación de contraseña'
      : 'Mostrar confirmación de contraseña'
  );

  public readonly errorSummary = computed<OrtErrorItem[]>(() => {
    const errors: OrtErrorItem[] = [];
    const controls = this.form.controls;
    if (controls.currentPassword.touched && controls.currentPassword.hasError('required')) {
      errors.push({ message: 'Contraseña actual es obligatoria' });
    }
    if (controls.password.touched && controls.password.hasError('required')) {
      errors.push({ message: 'Nueva contraseña es obligatoria' });
    } else if (controls.password.touched && controls.password.invalid) {
      errors.push({ message: 'La contraseña no cumple los requisitos de seguridad' });
    }
    if (controls.confirmPassword.touched && controls.confirmPassword.hasError('required')) {
      errors.push({ message: 'Confirmar contraseña es obligatorio' });
    } else if (
      controls.confirmPassword.touched &&
      controls.confirmPassword.hasError('confirmPasswordMismatch')
    ) {
      errors.push({ message: 'Las contraseñas no coinciden' });
    }
    return errors;
  });

  private readonly _password = signal('');

  public readonly requirements = computed<PasswordRequirement[]>(() => {
    const value = this._password();
    return [
      { label: '12 caracteres', met: value.length >= 12 },
      { label: 'Una letra mayúscula', met: /[A-Z]/.test(value) },
      { label: 'Una letra minúscula', met: /[a-z]/.test(value) },
      { label: 'Un número', met: /\d/.test(value) },
      { label: 'Un caracter especial ($%@_!.-)', met: /[$%@_.!-]/.test(value) },
    ];
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
    this.form.markAllAsTouched();
    if (this.form.invalid || this._isSubmitting()) {
      return;
    }

    this._isSubmitting.set(true);
    this._error.set(null);

    this.changePassword()
      .pipe(finalize(() => this._isSubmitting.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/inicio']),
        error: () =>
          this._error.set('No se pudo cambiar la contraseña. Verificá que la actual sea correcta.'),
      });
  }

  private changePassword(): Observable<unknown> {
    return this.api.request(postPersonaCambiarContrasenaEndpoint, {
      body: {
        passwordActual: this.form.controls.currentPassword.value,
        passwordNueva: this.form.controls.password.value,
      },
      withCredentials: true,
    });
  }
}

function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  if (password && confirm && password !== confirm) {
    group.get('confirmPassword')?.setErrors({ passwordMismatch: true });
    return { passwordMismatch: true };
  }
  return null;
}
