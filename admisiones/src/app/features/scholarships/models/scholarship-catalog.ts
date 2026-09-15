/**
 * Lo que la API **no** sabe de una beca: a qué ruta lleva su card y si exige
 * tener la inscripción paga antes de postularse.
 *
 * `GET /scholarships/available` devuelve el texto (`name`, `description`,
 * `requiresTest`) y los `scholarshipTypeIds`. La navegación es conocimiento del
 * front — el backend no conoce las rutas de Angular — así que vive acá y se une
 * con la respuesta en la page. Si mañana cambia una URL, se toca este archivo y
 * nada más.
 */

/** Las cuatro becas comparten page de proceso; cambia el `kind` de la ruta. */
export type ScholarshipKind = 'fbr' | 'fexa' | 'fcl' | 'fbc';

export interface ScholarshipCatalogEntry {
  kind: ScholarshipKind;
  route: string;
  /** Si la beca exige tener la inscripción paga antes de poder postularse. */
  requiresEnrollment: boolean;
}

const CATALOG: Readonly<Record<ScholarshipKind, ScholarshipCatalogEntry>> = {
  fbr: { kind: 'fbr', route: '/becas/fbr', requiresEnrollment: true },
  fexa: { kind: 'fexa', route: '/becas/fexa', requiresEnrollment: true },
  fbc: { kind: 'fbc', route: '/becas/fbc', requiresEnrollment: true },
  fcl: { kind: 'fcl', route: '/becas/fcl', requiresEnrollment: true },
};

/**
 * Vía principal: `ID_TIPO_BECA` → beca del front.
 *
 * TODO(becas): faltan los `ID_TIPO_BECA` de `fbr`, `fbc` y `fcl` — pedírselos a
 * backend. Los únicos confirmados por `becas.contract.json` son los dos fondos
 * que el front muestra como una sola card de Excelencia Académica: 33 (sin
 * declaración jurada) y 57 (con).
 */
const KIND_BY_TYPE_ID: ReadonlyMap<number, ScholarshipKind> = new Map<number, ScholarshipKind>([
  [33, 'fexa'],
  [57, 'fexa'],
]);

/**
 * Vía de respaldo **temporal**, mientras falten los ids de arriba: se matchea
 * una palabra clave del `name` que devuelve la API. Es frágil a propósito — se
 * rompe si backend recorta un texto — y por eso se borra en cuanto
 * `KIND_BY_TYPE_ID` esté completo.
 */
const KIND_BY_NAME_KEYWORD: ReadonlyArray<readonly [string, ScholarshipKind]> = [
  ['revalida', 'fbr'],
  ['excelencia', 'fexa'],
  ['concursable', 'fbc'],
  ['capacitacion laboral', 'fcl'],
];

/** Minúsculas y sin tildes, para que el match por nombre no dependa del acento. */
function normalize(value: string): string {
  return value
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');
}

/**
 * Resuelve la ruta y el gating de una card. Devuelve `null` si la beca no se
 * reconoce: en ese caso la page la muestra igual (el texto es del backend) pero
 * sin acción, porque esconder un fondo vigente es peor que ofrecerlo sin botón.
 */
export function resolveScholarshipCatalogEntry(
  scholarshipTypeIds: readonly number[],
  name: string
): ScholarshipCatalogEntry | null {
  for (const typeId of scholarshipTypeIds) {
    const kind = KIND_BY_TYPE_ID.get(typeId);
    if (kind) return CATALOG[kind];
  }

  const normalized = normalize(name);
  const match = KIND_BY_NAME_KEYWORD.find(([keyword]) => normalized.includes(keyword));

  return match ? CATALOG[match[1]] : null;
}
