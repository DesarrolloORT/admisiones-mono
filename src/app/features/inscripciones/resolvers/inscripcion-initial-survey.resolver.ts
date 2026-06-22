import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { Inscripciones } from '../services/inscripciones';

interface InscripcionInitialSurvey {
  tieneDerechoEncuesta?: boolean | null;
}

export interface InscripcionInitialSurveyResolved {
  initialSurvey: InscripcionInitialSurvey | null;
  loadFailed: boolean;
}

export const inscripcionInitialSurveyResolver: ResolveFn<InscripcionInitialSurveyResolved> = () =>
  inject(Inscripciones)
    .getInitialSurvey()
    .pipe(
      map(initialSurvey => ({ initialSurvey, loadFailed: false })),
      catchError(error =>
        isNotFoundError(error)
          ? of({ initialSurvey: { tieneDerechoEncuesta: true }, loadFailed: false })
          : of({ initialSurvey: null, loadFailed: true })
      )
    );

function isNotFoundError(error: unknown): error is { status: number } {
  return typeof error === 'object' && error !== null && 'status' in error && error.status === 404;
}
