import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';
import { InscripcionAcademicDecisionSection } from './inscription-academic-decision-section';

function createAcademicDecisionForm() {
  return new FormGroup({
    anioDecisionCarrera: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    apoyoDecision: new FormControl('', { nonNullable: true, validators: Validators.required }),
    anioDecisionOrt: new FormControl('', { nonNullable: true, validators: Validators.required }),
    otrasUniversidades: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    universidadesInformadas: new FormControl<string[]>([], { nonNullable: true }),
    universidadInformadaOtro: new FormControl('', { nonNullable: true }),
    certezaDecision: new FormControl('', { nonNullable: true, validators: Validators.required }),
    motivosOrt: new FormControl<string[]>([], {
      nonNullable: true,
      validators: Validators.required,
    }),
  });
}

type AcademicDecisionForm = ReturnType<typeof createAcademicDecisionForm>;

describe('InscripcionAcademicDecisionSection', () => {
  let fixture: ComponentFixture<InscripcionAcademicDecisionSection>;
  let academicDecisionForm: AcademicDecisionForm;

  beforeEach(() => {
    academicDecisionForm = createAcademicDecisionForm();

    const facade = {
      academicDecisionForm,
      options: {
        decisionYearOptions: () => [{ value: '1', label: 'Primero' }],
        supportOptions: () => [{ value: '1', label: 'Familia' }],
        universityOptions: () => [],
        motivesOptions: () => [{ value: '1', label: 'Calidad académica' }],
        loadingInitialSurveyCatalogs: () => false,
      },
      shouldAskInformedOtherUniversity: () =>
        academicDecisionForm.controls.otrasUniversidades.value === 'si' &&
        academicDecisionForm.controls.universidadesInformadas.value.includes('0'),
    };

    TestBed.configureTestingModule({
      imports: [InscripcionAcademicDecisionSection],
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
    fixture = TestBed.createComponent(InscripcionAcademicDecisionSection);
    fixture.componentRef.setInput('orientation', 'vertical');
    fixture.detectChanges();
  }

  it('renders the main academic decision controls with their legends', () => {
    createFixture();

    const legends = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-group')).map(
      element => (element as Element).getAttribute('legend')
    );

    expect(legends).toContain('¿En qué año de secundaria decidiste qué carrera estudiar?');
    expect(legends).toContain('¿En qué año decidiste estudiar en ORT?');
    expect(legends).toContain('¿Te informaste en otras universidades?');
    expect(legends).toContain('Grado de decisión al momento de inscribirte a la carrera');
  });

  it('shows the required error for otrasUniversidades when touched and invalid', () => {
    createFixture();

    academicDecisionForm.controls.otrasUniversidades.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#other-universities-error');
    expect(error).toBeTruthy();
    expect(error.getAttribute('role')).toBe('alert');
    expect(error.textContent).toContain('Seleccioná una opción');

    const fieldset = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="otrasUniversidades"] fieldset'
    );
    expect(fieldset.getAttribute('aria-describedby')).toContain('other-universities-error');
  });

  it('has no aria-describedby when the field is untouched', () => {
    createFixture();

    const fieldset = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="otrasUniversidades"] fieldset'
    );
    expect(fieldset.hasAttribute('aria-describedby')).toBe(false);
  });
});
