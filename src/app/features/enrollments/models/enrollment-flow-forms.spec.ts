import '@angular/compiler';

import { FormControl, FormGroup, Validators } from '@angular/forms';

import {
  createEnrollmentForms,
  createEnrollmentFormsState,
  disallowedHighSchoolYearForUniversity,
  NON_UNIVERSITY_HIGH_SCHOOL_YEAR_ERROR,
} from './enrollment-flow-forms';

describe('EnrollmentForms', () => {
  it('creates the complete personal-information form', () => {
    const forms = createEnrollmentForms();

    expect(Object.keys(forms.educationForm.controls)).toContain('higherEducationUniversities');
    expect(Object.keys(forms.educationForm.controls)).toContain('otherHigherEducationUniversity');
    expect(Object.keys(forms.educationForm.controls)).toContain('repeatsHighSchoolYear');
    expect(Object.keys(forms.educationForm.controls)).toContain('highSchoolYearRepeatCount');
    expect(Object.keys(forms.academicDecisionForm.controls)).toContain('researchedUniversities');
    expect(Object.keys(forms.academicDecisionForm.controls)).toContain('otherResearchedUniversity');
    expect(Object.keys(forms.ortExperienceForm.controls)).toContain('campusRating');
  });

  it('supports required validation for draft controls', () => {
    const forms = createEnrollmentForms();
    const control = forms.ortExperienceForm.controls.websiteRating;

    control.addValidators(Validators.required);
    control.updateValueAndValidity();

    expect(control.hasError('required')).toBe(true);
    control.setValue(3);
    expect(control.valid).toBe(true);
  });
});

describe('disallowedHighSchoolYearForUniversity', () => {
  it('rejects disallowed years when the degreeProgram is a university degreeProgram', () => {
    const validator = disallowedHighSchoolYearForUniversity(() => true);

    for (const value of [4, 10, '4', '10']) {
      const control = new FormControl(value);
      control.setValidators(validator);
      control.updateValueAndValidity();
      expect(control.hasError(NON_UNIVERSITY_HIGH_SCHOOL_YEAR_ERROR)).toBe(true);
    }
  });

  it('accepts allowed years when the degreeProgram is a university degreeProgram', () => {
    const validator = disallowedHighSchoolYearForUniversity(() => true);

    for (const value of [5, 6]) {
      const control = new FormControl(value);
      control.setValidators(validator);
      control.updateValueAndValidity();
      expect(control.valid).toBe(true);
    }
  });

  it('does not validate disallowed years for non-university degreePrograms', () => {
    const validator = disallowedHighSchoolYearForUniversity(() => false);
    const control = new FormControl(4);
    control.setValidators(validator);
    control.updateValueAndValidity();

    expect(control.valid).toBe(true);
  });

  it('ignores empty values', () => {
    const validator = disallowedHighSchoolYearForUniversity(() => true);

    for (const value of [null, '']) {
      const control = new FormControl(value);
      control.setValidators(validator);
      control.updateValueAndValidity();
      expect(control.valid).toBe(true);
    }
  });
});

describe('createEnrollmentFormsState', () => {
  it('builds the eight enrollment forms', () => {
    const { forms } = createEnrollmentFormsState();

    for (const form of Object.values(forms)) {
      expect(form).toBeInstanceOf(FormGroup);
    }
    expect(Object.keys(forms)).toHaveLength(8);
  });

  it('covers every survey section in the section config', () => {
    expect(Object.keys(createEnrollmentFormsState().sectionConfig)).toEqual([
      'education',
      'academic-decision',
      'ort-experience',
      'work-situation',
      'identity',
      'regulation',
    ]);
  });

  it('binds each section to its owning form', () => {
    const { forms, sectionConfig } = createEnrollmentFormsState();

    expect(sectionConfig.education.form).toBe(forms.educationForm);
    expect(sectionConfig['academic-decision'].form).toBe(forms.academicDecisionForm);
    expect(sectionConfig['ort-experience'].form).toBe(forms.ortExperienceForm);
    expect(sectionConfig['work-situation'].form).toBe(forms.workForm);
    expect(sectionConfig.identity.form).toBe(forms.identityForm);
    expect(sectionConfig.regulation.form).toBe(forms.regulationForm);
  });

  it('references only existing controls from the section error fields', () => {
    for (const [sectionId, section] of Object.entries(createEnrollmentFormsState().sectionConfig)) {
      for (const field of section.errorFields) {
        expect(
          section.form.get(field.controlName),
          `Control "${field.controlName}" referenced by section "${sectionId}" does not exist`
        ).not.toBeNull();
      }
    }
  });

  it('requires accepting the regulation with a true value', () => {
    const control = createEnrollmentFormsState().forms.regulationForm.controls.acceptsRegulation;

    expect(control.value).toBe(false);
    expect(control.hasError('required')).toBe(true);

    control.setValue(true);

    expect(control.valid).toBe(true);
  });

  it('requires the document expiration date', () => {
    const control = createEnrollmentFormsState().forms.identityForm.controls.documentExpiration;

    expect(control.hasError('required')).toBe(true);

    control.setValue(new Date('2030-01-01'));

    expect(control.valid).toBe(true);
  });
});
