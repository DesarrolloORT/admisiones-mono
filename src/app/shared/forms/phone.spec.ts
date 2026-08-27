import type { StoredPhoneNumber } from './phone';
import { PHONE_FALLBACK_ISO2, phoneMaxDigits, toBackendPhone, toPhoneInputValue } from './phone';

describe('phoneMaxDigits', () => {
  it('should cap uruguayan numbers at their fixed national length', () => {
    expect(phoneMaxDigits({ name: 'Uruguay', iso2: 'UY', prefix: 598 })).toBe(8);
  });

  it('should leave other countries with only the E.164 ceiling', () => {
    expect(phoneMaxDigits({ name: 'Estados Unidos', iso2: 'US', prefix: 1 })).toBe(14);
    expect(phoneMaxDigits({ name: 'Argentina', iso2: 'AR', prefix: 54 })).toBe(13);
  });

  it('should fall back to the full E.164 length when no country is selected', () => {
    expect(phoneMaxDigits(null)).toBe(15);
  });
});

describe('toBackendPhone', () => {
  it('should send only the fields the backend uses on save', () => {
    expect(
      toBackendPhone({ iso2: 'UY', number: ' 99333222 ', numberE164: '+59899333222' })
    ).toEqual({ nationalNumber: '99333222', iso2: 'UY' });
  });

  it('should map an empty country to null so the backend rejects it instead of guessing', () => {
    expect(toBackendPhone({ iso2: '', number: '+4915112345678', numberE164: '' })).toEqual({
      nationalNumber: '+4915112345678',
      iso2: null,
    });
  });

  it('should map a missing value to an empty required phone', () => {
    expect(toBackendPhone(null)).toEqual({ nationalNumber: '', iso2: null });
  });
});

describe('toPhoneInputValue', () => {
  /** Numero que el servidor no pudo desarmar: llega crudo, sin pais. */
  function unparsed(nationalNumber: string): StoredPhoneNumber {
    return { nationalNumber, iso2: null, e164: null, isValid: false };
  }

  it('should return null when there is no stored phone', () => {
    expect(toPhoneInputValue(null)).toBeNull();
    expect(toPhoneInputValue(unparsed('   '))).toBeNull();
  });

  it('should use the country the backend already resolved', () => {
    expect(
      toPhoneInputValue({
        nationalNumber: '99333222',
        iso2: 'UY',
        e164: '+59899333222',
        isValid: true,
      })
    ).toEqual({ iso2: 'UY', number: '99333222', numberE164: '+59899333222' });
  });

  it('should split an E.164 number the backend could not resolve', () => {
    expect(toPhoneInputValue(unparsed('+59899333222'))).toEqual({
      iso2: 'UY',
      number: '99333222',
      numberE164: '+59899333222',
    });
  });

  it('should resolve the longest matching prefix', () => {
    expect(toPhoneInputValue(unparsed('+5491122223333'))).toEqual({
      iso2: 'AR',
      number: '91122223333',
      numberE164: '+5491122223333',
    });
  });

  it('should treat 00 as an international prefix', () => {
    expect(toPhoneInputValue(unparsed('0059899333222'))).toEqual({
      iso2: 'UY',
      number: '99333222',
      numberE164: '+59899333222',
    });
  });

  it('should ignore spaces, dashes and parentheses', () => {
    expect(toPhoneInputValue(unparsed(' +598 99 333-222 '))).toEqual({
      iso2: 'UY',
      number: '99333222',
      numberE164: '+59899333222',
    });
  });

  it('should fall back to Uruguay for legacy numbers stored in local format', () => {
    expect(toPhoneInputValue(unparsed('99123456'))).toEqual({
      iso2: PHONE_FALLBACK_ISO2,
      number: '99123456',
      numberE164: '+59899123456',
    });
  });

  it('should drop the national trunk zero so the E.164 stays valid', () => {
    expect(toPhoneInputValue(unparsed('099333222'))).toEqual({
      iso2: PHONE_FALLBACK_ISO2,
      number: '99333222',
      numberE164: '+59899333222',
    });
  });

  it('should not trust the country when the backend flags the number as unresolved', () => {
    // iso2 informado pero isValid en false: se reprocesa en vez de darlo por bueno.
    expect(
      toPhoneInputValue({
        nationalNumber: '099333222',
        iso2: 'UY',
        e164: null,
        isValid: false,
      })
    ).toEqual({ iso2: 'UY', number: '99333222', numberE164: '+59899333222' });
  });

  it('should not relabel an international number whose prefix is unknown', () => {
    const value = toPhoneInputValue(unparsed('+9999123456'));

    expect(value?.iso2).toBe('');
    expect(value?.number).toBe('+9999123456');
    expect(toBackendPhone(value).iso2).toBeNull();
  });
});
