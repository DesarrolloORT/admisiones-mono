import type { FormControl } from '@angular/forms';

import type { Career, Comienzo, Seminario, Turno } from './catalog.interface';

export interface AcademicProposalForm {
  tipoPropuesta: FormControl<string>;
  carrera: FormControl<string>;
  comienzo: FormControl<string>;
  turno: FormControl<string>;
  seminarios: FormControl<string[]>;
}

export interface AcademicProposalOption {
  value: string;
  label: string;
  school?: string;
  icon?: string;
  hint?: string;
  description?: string;
}

/**
 * Textos del selector académico según el tipo de propuesta. Actualización
 * profesional habla de "Programa" y "Seminario"; el resto conserva la
 * terminología de carreras.
 */
export interface AcademicProposalTerminology {
  careerLabel: string;
  careerErrorText: string;
  careerLoadingLabel: string;
  careerLoadingMessage: string;
  careerFallbackLabel: string;
  startLabel: string;
  startErrorText: string;
  startLoadingLabel: string;
  startNoun: string;
}

const DEFAULT_TERMINOLOGY: AcademicProposalTerminology = {
  careerLabel: 'Carrera',
  careerErrorText: 'Seleccioná una carrera',
  careerLoadingLabel: 'Cargando carreras',
  careerLoadingMessage: 'Estamos cargando las carreras.',
  careerFallbackLabel: 'la carrera seleccionada',
  startLabel: 'Comienzo',
  startErrorText: 'Seleccioná un comienzo',
  startLoadingLabel: 'Cargando comienzos',
  startNoun: 'los comienzos',
};

interface AcademicProposalType extends AcademicProposalOption {
  levelIds: readonly number[];
  terminology?: Partial<AcademicProposalTerminology>;
}

const ACADEMIC_PROPOSAL_TYPES: readonly AcademicProposalType[] = [
  {
    value: '1',
    label: 'Carrera universitaria',
    icon: 'school',
    hint: 'Formación de grado con enfoque práctico y salida laboral',
    levelIds: [1],
  },
  {
    value: '2',
    label: 'Tecnicatura',
    icon: 'list_alt',
    hint: 'Carreras cortas, prácticas y orientadas al mercado',
    levelIds: [2],
  },
  {
    value: '3',
    label: 'Actualización profesional',
    icon: 'how_to_reg',
    hint: 'Cursos cortos para actualizar habilidades',
    levelIds: [3, 4],
    terminology: {
      careerLabel: 'Programa',
      careerErrorText: 'Seleccioná un programa',
      careerLoadingLabel: 'Cargando programas',
      careerLoadingMessage: 'Estamos cargando los programas.',
      careerFallbackLabel: 'el programa seleccionado',
      startLabel: 'Seminario',
      startErrorText: 'Seleccioná al menos un seminario',
      startLoadingLabel: 'Cargando seminarios',
      startNoun: 'los seminarios',
    },
  },
];

const ACTUALIZACION_PROFESIONAL_TYPE = '3';

export function isProfessionalUpdateType(value: string): boolean {
  return value === ACTUALIZACION_PROFESIONAL_TYPE;
}

export function isProfessionalUpdateLevel(levelId: number | null | undefined): boolean {
  return (
    levelId !== null &&
    levelId !== undefined &&
    getAcademicProposalLevelIds(ACTUALIZACION_PROFESIONAL_TYPE).includes(levelId)
  );
}

export function getAcademicProposalTerminology(value: string): AcademicProposalTerminology {
  const overrides = ACADEMIC_PROPOSAL_TYPES.find(option => option.value === value)?.terminology;
  return overrides ? { ...DEFAULT_TERMINOLOGY, ...overrides } : DEFAULT_TERMINOLOGY;
}

export function getAvailableAcademicProposalTypes(
  careers: readonly Career[]
): readonly AcademicProposalOption[] {
  const availableLevelIds = new Set(careers.map(career => career.idNivelProducto));

  return ACADEMIC_PROPOSAL_TYPES.filter(option =>
    option.levelIds.some(levelId => availableLevelIds.has(levelId))
  ).map(({ value, label, icon, hint }) => ({ value, label, icon, hint }));
}

export function getAcademicCareerOptions(
  careers: readonly Career[],
  proposalType: string
): readonly AcademicProposalOption[] {
  const levelIds = getAcademicProposalLevelIds(proposalType);

  return careers
    .filter(career => levelIds.includes(career.idNivelProducto))
    .map(career => ({
      value: career.idProducto.toString(),
      label: career.nombreProducto,
      school: career.nombreEscuela,
    }));
}

export function getAcademicProposalLevelIds(value: string): readonly number[] {
  return ACADEMIC_PROPOSAL_TYPES.find(option => option.value === value)?.levelIds ?? [];
}

export function getAcademicProposalTypeByLevel(
  levelId: number
): AcademicProposalOption | undefined {
  return ACADEMIC_PROPOSAL_TYPES.find(option => option.levelIds.includes(levelId));
}

export function toAcademicStartOption(start: Comienzo): AcademicProposalOption {
  return { value: start.idProceso.toString(), label: start.nombreProceso };
}

export function toAcademicShiftOption(turno: Turno): AcademicProposalOption {
  return {
    value: turno.idOferta.toString(),
    label: turno.horarioReferencia
      ? `${turno.nombreTurno} (${turno.horarioReferencia})`
      : turno.nombreTurno,
  };
}

export function toAcademicSeminarOption(seminario: Seminario): AcademicProposalOption {
  return {
    value: seminario.idOferta.toString(),
    label: seminario.nombre,
    description: formatSeminarStartDate(seminario.fechaComienzo),
  };
}

// `fechaReferencia` llega como fecha ISO con hora fija en 00:00:00 (`2026-10-16T00:00:00`)
// o ya en dd/MM/yyyy. La opción muestra solo la fecha, sin la hora.
function formatSeminarStartDate(value: string | null): string | undefined {
  if (!value) return undefined;

  const isoDate = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (!isoDate) return value;

  const [, year, month, day] = isoDate;
  return `${day}/${month}/${year}`;
}
