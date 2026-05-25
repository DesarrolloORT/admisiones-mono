import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';

import {
  EvaluateDocumentResult,
  RegisterResult,
  VerifyIdentityResult,
} from '../endpoints/auth.endpoint';
import { AuthEndpoint } from '../endpoints/auth.endpoint';
import {
  buildAuthRegisterRequest,
  RegisterCareerSelection,
  toConfirmExistingPersonPayload,
  toRegisterPayload,
  toVerifyIdentityPayload,
  VerifyExistingPersonIdentityInput,
} from '../mappers/registration.mapper';
import { AuthIdentityData, AuthRegisterPersonalData } from '../models/auth.interface';
import { formatDocumentForBackend } from '../models/document-number';
import { RegisterContinuableFlowKind } from '../models/register-flow';

export interface ConfirmCareerInterestInput {
  flow: RegisterContinuableFlowKind;
  identity: AuthIdentityData;
  personal: AuthRegisterPersonalData | null;
  selection: RegisterCareerSelection;
}

@Injectable({
  providedIn: 'root',
})
export class RegistrationService {
  private readonly endpoint = inject(AuthEndpoint);

  public evaluateDocument(identity: AuthIdentityData): Observable<EvaluateDocumentResult> {
    return this.endpoint.evaluateDocument({
      tipoDocumento: identity.documentType,
      documento: formatDocumentForBackend(identity.documentType, identity.documentNumber),
    });
  }

  public verifyExistingPersonIdentity(
    input: VerifyExistingPersonIdentityInput
  ): Observable<VerifyIdentityResult> {
    return this.endpoint.verifyIdentity(toVerifyIdentityPayload(input));
  }

  public confirmCareerInterest(input: ConfirmCareerInterestInput): Observable<RegisterResult> {
    switch (input.flow) {
      case 'existing-person':
        return this.endpoint.confirmExistingPerson(
          toConfirmExistingPersonPayload(input.identity, input.selection)
        );
      case 'new-person':
      case 'new-application':
        return this.confirmFullRegistration(input);
    }
  }

  private confirmFullRegistration(input: ConfirmCareerInterestInput): Observable<RegisterResult> {
    if (!input.personal) {
      return throwError(() => new Error('Personal data is required for this registration flow.'));
    }

    const payload = toRegisterPayload(
      buildAuthRegisterRequest(input.identity, input.personal, input.selection)
    );

    return input.flow === 'new-person'
      ? this.endpoint.register(payload)
      : this.endpoint.confirmApplicationRequest(payload);
  }
}
