import {
  findCountryByIso2,
  getIso2Codes,
  isValidIso2Code,
  OrtPhoneInputValue,
} from '@desarrolloort/components';

/**
 * Campos de `PhoneNumber` que el backend usa al guardar. `e164`, `countryCode` e `isValid`
 * son informativos: el servidor los recalcula (telefono.contract.json).
 */
export interface PhoneNumberValue {
  nationalNumber: string;
  iso2: string | null;
}

export interface PhoneValidationValue {
  iso2: string | null;
  countryPrefix: number | null;
  number: string;
  numberE164: string | null;
}

/**
 * Telefono tal como lo devuelve el backend al leer la persona. Con `isValid` en false el
 * servidor no pudo desarmar el numero guardado (dato previo a la migracion, o una linea
 * fija) y solo manda `nationalNumber` crudo, sin pais.
 */
export interface StoredPhoneNumber {
  nationalNumber: string;
  iso2: string | null;
  e164: string | null;
  isValid: boolean;
}

/** Los telefonos guardados antes de la migracion no traen pais y casi todos son uruguayos. */
export const PHONE_FALLBACK_ISO2 = 'UY';

export function toBackendPhone(value: OrtPhoneInputValue | null): PhoneNumberValue {
  return {
    nationalNumber: value?.number.trim() ?? '',
    iso2: value?.iso2 || null,
  };
}

/** Convierte el telefono almacenado en el valor que espera `ort-phone-input`. */
export function toPhoneInputValue(stored: StoredPhoneNumber | null): OrtPhoneInputValue | null {
  const nationalNumber = stored?.nationalNumber?.trim() ?? '';

  if (!nationalNumber) {
    return null;
  }

  // El servidor ya lo desarmo: se usa tal cual, sin adivinar el pais.
  if (stored?.isValid && stored.iso2) {
    return {
      iso2: stored.iso2,
      number: nationalNumber,
      numberE164: stored.e164 ?? '',
    };
  }

  return fromStoredText(nationalNumber);
}

/**
 * Ultimo recurso para un numero que el servidor no pudo desarmar: viene crudo y sin pais,
 * en el formato en que quedo guardado antes de la migracion.
 */
function fromStoredText(stored: string): OrtPhoneInputValue | null {
  const trimmed = stored.trim();
  const digits = trimmed.replaceAll(/\D/g, '');

  if (!digits) {
    return null;
  }

  // '+' y '00' son prefijo internacional; el resto se asume local.
  if (trimmed.startsWith('+')) {
    return fromInternational(digits);
  }

  if (digits.startsWith('00')) {
    return fromInternational(digits.slice(2));
  }

  // El backend descarta el 0 de salida nacional: lo replicamos para no armar un E.164
  // invalido al validar contra el servidor.
  const nationalNumber = digits.replace(/^0/, '');
  const fallbackPrefix = findPhoneCountryByIso2(PHONE_FALLBACK_ISO2)?.prefix;

  return {
    iso2: PHONE_FALLBACK_ISO2,
    number: nationalNumber,
    numberE164: fallbackPrefix ? `+${fallbackPrefix}${nationalNumber}` : '',
  };
}

export function toPhoneValidationValue(value: OrtPhoneInputValue): PhoneValidationValue {
  const iso2 = value.iso2 || null;

  return {
    iso2,
    countryPrefix: findPhoneCountryByIso2(iso2)?.prefix ?? null,
    number: value.number.trim(),
    numberE164: value.numberE164?.trim() || null,
  };
}

function fromInternational(digits: string): OrtPhoneInputValue | null {
  if (!digits) {
    return null;
  }

  const country = findPhoneCountryByPrefix(digits);

  // Sin pais reconocido no inventamos uno: se manda el numero internacional tal cual, que
  // el contrato acepta con iso2 nulo.
  if (!country) {
    return { iso2: '', number: `+${digits}`, numberE164: `+${digits}` };
  }

  return {
    iso2: country.iso2,
    number: digits.slice(country.prefix.toString().length),
    numberE164: `+${digits}`,
  };
}

function findPhoneCountryByIso2(iso2: string | null) {
  return iso2 && isValidIso2Code(iso2) ? findCountryByIso2(iso2) : undefined;
}

function findPhoneCountryByPrefix(digits: string) {
  return getIso2Codes()
    .map(iso2 => findCountryByIso2(iso2))
    .filter(country => !!country)
    .sort((a, b) => b.prefix - a.prefix)
    .find(country => digits.startsWith(country.prefix.toString()));
}
