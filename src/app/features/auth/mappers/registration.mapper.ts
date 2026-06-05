import type { RegisterPayload, VerifyIdentityPayload } from '../endpoints/auth.endpoint';
import {
  AuthIdentityData,
  AuthRegisterPersonalData,
  AuthRegisterRequest,
} from '../models/auth.interface';
import { formatDocumentForBackend } from '../models/document-number';
import { LocationValue } from '../models/location-value';

export interface RegisterPersonalFormValue {
  primerNombre: string;
  segundoNombre: string;
  primerApellido: string;
  segundoApellido: string;
  fechaNacimiento: string;
  sexo: string;
  location: LocationValue;
  direccion: string;
  telefono1: string;
  mail: string;
  verificacionMail: string;
}

export interface VerifyExistingPersonIdentityInput {
  identity: AuthIdentityData;
  primerApellido: string;
  mail: string;
}

export function toAuthRegisterPersonalData(
  value: RegisterPersonalFormValue
): AuthRegisterPersonalData {
  return {
    primerNombre: value.primerNombre,
    segundoNombre: value.segundoNombre,
    primerApellido: value.primerApellido,
    segundoApellido: value.segundoApellido,
    fechaNacimiento: value.fechaNacimiento,
    sexo: value.sexo,
    codigoPais: value.location.codigoPais,
    codigoEstado: value.location.codigoEstado,
    codigoCiudad: value.location.codigoCiudad,
    direccion: value.direccion,
    telefono1: value.telefono1,
    mail: value.mail,
    verificacionMail: value.verificacionMail,
  };
}

export function buildAuthRegisterRequest(
  identity: AuthIdentityData,
  personal: AuthRegisterPersonalData
): AuthRegisterRequest {
  return {
    identity,
    personal,
  };
}

export function toRegisterPayload(payload: AuthRegisterRequest): RegisterPayload {
  const { identity, personal } = payload;

  return {
    tipoDocumento: identity.documentType,
    documento: formatDocumentForBackend(identity.documentType, identity.documentNumber),
    primerNombre: personal.primerNombre,
    segundoNombre: personal.segundoNombre || null,
    primerApellido: personal.primerApellido,
    segundoApellido: personal.segundoApellido || null,
    fechaNacimiento: personal.fechaNacimiento,
    sexo: personal.sexo,
    direccion: personal.direccion,
    telefono1: personal.telefono1,
    mail: personal.mail,
    verificacionMail: personal.verificacionMail,
    codigoPais: personal.codigoPais ?? undefined,
    codigoEstado: personal.codigoEstado ?? undefined,
    codigoCiudad: personal.codigoCiudad ?? undefined,
  };
}

export function toVerifyIdentityPayload(
  input: VerifyExistingPersonIdentityInput
): VerifyIdentityPayload {
  return {
    tipoDocumento: input.identity.documentType,
    documento: formatDocumentForBackend(input.identity.documentType, input.identity.documentNumber),
    primerApellido: input.primerApellido,
    mail: input.mail,
  };
}
