import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client.service';
import { postAuthLoginEndpoint } from 'src/app/shared/api/endpoints/generated/auth.endpoints';
import {
  postRegistroAnalizarAdjuntoEndpoint,
  postRegistroConfirmarNuevaPersonaEndpoint,
} from 'src/app/shared/api/endpoints/generated/registro.endpoints';
import type { AuthRequest } from 'src/app/shared/api-models/model/authRequest';

import type {
  DocumentRecognitionRequest,
  DocumentRecognitionResponse,
} from '../models/document-recognition.interface';
import { AuthRequestError, AuthRequestOperation } from '../models/auth-error';
import { DocumentRecognitionRequestError } from '../models/document-recognition-error';

// ---------------------------------------------------------------------------
// Stable public types — these are the contract that the rest of the feature
// depends on. They do NOT change when `npm run update-api` regenerates
// endpoints or models. Only this file knows about generated constants and DTOs.
// ---------------------------------------------------------------------------

/** Input for login. Maps internally to generated `AuthRequest`. */
export interface LoginPayload {
  codigoPersona: number;
  password: string;
}

/** Stable output of login. Hides backend DTO shape (`DtoAuthenticationResponse`). */
export interface LoginResult {
  documento: string;
}

/** Input for user registration. Maps 1:1 to backend body but typed locally. */
export interface RegisterPayload {
  tipoDocumento: string;
  documento: string;
  primerNombre: string;
  segundoNombre: string | null;
  primerApellido: string;
  segundoApellido: string | null;
  fechaNacimiento: string;
  sexo: string;
  direccion: string;
  telefono1: string;
  mail: string;
  verificacionMail: string;
  codigoPais?: number;
  codigoEstado?: number;
  codigoCiudad?: number;
}

/** Stable output of registration. Hides `ObjectOperationResult` from backend. */
export interface RegisterResult {
  success: boolean;
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
 *           `postRegistroAnalizarAdjuntoEndpoint` and `AuthRequest` DTO.
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
    const body: AuthRequest = {
      codigoPersona: payload.codigoPersona,
      password: payload.password,
    };

    return this.api
      .data(postAuthLoginEndpoint, { body, withCredentials: true })
      .pipe(
        map(response => ({ documento: response.persona?.documento ?? '' })),
        catchError(error => this.toAuthError('login', error))
      );
  }

  /**
   * Register a new person.
   *
   * Behind the scenes: POST /Registro/ConfirmarNuevaPersona using generated endpoint.
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
