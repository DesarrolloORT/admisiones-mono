import { inject } from '@angular/core';
import { ParamMap, ResolveFn } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionEntryResolved } from '../models/inscription-entry';
import { InscriptionResumeContextStore } from '../services/inscription-resume-context';
import { Inscripciones } from '../services/inscriptions';

// Producto+proceso identifican la inscripción en la URL. Al continuar desde el panel,
// las ofertas viajan en sessionStorage; los idOferta de URL quedan como compatibilidad.
export const inscriptionDetailResolver: ResolveFn<InscripcionEntryResolved> = route => {
  const intent = resolveEntryIntent(route.queryParamMap);
  if (intent === 'nueva') return of({ intent });

  const idProducto = toPositiveInteger(route.queryParamMap.get('idProducto')) as number;
  const idProceso = toPositiveInteger(route.queryParamMap.get('idProceso')) as number;
  const urlOffers = toPositiveIntegers(route.queryParamMap.getAll('idOferta'));
  const storedOffers = inject(InscriptionResumeContextStore).read(idProducto, idProceso)?.idOfertas;
  const idOfertas = urlOffers.length ? urlOffers : (storedOffers ?? []);

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
      idOfertas,
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

function toPositiveIntegers(values: string[]): number[] {
  return [
    ...new Set(
      values
        .map(value => toPositiveInteger(value))
        .filter((value): value is number => value !== null)
    ),
  ];
}
