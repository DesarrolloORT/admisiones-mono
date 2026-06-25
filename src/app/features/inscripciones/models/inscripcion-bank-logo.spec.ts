import { resolveBankLogo, toBankOption } from './inscripcion-bank-logo';

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
});
