import { FormGroup } from '@angular/forms';

import { EnrollmentFormsStore } from './enrollment-forms';

describe('EnrollmentFormsStore', () => {
  let store: EnrollmentFormsStore;

  beforeEach(() => {
    store = new EnrollmentFormsStore();
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
      'education',
      'academic-decision',
      'ort-experience',
      'work-situation',
      'identity',
      'regulation',
    ]);
  });

  it('binds each section to its owning form', () => {
    expect(store.sectionConfig.education.form).toBe(store.educationForm);
    expect(store.sectionConfig['academic-decision'].form).toBe(store.academicDecisionForm);
    expect(store.sectionConfig['ort-experience'].form).toBe(store.ortExperienceForm);
    expect(store.sectionConfig['work-situation'].form).toBe(store.workForm);
    expect(store.sectionConfig.identity.form).toBe(store.identityForm);
    expect(store.sectionConfig.regulation.form).toBe(store.regulationForm);
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
    const control = store.regulationForm.controls.acceptsRegulation;

    expect(control.value).toBe(false);
    expect(control.hasError('required')).toBe(true);

    control.setValue(true);

    expect(control.valid).toBe(true);
  });

  it('requires the document expiration date', () => {
    const control = store.identityForm.controls.documentExpiration;

    expect(control.hasError('required')).toBe(true);

    control.setValue(new Date('2030-01-01'));

    expect(control.valid).toBe(true);
  });
});
