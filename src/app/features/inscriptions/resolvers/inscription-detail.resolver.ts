import { inject } from '@angular/core';
import { ParamMap, ResolveFn } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import type { InscripcionEntryResolved } from '../models/inscription-entry';
import { Inscripciones } from '../services/inscriptions';

// Resuelve la INTENCIÓN de entrada al flujo a partir de la URL, no del estado del
// backend. Una inscripción "nueva" no lleva params; "retomar" llega con
// idProducto+idProceso desde el panel; "reactivar" (futuro botón de una
// inscripción cancelada) agrega `modo=reactivar`. Para retomar/reactivar se carga
// el detalle; si falla o los params no son válidos se degrada a "nueva".
export const inscriptionDetailResolver: ResolveFn<InscripcionEntryResolved> = route => {
  const intent = resolveEntryIntent(route.queryParamMap);
  if (intent === 'nueva') return of({ intent });

  const idProducto = toPositiveInteger(route.queryParamMap.get('idProducto'));
  const idProceso = toPositiveInteger(route.queryParamMap.get('idProceso'));

  return inject(Inscripciones)
    .getDetail(idProducto as number, idProceso as number)
    .pipe(
      map(detail => ({ intent, detail })),
      catchError(() => of({ intent, detail: null }))
    );
};

// La intención es explícita: se decide solo con params válidos + `modo`. Función pura para poder testearla sin tocar el resolver.
export function resolveEntryIntent(params: ParamMap): InscripcionEntryResolved['intent'] {
  const hasOffering =
    toPositiveInteger(params.get('idProducto')) !== null &&
    toPositiveInteger(params.get('idProceso')) !== null;
  if (!hasOffering) return 'nueva';
  return params.get('modo') === 'reactivar' ? 'reactivar' : 'retomar';
}

function toPositiveInteger(value: string | null): number | null {
  if (!value || !/^\d+$/.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : null;
}
