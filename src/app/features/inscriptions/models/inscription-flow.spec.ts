import { type EscenarioInscripcion, SECCIONES_ENCUESTA } from './inscription-flow';

describe('inscription flow model', () => {
  it('keeps the supported inscription scenarios explicit', () => {
    const scenarios: EscenarioInscripcion[] = ['primera-vez', 'parcial', 'encuesta-completa'];

    expect(scenarios).toEqual(['primera-vez', 'parcial', 'encuesta-completa']);
  });

  it('includes work status in the full initial survey flow', () => {
    expect(SECCIONES_ENCUESTA).toContain('situacion-laboral');
  });
});
