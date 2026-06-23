import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonaDatosPersonaEndpoint,
  postPersonaCambiarContrasenaEndpoint,
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
  countryCode: number | null;
  stateCode: number | null;
  cityCode: number | null;
  address: string;
  phone: string;
  email: string;
  emailVerification: string;
}

export interface AccountChangePasswordPayload {
  currentPassword: string;
  password: string;
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
          codigoPais: this.toOptionalNumber(payload.countryCode),
          codigoEstado: this.toOptionalNumber(payload.stateCode),
          codigoCiudad: this.toOptionalNumber(payload.cityCode),
          direccion: payload.address.trim(),
          telefono1: payload.phone.trim(),
          mail: payload.email.trim(),
          verificacionMail: payload.emailVerification.trim(),
        },
      })
      .pipe(map(result => result === true));
  }

  public changePassword(payload: AccountChangePasswordPayload): Observable<unknown> {
    return this.api.request(postPersonaCambiarContrasenaEndpoint, {
      body: {
        passwordActual: payload.currentPassword,
        passwordNueva: payload.password,
      },
    });
  }

  private toOptionalNumber(value: number | null): number | undefined {
    return value ?? undefined;
  }
}
