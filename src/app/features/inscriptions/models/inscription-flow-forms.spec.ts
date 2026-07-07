import '@angular/compiler';

import { FormControl, Validators } from '@angular/forms';

import {
  BACHILLERATO_NO_UNIVERSITARIO,
  createInscripcionForms,
  disallowedBachilleratoForUniversity,
} from './inscription-flow-forms';

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

describe('disallowedBachilleratoForUniversity', () => {
  it('rejects disallowed years when the career is a university career', () => {
    const validator = disallowedBachilleratoForUniversity(() => true);

    for (const value of [4, 10, '4', '10']) {
      const control = new FormControl(value);
      control.setValidators(validator);
      control.updateValueAndValidity();
      expect(control.hasError(BACHILLERATO_NO_UNIVERSITARIO)).toBe(true);
    }
  });

  it('accepts allowed years when the career is a university career', () => {
    const validator = disallowedBachilleratoForUniversity(() => true);

    for (const value of [5, 6]) {
      const control = new FormControl(value);
      control.setValidators(validator);
      control.updateValueAndValidity();
      expect(control.valid).toBe(true);
    }
  });

  it('does not validate disallowed years for non-university careers', () => {
    const validator = disallowedBachilleratoForUniversity(() => false);
    const control = new FormControl(4);
    control.setValidators(validator);
    control.updateValueAndValidity();

    expect(control.valid).toBe(true);
  });

  it('ignores empty values', () => {
    const validator = disallowedBachilleratoForUniversity(() => true);

    for (const value of [null, '']) {
      const control = new FormControl(value);
      control.setValidators(validator);
      control.updateValueAndValidity();
      expect(control.valid).toBe(true);
    }
  });
});
