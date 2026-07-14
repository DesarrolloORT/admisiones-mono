import { DOCUMENT, Location } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
} from '@desarrolloort/components';
import { ValidationUtils } from '@desarrolloort/ngx-utils';
import { catchError, map, of, switchMap } from 'rxjs';

import {
  focusFieldById,
  FormErrorField,
  getFirstInvalidFieldId,
} from '../../../../shared/forms/form-error-summary';
import {
  buildOrtPasswordRequirements,
  buildOrtPasswordStrength,
  ORT_PASSWORD_ERROR_MESSAGES,
  ORT_PASSWORD_VALIDATORS,
} from '../../../../shared/forms/password-validation';
import { createPasswordVisibility } from '../../../../shared/forms/password-visibility';
import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { AuthForm } from '../../components/shared/auth-form/auth-form';
import { AuthSessionService } from '../../services/auth-session';
import { PasswordActivationService } from '../../services/password-activation';

interface SetPasswordForm {
  password: FormControl<string>;
  confirmPassword: FormControl<string>;
}

@Component({
  selector: 'app-set-password',
  imports: [
    AuthForm,
    OrtFormFieldModule,
    OrtInputModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './set-password.html',
  styleUrl: './set-password.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetPassword {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly location = inject(Location);
  private readonly passwordActivation = inject(PasswordActivationService);
  private readonly authSession = inject(AuthSessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarHandler);

  protected readonly isRecovery = this.route.snapshot.queryParamMap.get('flow') === 'recovery';

  protected readonly title = this.isRecovery ? 'Recuperar acceso' : 'Creá tu contraseña';
  protected readonly description = this.isRecovery
    ? 'Ingresá tus datos para verificar tu identidad. Te enviaremos por correo electrónico un link para actualizar tu contraseña.'
    : 'Para activar tu cuenta, definí una contraseña segura.';
  protected readonly heroTitle = this.isRecovery
    ? 'Recuperá el acceso a tu cuenta.'
    : 'Activá tu cuenta.';
  protected readonly submitLabel = this.isRecovery ? 'Actualizar contraseña' : 'Activar cuenta';
  protected readonly submittingLabel = this.isRecovery ? 'Actualizando...' : 'Activando...';

  protected readonly form = new FormGroup<SetPasswordForm>(
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

  private readonly isSubmittingState = signal(false);
  protected readonly isSubmitting = this.isSubmittingState.asReadonly();

  protected readonly submitted = signal(false);
  private readonly errorFields: FormErrorField[] = [
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
  ];

  private readonly tokenErrorState = signal<string | null>(null);
  protected readonly tokenError = this.tokenErrorState.asReadonly();

  protected readonly passwordVisibility = createPasswordVisibility();
  protected readonly confirmPasswordVisibility = createPasswordVisibility(
    'confirmación de contraseña'
  );

  private readonly password = toSignal(this.form.controls.password.valueChanges, {
    initialValue: this.form.controls.password.value,
  });
  private readonly formStatus = toSignal(this.form.statusChanges, {
    initialValue: this.form.status,
  });

  protected readonly requirements = computed(() => {
    this.password();
    return buildOrtPasswordRequirements(this.form.controls.password);
  });

  protected readonly strength = computed(() => buildOrtPasswordStrength(this.password()));
  protected readonly canSubmit = computed(() => {
    this.formStatus();
    return this.form.valid && !this.isSubmittingState();
  });

  constructor() {
    this.activateToken();
  }

  protected submit(): void {
    this.submitted.set(true);
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isSubmittingState()) {
      this.snackbar.error('Revisá los campos marcados.');
      focusFieldById(this.document, getFirstInvalidFieldId(this.form, this.errorFields));
      return;
    }

    this.isSubmittingState.set(true);

    this.passwordActivation
      .completePassword(this.form.controls.password.value)
      .pipe(
        switchMap(() => {
          this.snackbar.success(
            this.isRecovery
              ? 'Contraseña actualizada correctamente.'
              : 'Cuenta activada correctamente.'
          );
          return this.isRecovery ? of('/iniciar-sesion') : this.hydrateSessionAndGoHome();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: url => this.navigateAfterSubmit(url),
        error: () => {
          this.isSubmittingState.set(false);
          this.snackbar.error('No se pudo crear la contraseña. Intentá de nuevo.');
        },
      });
  }

  /** Emite la URL destino, o `null` si la sesión no pudo hidratarse (queda en la página). */
  private hydrateSessionAndGoHome() {
    return this.authSession.hydrateAuthenticatedSession().pipe(
      map(() => '/inicio'),
      catchError(() => {
        this.snackbar.error('No se pudo iniciar la sesión automáticamente.');
        return of(null);
      })
    );
  }

  private navigateAfterSubmit(url: string | null): void {
    if (!url) {
      this.isSubmittingState.set(false);
      return;
    }
    void this.router.navigateByUrl(url).finally(() => this.isSubmittingState.set(false));
  }

  private activateToken(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.tokenErrorState.set('El enlace no es válido. Verificá que copiaste la URL completa.');
      return;
    }

    this.location.replaceState(
      this.router.url.split('?')[0] || '/crear-password',
      this.isRecovery ? 'flow=recovery' : ''
    );

    this.passwordActivation
      .activateLink(token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () =>
          this.tokenErrorState.set('El enlace expiró o ya fue utilizado. Solicitá uno nuevo.'),
      });
  }
}
