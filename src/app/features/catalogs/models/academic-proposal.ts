import type { FormControl } from '@angular/forms';

import type { Career, Comienzo, Turno } from './catalog.interface';

export interface AcademicProposalForm {
  tipoPropuesta: FormControl<string>;
  carrera: FormControl<string>;
  comienzo: FormControl<string>;
  turno: FormControl<string>;
}

export interface AcademicProposalOption {
  value: string;
  label: string;
  school?: string;
  icon?: string;
  hint?: string;
}

interface AcademicProposalType extends AcademicProposalOption {
  levelIds: readonly number[];
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
  },
];

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
