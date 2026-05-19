import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client.service';
import {
  LoginPayload as GeneratedLoginPayload,
  postAuthLoginEndpoint,
} from 'src/app/shared/api/generated/endpoints/auth.endpoints';
import {
  ConfirmarNuevaPersonaPayload,
  ConfirmarPersonaExistentePayload,
  EvaluarDocumentoPayload,
  postRegistroAnalizarAdjuntoEndpoint,
  postRegistroConfirmarNuevaPersonaEndpoint,
  postRegistroConfirmarPersonaExistenteEndpoint,
  postRegistroEvaluarDocumentoEndpoint,
  postRegistroVerificarIdentidadEndpoint,
  VerificarIdentidadPayload,
} from 'src/app/shared/api/generated/endpoints/registro.endpoints';

import { AuthRequestError, AuthRequestOperation } from '../models/auth-error';
import { DocumentRecognitionRequestError } from '../models/document-recognition-error';
import type {
  DocumentRecognitionRequest,
  DocumentRecognitionResponse,
} from '../models/document-recognition.interface';

// ---------------------------------------------------------------------------
// Stable public types — these are the contract that the rest of the feature
// depends on. They do NOT change when `npm run update-api` regenerates
// endpoints or models. Only this file knows about generated constants and DTOs.
// ---------------------------------------------------------------------------

/** Input for login. Maps internally to generated `LoginPayload`. */
export interface LoginPayload {
  codigoPersona: number;
  password: string;
}

/** Stable output of login. Hides backend DTO shape (`DtoAuthenticationResponse`). */
export interface LoginResult {
  documento: string;
}

/**
 * Input for user registration.
 * Uses the auto-generated `ConfirmarNuevaPersonaPayload` — if the backend adds
 * or removes fields, `npm run update-api` regenerates this type and TypeScript
 * surfaces the change without manual sync.
 */
export type RegisterPayload = ConfirmarNuevaPersonaPayload;

/**
 * Input for confirming an existing person with academic interest.
 * Uses the auto-generated `ConfirmarPersonaExistentePayload`.
 */
export type ConfirmExistingPersonPayload = ConfirmarPersonaExistentePayload;

/** Input for identity verification. */
export type VerifyIdentityPayload = VerificarIdentidadPayload;

/** Stable output of identity verification. */
export interface VerifyIdentityResult {
  success: boolean;
}

/** Stable output of registration. Hides `ObjectOperationResult` from backend. */
export interface RegisterResult {
  success: boolean;
}

/** Input for document evaluation before registration. */
export type EvaluateDocumentPayload = EvaluarDocumentoPayload;

/** Stable output of document evaluation. */
export interface EvaluateDocumentResult {
  requiereAltaPersona: boolean;
  requiereAltaSolicitud: boolean;
  requiereVerificacion: boolean;
  solicitudAltaExistente: boolean;
  usuarioExistente: boolean;
}

