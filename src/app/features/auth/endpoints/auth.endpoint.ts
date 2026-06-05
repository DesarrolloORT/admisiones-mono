import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  LoginPayload as GeneratedLoginPayload,
  postAuthActivarLinkPasswordEndpoint,
  postAuthCompletarPasswordEndpoint,
  postAuthLoginEndpoint,
  postAuthLogoutEndpoint,
  postAuthRecuperarContrasenaEndpoint,
} from 'src/app/shared/api/generated/endpoints/auth.endpoints';
import {
  postRegistroAnalizarAdjuntoEndpoint,
  postRegistroConfirmarNuevaPersonaEndpoint,
  postRegistroConfirmarSolicitudAltaEndpoint,
  postRegistroEvaluarDocumentoEndpoint,
  postRegistroVerificarIdentidadEndpoint,
} from 'src/app/shared/api/generated/endpoints/registro.endpoints';

import type {
  DocumentRecognitionData,
  DocumentRecognitionRequest,
} from '../models/document-recognition.interface';

export const AUTH_FLOW_ID_HEADER = 'X-Flow-Id';

// ---------------------------------------------------------------------------
// Stable public types — these are the contract that the rest of the feature
// depends on. They do NOT change when `npm run update-api` regenerates
// endpoints or models. Only this file knows about generated constants and DTOs.
// ---------------------------------------------------------------------------

/** Input for login. Maps internally to generated `LoginPayload`. */
export interface LoginPayload {
  tipoDocumento: string;
  documento: string;
  password: string;
}

/** Stable output of login. Hides backend DTO shape (`DtoAuthenticationResponse`). */
export interface LoginResult {
  documento: string;
  primerNombre: string;
}

/** Input for user registration. */
export interface RegisterPayload {
  tipoDocumento: string;
  documento: string;
  primerNombre: string;
  segundoNombre?: string | null;
  primerApellido: string;
  segundoApellido?: string | null;
  fechaNacimiento?: string;
  sexo: string;
  codigoPais?: number;
  codigoEstado?: number;
  codigoCiudad?: number;
  direccion: string;
  telefono1: string;
  mail: string;
  verificacionMail: string;
}

/** Input for confirming an application request. */
export type ConfirmApplicationRequestPayload = RegisterPayload;

/** Input for identity verification. */
export interface VerifyIdentityPayload {
  tipoDocumento: string;
  documento: string;
  primerApellido: string;
  mail: string;
}

/** Stable output of identity verification. */
export interface VerifyIdentityResult {
  success: boolean;
}

/** Stable output of registration. Hides `ObjectOperationResult` from backend. */
export interface RegisterResult {
  success: boolean;
}

/** Input for document evaluation before registration. */
export interface EvaluateDocumentPayload {
  tipoDocumento: string;
  documento: string;
}

/** Stable output of document evaluation. */
export interface EvaluateDocumentResult {
  flowId: string | null;
  requiereAltaPersona: boolean;
  requiereAltaSolicitud: boolean;
  requiereVerificacion: boolean;
  solicitudAltaExistente: boolean;
  usuarioExistente: boolean;
  message: string | null;
}

/** Input for validating a password activation link. */
export interface ActivatePasswordLinkPayload {
  token: string;
}

/** Input for completing the password activation flow. */
export interface CompletePasswordPayload {
  passwordNueva: string;
}

/** Input for initiating password recovery. */
export interface RecoverPasswordPayload {
  tipoDocumento: string;
  documento: string;
  primerApellido: string;
}

