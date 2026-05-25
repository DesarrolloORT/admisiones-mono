import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CacheService } from '@desarrolloort/ngx-utils';
import { EMPTY, Observable } from 'rxjs';
import { catchError, finalize, map, tap } from 'rxjs/operators';
import { storageKeys } from 'src/app/core/storage/keys';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { AuthLoginRequest, AuthSession } from '../models/auth.interface';
import { formatDocumentForBackend } from '../models/document-number';

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

  public login(payload: AuthLoginRequest): Observable<AuthSession> {
    return this.endpoint
      .login({
        tipoDocumento: payload.documentType,
        documento: formatDocumentForBackend(payload.documentType, payload.documentNumber),
        password: payload.password,
      })
      .pipe(
        map(result => this.toSession(result, payload)),
        tap(session => this.storeSession(session))
      );
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
          this.router.navigateByUrl('/login');
        })
      )
      .subscribe();
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
