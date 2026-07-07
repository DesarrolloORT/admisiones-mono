import { getSeccionesVisibles, parseResultadoForzado } from './inscription-flow-policy';

describe('inscription flow policy', () => {
  it('uses a safe default for unknown forced results', () => {
    expect(parseResultadoForzado('error')).toBeNull();
    expect(parseResultadoForzado('en-proceso')).toBe('en-proceso');
  });

  it('keeps the work-status section in first-time and partial surveys', () => {
    expect(getSeccionesVisibles('primera-vez')).toContain('situacion-laboral');
    expect(getSeccionesVisibles('parcial')).toContain('situacion-laboral');
  });

  it('limits a completed survey to identity and regulation', () => {
    expect(getSeccionesVisibles('encuesta-completa')).toEqual(['identidad', 'reglamento']);
  });
});
