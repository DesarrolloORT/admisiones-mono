import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { EnrollmentSurveyFacade } from '../../../../../facades/enrollment-survey';
import { disallowedHighSchoolYearForUniversity } from '../../../../../models/enrollment-flow-forms';
import { EnrollmentEducationSection } from './enrollment-education-section';

function createEducationForm(isUniversityCareer: () => boolean) {
  return new FormGroup({
    studiesHighSchool: new FormControl('', { nonNullable: true, validators: Validators.required }),
    highSchoolYear: new FormControl('', {
      nonNullable: true,
      validators: disallowedHighSchoolYearForUniversity(isUniversityCareer),
    }),
    orientation: new FormControl('', { nonNullable: true }),
    repeatsHighSchoolYear: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    highSchoolYearRepeatCount: new FormControl<number | null>(null),
    highSchoolLocation: new FormControl('', { nonNullable: true, validators: Validators.required }),
    state: new FormControl('', { nonNullable: true }),
    educationalInstitution: new FormControl('', { nonNullable: true }),
    higherEducationStatus: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    higherEducationUniversities: new FormControl<string[]>([], { nonNullable: true }),
    otherHigherEducationUniversity: new FormControl('', { nonNullable: true }),
    motherEducation: new FormControl('', { nonNullable: true, validators: Validators.required }),
    motherOrtDegree: new FormControl('', { nonNullable: true }),
    fatherEducation: new FormControl('', { nonNullable: true, validators: Validators.required }),
    fatherOrtDegree: new FormControl('', { nonNullable: true }),
  });
}

type EducationForm = ReturnType<typeof createEducationForm>;

describe('EnrollmentEducationSection', () => {
  let fixture: ComponentFixture<EnrollmentEducationSection>;
  let educationForm: EducationForm;
  let isUniversityCareer: boolean;

  beforeEach(() => {
    isUniversityCareer = false;
    educationForm = createEducationForm(() => isUniversityCareer);

    const facade = {
      educationForm,
      options: {
        schoolYearOptions: () => [
          { value: '5', label: 'Quinto año' },
          { value: '6', label: 'Sexto año' },
        ],
        orientationOptions: () => [{ value: 'humanistico', label: 'Humanístico' }],
        previousCareerOptions: () => [{ value: '1', label: 'Sí' }],
        departmentOptions: () => [],
        institutionOptions: () => [],
        higherEducationUniversityOptions: () => [],
        educationLevelOptions: () => [{ value: '1', label: 'Primaria' }],
        loadingInitialSurveyCatalogs: () => false,
      },
      shouldAskHighSchoolOrientation: () =>
        educationForm.controls.studiesHighSchool.value === 'studying' &&
        !!educationForm.controls.highSchoolYear.value,
      shouldAskHighSchoolRepeatCount: () =>
        educationForm.controls.repeatsHighSchoolYear.value === 'yes',
      isNationalSchoolPlace: () => educationForm.controls.highSchoolLocation.value === '1',
      isForeignSchoolPlace: () => educationForm.controls.highSchoolLocation.value === '2',
      shouldAskHigherEducationUniversities: () =>
        educationForm.controls.higherEducationStatus.value === '1',
      shouldAskHigherEducationOtherUniversity: () =>
        educationForm.controls.higherEducationStatus.value === '1' &&
        educationForm.controls.higherEducationUniversities.value.includes('0'),
      shouldAskMotherOrtDegree: () => false,
      shouldAskFatherOrtDegree: () => false,
    };

    TestBed.configureTestingModule({
      imports: [EnrollmentEducationSection],
      providers: [
        {
          provide: BreakpointService,
          useValue: {
            breakpoint: signal({
              isXSmall: true,
              isSmall: false,
              isMedium: false,
              isLarge: false,
              currentBreakpoint: 'xs',
              screenWidth: 375,
            }),
          },
        },
        { provide: EnrollmentSurveyFacade, useValue: facade },
      ],
    });
  });

  function createFixture(): void {
    fixture = TestBed.createComponent(EnrollmentEducationSection);
    fixture.componentRef.setInput('orientation', 'vertical');
    fixture.detectChanges();
  }

  it('renders the main education controls with their legends', () => {
    createFixture();

    const legends = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-group')).map(
      element => (element as Element).getAttribute('legend')
    );

    expect(legends).toContain('¿Cursás secundaria actualmente?');
    expect(legends).toContain('¿Recursaste algún año de bachillerato?');
    expect(legends).toContain('¿Dónde cursaste el último año de secundaria?');
    expect(legends).toContain('¿Cursaste carreras previamente?');
  });

  it('shows the required error for cursaSecundaria when touched and invalid', () => {
    createFixture();

    educationForm.controls.studiesHighSchool.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#school-status-error');
    expect(error).toBeTruthy();
    expect(error.getAttribute('role')).toBe('alert');
    expect(error.textContent).toContain('Seleccioná una opción');

    const fieldset = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="studiesHighSchool"] fieldset'
    );
    expect(fieldset.getAttribute('aria-describedby')).toContain('school-status-error');
  });

  it('shows the anioSecundaria block only while cursaSecundaria is "cursando"', () => {
    createFixture();

    expect(fixture.nativeElement.querySelector('[name="school-year"]')).toBeFalsy();

    educationForm.controls.studiesHighSchool.setValue('studying');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[name="school-year"]')).toBeTruthy();
  });

  it('shows the bachilleratoNoUniversitario error with its exact message', () => {
    isUniversityCareer = true;
    createFixture();

    educationForm.controls.studiesHighSchool.setValue('studying');
    educationForm.controls.highSchoolYear.setValue('4');
    educationForm.controls.highSchoolYear.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#school-year-university-error');
    expect(error).toBeTruthy();
    expect(error.textContent.trim()).toBe(
      'Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto año.'
    );
  });
});
