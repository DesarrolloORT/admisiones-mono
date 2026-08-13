import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY, Observable, of } from 'rxjs';
import { catchError, finalize, map, tap } from 'rxjs/operators';

import { AuthEndpoint, LoginResult, ResendTwoFactorCodeResult } from '../endpoints/auth.endpoint';
import { AuthLoginRequest, AuthSession } from '../models/auth.interface';
import { formatDocumentForBackend } from '../models/document-number';
import { AccountService } from './account';

/**
 * Outcome of `AuthSessionService.login`.
 * - `authenticated`: session is established and stored.
 * - `twoFactorRequired`: caller must navigate to the 2FA page and complete verification.
 */
export type LoginOutcome =
  { kind: 'authenticated'; session: AuthSession } | { kind: 'twoFactorRequired' };

interface PendingTwoFactorContext {
  documentNumber: string;
  documentType: string;
  email: string;
  sessionId: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthSessionService {
  private readonly endpoint = inject(AuthEndpoint);
  private readonly account = inject(AccountService);
  private readonly router = inject(Router);
  private readonly sessionState = signal<AuthSession | null>(null);
  private pendingTwoFactorContext: PendingTwoFactorContext | null = null;

  public readonly session = this.sessionState.asReadonly();
  public readonly isAuthenticated = computed(() => this.sessionState() !== null);

  public login(payload: AuthLoginRequest): Observable<LoginOutcome> {
    return this.endpoint
      .login({
        tipoDocumento: payload.documentType,
        documento: formatDocumentForBackend(payload.documentType, payload.documentNumber),
        password: payload.password,
      })
      .pipe(map(result => this.toOutcome(result, payload)));
  }

  /**
   * Verify the 6-digit code received by email and complete authentication.
   * On success, stores the session locally so the auth guard accepts the user.
   */
  public completeTwoFactor(payload: {
    sessionId: string;
    code: string;
    documentType: string;
    documentNumber: string;
  }): Observable<AuthSession> {
    return this.endpoint
      .verifyTwoFactorCode({ sessionId: payload.sessionId, codigo: payload.code })
      .pipe(
        map(result =>
          this.toSession(result, {
            documentType: payload.documentType,
            documentNumber: payload.documentNumber,
            password: '',
          })
        ),
        tap(session => this.storeSession(session))
      );
  }

  public resendTwoFactorCode(sessionId: string): Observable<ResendTwoFactorCodeResult> {
    return this.endpoint.resendTwoFactorCode({ sessionId });
  }

  public takePendingTwoFactorContext(): PendingTwoFactorContext | null {
    const context = this.pendingTwoFactorContext;
    this.pendingTwoFactorContext = null;
    return context;
  }

  public hydrateAuthenticatedSession(): Observable<AuthSession> {
    return this.account.getPersonalData().pipe(
      map(personalData => ({
        documentType: personalData.documentType,
        documentNumber: personalData.documentNumber,
        primerNombre: personalData.firstName,
      })),
      tap(session => this.storeSession(session))
    );
  }

  public ensureAuthenticatedSession(): Observable<boolean> {
    return this.hydrateAuthenticatedSession().pipe(
      map(() => true),
      catchError(() => {
        this.clearSession();
        return of(false);
      })
    );
  }

  public refreshAccessToken(): Observable<void> {
    return this.endpoint.refreshToken();
  }

  public clearSession(): void {
    this.sessionState.set(null);
    this.pendingTwoFactorContext = null;
  }

  public logout(): void {
    this.endpoint
      .logout()
      .pipe(
        catchError(() => EMPTY),
        finalize(() => {
          this.clearSession();
          this.router.navigateByUrl('/iniciar-sesion');
        })
      )
      .subscribe();
  }

  private toOutcome(result: LoginResult, payload: AuthLoginRequest): LoginOutcome {
    if (result.kind === 'twoFactorRequired') {
      this.pendingTwoFactorContext = {
        documentNumber: payload.documentNumber,
        documentType: payload.documentType,
        email: result.maskedEmail,
        sessionId: result.sessionId,
      };
      return { kind: 'twoFactorRequired' };
    }

    const session = this.toSession(result, payload);
    this.storeSession(session);
    return { kind: 'authenticated', session };
  }

  private toSession(
    result: { documento: string; primerNombre: string },
    payload: AuthLoginRequest
  ): AuthSession {
    return {
      documentType: payload.documentType,
      documentNumber: result.documento || payload.documentNumber,
      primerNombre: result.primerNombre,
    };
  }

  private storeSession(session: AuthSession): void {
    this.sessionState.set(session);
  }
}
