import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CacheService } from '@desarrolloort/ngx-utils';
import { EMPTY, Observable } from 'rxjs';
import { catchError, finalize, map, tap } from 'rxjs/operators';
import { storageKeys } from 'src/app/core/storage/keys';

import type {
  EvaluateDocumentResult,
  RegisterPayload,
  VerifyIdentityPayload,
  VerifyIdentityResult,
} from '../endpoints/auth.endpoint';
import { AuthEndpoint } from '../endpoints/auth.endpoint';
import {
  AuthLoginRequest,
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
  private readonly router = inject(Router);
  private readonly cache = inject(CacheService);
  private readonly sessionState = signal<AuthSession | null>(this.restoreSession());

  public readonly session = this.sessionState.asReadonly();
  public readonly isAuthenticated = computed(() => this.sessionState() !== null);

  public login(payload: AuthLoginRequest): Observable<AuthSession> {
    return this.endpoint
      .login({
        tipoDocumento: payload.documentType,
        documento: this.formatDocumentForBackend(payload.documentType, payload.documentNumber),
        password: payload.password,
      })
      .pipe(
        map(result => this.toSession(result, payload)),
        tap(session => this.storeSession(session))
      );
  }

  public register(payload: AuthRegisterRequest, flowId: string): Observable<AuthRegisterResponse> {
    return this.endpoint.register(this.toRegisterPayload(payload), flowId);
  }

  public confirmApplicationRequest(
    payload: AuthRegisterRequest,
    flowId: string
  ): Observable<AuthRegisterResponse> {
    return this.endpoint.confirmApplicationRequest(this.toRegisterPayload(payload), flowId);
  }

  private toRegisterPayload(payload: AuthRegisterRequest): RegisterPayload {
    const { identity, personal } = payload;

    return {
      tipoDocumento: identity.documentType,
      documento: this.formatDocumentForBackend(identity.documentType, identity.documentNumber),
      primerNombre: personal.primerNombre,
      segundoNombre: personal.segundoNombre || null,
      primerApellido: personal.primerApellido,
      segundoApellido: personal.segundoApellido || null,
      fechaNacimiento: personal.fechaNacimiento,
      sexo: personal.sexo,
      direccion: personal.direccion,
      telefono1: personal.telefono1,
      mail: personal.mail,
      verificacionMail: personal.verificacionMail,
      codigoPais: personal.codigoPais ?? undefined,
      codigoEstado: personal.codigoEstado ?? undefined,
      codigoCiudad: personal.codigoCiudad ?? undefined,
    };
  }

  public evaluateDocument(
    tipoDocumento: string,
    documento: string
  ): Observable<EvaluateDocumentResult> {
    return this.endpoint.evaluateDocument({
      tipoDocumento,
      documento: this.formatDocumentForBackend(tipoDocumento, documento),
    });
  }

  public verifyIdentity(
    payload: VerifyIdentityPayload,
    flowId: string
  ): Observable<VerifyIdentityResult> {
    const formatted = {
      ...payload,
      documento: this.formatDocumentForBackend(payload.tipoDocumento, payload.documento),
    };

    return this.endpoint.verifyIdentity(formatted, flowId);
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

  private formatDocumentForBackend(tipoDocumento: string, documento: string): string {
    if (tipoDocumento !== 'CI') {
      return documento;
    }

    const digits = documento.replace(/\D/g, '');

    return `${digits.slice(0, -1)}-${digits.slice(-1)}`;
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
