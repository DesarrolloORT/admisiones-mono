import { inject } from '@angular/core';
import { ParamMap, ResolveFn } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionEntryResolved } from '../models/inscription-entry';
import { Inscripciones } from '../services/inscriptions';

// Resuelve la INTENCIÓN de entrada al flujo a partir de la URL, no del estado del backend. Una inscripción "nueva" no lleva params; "retomar" llega con idProducto+idProceso desde el panel; "reactivar" agrega `modo=reactivar`. Los params se propagan resueltos: son la prueba de que la inscripción existe, así que si el Detalle falla el flujo igual retoma en el paso 2 con esa precarga mínima (solo params inválidos degradan a "nueva"). El nivel del producto (del catálogo de carreras) decide si aplica el flujo de Actualización profesional; si el catálogo falla queda `null`.
export const inscriptionDetailResolver: ResolveFn<InscripcionEntryResolved> = route => {
  const intent = resolveEntryIntent(route.queryParamMap);
  if (intent === 'nueva') return of({ intent });

  const idProducto = toPositiveInteger(route.queryParamMap.get('idProducto')) as number;
  const idProceso = toPositiveInteger(route.queryParamMap.get('idProceso')) as number;

  return forkJoin({
    detail: inject(Inscripciones)
      .getDetail(idProducto, idProceso)
      .pipe(catchError(() => of(null))),
    careers: inject(Catalogs)
      .getCareers()
      .pipe(catchError(() => of([]))),
  }).pipe(
    map(({ detail, careers }) => ({
      intent,
      detail,
      idProducto,
      idProceso,
      // El Detalle es la fuente preferida del producto; con el Detalle caído, el param.
      idNivelProducto:
        careers.find(career => career.idProducto === (detail?.detalle?.idProducto ?? idProducto))
          ?.idNivelProducto ?? null,
    }))
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
