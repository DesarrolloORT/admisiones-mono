import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';
import { disallowedBachilleratoForUniversity } from '../../../../../models/inscription-flow-forms';
import { InscripcionEducationSection } from './inscription-education-section';

function createEducationForm(isUniversityCareer: () => boolean) {
  return new FormGroup({
    cursaSecundaria: new FormControl('', { nonNullable: true, validators: Validators.required }),
    anioSecundaria: new FormControl('', {
      nonNullable: true,
      validators: disallowedBachilleratoForUniversity(isUniversityCareer),
    }),
    orientacion: new FormControl('', { nonNullable: true }),
    recursaAnioBachillerato: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    vecesRecursaAnioBachillerato: new FormControl<number | null>(null),
    lugarSecundaria: new FormControl('', { nonNullable: true, validators: Validators.required }),
    departamento: new FormControl('', { nonNullable: true }),
    institucionEducativa: new FormControl('', { nonNullable: true }),
    estadoEducacionSuperior: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    universidadesEducacionSuperior: new FormControl<string[]>([], { nonNullable: true }),
    universidadEducacionSuperiorOtro: new FormControl('', { nonNullable: true }),
    formacionMadre: new FormControl('', { nonNullable: true, validators: Validators.required }),
    tituloOrtMadre: new FormControl('', { nonNullable: true }),
    formacionPadre: new FormControl('', { nonNullable: true, validators: Validators.required }),
    tituloOrtPadre: new FormControl('', { nonNullable: true }),
  });
}

type EducationForm = ReturnType<typeof createEducationForm>;

describe('InscripcionEducationSection', () => {
  let fixture: ComponentFixture<InscripcionEducationSection>;
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
      shouldAskBaccalaureateOrientation: () =>
        educationForm.controls.cursaSecundaria.value === 'cursando' &&
        !!educationForm.controls.anioSecundaria.value,
      shouldAskRecursaCount: () => educationForm.controls.recursaAnioBachillerato.value === 'si',
      isNationalSchoolPlace: () => educationForm.controls.lugarSecundaria.value === '1',
      isForeignSchoolPlace: () => educationForm.controls.lugarSecundaria.value === '2',
      shouldAskHigherEducationUniversities: () =>
        educationForm.controls.estadoEducacionSuperior.value === '1',
      shouldAskHigherEducationOtherUniversity: () =>
        educationForm.controls.estadoEducacionSuperior.value === '1' &&
        educationForm.controls.universidadesEducacionSuperior.value.includes('0'),
      shouldAskMotherOrtDegree: () => false,
      shouldAskFatherOrtDegree: () => false,
    };

    TestBed.configureTestingModule({
      imports: [InscripcionEducationSection],
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
        { provide: InscripcionSurveyFacade, useValue: facade },
      ],
    });
  });

  function createFixture(): void {
    fixture = TestBed.createComponent(InscripcionEducationSection);
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

    educationForm.controls.cursaSecundaria.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#school-status-error');
    expect(error).toBeTruthy();
    expect(error.getAttribute('role')).toBe('alert');
    expect(error.textContent).toContain('Seleccioná una opción');

    const fieldset = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="cursaSecundaria"] fieldset'
    );
    expect(fieldset.getAttribute('aria-describedby')).toContain('school-status-error');
  });

  it('shows the anioSecundaria block only while cursaSecundaria is "cursando"', () => {
    createFixture();

    expect(fixture.nativeElement.querySelector('[name="school-year"]')).toBeFalsy();

    educationForm.controls.cursaSecundaria.setValue('cursando');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[name="school-year"]')).toBeTruthy();
  });

  it('shows the bachilleratoNoUniversitario error with its exact message', () => {
    isUniversityCareer = true;
    createFixture();

    educationForm.controls.cursaSecundaria.setValue('cursando');
    educationForm.controls.anioSecundaria.setValue('4');
    educationForm.controls.anioSecundaria.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#school-year-university-error');
    expect(error).toBeTruthy();
    expect(error.textContent.trim()).toBe(
      'Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto año.'
    );
  });
});
