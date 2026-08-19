import { createScholarshipAcademicForm, SCHOLARSHIP_STEPS } from './scholarship-process';

describe('SCHOLARSHIP_STEPS', () => {
  it('defines the three ordered scholarship steps', () => {
    expect(SCHOLARSHIP_STEPS.map(step => step.id)).toEqual([
      'application-info',
      'personal-info',
      'confirmation',
    ]);
  });
});

describe('createScholarshipAcademicForm', () => {
  it('requires a complete academic proposal', () => {
    const form = createScholarshipAcademicForm();

    expect(form.invalid).toBe(true);

    form.setValue({
      proposalType: '1',
      degreeProgram: '20',
      intake: '200',
      shift: '300',
      seminars: [],
    });

    expect(form.valid).toBe(true);
  });
});
