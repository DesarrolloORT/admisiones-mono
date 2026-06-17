import type { PhoneInputValue } from '@desarrolloort/components';

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
  fechaNacimiento: string | Date | null;
  sexo: string;
  location: LocationValue;
  direccion: string;
  telefono1: PhoneInputValue | null;
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
    fechaNacimiento: toIsoDateOnly(value.fechaNacimiento),
    sexo: value.sexo,
    codigoPais: value.location.codigoPais,
    codigoEstado: value.location.codigoEstado,
    codigoCiudad: value.location.codigoCiudad,
    direccion: value.direccion,
    telefono1: toBackendPhone(value.telefono1),
    mail: value.mail,
    verificacionMail: value.verificacionMail,
  };
}

function toBackendPhone(value: PhoneInputValue | null): string {
  if (!value) {
    return '';
  }

  return (value.iso2 === 'UY' ? value.number : value.numberE164 || value.number).trim();
}

function toIsoDateOnly(value: string | Date | null): string {
  if (value instanceof Date) {
    return formatDateOnly(value);
  }

  if (!value) {
    return '';
  }

  const trimmed = value.trim();
  const isoMatch = /^(\d{4})-(\d{2})-(\d{2})/.exec(trimmed);
  if (isoMatch) {
    return isoMatch[0];
  }

  const displayMatch = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(trimmed);
  if (!displayMatch) {
    return trimmed;
  }

  const [, day, month, year] = displayMatch;
  return `${year}-${month}-${day}`;
}

function formatDateOnly(value: Date): string {
  const year = value.getFullYear();
  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
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
