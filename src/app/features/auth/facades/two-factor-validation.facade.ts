import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { isNormalizedApiError } from '@desarrolloort/ngx-utils';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { AuthSessionService } from '../services/auth-session';

interface TwoFactorRouterState {
  email?: unknown;
  sessionId?: unknown;
  documentType?: unknown;
  documentNumber?: unknown;
}

@Injectable({
  providedIn: 'root',
})
export class TwoFactorValidationFacade {
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarHandler);
  private readonly document = inject(DOCUMENT);

  public readonly email = signal<string>('');
  public readonly isSubmitting = signal<boolean>(false);
  public readonly error = signal<string | null>(null);
  public readonly canResend = signal<boolean>(true);

  private readonly sessionId = signal<string>('');
  private readonly documentType = signal<string>('');
  private readonly documentNumber = signal<string>('');

  constructor() {
    this.restoreStateFromNavigation();
  }

  public verify(code: string): void {
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
      .subscribe({
        next: () => {
          this.router.navigateByUrl('/inicio').finally(() => this.isSubmitting.set(false));
        },
        error: error => {
          this.isSubmitting.set(false);
          this.error.set(
            isNormalizedApiError(error)
              ? error.message
              : 'No pudimos validar el código. Verificá los dígitos e intentá nuevamente.'
          );
        },
      });
  }

  public resend(): void {
    const sessionId = this.sessionId();

    if (!sessionId || this.isSubmitting()) {
      return;
    }

    this.error.set(null);
    this.isSubmitting.set(true);

    this.authSession.resendTwoFactorCode(sessionId).subscribe({
      next: result => {
        this.sessionId.set(result.sessionId);

        if (result.maskedEmail) {
          this.email.set(result.maskedEmail);
        }

        this.isSubmitting.set(false);
        this.snackbar.success(result.message || 'Te enviamos un nuevo código a tu correo.');
      },
      error: error => {
        this.isSubmitting.set(false);
        this.error.set(
          isNormalizedApiError(error)
            ? error.message
            : 'No pudimos reenviar el código. Intentá nuevamente.'
        );
      },
    });
  }

  private restoreStateFromNavigation(): void {
    const navigation = this.router.getCurrentNavigation();
    const navigationState = navigation?.extras.state as TwoFactorRouterState | undefined;
    const historyState = this.document.defaultView?.history.state as
      | TwoFactorRouterState
      | undefined;
    const state = navigationState ?? historyState ?? {};

    const email = typeof state.email === 'string' ? state.email : '';
    const sessionId = typeof state.sessionId === 'string' ? state.sessionId : '';
    const documentType = typeof state.documentType === 'string' ? state.documentType : '';
    const documentNumber = typeof state.documentNumber === 'string' ? state.documentNumber : '';

    if (!sessionId) {
      void this.router.navigateByUrl('/iniciar-sesion');
      return;
    }

    this.email.set(email);
    this.sessionId.set(sessionId);
    this.documentType.set(documentType);
    this.documentNumber.set(documentNumber);
  }
}
