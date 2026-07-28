import { type EscenarioInscripcion, SECCIONES_ENCUESTA } from './inscription-flow';

describe('inscription flow model', () => {
  it('keeps the supported inscription scenarios explicit', () => {
    const scenarios: EscenarioInscripcion[] = ['primera-vez', 'parcial', 'encuesta-completa'];

    expect(scenarios).toEqual(['primera-vez', 'parcial', 'encuesta-completa']);
  });

  it('lists the initial survey sections without the work section', () => {
    expect(SECCIONES_ENCUESTA).toEqual([
      'educacion',
      'decision-academica',
      'experiencia-ort',
      'identidad',
      'reglamento',
    ]);
  });
});
