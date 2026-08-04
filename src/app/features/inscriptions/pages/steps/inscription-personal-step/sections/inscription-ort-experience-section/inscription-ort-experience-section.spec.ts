import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';
import { InscripcionOrtExperienceSection } from './inscription-ort-experience-section';

function createOrtExperienceForm() {
  return new FormGroup({
    reunionAsesoramiento: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    calificacionAsesoramiento: new FormControl<number | null>(null),
    visitoWeb: new FormControl('', { nonNullable: true, validators: Validators.required }),
    calificacionWeb: new FormControl<number | null>(null),
    visitoSede: new FormControl('', { nonNullable: true, validators: Validators.required }),
    calificacionSede: new FormControl<number | null>(null),
    recuerdaPublicidad: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    mediosPublicidad: new FormControl<string[]>([], { nonNullable: true }),
  });
}

type OrtExperienceForm = ReturnType<typeof createOrtExperienceForm>;

describe('InscripcionOrtExperienceSection', () => {
  let fixture: ComponentFixture<InscripcionOrtExperienceSection>;
  let ortExperienceForm: OrtExperienceForm;

  beforeEach(() => {
    ortExperienceForm = createOrtExperienceForm();

    const facade = {
      ortExperienceForm,
      options: {
        ratingLabels: () => ({ 1: '1 estrella: Malo' }),
        advertisingOptions: () => [],
      },
    };

    TestBed.configureTestingModule({
      imports: [InscripcionOrtExperienceSection],
      providers: [{ provide: InscripcionSurveyFacade, useValue: facade }],
    });
  });

  function createFixture(): void {
    fixture = TestBed.createComponent(InscripcionOrtExperienceSection);
    fixture.componentRef.setInput('orientation', 'vertical');
    fixture.detectChanges();
  }

  it('renders the main experience controls with their legends', () => {
    createFixture();

    const legends = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-group')).map(
      element => (element as Element).getAttribute('legend')
    );

    expect(legends).toContain('¿Tuviste una reunión de asesoramiento?');
    expect(legends).toContain('¿Visitaste el sitio web de ORT?');
    expect(legends).toContain('¿Visitaste las instalaciones de ORT?');
    expect(legends).toContain('¿Recordás haber visto publicidad de ORT?');
  });

  it('shows the required error for reunionAsesoramiento when touched and invalid', () => {
    createFixture();

    ortExperienceForm.controls.reunionAsesoramiento.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#advice-meeting-error');
    expect(error).toBeTruthy();
    expect(error.getAttribute('role')).toBe('alert');
    expect(error.textContent).toContain('Seleccioná una opción');

    const fieldset = fixture.nativeElement.querySelector(
      'ort-radio-group[formcontrolname="reunionAsesoramiento"] fieldset'
    );
    expect(fieldset.getAttribute('aria-describedby')).toContain('advice-meeting-error');
  });

  it('shows the rating control only after answering "si" to the advice meeting question', () => {
    createFixture();

    expect(fixture.nativeElement.querySelector('[name="advice-rating"]')).toBeFalsy();

    ortExperienceForm.controls.reunionAsesoramiento.setValue('si');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[name="advice-rating"]')).toBeTruthy();
  });
});
