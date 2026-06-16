import {
  EscenarioInscripcion,
  MetodoPago,
  PantallaInscripcion,
  ResultadoPago,
  SeccionEncuestaId,
  SECCIONES_ENCUESTA,
  SECCIONES_ENCUESTA_COMPLETA,
} from './inscripcion-flow';

const ESCENARIOS: readonly EscenarioInscripcion[] = ['primera-vez', 'parcial', 'encuesta-completa'];

const METODOS_RESERVA: readonly MetodoPago[] = ['abitab', 'paganza', 'banred'];

export function parseEscenario(value: string | null): EscenarioInscripcion {
  return ESCENARIOS.includes(value as EscenarioInscripcion)
    ? (value as EscenarioInscripcion)
    : 'primera-vez';
}

export function parseResultadoForzado(value: string | null): ResultadoPago | null {
  return value === 'en-proceso' ? 'en-proceso' : null;
}

export function getSeccionesVisibles(
  escenario: EscenarioInscripcion
): readonly SeccionEncuestaId[] {
  return escenario === 'encuesta-completa' ? SECCIONES_ENCUESTA_COMPLETA : SECCIONES_ENCUESTA;
}

export function findFirstIncompleteSection(
  sections: readonly SeccionEncuestaId[],
  completedSections: readonly SeccionEncuestaId[]
): SeccionEncuestaId {
  return sections.find(section => !completedSections.includes(section)) ?? sections.at(-1)!;
}

export function getPreviousScreen(
  screen: PantallaInscripcion,
  activeSection: SeccionEncuestaId,
  visibleSections: readonly SeccionEncuestaId[]
): PantallaInscripcion | null {
  if (screen === 'encuesta') {
    return visibleSections.indexOf(activeSection) === 0 ? 'propuesta' : 'encuesta';
  }

  if (screen === 'lector-reglamento') {
    return 'encuesta';
  }

  if (screen === 'pago' || screen === 'confirmacion-pago') {
    return 'encuesta';
  }

  return null;
}

export function getResultadoPago(
  method: MetodoPago,
  forcedResult: ResultadoPago | null
): ResultadoPago {
  if (forcedResult) {
    return forcedResult;
  }

  return METODOS_RESERVA.includes(method) ? 'reservada' : 'confirmada';
}
