import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonaDatosPersonaEndpoint,
  postPersonaCambiarPasswordEndpoint,
  postPersonaValidarTelefonoEndpoint,
  putPersonaDatosPersonaEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';

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
  phone: string;
  email: string;
  emailVerification: string;
  identityRestricted: boolean;
}

export interface UpdateAccountPersonalDataPayload {
  countryCode?: number;
  stateCode?: number;
  cityCode?: number;
  address: string;
  phone: string;
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
    return this.api.request(getPersonaDatosPersonaEndpoint, { cache: false }).pipe(
      map(data => ({
        documentType: data.tipoDocumento ?? '',
        documentNumber: data.documento ?? '',
        firstName: data.primerNombre ?? '',
        secondName: data.segundoNombre ?? '',
        firstLastName: data.primerApellido ?? '',
        secondLastName: data.segundoApellido ?? '',
        birthDate: data.fechaNacimiento ?? '',
        sex: data.sexo ?? '',
        countryCode: data.codigoPais ?? null,
        stateCode: data.codigoEstado ?? null,
        cityCode: data.codigoCiudad ?? null,
        address: data.direccion ?? '',
        phone: data.telefono1 ?? '',
        email: data.mail ?? '',
        emailVerification: data.verificacionMail ?? data.mail ?? '',
        identityRestricted: data.identidadRestringida ?? false,
      }))
    );
  }

  public updatePersonalData(payload: UpdateAccountPersonalDataPayload): Observable<boolean> {
    return this.api
      .request(putPersonaDatosPersonaEndpoint, {
        body: {
          codigoPais: payload.countryCode,
          codigoEstado: payload.stateCode,
          codigoCiudad: payload.cityCode,
          direccion: payload.address,
          telefono1: payload.phone,
          mail: payload.email,
          verificacionMail: payload.emailVerification,
        },
      })
      .pipe(map(result => result === true));
  }

  public changePassword(payload: AccountChangePasswordPayload): Observable<void> {
    return this.api
      .request(postPersonaCambiarPasswordEndpoint, {
        body: {
          passwordActual: payload.currentPassword,
          passwordNueva: payload.password,
        },
      })
      .pipe(map(() => undefined));
  }

  public validatePhone(payload: AccountPhoneValidationPayload): Observable<boolean> {
    return this.api
      .request(postPersonaValidarTelefonoEndpoint, {
        queryParams: { telefono1: true },
        body: {
          telefonoE164: payload.numberE164,
          iso2: payload.iso2,
          caracteristicaPais: payload.countryPrefix ?? undefined,
          telefonoSimple: payload.number,
        },
      })
      .pipe(map(result => result === true));
  }
}
