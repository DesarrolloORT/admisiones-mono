import type { CatalogItem } from '../../catalogs/models/catalog.interface';
import type { OpcionInscripcion } from './inscripcion-flow';

export function toCatalogOptions(items: readonly CatalogItem[]): OpcionInscripcion[] {
  return items.map(item => ({ value: item.id.toString(), label: item.label }));
}

export function getOptionLabel(
  options: readonly OpcionInscripcion[],
  value: string,
  fallback: string
): string {
  return options.find(option => option.value === value)?.label ?? fallback;
}
