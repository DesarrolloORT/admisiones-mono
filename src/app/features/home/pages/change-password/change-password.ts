import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import type { OrtErrorItem } from '@desarrolloort/components';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
} from '@desarrolloort/components';
import { finalize } from 'rxjs/operators';

import { matchingFieldsValidator } from '../../../../shared/forms/matching-fields.validator';
import {
  buildOrtPasswordRequirements,
  ORT_PASSWORD_VALIDATORS,
} from '../../../../shared/forms/password-validation';
import { createPasswordVisibility } from '../../../../shared/forms/password-visibility';
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
        validators: ORT_PASSWORD_VALIDATORS,
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    {
      validators: [
        matchingFieldsValidator('password', 'confirmPassword', {
          errorKey: 'confirmPasswordMismatch',
        }),
      ],
    }
  );

  private readonly isSubmittingState = signal(false);
  protected readonly isSubmitting = this.isSubmittingState.asReadonly();

  private readonly errorState = signal<string | null>(null);
  protected readonly error = this.errorState.asReadonly();

  protected readonly currentPasswordVisibility = createPasswordVisibility('contraseña actual');
  protected readonly passwordVisibility = createPasswordVisibility('nueva contraseña');
  protected readonly confirmPasswordVisibility = createPasswordVisibility(
    'confirmación de contraseña'
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
    this.password();
    return buildOrtPasswordRequirements(this.form.controls.password);
  });

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
