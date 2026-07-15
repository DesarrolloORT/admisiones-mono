import {
  EscenarioInscripcion,
  ResultadoPago,
  SeccionEncuestaId,
  SECCIONES_ENCUESTA,
  SECCIONES_ENCUESTA_COMPLETA,
} from './inscription-flow';

export function parseResultadoForzado(value: string | null): ResultadoPago | null {
  return value === 'en-proceso' ? 'en-proceso' : null;
}

export function getSeccionesVisibles(
  escenario: EscenarioInscripcion
): readonly SeccionEncuestaId[] {
  return escenario === 'encuesta-completa' ? SECCIONES_ENCUESTA_COMPLETA : SECCIONES_ENCUESTA;
}
