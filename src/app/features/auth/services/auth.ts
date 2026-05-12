import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { storageKeys } from 'src/app/core/storage/keys';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import {
  AuthLoginRequest,
  AuthLoginResponse,
  AuthRegisterRequest,
  AuthRegisterResponse,
  AuthSession,
} from '../models/auth.interface';

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly endpoint = inject(AuthEndpoint);
  private readonly document = inject(DOCUMENT);
  private readonly sessionState = signal<AuthSession | null>(this.restoreSession());

  public readonly session = this.sessionState.asReadonly();
  public readonly isAuthenticated = computed(() => this.sessionState() !== null);

  public login(payload: AuthLoginRequest): Observable<AuthSession> {
    return this.endpoint.login(payload).pipe(
      map(response => this.toSession(response, payload)),
      tap(session => this.storeSession(session))
    );
  }

  public register(payload: AuthRegisterRequest): Observable<AuthRegisterResponse> {
    return this.endpoint.register(payload);
  }

  public logout(): void {
    this.sessionState.set(null);
    this.storage?.removeItem(storageKeys.token);
    this.storage?.removeItem(storageKeys.session);
  }

  private toSession(response: AuthLoginResponse, payload: AuthLoginRequest): AuthSession {
    return {
      token:
        response.token ??
        response.accessToken ??
        response.data?.token ??
        response.data?.accessToken ??
        null,
      documentType: payload.documentType,
      documentNumber: payload.documentNumber,
      expiresAt: response.expiresAt ?? response.data?.expiresAt ?? null,
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
