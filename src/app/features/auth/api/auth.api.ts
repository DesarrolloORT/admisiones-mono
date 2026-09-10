import { inject, Injectable } from '@angular/core';
import { suppressGlobalErrorContext } from '@desarrolloort/ngx-utils';
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

export interface LoginPayload {
  documentType: string;
  documentNumber: string;
  password: string;
}

/** El backend responde 200 con la persona, o 202 cuando ya mando el codigo 2FA por mail. */
export type LoginResult =
  | {
      kind: 'authenticated';
      documentNumber: string;
      firstName: string;
    }
  | {
      kind: 'twoFactorRequired';
      sessionId: string;
      maskedEmail: string;
      message: string;
    };

export interface RegisterPayload {
  documentType: string;
  documentNumber: string;
  firstName: string;
  middleName?: string | null;
  firstSurname: string;
  secondSurname?: string | null;
  birthDate?: string;
  sex: string;
  countryCode?: number;
  stateCode?: number;
  cityCode?: number;
  address: string;
  primaryPhone: AuthPhoneNumber;
  email: string;
  emailConfirmation: string;
}

export type ConfirmApplicationRequestPayload = RegisterPayload;

export interface VerifyIdentityPayload {
  documentType: string;
  documentNumber: string;
  firstSurname: string;
  email: string;
}

/**
 * No expone `success`: el interceptor de `OperationResult` convierte cualquier
 * `success: false` en error HTTP, asi que un valor emitido siempre es un exito.
 * `mailSent: false` es exito parcial: el usuario quedo creado pero el correo de
 * activacion no salio y hay que ofrecer el recupero de contrasena.
 */
export interface VerifyIdentityResult {
  mailSent: boolean;
}

/**
 * `pendingReview` es la unica fuente de verdad de la pantalla final: `true`
 * significa solicitud de alta esperando revision manual, sin usuario ni correo.
 */
export interface RegisterResult {
  pendingReview: boolean;
  mailSent: boolean;
}

export interface EvaluateDocumentPayload {
  documentType: string;
  documentNumber: string;
}

export interface EvaluateDocumentResult {
  flowId: string | null;
  requiresPersonCreation: boolean;
  requiresApplicationCreation: boolean;
  requiresVerification: boolean;
  hasExistingApplication: boolean;
  userExists: boolean;
  message: string | null;
}

export interface ActivatePasswordLinkPayload {
  token: string;
}

export interface CompletePasswordPayload {
  newPassword: string;
}

export interface RecoverPasswordPayload {
  documentType: string;
  documentNumber: string;
  firstSurname: string;
}

export interface VerifyTwoFactorCodePayload {
  sessionId: string;
  code: string;
}

export interface ResendTwoFactorCodePayload {
  sessionId: string;
}

export interface ResendTwoFactorCodeResult {
  sessionId: string;
  maskedEmail: string;
  message: string;
}

