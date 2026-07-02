import {
  FALLBACK_BANK_OPTIONS,
  resolveBankLogo,
  toBankOption,
  toBankOptions,
} from './inscripcion-bank-logo';

describe('inscripcion-bank-logo', () => {
  it('resolves a known bank by code', () => {
    expect(resolveBankLogo({ label: 'Banco BROU', code: 'brou' })).toBe('assets/banks/brou.svg');
  });

  it('resolves a known bank by normalized name when code is missing', () => {
    expect(resolveBankLogo({ label: 'Itaú', code: null })).toBe('assets/banks/itau.svg');
  });

  it('matches multi-token logos', () => {
    expect(resolveBankLogo({ label: 'Banco La Nación', code: null })).toBe(
      'assets/banks/la_nacion.svg'
    );
  });

  it('falls back to the default logo for unknown banks', () => {
    expect(resolveBankLogo({ label: 'Banco Inexistente', code: 'xyz' })).toBe(
      'assets/banks/default.svg'
    );
  });

  it('maps a catalog bank into a combo option with its logo', () => {
    expect(toBankOption({ id: 7, label: 'Santander', code: 'santander' })).toEqual({
      value: '7',
      label: 'Santander',
      icon: 'assets/banks/santander.svg',
    });
  });

  it('uses fallback bank options when the catalog has no usable banks', () => {
    expect(toBankOptions([])).toBe(FALLBACK_BANK_OPTIONS);
    expect(toBankOptions([{ id: 1, label: ' ', code: null }])).toBe(FALLBACK_BANK_OPTIONS);
    expect(FALLBACK_BANK_OPTIONS).toContainEqual({
      value: 'santander',
      label: 'Santander',
      icon: 'assets/banks/santander.svg',
    });
  });
});
