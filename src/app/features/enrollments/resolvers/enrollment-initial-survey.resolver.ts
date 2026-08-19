import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { EnrollmentsApi } from '../api/enrollments.api';
import {
  EMPTY_INITIAL_SURVEY_RESPONSE,
  type EnrollmentInitialSurveyResolved,
} from '../models/enrollment-entry';
import type { EnrollmentInitialSurveyResponse } from '../models/enrollment-flow';

export type { EnrollmentInitialSurveyResolved };

/**
 * Mapea la consulta de estado de encuesta a `EnrollmentInitialSurveyResolved`.
 * Compartido por el resolver y el retry manual (`ProcessFacade.retryInitialSurvey`)
 * para que ambos traten el 404 (persona sin encuesta) y el error de red igual.
 */
export function resolveInitialSurvey(
  source$: Observable<EnrollmentInitialSurveyResponse>
): Observable<EnrollmentInitialSurveyResolved> {
  return source$.pipe(
    map(initialSurvey => ({ initialSurvey, loadFailed: false })),
    catchError(error =>
      isNotFoundError(error)
        ? of({ initialSurvey: EMPTY_INITIAL_SURVEY_RESPONSE, loadFailed: false })
        : of({ initialSurvey: null, loadFailed: true })
    )
  );
}

export const enrollmentInitialSurveyResolver: ResolveFn<EnrollmentInitialSurveyResolved> = () =>
  resolveInitialSurvey(inject(EnrollmentsApi).getInitialSurvey());

export function isNotFoundError(error: unknown): error is { status: number } {
  return typeof error === 'object' && error !== null && 'status' in error && error.status === 404;
}
