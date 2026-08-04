import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AccountChangePasswordPayload,
  AccountEndpoint,
  AccountPersonalData,
  AccountPhoneValidationPayload,
  UpdateAccountPersonalDataPayload,
} from '../endpoints/account.endpoint';

export type PersonalDataRecord = AccountPersonalData;
export type UpdatePersonalDataPayload = UpdateAccountPersonalDataPayload;
export type ChangePasswordPayload = AccountChangePasswordPayload;
export type PhoneValidationPayload = AccountPhoneValidationPayload;

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private readonly endpoint = inject(AccountEndpoint);

  public getPersonalData(): Observable<PersonalDataRecord> {
    return this.endpoint.getPersonalData();
  }

  public updatePersonalData(payload: UpdatePersonalDataPayload): Observable<boolean> {
    return this.endpoint.updatePersonalData(payload);
  }

  public changePassword(payload: ChangePasswordPayload): Observable<void> {
    return this.endpoint.changePassword(payload);
  }

  public validatePhone(payload: PhoneValidationPayload): Observable<boolean> {
    return this.endpoint.validatePhone(payload);
  }
}
