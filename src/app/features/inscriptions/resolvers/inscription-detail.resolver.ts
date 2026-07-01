import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import type { InscripcionDetail } from '../models/inscripcion-detail';
import { Inscripciones } from '../services/inscripciones';

// Resuelve el contexto de una inscripción retomada desde el panel para posicionar
// el flujo común en el paso correcto. Sin query params válidos o ante un error se
// resuelve a null: el flujo arranca normalmente desde la propuesta académica.
export const inscripcionDetailResolver: ResolveFn<InscripcionDetail | null> = route => {
  const idProducto = toPositiveInteger(route.queryParamMap.get('idProducto'));
  const idProceso = toPositiveInteger(route.queryParamMap.get('idProceso'));

  if (idProducto === null || idProceso === null) return of(null);

  return inject(Inscripciones)
    .getDetail(idProducto, idProceso)
    .pipe(catchError(() => of(null)));
};

function toPositiveInteger(value: string | null): number | null {
  if (!value || !/^\d+$/.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : null;
}
