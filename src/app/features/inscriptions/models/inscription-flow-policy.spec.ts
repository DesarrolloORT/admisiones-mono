import { getSeccionesVisibles, parseResultadoForzado } from './inscription-flow-policy';

describe('inscription flow policy', () => {
  it('uses a safe default for unknown forced results', () => {
    expect(parseResultadoForzado('error')).toBeNull();
    expect(parseResultadoForzado('en-proceso')).toBe('en-proceso');
  });

  it('does not show the work section outside professional update flows', () => {
    expect(getSeccionesVisibles('primera-vez')).not.toContain('situacion-laboral');
    expect(getSeccionesVisibles('parcial')).not.toContain('situacion-laboral');
  });

  it('limits a completed survey to identity and regulation', () => {
    expect(getSeccionesVisibles('encuesta-completa')).toEqual(['identidad', 'reglamento']);
  });

  it('reduces professional update flows to work status, identity and regulation', () => {
    expect(getSeccionesVisibles('primera-vez', true)).toEqual([
      'situacion-laboral',
      'identidad',
      'reglamento',
    ]);
    expect(getSeccionesVisibles('parcial', true)).toEqual([
      'situacion-laboral',
      'identidad',
      'reglamento',
    ]);
    expect(getSeccionesVisibles('encuesta-completa', true)).toEqual([
      'situacion-laboral',
      'identidad',
      'reglamento',
    ]);
  });
});