export interface VerifyTwoFactorCodeResult {
  documentNumber: string;
  firstName: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthApi {
  private readonly api = inject(ApiHttpClient);

  public login(payload: LoginPayload): Observable<LoginResult> {
    // `LoginPayload` ya coincide en forma con el request generado: el tipado
    // explicito es la red de seguridad, no hace falta reescribir los campos.
    const body: GeneratedLoginPayload = payload;

    return this.api
      .data(postAuthLoginEndpoint, {
        body,
        withCredentials: true,
        captchaAction: 'login',
        context: suppressGlobalErrorContext(),
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
            documentNumber: response.person?.documentNumber ?? '',
            firstName: response.person?.firstName ?? '',
          } satisfies LoginResult;
        })
      );
  }

  /** Deja la cookie temporal de contrasena que despues consume `completePassword`. */
  public activatePasswordLink(payload: ActivatePasswordLinkPayload): Observable<void> {
    return this.api
      .request(postAuthActivatePasswordLinkEndpoint, {
        body: payload,
        withCredentials: true,
        showLoader: true,
      })
      .pipe(map(() => undefined));
  }

  /** Requiere la cookie temporal creada por `activatePasswordLink`; no manda el token. */
  public completePassword(payload: CompletePasswordPayload): Observable<void> {
    return this.api
      .request(postAuthCompleteInitialPasswordEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(map(() => undefined));
  }

  public register(payload: RegisterPayload, flowId: string): Observable<RegisterResult> {
    const body = this.toRegisterPersonRequest(payload);

    return this.api
      .request(postRegistrationConfirmNewPersonEndpoint, {
        body,
        headers: this.getFlowHeaders(flowId),
        withCredentials: true,
        captchaAction: 'ConfirmNewPerson',
      })
      .pipe(
        map(data => ({
          pendingReview: data.pendingReview ?? false,
          mailSent: data.mailSent ?? false,
        }))
      );
  }

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
      .pipe(
        map(data => ({
          pendingReview: data.pendingReview ?? false,
          mailSent: data.mailSent ?? false,
        }))
      );
  }

  public evaluateDocument(payload: EvaluateDocumentPayload): Observable<EvaluateDocumentResult> {
    return this.api
      .requestWithMessage(postRegistrationEvaluateDocumentEndpoint, {
        body: payload,
        withCredentials: true,
        captchaAction: 'EvaluateDocument',
      })
      .pipe(
        map(({ data, message }) => ({
          flowId: data.flowId ?? null,
          requiresPersonCreation: data.requiresPersonRegistration ?? false,
          requiresApplicationCreation: data.requiresRegistrationRequest ?? false,
          requiresVerification: data.requiresIdentityVerification ?? false,
          hasExistingApplication: data.registrationRequestPending ?? false,
          userExists: data.userAlreadyRegistered ?? false,
          message,
        }))
      );
  }

  public recognizeDocument(
    payload: DocumentRecognitionRequest
  ): Observable<DocumentRecognitionData> {
    return this.api
      .request(postRegistrationAnalyzeAttachmentEndpoint, {
        body: {
          mimeType: payload.mimeType,
          file: {
            fileName: payload.attachment.fileName,
            content: payload.attachment.content,
          },
        },
        withCredentials: true,
        captchaAction: 'AnalyzeAttachment',
      })
      .pipe(
        map(response => ({
          fields: response?.fields
            ? {
                documentType: response.fields.documentType ?? null,
                documentNumber: response.fields.documentNumber ?? null,
                firstName: response.fields.firstName ?? null,
                middleName: response.fields.middleName ?? null,
                firstSurname: response.fields.firstSurname ?? null,
                secondSurname: response.fields.secondSurname ?? null,
                birthDate: response.fields.birthDate ?? null,
                birthplace: response.fields.birthPlace ?? null,
                sex: response.fields.sex ?? null,
              }
            : undefined,
        }))
      );
  }

  public verifyIdentity(
    payload: VerifyIdentityPayload,
    flowId: string
  ): Observable<VerifyIdentityResult> {
    return this.api
      .request(postRegistrationVerifyIdentityEndpoint, {
        body: payload,
        headers: this.getFlowHeaders(flowId),
        withCredentials: true,
        captchaAction: 'VerifyIdentity',
      })
      .pipe(map(data => ({ mailSent: data.mailSent ?? false })));
  }

  /** La respuesta es generica a proposito: no debe revelar si la persona existe. */
  public recoverPassword(payload: RecoverPasswordPayload): Observable<void> {
    return this.api
      .request(postAuthRecoverPasswordEndpoint, {
        body: payload,
        withCredentials: true,
        captchaAction: 'RecoverPassword',
      })
      .pipe(map(() => undefined));
  }

  public logout(): Observable<void> {
    return this.api
      .request(postAuthLogoutEndpoint, { withCredentials: true })
      .pipe(map(() => undefined));
  }

  /** Sin body a proposito: el refresh token viaja en la cookie HttpOnly. */
  public refreshToken(): Observable<void> {
    return this.api
      .request(postAuthRefreshTokenEndpoint, {
        withCredentials: true,
        context: suppressGlobalErrorContext(),
      })
      .pipe(map(() => undefined));
  }

  public verifyTwoFactorCode(
    payload: VerifyTwoFactorCodePayload
  ): Observable<VerifyTwoFactorCodeResult> {
    return this.api
      .data(postAuthVerifyTwoFactorCodeEndpoint, {
        body: payload,
        withCredentials: true,
      })
      .pipe(
        map(response => ({
          documentNumber: response.person?.documentNumber ?? '',
          firstName: response.person?.firstName ?? '',
        }))
      );
  }

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
      documentType: payload.documentType,
      documentNumber: payload.documentNumber,
      firstName: payload.firstName,
      middleName: payload.middleName,
      firstSurname: payload.firstSurname,
      secondSurname: payload.secondSurname,
      birthDate: payload.birthDate,
      sex: payload.sex,
      countryId: payload.countryCode,
      stateId: payload.stateCode,
      cityId: payload.cityCode,
      address: payload.address,
      primaryPhone: {
        nationalNumber: payload.primaryPhone.nationalNumber,
        iso2: payload.primaryPhone.iso2,
      },
      email: payload.email,
      emailConfirmation: payload.emailConfirmation,
    };
  }
}
