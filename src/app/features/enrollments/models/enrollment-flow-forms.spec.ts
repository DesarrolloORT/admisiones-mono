import '@angular/compiler';

import { FormControl, Validators } from '@angular/forms';

import {
  createEnrollmentForms,
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
  it('rejects disallowed years when the career is a university career', () => {
    const validator = disallowedHighSchoolYearForUniversity(() => true);

    for (const value of [4, 10, '4', '10']) {
      const control = new FormControl(value);
      control.setValidators(validator);
      control.updateValueAndValidity();
      expect(control.hasError(NON_UNIVERSITY_HIGH_SCHOOL_YEAR_ERROR)).toBe(true);
    }
  });

  it('accepts allowed years when the career is a university career', () => {
    const validator = disallowedHighSchoolYearForUniversity(() => true);

    for (const value of [5, 6]) {
      const control = new FormControl(value);
      control.setValidators(validator);
      control.updateValueAndValidity();
      expect(control.valid).toBe(true);
    }
  });

  it('does not validate disallowed years for non-university careers', () => {
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
