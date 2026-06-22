import type { Career, CatalogItem, Comienzo, Turno } from '../../catalogs/models/catalog.interface';
import type { OpcionInscripcion } from './inscripcion-flow';

export interface ProposalOptionConfig extends OpcionInscripcion {
  levelIds: readonly number[];
}

export const PROPOSAL_OPTIONS: readonly ProposalOptionConfig[] = [
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

export function getAvailableProposalOptions(
  careers: readonly Career[]
): readonly OpcionInscripcion[] {
  const availableLevelIds = new Set(careers.map(career => career.idNivelProducto));

  return PROPOSAL_OPTIONS.filter(option =>
    option.levelIds.some(levelId => availableLevelIds.has(levelId))
  ).map(({ value, label, icon, hint }) => ({ value, label, icon, hint }));
}

export function getCareerOptions(
  careers: readonly Career[],
  proposalType: string
): readonly OpcionInscripcion[] {
  const proposalLevelIds = getProposalLevelIds(proposalType);

  return careers
    .filter(career => proposalLevelIds.includes(career.idNivelProducto))
    .map(career => ({
      value: career.idProducto.toString(),
      label: career.nombreProducto,
    }));
}

export function getProposalLevelIds(value: string): readonly number[] {
  return PROPOSAL_OPTIONS.find(option => option.value === value)?.levelIds ?? [];
}

export function getProposalOptionByLevel(levelId: number): ProposalOptionConfig | undefined {
  return PROPOSAL_OPTIONS.find(option => option.levelIds.includes(levelId));
}

export function toCatalogOptions(items: readonly CatalogItem[]): OpcionInscripcion[] {
  return items.map(item => ({ value: item.id.toString(), label: item.label }));
}

export function toStartOption(start: Comienzo): OpcionInscripcion {
  return { value: start.idProceso.toString(), label: start.nombreProceso };
}

export function toTurnoOption(turno: Turno): OpcionInscripcion {
  return {
    value: turno.idOferta.toString(),
    label: turno.horarioReferencia
      ? `${turno.nombreTurno} (${turno.horarioReferencia})`
      : turno.nombreTurno,
  };
}

export function getOptionLabel(
  options: readonly OpcionInscripcion[],
  value: string,
  fallback: string
): string {
  return options.find(option => option.value === value)?.label ?? fallback;
}
