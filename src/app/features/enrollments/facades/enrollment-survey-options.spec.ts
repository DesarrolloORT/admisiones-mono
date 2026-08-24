import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsApi } from '../../catalogs/api/catalogs.api';
import type {
  InitialSurveyCatalogs,
  LocationCountry,
} from '../../catalogs/models/catalog.interface';
import { createEnrollmentFormsState, ENROLLMENT_FORMS } from '../models/enrollment-flow-forms';
import { EnrollmentSurveyOptionsFacade } from './enrollment-survey-options';

describe('EnrollmentSurveyOptionsFacade', () => {
  const getInitialSurveyCatalogs = vi.fn();
  const getCountryLocations = vi.fn();
  const getInstitutions = vi.fn();
  const onOptionsChanged = vi.fn();
  let isSurveyStepActive: WritableSignal<boolean>;

  beforeEach(() => {
    isSurveyStepActive = signal(true);
    getInitialSurveyCatalogs.mockReset().mockReturnValue(of(emptyCatalogs()));
    getCountryLocations.mockReset().mockReturnValue(of([]));
    getInstitutions.mockReset().mockReturnValue(of([]));
    onOptionsChanged.mockReset();
  });

  it('maps the initial survey catalogs into options and notifies callbacks', () => {
    getInitialSurveyCatalogs.mockReturnValue(
      of({
        ...emptyCatalogs(),
        ortExperience: {
          ratings: [{ id: '1', label: 'Malo' }],
          ortAdvertisements: [{ id: 7, label: 'Redes' }],
        },
      })
    );

    const options = createFacade();

    expect(options.advertisingOptions()).toEqual([{ value: '7', label: 'Redes' }]);
    expect(options.ratingLabels()[1]).toBe('1 estrella: Malo');
    expect(options.initialized()).toBe(true);
    expect(options.loadingInitialSurveyCatalogs()).toBe(false);
    expect(options.catalogError()).toBeNull();
    expect(onOptionsChanged).toHaveBeenCalled();
  });

  it('reports the error and keeps empty options when the catalogs fail', () => {
    getInitialSurveyCatalogs.mockReturnValue(throwError(() => new Error('network error')));

    const options = createFacade();

    expect(options.catalogError()).toBe('No se pudieron cargar los catálogos de encuesta inicial.');
    expect(options.advertisingOptions()).toEqual([]);
    expect(options.initialized()).toBe(true);
  });

  it('loads Uruguay departments and the institutions of the selected department', () => {
    getCountryLocations.mockReturnValue(
      of([
        {
          countryCode: 1,
          name: 'Uruguay',
          states: [{ countryCode: 1, stateCode: 5, name: 'Montevideo' }],
        },
        {
          countryCode: 2,
          name: 'Argentina',
          states: [{ countryCode: 2, stateCode: 9, name: 'Buenos Aires' }],
        },
      ] satisfies LocationCountry[])
    );
    getInstitutions.mockReturnValue(of([{ id: 9, label: 'Liceo 1' }]));

    const options = createFacade();
    const educationForm = TestBed.inject(ENROLLMENT_FORMS).forms.educationForm;

    expect(options.departmentOptions()).toEqual([{ value: '5', label: 'Montevideo' }]);

    educationForm.controls.educationalInstitution.setValue('999');
    educationForm.controls.state.setValue('5');

    expect(getInstitutions).toHaveBeenCalledWith(1, 5);
    expect(options.institutionOptions()).toEqual([{ value: '9', label: 'Liceo 1' }]);
    expect(educationForm.controls.educationalInstitution.value).toBe('');
  });

  it('keeps the answered orientation while the high school years catalog has not loaded', () => {
    const options = createFacade();
    const educationForm = TestBed.inject(ENROLLMENT_FORMS).forms.educationForm;

    educationForm.controls.highSchoolYear.setValue('11', { emitEvent: false });
    educationForm.controls.orientation.setValue('12', { emitEvent: false });

    options.refreshOrientationOptions();

    expect(options.orientationOptions()).toEqual([]);
    expect(educationForm.controls.orientation.value).toBe('12');
  });

  it('clears an orientation that no longer exists once the catalog is loaded', () => {
    getInitialSurveyCatalogs.mockReturnValue(
      of({
        ...emptyCatalogs(),
        education: {
          ...emptyCatalogs().education,
          highSchoolYears: [
            { id: 11, label: 'Quinto', baccalaureates: [{ id: 12, label: 'C', orientation: 'C' }] },
          ],
        },
      })
    );

    const options = createFacade();
    const educationForm = TestBed.inject(ENROLLMENT_FORMS).forms.educationForm;

    educationForm.controls.highSchoolYear.setValue('11', { emitEvent: false });
    educationForm.controls.orientation.setValue('999', { emitEvent: false });

    options.refreshOrientationOptions();

    expect(options.orientationOptions()).toEqual([{ value: '12', label: 'C' }]);
    expect(educationForm.controls.orientation.value).toBe('');
  });

  it('seeds the answered institution with the name sent by the backend', () => {
    const options = createFacade();

    options.seedEducationalInstitution(500, 'Liceo Nº 1');

    expect(getInstitutions).not.toHaveBeenCalled();
    expect(options.institutionOptions()).toEqual([{ value: '500', label: 'Liceo Nº 1' }]);
  });

  it('resolves the answered institution from the country wide catalog when there is no name', () => {
    getInstitutions.mockReturnValue(
      of([
        { id: 9, label: 'Liceo 9' },
        { id: 500, label: 'Liceo Nº 1' },
      ])
    );

    const options = createFacade();

    options.seedEducationalInstitution(500, null);

    expect(getInstitutions).toHaveBeenCalledWith(1);
    expect(options.institutionOptions()).toEqual([{ value: '500', label: 'Liceo Nº 1' }]);
  });

  it('keeps the answered institution when its label cannot be resolved', () => {
    getInstitutions.mockReturnValue(throwError(() => new Error('network error')));

    const options = createFacade();
    const educationForm = TestBed.inject(ENROLLMENT_FORMS).forms.educationForm;
    educationForm.controls.educationalInstitution.setValue('500', { emitEvent: false });

    options.seedEducationalInstitution(500, null);

    expect(options.institutionOptions()).toEqual([]);
    expect(educationForm.controls.educationalInstitution.value).toBe('500');
  });

  it('does not query the survey catalogs until the survey step is active', () => {
    isSurveyStepActive.set(false);

    const options = createFacade();

    expect(getInitialSurveyCatalogs).not.toHaveBeenCalled();
    expect(getCountryLocations).not.toHaveBeenCalled();

    isSurveyStepActive.set(true);
    TestBed.tick();

    expect(getInitialSurveyCatalogs).toHaveBeenCalledOnce();
    expect(getCountryLocations).toHaveBeenCalledOnce();
    expect(options.initialized()).toBe(true);
  });

  it('queries the survey catalogs only once across repeated step activations', () => {
    const options = createFacade();

    expect(getInitialSurveyCatalogs).toHaveBeenCalledOnce();

    isSurveyStepActive.set(false);
    TestBed.tick();
    isSurveyStepActive.set(true);
    TestBed.tick();

    expect(getInitialSurveyCatalogs).toHaveBeenCalledOnce();
    expect(getCountryLocations).toHaveBeenCalledOnce();
    expect(options).toBeTruthy();
  });

  function createFacade(): EnrollmentSurveyOptionsFacade {
    TestBed.configureTestingModule({
      providers: [
        { provide: ENROLLMENT_FORMS, useFactory: createEnrollmentFormsState },
        EnrollmentSurveyOptionsFacade,
        {
          provide: CatalogsApi,
          useValue: { getInitialSurveyCatalogs, getCountryLocations, getInstitutions },
        },
      ],
    });
    const options = TestBed.inject(EnrollmentSurveyOptionsFacade);
    options.initialize({ isSurveyStepActive, onOptionsChanged });
    TestBed.tick();
    return options;
  }

  function emptyCatalogs(): InitialSurveyCatalogs {
    return {
      education: {
        lastSecondaryYearLocations: [],
        highSchoolYears: [],
        previousHigherEducationOptions: [],
        universities: [],
        guardianEducationLevels: [],
      },
      academicDecision: {
        upperSecondaryYears: [],
        decisionSupports: [],
        decisionLevels: [],
        universities: [],
        ortChoiceReasons: [],
      },
      ortExperience: { ratings: [], ortAdvertisements: [] },
    };
  }
});
