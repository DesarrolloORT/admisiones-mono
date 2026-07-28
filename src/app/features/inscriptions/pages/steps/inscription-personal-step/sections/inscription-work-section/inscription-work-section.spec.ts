import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';
import { InscripcionWorkSection } from './inscription-work-section';

function createWorkForm() {
  return new FormGroup({
    isCorporate: new FormControl<boolean | null>(null, Validators.required),
  });
}

type WorkForm = ReturnType<typeof createWorkForm>;

describe('InscripcionWorkSection', () => {
  let fixture: ComponentFixture<InscripcionWorkSection>;
  let workForm: WorkForm;

  beforeEach(() => {
    workForm = createWorkForm();

    const facade = { workForm };

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

  it('renders only the corporate inscription control', () => {
    createFixture();

    const groups = fixture.nativeElement.querySelectorAll('ort-radio-group');
    const group = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="isCorporate"]'
    );
    const radios = group.querySelectorAll('input[type="radio"]') as NodeListOf<HTMLInputElement>;

    expect(groups).toHaveLength(1);
    expect(group.getAttribute('legend')).toBe('¿A título de quién deseás realizar la inscripción?');
    radios[0].click();
    expect(workForm.controls.isCorporate.value).toBe(false);
    radios[1].click();
    expect(workForm.controls.isCorporate.value).toBe(true);
  });

  it('shows the required error for isCorporate when touched and invalid', () => {
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
});
