import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import type { OrtErrorItem } from '@desarrolloort/components';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
} from '@desarrolloort/components';
import { finalize } from 'rxjs/operators';

import { AccountService } from '../../../auth/services/account';

interface ChangePasswordForm {
  currentPassword: FormControl<string>;
  password: FormControl<string>;
  confirmPassword: FormControl<string>;
}

interface PasswordRequirement {
  label: string;
  met: boolean;
}

@Component({
  selector: 'app-change-password',
  imports: [
    OrtFormFieldModule,
    OrtInputModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePassword {
  private readonly account = inject(AccountService);
  private readonly router = inject(Router);

  protected readonly form = new FormGroup<ChangePasswordForm>(
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

  private readonly isSubmittingState = signal(false);
  protected readonly isSubmitting = this.isSubmittingState.asReadonly();

  private readonly errorState = signal<string | null>(null);
  protected readonly error = this.errorState.asReadonly();

  protected readonly currentPasswordVisible = signal(false);
  protected readonly passwordVisible = signal(false);
  protected readonly confirmPasswordVisible = signal(false);

  protected readonly currentPasswordInputType = computed(() =>
    this.currentPasswordVisible() ? 'text' : 'password'
  );
  protected readonly currentPasswordIcon = computed(() =>
    this.currentPasswordVisible() ? 'visibility' : 'visibility_off'
  );
  protected readonly passwordInputType = computed(() =>
    this.passwordVisible() ? 'text' : 'password'
  );
  protected readonly passwordIcon = computed(() =>
    this.passwordVisible() ? 'visibility' : 'visibility_off'
  );
  protected readonly confirmPasswordInputType = computed(() =>
    this.confirmPasswordVisible() ? 'text' : 'password'
  );
  protected readonly confirmPasswordIcon = computed(() =>
    this.confirmPasswordVisible() ? 'visibility' : 'visibility_off'
  );

  protected readonly currentPasswordToggleLabel = computed(() =>
    this.currentPasswordVisible() ? 'Ocultar contraseña actual' : 'Mostrar contraseña actual'
  );
  protected readonly passwordToggleLabel = computed(() =>
    this.passwordVisible() ? 'Ocultar nueva contraseña' : 'Mostrar nueva contraseña'
  );
  protected readonly confirmPasswordToggleLabel = computed(() =>
    this.confirmPasswordVisible()
      ? 'Ocultar confirmación de contraseña'
      : 'Mostrar confirmación de contraseña'
  );

  protected readonly errorSummary = computed<OrtErrorItem[]>(() => {
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

  private readonly password = toSignal(this.form.controls.password.valueChanges, {
    initialValue: this.form.controls.password.value,
  });

  protected readonly requirements = computed<PasswordRequirement[]>(() => {
    const value = this.password();
    return [
      { label: '12 caracteres', met: value.length >= 12 },
      { label: 'Una letra mayúscula', met: /[A-Z]/.test(value) },
      { label: 'Una letra minúscula', met: /[a-z]/.test(value) },
      { label: 'Un número', met: /\d/.test(value) },
      { label: 'Un caracter especial ($%@_!.-)', met: /[$%@_.!-]/.test(value) },
    ];
  });

  protected toggleCurrentPasswordVisibility(): void {
    this.currentPasswordVisible.update(v => !v);
  }

  protected togglePasswordVisibility(): void {
    this.passwordVisible.update(v => !v);
  }

  protected toggleConfirmPasswordVisibility(): void {
    this.confirmPasswordVisible.update(v => !v);
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isSubmittingState()) {
      return;
    }

    this.isSubmittingState.set(true);
    this.errorState.set(null);

    this.account
      .changePassword({
        currentPassword: this.form.controls.currentPassword.value,
        password: this.form.controls.password.value,
      })
      .pipe(finalize(() => this.isSubmittingState.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/inicio']),
        error: () =>
          this.errorState.set(
            'No se pudo cambiar la contraseña. Verificá que la actual sea correcta.'
          ),
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
