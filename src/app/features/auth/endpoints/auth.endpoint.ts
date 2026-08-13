import { inject, Injectable } from '@angular/core';
import { CUSTOM_ERROR_MESSAGES, suppressGlobalErrorContext } from '@desarrolloort/ngx-utils';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  LoginPayload as GeneratedLoginPayload,
  postAuthActivatePasswordLinkEndpoint,
  postAuthCompleteInitialPasswordEndpoint,
  postAuthLoginEndpoint,
  postAuthLogoutEndpoint,
  postAuthRecoverPasswordEndpoint,
  postAuthRefreshTokenEndpoint,
  postAuthResendTwoFactorCodeEndpoint,
  postAuthVerifyTwoFactorCodeEndpoint,
} from 'src/app/shared/api/generated/endpoints/auth.endpoints';
import {
  postRegistrationAnalyzeAttachmentEndpoint,
  postRegistrationConfirmNewPersonEndpoint,
  postRegistrationConfirmRegistrationRequestEndpoint,
  postRegistrationEvaluateDocumentEndpoint,
  postRegistrationVerifyIdentityEndpoint,
} from 'src/app/shared/api/generated/endpoints/registration.endpoints';

import type { AuthPhoneNumber } from '../models/auth.interface';
import type {
  DocumentRecognitionData,
  DocumentRecognitionRequest,
} from '../models/document-recognition.interface';

export const AUTH_FLOW_ID_HEADER = 'X-Flow-Id';

const LOGIN_ERROR_MESSAGES: Record<number, string> = {
  401: 'Credenciales inválidas.',
  429: 'Demasiados intentos. Intentá nuevamente más tarde.',
};

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

/**
 * Stable output of login. Discriminated union: 200 → authenticated, 202 → 2FA required.
 * Hides backend DTO shape (`DtoAuthenticationResponse` / `DtoLogin2FARequired`).
 */
export type LoginResult =
  | {
      kind: 'authenticated';
      documento: string;
      primerNombre: string;
    }
  | {
      kind: 'twoFactorRequired';
      sessionId: string;
      maskedEmail: string;
      message: string;
    };

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
  telefono1: AuthPhoneNumber;
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

/** Input for verifying the two-factor authentication code. */
export interface VerifyTwoFactorCodePayload {
  sessionId: string;
  codigo: string;
}

/** Input for resending the two-factor authentication code. */
export interface ResendTwoFactorCodePayload {
  sessionId: string;
}

/** Stable output of resending the two-factor authentication code. */
export interface ResendTwoFactorCodeResult {
  sessionId: string;
  maskedEmail: string;
  message: string;
}

