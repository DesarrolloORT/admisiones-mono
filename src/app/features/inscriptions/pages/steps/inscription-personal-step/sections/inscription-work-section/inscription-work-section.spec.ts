import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';
import { InscripcionWorkSection } from './inscription-work-section';

function createWorkForm() {
  return new FormGroup({
    situacionLaboral: new FormControl('', { nonNullable: true, validators: Validators.required }),
    tipoJornadaLaboral: new FormControl('', { nonNullable: true }),
  });
}

type WorkForm = ReturnType<typeof createWorkForm>;

describe('InscripcionWorkSection', () => {
  let fixture: ComponentFixture<InscripcionWorkSection>;
  let workForm: WorkForm;

  beforeEach(() => {
    workForm = createWorkForm();

    const facade = {
      workForm,
      options: {
        workScheduleOptions: () => [{ value: '1', label: 'Tiempo completo' }],
      },
    };

    TestBed.configureTestingModule({
      imports: [InscripcionWorkSection],
      providers: [{ provide: InscripcionSurveyFacade, useValue: facade }],
    });
  });

  function createFixture(): void {
    fixture = TestBed.createComponent(InscripcionWorkSection);
    fixture.componentRef.setInput('orientation', 'vertical');
    fixture.detectChanges();
  }

  it('renders the work status control with its legend', () => {
    createFixture();

    const legends = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-group')).map(
      element => (element as Element).getAttribute('legend')
    );

    expect(legends).toContain('¿Trabajás actualmente?');
  });

  it('shows the required error for situacionLaboral when touched and invalid', () => {
    createFixture();

    workForm.controls.situacionLaboral.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#work-status-error');
    expect(error).toBeTruthy();
    expect(error.getAttribute('role')).toBe('alert');
    expect(error.textContent).toContain('Seleccioná una opción');

    const fieldset = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="situacionLaboral"] fieldset'
    );
    expect(fieldset.getAttribute('aria-describedby')).toBe('work-status-error');
  });

  it('shows the work schedule control only when currently working', () => {
    createFixture();

    expect(fixture.nativeElement.querySelector('[name="tipoJornadaLaboral"]')).toBeFalsy();

    workForm.controls.situacionLaboral.setValue('trabaja');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[name="tipoJornadaLaboral"]')).toBeTruthy();
  });

  it('shows the required error for tipoJornadaLaboral when touched and invalid', () => {
    createFixture();

    workForm.controls.situacionLaboral.setValue('trabaja');
    workForm.controls.tipoJornadaLaboral.setValidators(Validators.required);
    workForm.controls.tipoJornadaLaboral.updateValueAndValidity();
    workForm.controls.tipoJornadaLaboral.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#work-schedule-error');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Seleccioná una opción');
  });
});