/**
 * Auth endpoint adapter.
 *
 * This adapter is one of the auth feature files allowed to import generated
 * endpoints and backend DTOs. It translates between the unstable generated
 * layer and the stable frontend types consumed by services, pages and
 * components.
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
      tipoDocumento: payload.tipoDocumento,
      documento: payload.documento,
      password: payload.password,
    };

    return this.api.data(postAuthLoginEndpoint, { body, withCredentials: true }).pipe(
      map(response => ({
        documento: response.persona?.documento ?? '',
        primerNombre: response.persona?.primerNombre ?? '',
      }))
    );
  }

  /**
   * Validate an activation/recovery token and create the temporary password cookie.
   *
   * Behind the scenes: POST /Auth/ActivarLinkPassword using generated endpoint.
   */
  public activatePasswordLink(payload: ActivatePasswordLinkPayload): Observable<void> {
    return this.api
      .request(postAuthActivarLinkPasswordEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(map(() => undefined));
  }

  /**
   * Complete password activation using the temporary cookie created by the link.
   *
   * Behind the scenes: POST /Auth/CompletarPassword using generated endpoint.
   */
  public completePassword(payload: CompletePasswordPayload): Observable<void> {
    return this.api
      .request(postAuthCompletarPasswordEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(map(() => undefined));
  }

  /**
   * Register a new person.
   *
   * Behind the scenes: POST /Registro/ConfirmarNuevaPersona using generated endpoint.
   * `RegisterPayload` is a direct alias of `ConfirmarNuevaPersonaPayload` so
   * no field mapping is needed — the body is passed through as-is.
   * Response mapped from `ObjectOperationResult` → `RegisterResult`.
   */
  public register(payload: RegisterPayload, flowId: string): Observable<RegisterResult> {
    return this.api
      .request(postRegistroConfirmarNuevaPersonaEndpoint, {
        body: payload,
        headers: this.getFlowHeaders(flowId),
        withCredentials: true,
      })
      .pipe(map(() => ({ success: true })));
  }

  /**
   * Confirm a pending application request.
   *
   * Behind the scenes: POST /Registro/ConfirmarSolicitudAlta using generated endpoint.
   * Response mapped from `ObjectOperationResult` → `RegisterResult`.
   */
  public confirmApplicationRequest(
    payload: ConfirmApplicationRequestPayload,
    flowId: string
  ): Observable<RegisterResult> {
    return this.api
      .request(postRegistroConfirmarSolicitudAltaEndpoint, {
        body: payload,
        headers: this.getFlowHeaders(flowId),
        withCredentials: true,
      })
      .pipe(map(() => ({ success: true })));
  }

  /**
   * Evaluate whether a document can start registration.
   *
   * Behind the scenes: POST /Registro/EvaluarDocumento using generated endpoint.
   * Response mapped from `RegistroEvaluacionResponseOperationResult` → `EvaluateDocumentResult`.
   */
  public evaluateDocument(payload: EvaluateDocumentPayload): Observable<EvaluateDocumentResult> {
    return this.api
      .requestWithMessage(postRegistroEvaluarDocumentoEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(
        map(({ data, message }) => ({
          flowId: data.flowId ?? null,
          requiereAltaPersona: data.requiereAltaPersona ?? false,
          requiereAltaSolicitud: data.requiereAltaSolicitud ?? false,
          requiereVerificacion: data.requiereVerificacion ?? false,
          solicitudAltaExistente: data.solicitudAltaExistente ?? false,
          usuarioExistente: data.usuarioExistente ?? false,
          message,
        }))
      );
  }

  /**
   * Analyze an uploaded document image via OCR.
   *
   * Behind the scenes: POST /Registro/AnalizarAdjunto using generated endpoint.
   * Response mapped from `ReconocimientoDocumentoResponseOperationResult` → `DocumentRecognitionData`.
   */
  public recognizeDocument(
    payload: DocumentRecognitionRequest
  ): Observable<DocumentRecognitionData> {
    return this.api.request(postRegistroAnalizarAdjuntoEndpoint, {
      body: payload,
      withCredentials: true,
    });
  }

  /**
   * Verify the identity of a person before completing registration.
   *
   * Behind the scenes: POST /Registro/VerificarIdentidad using generated endpoint.
   * Response mapped from `ObjectOperationResult` → `VerifyIdentityResult`.
   */
  public verifyIdentity(
    payload: VerifyIdentityPayload,
    flowId: string
  ): Observable<VerifyIdentityResult> {
    return this.api
      .request(postRegistroVerificarIdentidadEndpoint, {
        body: payload,
        headers: this.getFlowHeaders(flowId),
        withCredentials: true,
      })
      .pipe(map(() => ({ success: true })));
  }

  /**
   * Initiate password recovery. Sends an email with a secure link if data matches.
   *
   * Behind the scenes: POST /Auth/RecuperarContraseña using generated endpoint.
   * Response is intentionally generic to avoid revealing whether the person exists.
   */
  public recoverPassword(payload: RecoverPasswordPayload): Observable<void> {
    return this.api
      .request(postAuthRecuperarContrasenaEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(map(() => undefined));
  }

  /**
   * Close the current session on the backend, clearing HttpOnly cookies.
   *
   * Behind the scenes: POST /Auth/Logout using generated endpoint.
   */
  public logout(): Observable<void> {
    return this.api
      .request(postAuthLogoutEndpoint, { withCredentials: true })
      .pipe(map(() => undefined));
  }

  private getFlowHeaders(flowId: string): Record<string, string> {
    return { [AUTH_FLOW_ID_HEADER]: flowId.trim() };
  }
}