/** Stable output of 2FA verification. Same shape as authenticated login. */
export interface VerifyTwoFactorCodeResult {
  documento: string;
  primerNombre: string;
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
 * @unstable Internal usage of `postAuthLoginEndpoint`,
 *           `postRegistrationConfirmNewPersonEndpoint`,
 *           `postRegistrationAnalyzeAttachmentEndpoint` and generated payload types.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthEndpoint {
  private readonly api = inject(ApiHttpClient);

  /**
   * Authenticate user credentials.
   *
   * Behind the scenes: POST /auth/login using generated `postAuthLoginEndpoint`.
   * The backend returns either:
   *   - 200 with `data.persona` → fully authenticated, cookies set.
   *   - 202 with `data.sessionId` + `data.maskedEmail` → 2FA code emailed; caller must verify.
   * Discriminates by presence of `sessionId` in the unwrapped data.
   */
  public login(payload: LoginPayload): Observable<LoginResult> {
    const body: GeneratedLoginPayload = {
      documentType: payload.tipoDocumento,
      documentNumber: payload.documento,
      password: payload.password,
    };

    return this.api
      .data(postAuthLoginEndpoint, {
        body,
        withCredentials: true,
        captchaAction: 'login',
        context: suppressGlobalErrorContext().set(CUSTOM_ERROR_MESSAGES, LOGIN_ERROR_MESSAGES),
      })
      .pipe(
        map(response => {
          if ('sessionId' in response) {
            return {
              kind: 'twoFactorRequired',
              sessionId: response.sessionId ?? '',
              maskedEmail: response.maskedEmail ?? '',
              message: response.message ?? '',
            } satisfies LoginResult;
          }

          return {
            kind: 'authenticated',
            documento: response.person?.documentNumber ?? '',
            primerNombre: response.person?.firstName ?? '',
          } satisfies LoginResult;
        })
      );
  }

  /**
   * Validate an activation/recovery token and create the temporary password cookie.
   *
   * Behind the scenes: POST /auth/activate-password-link using generated endpoint.
   */
  public activatePasswordLink(payload: ActivatePasswordLinkPayload): Observable<void> {
    return this.api
      .request(postAuthActivatePasswordLinkEndpoint, {
        body: payload,
        withCredentials: true,
        showLoader: true,
      })
      .pipe(map(() => undefined));
  }

  /**
   * Complete password activation using the temporary cookie created by the link.
   *
   * Behind the scenes: POST /auth/complete-initial-password using generated endpoint.
   */
  public completePassword(payload: CompletePasswordPayload): Observable<void> {
    return this.api
      .request(postAuthCompleteInitialPasswordEndpoint, {
        body: { newPassword: payload.passwordNueva },
        withCredentials: true,
      })
      .pipe(map(() => undefined));
  }

  /**
   * Register a new person.
   *
   * Behind the scenes: POST /registration/confirm-new-person using generated endpoint.
   * `RegisterPayload` is a direct alias of `ConfirmarNuevaPersonaPayload` so
   * no field mapping is needed — the body is passed through as-is.
   * Response mapped from `ObjectOperationResult` → `RegisterResult`.
   */
  public register(payload: RegisterPayload, flowId: string): Observable<RegisterResult> {
    const body = this.toRegisterPersonRequest(payload);

    return this.api
      .request(postRegistrationConfirmNewPersonEndpoint, {
        body,
        headers: this.getFlowHeaders(flowId),
        withCredentials: true,
        captchaAction: 'ConfirmNewPerson',
      })
      .pipe(map(() => ({ success: true })));
  }

  /**
   * Confirm a pending application request.
   *
   * Behind the scenes: POST /registration/confirm-registration-request using generated endpoint.
   * Response mapped from `ObjectOperationResult` → `RegisterResult`.
   */
  public confirmApplicationRequest(
    payload: ConfirmApplicationRequestPayload,
    flowId: string
  ): Observable<RegisterResult> {
    const body = this.toRegisterPersonRequest(payload);

    return this.api
      .request(postRegistrationConfirmRegistrationRequestEndpoint, {
        body,
        headers: this.getFlowHeaders(flowId),
        withCredentials: true,
        captchaAction: 'ConfirmRegistrationRequest',
      })
      .pipe(map(() => ({ success: true })));
  }

  /**
   * Evaluate whether a document can start registration.
   *
   * Behind the scenes: POST /registration/evaluate-document using generated endpoint.
   * Response mapped from `RegistroEvaluacionResponseOperationResult` → `EvaluateDocumentResult`.
   */
  public evaluateDocument(payload: EvaluateDocumentPayload): Observable<EvaluateDocumentResult> {
    return this.api
      .requestWithMessage(postRegistrationEvaluateDocumentEndpoint, {
        body: { documentType: payload.tipoDocumento, documentNumber: payload.documento },
        withCredentials: true,
        captchaAction: 'EvaluateDocument',
      })
      .pipe(
        map(({ data, message }) => ({
          flowId: data.flowId ?? null,
          requiereAltaPersona: data.requiresPersonRegistration ?? false,
          requiereAltaSolicitud: data.requiresRegistrationRequest ?? false,
          requiereVerificacion: data.requiresIdentityVerification ?? false,
          solicitudAltaExistente: data.registrationRequestPending ?? false,
          usuarioExistente: data.userAlreadyRegistered ?? false,
          message,
        }))
      );
  }

  /**
   * Analyze an uploaded document image via OCR.
   *
   * Behind the scenes: POST /registration/analyze-attachment using generated endpoint.
   * Response mapped from `ReconocimientoDocumentoResponseOperationResult` → `DocumentRecognitionData`.
   */
  public recognizeDocument(
    payload: DocumentRecognitionRequest
  ): Observable<DocumentRecognitionData> {
    return this.api
      .request(postRegistrationAnalyzeAttachmentEndpoint, {
        body: {
          mimeType: payload.tipoMime,
          file: {
            fileName: payload.archivoAdjunto.nombreArchivo,
            content: payload.archivoAdjunto.archivo,
          },
        },
        withCredentials: true,
        captchaAction: 'AnalyzeAttachment',
      })
      .pipe(
        map(response => ({
          campos: response?.fields
            ? {
                tipoDocumento: response.fields.documentType ?? null,
                numeroDocumento: response.fields.documentNumber ?? null,
                primerNombre: response.fields.firstName ?? null,
                segundoNombre: response.fields.middleName ?? null,
                primerApellido: response.fields.firstSurname ?? null,
                segundoApellido: response.fields.secondSurname ?? null,
                fechaNacimiento: response.fields.birthDate ?? null,
                lugarNacimiento: response.fields.birthPlace ?? null,
                sexo: response.fields.sex ?? null,
              }
            : undefined,
        }))
      );
  }

  /**
   * Verify the identity of a person before completing registration.
   *
   * Behind the scenes: POST /registration/verify-identity using generated endpoint.
   * Response mapped from `ObjectOperationResult` → `VerifyIdentityResult`.
   */
  public verifyIdentity(
    payload: VerifyIdentityPayload,
    flowId: string
  ): Observable<VerifyIdentityResult> {
    return this.api
      .request(postRegistrationVerifyIdentityEndpoint, {
        body: {
          documentType: payload.tipoDocumento,
          documentNumber: payload.documento,
          firstSurname: payload.primerApellido,
          email: payload.mail,
        },
        headers: this.getFlowHeaders(flowId),
        withCredentials: true,
        captchaAction: 'VerifyIdentity',
      })
      .pipe(map(() => ({ success: true })));
  }

  /**
   * Initiate password recovery. Sends an email with a secure link if data matches.
   *
   * Behind the scenes: POST /auth/recover-password using generated endpoint.
   * Response is intentionally generic to avoid revealing whether the person exists.
   */
  public recoverPassword(payload: RecoverPasswordPayload): Observable<void> {
    return this.api
      .request(postAuthRecoverPasswordEndpoint, {
        body: {
          documentType: payload.tipoDocumento,
          documentNumber: payload.documento,
          firstSurname: payload.primerApellido,
        },
        withCredentials: true,
        captchaAction: 'RecoverPassword',
      })
      .pipe(map(() => undefined));
  }

  /**
   * Close the current session on the backend, clearing HttpOnly cookies.
   *
   * Behind the scenes: POST /auth/logout using generated endpoint.
   */
  public logout(): Observable<void> {
    return this.api
      .request(postAuthLogoutEndpoint, { withCredentials: true })
      .pipe(map(() => undefined));
  }

  /**
   * Refresh the access token using the HttpOnly refresh-token cookie.
   *
   * Behind the scenes: POST /auth/refresh-token using generated endpoint.
   * The frontend intentionally sends no body; the API validates the refresh
   * cookie and updates authentication cookies on success.
   */
  public refreshToken(): Observable<void> {
    return this.api
      .request(postAuthRefreshTokenEndpoint, {
        withCredentials: true,
        context: suppressGlobalErrorContext(),
      })
      .pipe(map(() => undefined));
  }

  /**
   * Verify the 6-digit two-factor code emailed to the user and complete authentication.
   *
   * Behind the scenes: POST /auth/verify-two-factor-code using generated endpoint.
   * Sets the secure HttpOnly cookies on success and returns persona data so the
   * caller can hydrate the local session.
   */
  public verifyTwoFactorCode(
    payload: VerifyTwoFactorCodePayload
  ): Observable<VerifyTwoFactorCodeResult> {
    return this.api
      .data(postAuthVerifyTwoFactorCodeEndpoint, {
        body: { sessionId: payload.sessionId, code: payload.codigo },
        withCredentials: true,
      })
      .pipe(
        map(response => ({
          documento: response.person?.documentNumber ?? '',
          primerNombre: response.person?.firstName ?? '',
        }))
      );
  }

  /**
   * Resend the two-factor code for an active 2FA session.
   *
   * Behind the scenes: POST /auth/resend-two-factor-code using generated endpoint.
   */
  public resendTwoFactorCode(
    payload: ResendTwoFactorCodePayload
  ): Observable<ResendTwoFactorCodeResult> {
    return this.api
      .data(postAuthResendTwoFactorCodeEndpoint, {
        body: payload,
        withCredentials: true,
        context: suppressGlobalErrorContext(),
      })
      .pipe(
        map(response => ({
          sessionId: response.sessionId ?? payload.sessionId,
          maskedEmail: response.maskedEmail ?? '',
          message: response.message ?? '',
        }))
      );
  }

  private getFlowHeaders(flowId: string): Record<string, string> {
    return { [AUTH_FLOW_ID_HEADER]: flowId.trim() };
  }

  private toRegisterPersonRequest(payload: RegisterPayload) {
    return {
      documentType: payload.tipoDocumento,
      documentNumber: payload.documento,
      firstName: payload.primerNombre,
      middleName: payload.segundoNombre,
      firstSurname: payload.primerApellido,
      secondSurname: payload.segundoApellido,
      birthDate: payload.fechaNacimiento,
      sex: payload.sexo,
      countryId: payload.codigoPais,
      stateId: payload.codigoEstado,
      cityId: payload.codigoCiudad,
      address: payload.direccion,
      primaryPhone: {
        nationalNumber: payload.telefono1.nationalNumber,
        iso2: payload.telefono1.iso2,
      },
      email: payload.mail,
      emailConfirmation: payload.verificacionMail,
    };
  }
}
