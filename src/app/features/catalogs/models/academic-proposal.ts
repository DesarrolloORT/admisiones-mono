import type { FormControl } from '@angular/forms';

import type { DegreeProgram, Intake, Seminar, Shift } from './catalog.interface';

export interface AcademicProposalForm {
  proposalType: FormControl<string>;
  degreeProgram: FormControl<string>;
  intake: FormControl<string>;
  shift: FormControl<string>;
  seminars: FormControl<string[]>;
}

export interface AcademicProposalOption {
  value: string;
  label: string;
  school?: string;
  icon?: string;
  hint?: string;
  description?: string;
}

export const ACADEMIC_PROPOSAL_TYPE_IDS = [1, 2, 3] as const;
export type AcademicProposalTypeId = (typeof ACADEMIC_PROPOSAL_TYPE_IDS)[number];

/**
 * Textos del selector académico según el tipo de propuesta. Actualización
 * profesional habla de "Programa" y "Seminario"; el resto conserva la
 * terminología de carreras.
 */
export interface AcademicProposalTerminology {
  degreeProgramLabel: string;
  degreeProgramErrorText: string;
  degreeProgramLoadingLabel: string;
  degreeProgramLoadingMessage: string;
  degreeProgramFallbackLabel: string;
  startLabel: string;
  startErrorText: string;
  startLoadingLabel: string;
  startNoun: string;
}

const DEFAULT_TERMINOLOGY: AcademicProposalTerminology = {
  degreeProgramLabel: 'Carrera',
  degreeProgramErrorText: 'Seleccioná una carrera',
  degreeProgramLoadingLabel: 'Cargando carreras',
  degreeProgramLoadingMessage: 'Estamos cargando las carreras.',
  degreeProgramFallbackLabel: 'la carrera seleccionada',
  startLabel: 'Comienzo',
  startErrorText: 'Seleccioná un comienzo',
  startLoadingLabel: 'Cargando comienzos',
  startNoun: 'los comienzos',
};

interface AcademicProposalType extends AcademicProposalOption {
  id: AcademicProposalTypeId;
  levelIds: readonly number[];
  terminology?: Partial<AcademicProposalTerminology>;
}

const ACADEMIC_PROPOSAL_TYPES: readonly AcademicProposalType[] = [
  {
    id: 1,
    value: '1',
    label: 'Carrera universitaria',
    icon: 'school',
    hint: 'Formación de grado con enfoque práctico y salida laboral',
    levelIds: [1],
  },
  {
    id: 2,
    value: '2',
    label: 'Tecnicatura',
    icon: 'list_alt',
    hint: 'Carreras cortas, prácticas y orientadas al mercado',
    levelIds: [2],
  },
  {
    id: 3,
    value: '3',
    label: 'Actualización profesional',
    icon: 'how_to_reg',
    hint: 'Cursos cortos para actualizar habilidades',
    levelIds: [3, 4],
    terminology: {
      degreeProgramLabel: 'Programa',
      degreeProgramErrorText: 'Seleccioná un programa',
      degreeProgramLoadingLabel: 'Cargando programas',
      degreeProgramLoadingMessage: 'Estamos cargando los programas.',
      degreeProgramFallbackLabel: 'el programa seleccionado',
      startLabel: 'Seminario',
      startErrorText: 'Seleccioná al menos un seminario',
      startLoadingLabel: 'Cargando seminarios',
      startNoun: 'los seminarios',
    },
  },
];

const PROFESSIONAL_UPDATE_TYPE = '3';

export function isProfessionalUpdateType(value: string): boolean {
  return value === PROFESSIONAL_UPDATE_TYPE;
}

export function isProfessionalUpdateLevel(levelId: number | null | undefined): boolean {
  return (
    levelId !== null &&
    levelId !== undefined &&
    getAcademicProposalLevelIds(PROFESSIONAL_UPDATE_TYPE).includes(levelId)
  );
}

export function getAcademicProposalTerminology(value: string): AcademicProposalTerminology {
  const overrides = ACADEMIC_PROPOSAL_TYPES.find(option => option.value === value)?.terminology;
  return overrides ? { ...DEFAULT_TERMINOLOGY, ...overrides } : DEFAULT_TERMINOLOGY;
}

export function getAcademicProposalTypes(): readonly AcademicProposalOption[] {
  return ACADEMIC_PROPOSAL_TYPES.map(({ value, label, icon, hint }) => ({
    value,
    label,
    icon,
    hint,
  }));
}

export function getAcademicProposalTypeId(value: string): AcademicProposalTypeId | null {
  return ACADEMIC_PROPOSAL_TYPES.find(option => option.value === value)?.id ?? null;
}

export function getAcademicDegreeProgramOptions(
  degreePrograms: readonly DegreeProgram[],
  proposalType: string
): readonly AcademicProposalOption[] {
  const levelIds = getAcademicProposalLevelIds(proposalType);

  return degreePrograms
    .filter(degreeProgram => levelIds.includes(degreeProgram.productLevelId))
    .map(degreeProgram => ({
      value: degreeProgram.productId.toString(),
      label: degreeProgram.productName,
      school: degreeProgram.schoolName,
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

export function toAcademicIntakeOption(intake: Intake): AcademicProposalOption {
  return { value: intake.admissionProcessId.toString(), label: intake.admissionProcessName };
}

export function toAcademicShiftOption(shift: Shift): AcademicProposalOption {
  return {
    value: shift.offeringId.toString(),
    label: shift.referenceSchedule
      ? `${shift.shiftName} (${shift.referenceSchedule})`
      : shift.shiftName,
  };
}

export function toAcademicSeminarOption(seminar: Seminar): AcademicProposalOption {
  return {
    value: seminar.offeringId.toString(),
    label: seminar.name,
    description: formatSeminarStartDate(seminar.startDate),
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
