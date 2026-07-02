import {
  findFirstIncompleteSection,
  getResultadoPago,
  getSeccionesVisibles,
  parseEscenario,
  parseResultadoForzado,
} from './inscription-flow-policy';

describe('inscription flow policy', () => {
  it('uses safe defaults for unknown query parameters', () => {
    expect(parseEscenario('desconocido')).toBe('primera-vez');
    expect(parseResultadoForzado('error')).toBeNull();
  });

  it('keeps the work-status section in first-time and partial surveys', () => {
    expect(getSeccionesVisibles('primera-vez')).toContain('situacion-laboral');
    expect(getSeccionesVisibles('parcial')).toContain('situacion-laboral');
  });

  it('limits a completed survey to identity and regulation', () => {
    expect(getSeccionesVisibles('encuesta-completa')).toEqual(['identidad', 'reglamento']);
  });

  it('finds the first incomplete visible section', () => {
    expect(
      findFirstIncompleteSection(['educacion', 'decision-academica', 'identidad'], ['educacion'])
    ).toBe('decision-academica');
  });

  it('maps payment methods and forced outcomes', () => {
    expect(getResultadoPago('abitab', null)).toBe('reservada');
    expect(getResultadoPago('tarjeta-credito', null)).toBe('confirmada');
    expect(getResultadoPago('tarjeta-credito', 'en-proceso')).toBe('en-proceso');
  });
});
