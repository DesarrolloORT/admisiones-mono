import { LocationCountry } from '../../catalogs/models/catalog.interface';
import { DocumentRecognitionFields } from '../models/document-recognition.interface';

export interface RecognizedIdentityPatch {
  documentType?: string;
  documentNumber?: string;
}

export interface RecognizedPersonalPatch {
  firstName?: string;
  middleName?: string;
  firstSurname?: string;
  secondSurname?: string;
  birthDate?: Date;
  sex?: string;
}

export interface RecognizedFormPatch {
  identity: RecognizedIdentityPatch;
  personal: RecognizedPersonalPatch;
  countryCode: number | null;
  birthplace: string | null | undefined;
}

export function toRecognizedFormPatch(
  fields: DocumentRecognitionFields | undefined
): RecognizedFormPatch | null {
  if (!fields) {
    return null;
  }

  return {
    identity: withoutEmptyValues({
      documentType: getStringValue(fields.documentType),
      documentNumber: getStringValue(fields.documentNumber),
    }),
    personal: withoutEmptyValues({
      firstName: getStringValue(fields.firstName),
      middleName: getStringValue(fields.middleName),
      firstSurname: getStringValue(fields.firstSurname),
      secondSurname: getStringValue(fields.secondSurname),
      birthDate: toDateInputValue(getStringValue(fields.birthDate)),
      sex: getStringValue(fields.sex),
    }),
    countryCode: getCountryCodeFromBirthplace(fields.birthplace),
    birthplace: fields.birthplace,
  };
}

export function resolveStateCodeFromBirthplace(
  locations: LocationCountry[],
  countryCode: number,
  birthplace: string | null | undefined
): number | null {
  const departmentName = getBirthplaceDepartmentName(birthplace);

  if (!departmentName) {
    return null;
  }

  const country = locations.find(c => c.countryCode === countryCode);
  const state = country?.states?.find(s => s.name.toUpperCase() === departmentName);
  return state?.stateCode ?? null;
}

export function getCountryCodeFromBirthplace(birthplace: string | null | undefined): number | null {
  if (!birthplace) {
    return null;
  }

  const upper = birthplace.toUpperCase();

  if (upper.includes('ARG')) {
    return 9;
  }

  if (upper.includes('URY')) {
    return 1;
  }

  return null;
}

export function toDateInputValue(value: string | null): Date | null {
  if (!value) {
    return null;
  }

  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (!match) {
    return null;
  }

  const [, year, month, day] = match;
  return new Date(Number(year), Number(month) - 1, Number(day));
}

export function getStringValue(value: unknown): string | null {
  if (typeof value !== 'string') {
    return null;
  }

  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function getBirthplaceDepartmentName(birthplace: string | null | undefined): string | null {
  if (!birthplace) {
    return null;
  }

  const departmentName = birthplace.split('/')[0]?.trim().toUpperCase();
  return departmentName || null;
}

type NonNullPatch<T extends Record<string, unknown>> = Partial<{
  [K in keyof T]: Exclude<T[K], null>;
}>;

function withoutEmptyValues<T extends Record<string, unknown>>(value: T): NonNullPatch<T> {
  const result: NonNullPatch<T> = {};

  (Object.keys(value) as Array<keyof T>).forEach(key => {
    const fieldValue = value[key];

    if (fieldValue !== null && fieldValue !== '') {
      result[key] = fieldValue as Exclude<T[typeof key], null>;
    }
  });

  return result;
}
