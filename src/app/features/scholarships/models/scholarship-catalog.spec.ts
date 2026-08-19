import { resolveScholarshipCatalogEntry } from './scholarship-catalog';

describe('resolveScholarshipCatalogEntry', () => {
  it('resolves both funds of Excelencia Académica to the same card', () => {
    // 33 (sin declaración jurada) y 57 (con) son dos ID_TIPO_BECA distintos que
    // el front muestra como una sola card.
    expect(resolveScholarshipCatalogEntry([33], 'Fondo de Excelencia Académica')).toEqual({
      kind: 'fexa',
      route: '/becas/fexa',
      requiresEnrollment: true,
    });
    expect(resolveScholarshipCatalogEntry([57], 'Fondo de Excelencia Académica')?.kind).toBe(
      'fexa'
    );
    expect(resolveScholarshipCatalogEntry([33, 57], 'Fondo de Excelencia Académica')?.kind).toBe(
      'fexa'
    );
  });

  it('prefers the type id over the name when both are known', () => {
    expect(resolveScholarshipCatalogEntry([33], 'Fondo de becas de reválidas')?.kind).toBe('fexa');
  });

  it('falls back to the name while the remaining type ids are unconfirmed', () => {
    expect(resolveScholarshipCatalogEntry([], 'Fondo de becas de reválidas')?.route).toBe(
      '/becas/fbr'
    );
    expect(resolveScholarshipCatalogEntry([], 'Fondo de becas concursables')?.route).toBe(
      '/becas/fbc'
    );
    expect(
      resolveScholarshipCatalogEntry([], 'Fondo de becas de capacitación laboral')?.route
    ).toBe('/becas/fcl');
  });

  it('ignores accents and casing in the name fallback', () => {
    expect(resolveScholarshipCatalogEntry([], 'BECAS DE REVALIDAS')?.kind).toBe('fbr');
  });

  it('marks only reválidas as available without a prior enrollment', () => {
    expect(resolveScholarshipCatalogEntry([], 'Becas de Reválidas')?.requiresEnrollment).toBe(
      false
    );
    expect(resolveScholarshipCatalogEntry([], 'Becas Concursables')?.requiresEnrollment).toBe(true);
  });

  it('returns null for a scholarship the front does not know', () => {
    expect(resolveScholarshipCatalogEntry([9999], 'Fondo nuevo sin pantalla')).toBeNull();
    expect(resolveScholarshipCatalogEntry([], '')).toBeNull();
  });
});
