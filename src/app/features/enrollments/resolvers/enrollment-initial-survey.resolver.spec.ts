import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import type { EnrollmentInitialSurveyResponse } from '../models/enrollment-flow';
import { Enrollments } from '../services/enrollments';
import {
  EnrollmentInitialSurveyResolved,
  enrollmentInitialSurveyResolver,
} from './enrollment-initial-survey.resolver';

describe('enrollmentInitialSurveyResolver', () => {
  const emptySurveyFallback: EnrollmentInitialSurveyResolved = {
    initialSurvey: {
      isEligibleForSurvey: true,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    },
    loadFailed: false,
  };

  let getInitialSurvey: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getInitialSurvey = vi.fn();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: Enrollments, useValue: { getInitialSurvey } }],
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

  function resolve(): Promise<EnrollmentInitialSurveyResolved> {
    const result = TestBed.runInInjectionContext(() =>
      enrollmentInitialSurveyResolver(new ActivatedRouteSnapshot(), {} as RouterStateSnapshot)
    );
    return firstValueFrom(result as Observable<EnrollmentInitialSurveyResolved>);
  }

  function createSurveyResponse(): EnrollmentInitialSurveyResponse {
    return {
      isEligibleForSurvey: true,
      survey: null,
      consideredUniversities: [3],
      otherConsideredUniversities: ['UCU'],
      higherEducationUniversities: [5],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [1, 2],
      selectedAdvertisingOptions: [4],
    };
  }
});
