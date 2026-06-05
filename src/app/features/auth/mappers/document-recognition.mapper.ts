import { LocationCountry } from '../../catalogs/models/catalog.interface';
import { DocumentRecognitionFields } from '../models/document-recognition.interface';

export interface RecognizedIdentityPatch {
  documentType?: string;
  documentNumber?: string;
}

export interface RecognizedPersonalPatch {
  primerNombre?: string;
  segundoNombre?: string;
  primerApellido?: string;
  segundoApellido?: string;
  fechaNacimiento?: string;
  sexo?: string;
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
      documentType: getStringValue(fields.tipoDocumento),
      documentNumber: getStringValue(fields.numeroDocumento),
    }),
    personal: withoutEmptyValues({
      primerNombre: getStringValue(fields.primerNombre),
      segundoNombre: getStringValue(fields.segundoNombre),
      primerApellido: getStringValue(fields.primerApellido),
      segundoApellido: getStringValue(fields.segundoApellido),
      fechaNacimiento: toDateInputValue(getStringValue(fields.fechaNacimiento)),
      sexo: getStringValue(fields.sexo),
    }),
    countryCode: getCountryCodeFromBirthplace(fields.lugarNacimiento),
    birthplace: fields.lugarNacimiento,
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

  const country = locations.find(c => c.codigoPais === countryCode);
  const state = country?.estado?.find(s => s.nombre.toUpperCase() === departmentName);
  return state?.codigoEstado ?? null;
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

export function toDateInputValue(value: string | null): string | null {
  if (!value) {
    return null;
  }

  const match = /^(\d{4}-\d{2}-\d{2})/.exec(value);
  return match ? match[1] : null;
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

function withoutEmptyValues<T extends Record<string, string | null>>(
  value: T
): Partial<Record<keyof T, string>> {
  const result: Partial<Record<keyof T, string>> = {};

  (Object.keys(value) as Array<keyof T>).forEach(key => {
    const fieldValue = value[key];

    if (fieldValue) {
      result[key] = fieldValue;
    }
  });

  return result;
}
