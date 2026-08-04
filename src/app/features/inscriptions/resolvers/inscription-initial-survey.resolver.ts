import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import {
  EMPTY_INITIAL_SURVEY_RESPONSE,
  type InscripcionInitialSurveyResolved,
} from '../models/inscription-entry';
import type { InscripcionInitialSurveyResponse } from '../models/inscription-flow';
import { Inscripciones } from '../services/inscriptions';

export type { InscripcionInitialSurveyResolved };

/**
 * Mapea la consulta de estado de encuesta a `InscripcionInitialSurveyResolved`.
 * Compartido por el resolver y el retry manual (`ProcessFacade.retryInitialSurvey`)
 * para que ambos traten el 404 (persona sin encuesta) y el error de red igual.
 */
export function resolveInitialSurvey(
  source$: Observable<InscripcionInitialSurveyResponse>
): Observable<InscripcionInitialSurveyResolved> {
  return source$.pipe(
    map(initialSurvey => ({ initialSurvey, loadFailed: false })),
    catchError(error =>
      isNotFoundError(error)
        ? of({ initialSurvey: EMPTY_INITIAL_SURVEY_RESPONSE, loadFailed: false })
        : of({ initialSurvey: null, loadFailed: true })
    )
  );
}

export const inscriptionInitialSurveyResolver: ResolveFn<InscripcionInitialSurveyResolved> = () =>
  resolveInitialSurvey(inject(Inscripciones).getInitialSurvey());

export function isNotFoundError(error: unknown): error is { status: number } {
  return typeof error === 'object' && error !== null && 'status' in error && error.status === 404;
}
