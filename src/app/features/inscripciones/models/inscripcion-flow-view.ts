import type { InstruccionReserva, ItemResumenInscripcion } from './inscripcion-flow';
import {
  InscripcionPreEnrollmentResponse,
  METADATOS_PASOS_INSCRIPCION,
  MetodoPago,
  OpcionInscripcion,
  PantallaInscripcion,
  PasoInscripcion,
} from './inscripcion-flow';
import { getOptionLabel } from './inscripcion-flow-options';
import { RESERVATION_INSTRUCTIONS } from './inscripcion-static-data';

export function getStepNumber(screen: PantallaInscripcion): 1 | 2 | 3 {
  if (screen === 'propuesta') return 1;
  if (screen === 'encuesta' || screen === 'lector-reglamento') return 2;
  return 3;
}

export function getStepSupportLabel(number: 1 | 2 | 3): string {
  if (number === 1) return METADATOS_PASOS_INSCRIPCION.propuesta.supportLabel;
  if (number === 2) return METADATOS_PASOS_INSCRIPCION.encuesta.supportLabel;
  return METADATOS_PASOS_INSCRIPCION.pago.supportLabel;
}

export function buildStepperSteps(stepNumber: 1 | 2 | 3): PasoInscripcion[] {
  return [
    {
      id: 'propuesta',
      title: 'Propuesta académica',
      overline: 'Paso 1',
      status: stepNumber > 1 ? 'completo' : 'actual',
    },
    {
      id: 'encuesta',
      title: 'Información personal',
      overline: 'Paso 2',
      status: _setPersonalInformationStatus(stepNumber),
    },
    {
      id: 'pago',
      title: 'Confirmación',
      overline: 'Paso 3',
      status: stepNumber === 3 ? 'actual' : 'pendiente',
    },
  ];
}

export function buildSummaryItems(context: {
  response: InscripcionPreEnrollmentResponse | null;
  selectedCareer: string;
  selectedStart: string;
  selectedTurno: string;
  careerOptions: readonly OpcionInscripcion[];
  startOptions: readonly OpcionInscripcion[];
  turnoOptions: readonly OpcionInscripcion[];
}): ItemResumenInscripcion[] {
  return [
    {
      icon: 'school',
      label: 'Carrera',
      value:
        context.response?.resumen?.carrera ??
        getOptionLabel(context.careerOptions, context.selectedCareer, 'Sin seleccionar'),
    },
    {
      icon: 'calendar_today',
      label: 'Comienzo',
      value:
        context.response?.resumen?.comienzo ??
        getOptionLabel(context.startOptions, context.selectedStart, 'Sin seleccionar'),
    },
    {
      icon: 'schedule',
      label: 'Turno',
      value:
        context.response?.resumen?.turno ??
        getOptionLabel(context.turnoOptions, context.selectedTurno, 'Sin seleccionar'),
    },
  ];
}

export function formatInscriptionAmount(value: number | null | undefined): string {
  if (typeof value !== 'number' || !Number.isFinite(value)) return '$ 15.500';

  return `$ ${new Intl.NumberFormat('es-UY', { maximumFractionDigits: 0 }).format(value)}`;
}

export function formatPaymentDeadline(value: string | null | undefined): string {
  if (!value) return '04/03/2027';
  if (/^\d{2}\/\d{2}\/\d{4}$/.test(value)) return value;

  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  if (!year || !month || !day) return value;

  return `${day.toString().padStart(2, '0')}/${month.toString().padStart(2, '0')}/${year}`;
}

export function getReservationInstructions(method: MetodoPago | null): InstruccionReserva {
  return method === 'paganza' || method === 'banred' || method === 'abitab'
    ? RESERVATION_INSTRUCTIONS[method]
    : RESERVATION_INSTRUCTIONS.abitab;
}

function _setPersonalInformationStatus(stepNumber: 1 | 2 | 3): 'completo' | 'actual' | 'pendiente' {
  if (stepNumber === 1) return 'pendiente';
  if (stepNumber === 2) return 'actual';
  return 'completo';
}
