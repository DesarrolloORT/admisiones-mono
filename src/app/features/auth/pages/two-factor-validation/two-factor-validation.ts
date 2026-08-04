import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { AuthForm } from '../../components/shared/auth-form/auth-form';
import { TwoFactorValidation } from '../../components/two-factor-validation/two-factor-validation';
import { getApiErrorMessage } from '../../models/api-error-message';
import { AuthSessionService } from '../../services/auth-session';

@Component({
  selector: 'app-two-factor-validation-page',
  imports: [AuthForm, TwoFactorValidation],
  templateUrl: './two-factor-validation.html',
  styleUrl: './two-factor-validation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TwoFactorValidationPage {
  private readonly authSession = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarHandler);

  protected readonly email = signal<string>('');
  protected readonly isSubmitting = signal<boolean>(false);
  protected readonly error = signal<string | null>(null);

  private readonly sessionId = signal<string>('');
  private readonly documentType = signal<string>('');
  private readonly documentNumber = signal<string>('');

  constructor() {
    this.restoreStateFromNavigation();
  }

  protected verify(code: string): void {
    const sessionId = this.sessionId();

    if (!sessionId) {
      this.error.set('La sesión expiró. Iniciá sesión nuevamente.');
      return;
    }

    this.error.set(null);
    this.isSubmitting.set(true);

    this.authSession
      .completeTwoFactor({
        sessionId,
        code,
        documentType: this.documentType(),
        documentNumber: this.documentNumber(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.snackbar.success('Código validado correctamente.');
          this.router.navigateByUrl('/inicio').finally(() => this.isSubmitting.set(false));
        },
        error: error => {
          const message = getApiErrorMessage(
            error,
            'No pudimos validar el código. Verificá los dígitos e intentá nuevamente.'
          );
          this.isSubmitting.set(false);
          this.snackbar.error(message);
        },
      });
  }

  protected resend(): void {
    const sessionId = this.sessionId();

    if (!sessionId || this.isSubmitting()) {
      return;
    }

    this.error.set(null);
    this.isSubmitting.set(true);

    this.authSession
      .resendTwoFactorCode(sessionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.sessionId.set(result.sessionId);

          if (result.maskedEmail) {
            this.email.set(result.maskedEmail);
          }

          this.snackbar.success('Código reenviado.');
          this.isSubmitting.set(false);
        },
        error: error => {
          const message = getApiErrorMessage(
            error,
            'No pudimos reenviar el código. Intentá nuevamente.'
          );
          this.isSubmitting.set(false);
          this.snackbar.error(message);
        },
      });
  }

  private restoreStateFromNavigation(): void {
    const context = this.authSession.takePendingTwoFactorContext();

    if (!context) {
      this.router.navigateByUrl('/iniciar-sesion');
      return;
    }

    this.email.set(context.email);
    this.sessionId.set(context.sessionId);
    this.documentType.set(context.documentType);
    this.documentNumber.set(context.documentNumber);
  }
}