/**
 * Auth endpoint adapter.
 *
 * This is the **only** file in the auth feature that imports generated endpoints
 * and backend DTOs. It translates between the unstable generated layer and the
 * stable frontend types consumed by services, pages and components.
 *
 * When the backend changes (URL, DTO shape, field names), only this file needs
 * adjustment — the rest of the feature keeps compiling unchanged.
 *
 * @stable Public methods and their input/output types.
 * @unstable Internal usage of `postAuthLoginEndpoint`, `postRegistroConfirmarNuevaPersonaEndpoint`,
 *           `postRegistroAnalizarAdjuntoEndpoint` and generated payload types.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthEndpoint {
  private readonly api = inject(ApiHttpClient);

  /**
   * Authenticate user credentials.
   *
   * Behind the scenes: POST /Auth/Login using generated `postAuthLoginEndpoint`.
   * Response mapped from `DtoAuthenticationResponse` → `LoginResult`.
   */
  public login(payload: LoginPayload): Observable<LoginResult> {
    const body: GeneratedLoginPayload = {
      codigoPersona: payload.codigoPersona,
      password: payload.password,
    };

    return this.api.data(postAuthLoginEndpoint, { body, withCredentials: true }).pipe(
      map(response => ({ documento: response.persona?.documento ?? '' })),
      catchError(error => this.toAuthError('login', error))
    );
  }

  /**
   * Register a new person.
   *
   * Behind the scenes: POST /Registro/ConfirmarNuevaPersona using generated endpoint.
   * `RegisterPayload` is a direct alias of `ConfirmarNuevaPersonaPayload` so
   * no field mapping is needed — the body is passed through as-is.
   * Response mapped from `ObjectOperationResult` → `RegisterResult`.
   */
  public register(payload: RegisterPayload): Observable<RegisterResult> {
    return this.api
      .request(postRegistroConfirmarNuevaPersonaEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(
        map(result => ({ success: result.success ?? false })),
        catchError(error => this.toAuthError('register', error))
      );
  }

  /**
   * Evaluate whether a document can start registration.
   *
   * Behind the scenes: POST /Registro/EvaluarDocumento using generated endpoint.
   * Response mapped from `RegistroEvaluacionResponseOperationResult` → `EvaluateDocumentResult`.
   */
  public evaluateDocument(payload: EvaluateDocumentPayload): Observable<EvaluateDocumentResult> {
    return this.api
      .request(postRegistroEvaluarDocumentoEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(
        map(result => ({
          requiereAltaPersona: result.data?.requiereAltaPersona ?? false,
          requiereAltaSolicitud: result.data?.requiereAltaSolicitud ?? false,
          requiereVerificacion: result.data?.requiereVerificacion ?? false,
          solicitudAltaExistente: result.data?.solicitudAltaExistente ?? false,
          usuarioExistente: result.data?.usuarioExistente ?? false,
        })),
        catchError(error => this.toAuthError('evaluateDocument', error))
      );
  }

  /**
   * Analyze an uploaded document image via OCR.
   *
   * Behind the scenes: POST /Registro/AnalizarAdjunto using generated endpoint.
   * Response passed through as `DocumentRecognitionResponse` (already a feature type).
   */
  public recognizeDocument(
    payload: DocumentRecognitionRequest
  ): Observable<DocumentRecognitionResponse> {
    return this.api
      .request(postRegistroAnalizarAdjuntoEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(catchError(error => this.toDocRecognitionError(error)));
  }

  /**
   * Verify the identity of a person before confirming academic interest.
   *
   * Behind the scenes: POST /Registro/VerificarIdentidad using generated endpoint.
   * Response mapped from `ObjectOperationResult` → `VerifyIdentityResult`.
   */
  public verifyIdentity(payload: VerifyIdentityPayload): Observable<VerifyIdentityResult> {
    return this.api
      .request(postRegistroVerificarIdentidadEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(
        map(result => ({ success: result.success ?? false })),
        catchError(error => this.toAuthError('verifyIdentity', error))
      );
  }

  /**
   * Confirm an existing person with academic interest.
   *
   * Behind the scenes: POST /Registro/ConfirmarPersonaExistente using generated endpoint.
   * Response mapped from `ObjectOperationResult` → `RegisterResult`.
   */
  public confirmExistingPerson(payload: ConfirmExistingPersonPayload): Observable<RegisterResult> {
    return this.api
      .request(postRegistroConfirmarPersonaExistenteEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(
        map(result => ({ success: result.success ?? false })),
        catchError(error => this.toAuthError('register', error))
      );
  }

  private toAuthError(operation: AuthRequestOperation, error: unknown): Observable<never> {
    const status = this.getHttpStatus(error);
    return throwError(() => new AuthRequestError(operation, status));
  }

  private toDocRecognitionError(error: unknown): Observable<never> {
    const status = this.getHttpStatus(error);
    return throwError(() => new DocumentRecognitionRequestError(status));
  }

  private getHttpStatus(error: unknown): number | null {
    return error instanceof HttpErrorResponse ? error.status : null;
  }
}

