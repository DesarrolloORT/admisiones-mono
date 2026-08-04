import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import type { InscripcionInitialSurveyResponse } from '../models/inscription-flow';
import { Inscripciones } from '../services/inscriptions';
import {
  InscripcionInitialSurveyResolved,
  inscriptionInitialSurveyResolver,
} from './inscription-initial-survey.resolver';

describe('inscriptionInitialSurveyResolver', () => {
  const emptySurveyFallback: InscripcionInitialSurveyResolved = {
    initialSurvey: {
      tieneDerechoEncuesta: true,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    },
    loadFailed: false,
  };

  let getInitialSurvey: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getInitialSurvey = vi.fn();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: Inscripciones, useValue: { getInitialSurvey } }],
    });
  });

  it('resolves the survey returned by the service', async () => {
    const initialSurvey = createSurveyResponse();
    getInitialSurvey.mockReturnValue(of(initialSurvey));

    await expect(resolve()).resolves.toEqual({ initialSurvey, loadFailed: false });
  });

  it('falls back to an empty survey when the service responds 404', async () => {
    getInitialSurvey.mockReturnValue(throwError(() => ({ status: 404 })));

    await expect(resolve()).resolves.toEqual(emptySurveyFallback);
  });

  it('treats any object carrying a 404 status as not found', async () => {
    getInitialSurvey.mockReturnValue(throwError(() => ({ status: 404 })));

    await expect(resolve()).resolves.toEqual(emptySurveyFallback);
  });

  it('flags the load as failed on non-404 http errors', async () => {
    getInitialSurvey.mockReturnValue(throwError(() => ({ status: 500 })));

    await expect(resolve()).resolves.toEqual({ initialSurvey: null, loadFailed: true });
  });

  it('flags the load as failed on errors without a status', async () => {
    getInitialSurvey.mockReturnValue(throwError(() => new Error('network down')));

    await expect(resolve()).resolves.toEqual({ initialSurvey: null, loadFailed: true });
  });

  function resolve(): Promise<InscripcionInitialSurveyResolved> {
    const result = TestBed.runInInjectionContext(() =>
      inscriptionInitialSurveyResolver(new ActivatedRouteSnapshot(), {} as RouterStateSnapshot)
    );
    return firstValueFrom(result as Observable<InscripcionInitialSurveyResolved>);
  }

  function createSurveyResponse(): InscripcionInitialSurveyResponse {
    return {
      tieneDerechoEncuesta: true,
      encuesta: null,
      universidadesConsideradas: [3],
      universidadesConsideradasOtros: ['UCU'],
      universidadesEducacionSuperior: [5],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [1, 2],
      opcionesPublicidadSeleccionadas: [4],
    };
  }
});
