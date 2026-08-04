import { FormGroup } from '@angular/forms';

import { InscripcionFormsStore } from './inscription-forms';

describe('InscripcionFormsStore', () => {
  let store: InscripcionFormsStore;

  beforeEach(() => {
    store = new InscripcionFormsStore();
  });

  it('builds the eight enrollment forms', () => {
    const forms = [
      store.academicForm,
      store.educationForm,
      store.academicDecisionForm,
      store.ortExperienceForm,
      store.workForm,
      store.identityForm,
      store.regulationForm,
      store.paymentForm,
    ];

    for (const form of forms) {
      expect(form).toBeInstanceOf(FormGroup);
    }
    expect(Object.keys(store.forms)).toHaveLength(forms.length);
  });

  it('covers every survey section in the section config', () => {
    expect(Object.keys(store.sectionConfig)).toEqual([
      'educacion',
      'decision-academica',
      'experiencia-ort',
      'situacion-laboral',
      'identidad',
      'reglamento',
    ]);
  });

  it('binds each section to its owning form', () => {
    expect(store.sectionConfig.educacion.form).toBe(store.educationForm);
    expect(store.sectionConfig['decision-academica'].form).toBe(store.academicDecisionForm);
    expect(store.sectionConfig['experiencia-ort'].form).toBe(store.ortExperienceForm);
    expect(store.sectionConfig['situacion-laboral'].form).toBe(store.workForm);
    expect(store.sectionConfig.identidad.form).toBe(store.identityForm);
    expect(store.sectionConfig.reglamento.form).toBe(store.regulationForm);
  });

  it('references only existing controls from the section error fields', () => {
    for (const [sectionId, section] of Object.entries(store.sectionConfig)) {
      for (const field of section.errorFields) {
        expect(
          section.form.get(field.controlName),
          `Control "${field.controlName}" referenced by section "${sectionId}" does not exist`
        ).not.toBeNull();
      }
    }
  });

  it('requires accepting the regulation with a true value', () => {
    const control = store.regulationForm.controls.aceptaReglamento;

    expect(control.value).toBe(false);
    expect(control.hasError('required')).toBe(true);

    control.setValue(true);

    expect(control.valid).toBe(true);
  });

  it('requires the document expiration date', () => {
    const control = store.identityForm.controls.vencimientoDocumento;

    expect(control.hasError('required')).toBe(true);

    control.setValue(new Date('2030-01-01'));

    expect(control.valid).toBe(true);
  });
});
