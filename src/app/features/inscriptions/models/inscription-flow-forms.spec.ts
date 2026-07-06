import '@angular/compiler';

import { Validators } from '@angular/forms';

import { createInscripcionForms } from './inscription-flow-forms';

describe('InscripcionForms', () => {
  it('creates the complete personal-information form', () => {
    const forms = createInscripcionForms();

    expect(Object.keys(forms.educationForm.controls)).toContain('universidadesEducacionSuperior');
    expect(Object.keys(forms.educationForm.controls)).toContain('universidadEducacionSuperiorOtro');
    expect(Object.keys(forms.educationForm.controls)).toContain('recursaAnioBachillerato');
    expect(Object.keys(forms.educationForm.controls)).toContain('vecesRecursaAnioBachillerato');
    expect(Object.keys(forms.academicDecisionForm.controls)).toContain('universidadesInformadas');
    expect(Object.keys(forms.academicDecisionForm.controls)).toContain('universidadInformadaOtro');
    expect(Object.keys(forms.ortExperienceForm.controls)).toContain('calificacionSede');
  });

  it('supports required validation for draft controls', () => {
    const forms = createInscripcionForms();
    const control = forms.ortExperienceForm.controls.calificacionWeb;

    control.addValidators(Validators.required);
    control.updateValueAndValidity();

    expect(control.hasError('required')).toBe(true);
    control.setValue(3);
    expect(control.valid).toBe(true);
  });
});
