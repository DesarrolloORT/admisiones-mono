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

import type {
  AccountChangePasswordPayload,
  AccountPersonalData,
  UpdateAccountPersonalDataPayload,
} from '../models/account.interface';
import type { AuthPhoneNumber } from '../models/auth.interface';

@Injectable({
  providedIn: 'root',
})
export class AccountApi {
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

  public validatePhone(payload: AuthPhoneNumber): Observable<boolean> {
    return this.api
      .request(postPersonValidatePhoneNumberEndpoint, {
        queryParams: { isPrimaryPhone: true },
        body: {
          nationalNumber: payload.nationalNumber,
          iso2: payload.iso2,
        },
      })
      .pipe(map(result => result === true));
  }
}
