import type { OrtPhoneInputValue } from '@desarrolloort/components';
import { toBackendPhone } from 'src/app/shared/forms/phone';

import { LocationValue } from '../../catalogs/models/location-value';
import type { RegisterPayload, VerifyIdentityPayload } from '../api/auth.api';
import {
  AuthIdentityData,
  AuthRegisterPersonalData,
  AuthRegisterRequest,
} from '../models/auth.interface';
import { formatDocumentForBackend } from '../models/document-number';

export interface RegisterPersonalFormValue {
  firstName: string;
  middleName: string;
  firstSurname: string;
  secondSurname: string;
  birthDate: string | Date | null;
  sex: string;
  location: LocationValue;
  address: string;
  primaryPhone: OrtPhoneInputValue | null;
  email: string;
  emailConfirmation: string;
}

export interface VerifyExistingPersonIdentityInput {
  identity: AuthIdentityData;
  firstSurname: string;
  email: string;
}

export function toAuthRegisterPersonalData(
  value: RegisterPersonalFormValue
): AuthRegisterPersonalData {
  return {
    firstName: value.firstName.trim(),
    middleName: value.middleName.trim(),
    firstSurname: value.firstSurname.trim(),
    secondSurname: value.secondSurname.trim(),
    birthDate: toIsoDateOnly(value.birthDate),
    sex: value.sex,
    countryCode: value.location.countryCode,
    stateCode: value.location.stateCode,
    cityCode: value.location.cityCode,
    address: value.address.trim(),
    primaryPhone: toBackendPhone(value.primaryPhone),
    email: value.email.trim().toLowerCase(),
    emailConfirmation: value.emailConfirmation.trim().toLowerCase(),
  };
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
    documentType: identity.documentType,
    documentNumber: formatDocumentForBackend(identity.documentType, identity.documentNumber),
    firstName: personal.firstName,
    middleName: personal.middleName || null,
    firstSurname: personal.firstSurname,
    secondSurname: personal.secondSurname || null,
    birthDate: personal.birthDate,
    sex: personal.sex,
    address: personal.address,
    primaryPhone: personal.primaryPhone,
    email: personal.email,
    emailConfirmation: personal.emailConfirmation,
    countryCode: personal.countryCode ?? undefined,
    stateCode: personal.stateCode ?? undefined,
    cityCode: personal.cityCode ?? undefined,
  };
}

export function toVerifyIdentityPayload(
  input: VerifyExistingPersonIdentityInput
): VerifyIdentityPayload {
  return {
    documentType: input.identity.documentType,
    documentNumber: formatDocumentForBackend(
      input.identity.documentType,
      input.identity.documentNumber
    ),
    firstSurname: input.firstSurname.trim(),
    email: input.email.trim().toLowerCase(),
  };
}
