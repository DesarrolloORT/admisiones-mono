import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonDetailsEndpoint,
  postPersonChangePasswordEndpoint,
  postPersonValidatePhoneNumberEndpoint,
  putPersonDetailsEndpoint,
} from 'src/app/shared/api/generated/endpoints/person.endpoints';
import type { StoredPhoneNumber } from 'src/app/shared/forms/phone';

import type { AuthPhoneNumber } from '../models/auth.interface';

export interface AccountPersonalData {
  documentType: string;
  documentNumber: string;
  firstName: string;
  secondName: string;
  firstLastName: string;
  secondLastName: string;
  birthDate: string;
  sex: string;
  countryCode: number | null;
  stateCode: number | null;
  cityCode: number | null;
  address: string;
  phone: StoredPhoneNumber;
  email: string;
  emailVerification: string;
  identityRestricted: boolean;
}

export interface UpdateAccountPersonalDataPayload {
  countryCode?: number;
  stateCode?: number;
  cityCode?: number;
  address: string;
  phone: AuthPhoneNumber;
  email: string;
  emailVerification: string;
}

export interface AccountChangePasswordPayload {
  currentPassword: string;
  password: string;
}

export interface AccountPhoneValidationPayload {
  iso2: string | null;
  countryPrefix: number | null;
  number: string;
  numberE164: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class AccountEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getPersonalData(): Observable<AccountPersonalData> {
    return this.api.request(getPersonDetailsEndpoint).pipe(
      map(data => ({
        documentType: data.documentType ?? '',
        documentNumber: data.documentNumber ?? '',
        firstName: data.firstName ?? '',
        secondName: data.middleName ?? '',
        firstLastName: data.firstSurname ?? '',
        secondLastName: data.secondSurname ?? '',
        birthDate: data.birthDate ?? '',
        sex: data.sex ?? '',
        countryCode: data.countryId ?? null,
        stateCode: data.stateId ?? null,
        cityCode: data.cityId ?? null,
        address: data.address ?? '',
        phone: {
          nationalNumber: data.primaryPhone?.nationalNumber ?? '',
          iso2: data.primaryPhone?.iso2 ?? null,
          e164: data.primaryPhone?.e164 ?? null,
          isValid: data.primaryPhone?.isValid ?? false,
        },
        email: data.email ?? '',
        emailVerification: data.emailConfirmation ?? data.email ?? '',
        identityRestricted: data.hasRestrictedIdentity ?? false,
      }))
    );
  }

  public updatePersonalData(payload: UpdateAccountPersonalDataPayload): Observable<boolean> {
    return this.api
      .request(putPersonDetailsEndpoint, {
        body: {
          countryId: payload.countryCode,
          stateId: payload.stateCode,
          cityId: payload.cityCode,
          address: payload.address,
          primaryPhone: {
            nationalNumber: payload.phone.nationalNumber,
            iso2: payload.phone.iso2,
          },
          email: payload.email,
          emailConfirmation: payload.emailVerification,
        },
      })
      .pipe(map(result => result === true));
  }

  public changePassword(payload: AccountChangePasswordPayload): Observable<void> {
    return this.api
      .request(postPersonChangePasswordEndpoint, {
        body: {
          currentPassword: payload.currentPassword,
          newPassword: payload.password,
        },
      })
      .pipe(map(() => undefined));
  }

  public validatePhone(payload: AccountPhoneValidationPayload): Observable<boolean> {
    return this.api
      .request(postPersonValidatePhoneNumberEndpoint, {
        queryParams: { isPrimaryPhone: true },
        body: {
          e164: payload.numberE164,
          iso2: payload.iso2,
          countryCode: payload.countryPrefix ?? undefined,
          nationalNumber: payload.number,
        },
      })
      .pipe(map(result => result === true));
  }
}
