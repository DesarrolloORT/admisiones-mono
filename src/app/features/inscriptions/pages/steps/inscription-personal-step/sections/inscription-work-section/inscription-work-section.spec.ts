import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';
import { InscripcionWorkSection } from './inscription-work-section';

function createWorkForm() {
  return new FormGroup({
    isCorporate: new FormControl<boolean | null>(null, Validators.required),
    situacionLaboral: new FormControl('', { nonNullable: true, validators: Validators.required }),
    tipoJornadaLaboral: new FormControl('', { nonNullable: true }),
  });
}

type WorkForm = ReturnType<typeof createWorkForm>;

describe('InscripcionWorkSection', () => {
  let fixture: ComponentFixture<InscripcionWorkSection>;
  let workForm: WorkForm;
  let professionalUpdate: boolean;

  beforeEach(() => {
    workForm = createWorkForm();
    professionalUpdate = false;

    const facade = {
      isProfessionalUpdate: () => professionalUpdate,
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
    expect(legends).not.toContain('¿A título de quién deseás realizar la inscripción?');
  });

  it('renders only the corporate inscription control for professional updates', () => {
    professionalUpdate = true;
    createFixture();

    const group = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="isCorporate"]'
    );
    const radios = group.querySelectorAll('input[type="radio"]') as NodeListOf<HTMLInputElement>;

    expect(group.getAttribute('legend')).toBe('¿A título de quién deseás realizar la inscripción?');
    radios[0].click();
    expect(workForm.controls.isCorporate.value).toBe(false);
    radios[1].click();
    expect(workForm.controls.isCorporate.value).toBe(true);
    expect(fixture.nativeElement.querySelector('[name="situacionLaboral"]')).toBeFalsy();
    expect(fixture.nativeElement.querySelector('[name="tipoJornadaLaboral"]')).toBeFalsy();
  });

  it('shows the required error for isCorporate when touched and invalid', () => {
    professionalUpdate = true;
    createFixture();

    workForm.controls.isCorporate.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#corporate-inscription-error');
    const fieldset = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="isCorporate"] fieldset'
    );
    expect(error.getAttribute('role')).toBe('alert');
    expect(error.textContent).toContain('Seleccioná una opción');
    expect(fieldset.getAttribute('aria-describedby')).toBe('corporate-inscription-error');
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
