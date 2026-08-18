import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  EvaluateDocumentResult,
  RegisterResult,
  VerifyIdentityResult,
} from '../endpoints/auth.endpoint';
import { AuthEndpoint } from '../endpoints/auth.endpoint';
import {
  toRegisterPayload,
  toVerifyIdentityPayload,
  VerifyExistingPersonIdentityInput,
} from '../mappers/registration.mapper';
import { AuthIdentityData, AuthRegisterPersonalData } from '../models/auth.interface';
import { formatDocumentForBackend } from '../models/document-number';
import { RegisterContinuableFlowKind } from '../models/register-flow';

type FullRegistrationFlow = Exclude<RegisterContinuableFlowKind, 'existing-person'>;

export interface ConfirmRegistrationInput {
  flow: FullRegistrationFlow;
  flowId: string;
  identity: AuthIdentityData;
  personal: AuthRegisterPersonalData;
}

export type VerifyExistingPersonIdentityFlowInput = VerifyExistingPersonIdentityInput & {
  flowId: string;
};

@Injectable({
  providedIn: 'root',
})
export class RegistrationService {
  private readonly endpoint = inject(AuthEndpoint);

  public evaluateDocument(identity: AuthIdentityData): Observable<EvaluateDocumentResult> {
    return this.endpoint.evaluateDocument({
      documentType: identity.documentType,
      documentNumber: formatDocumentForBackend(identity.documentType, identity.documentNumber),
    });
  }

  public verifyExistingPersonIdentity(
    input: VerifyExistingPersonIdentityFlowInput
  ): Observable<VerifyIdentityResult> {
    return this.endpoint.verifyIdentity(toVerifyIdentityPayload(input), input.flowId);
  }

  public confirmRegistration(input: ConfirmRegistrationInput): Observable<RegisterResult> {
    const payload = toRegisterPayload({
      identity: input.identity,
      personal: input.personal,
    });

    return input.flow === 'new-person'
      ? this.endpoint.register(payload, input.flowId)
      : this.endpoint.confirmApplicationRequest(payload, input.flowId);
  }
}
