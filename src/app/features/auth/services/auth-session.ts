import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CacheService } from '@desarrolloort/ngx-utils';
import { EMPTY, Observable } from 'rxjs';
import { catchError, finalize, map, tap } from 'rxjs/operators';
import { storageKeys } from 'src/app/core/storage/keys';

import { AuthEndpoint, LoginResult, ResendTwoFactorCodeResult } from '../endpoints/auth.endpoint';
import { AuthLoginRequest, AuthSession } from '../models/auth.interface';
import { formatDocumentForBackend } from '../models/document-number';

/**
 * Outcome of `AuthSessionService.login`.
 * - `authenticated`: session is established and stored.
 * - `twoFactorRequired`: caller must navigate to the 2FA page and complete verification.
 */
export type LoginOutcome =
  | { kind: 'authenticated'; session: AuthSession }
  | { kind: 'twoFactorRequired'; sessionId: string; maskedEmail: string; message: string };

@Injectable({
  providedIn: 'root',
})
export class AuthSessionService {
  private readonly endpoint = inject(AuthEndpoint);
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly cache = inject(CacheService);
  private readonly sessionState = signal<AuthSession | null>(this.restoreSession());

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

  public logout(): void {
    this.endpoint
      .logout()
      .pipe(
        catchError(() => EMPTY),
        finalize(() => {
          this.sessionState.set(null);
          this.storage?.removeItem(storageKeys.token);
          this.storage?.removeItem(storageKeys.session);
          this.cache.clear();
          this.router.navigateByUrl('/iniciar-sesion');
        })
      )
      .subscribe();
  }

  private toOutcome(result: LoginResult, payload: AuthLoginRequest): LoginOutcome {
    if (result.kind === 'twoFactorRequired') {
      return {
        kind: 'twoFactorRequired',
        sessionId: result.sessionId,
        maskedEmail: result.maskedEmail,
        message: result.message,
      };
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
      token: null,
      documentType: payload.documentType,
      documentNumber: result.documento || payload.documentNumber,
      primerNombre: result.primerNombre,
      expiresAt: null,
    };
  }

  private storeSession(session: AuthSession): void {
    this.sessionState.set(session);
    this.storage?.setItem(storageKeys.session, JSON.stringify(session));

    if (session.token) {
      this.storage?.setItem(storageKeys.token, session.token);
    }
  }

  private restoreSession(): AuthSession | null {
    const rawSession = this.storage?.getItem(storageKeys.session);

    if (!rawSession) {
      return null;
    }

    try {
      const parsed = JSON.parse(rawSession) as Partial<AuthSession>;

      if (typeof parsed.documentType !== 'string' || typeof parsed.documentNumber !== 'string') {
        return null;
      }

      return {
        token: typeof parsed.token === 'string' ? parsed.token : null,
        documentType: parsed.documentType,
        documentNumber: parsed.documentNumber,
        primerNombre: typeof parsed.primerNombre === 'string' ? parsed.primerNombre : '',
        expiresAt: typeof parsed.expiresAt === 'string' ? parsed.expiresAt : null,
      };
    } catch {
      return null;
    }
  }

  private get storage(): Storage | null {
    return this.document.defaultView?.localStorage ?? null;
  }
}
