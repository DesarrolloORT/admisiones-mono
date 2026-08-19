import { createScholarshipApplicationForm, SCHOLARSHIP_STEPS } from './scholarship-process';

describe('SCHOLARSHIP_STEPS', () => {
  it('defines the three ordered scholarship steps', () => {
    expect(SCHOLARSHIP_STEPS.map(step => step.id)).toEqual([
      'application-info',
      'personal-info',
      'confirmation',
    ]);
  });
});

describe('createScholarshipApplicationForm', () => {
  it('requires choosing an inscription', () => {
    const form = createScholarshipApplicationForm();

    expect(form.invalid).toBe(true);

    form.controls.inscription.controls.selectedInscription.setValue('Ingeniería');

    expect(form.valid).toBe(true);
  });

  it('leaves the evaluation period optional by default', () => {
    const form = createScholarshipApplicationForm();

    expect(form.controls.evaluation.valid).toBe(true);
  });
});
