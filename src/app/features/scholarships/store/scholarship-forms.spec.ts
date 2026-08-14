import { ScholarshipFormsStore } from './scholarship-forms';

describe('ScholarshipFormsStore', () => {
  it('requires a complete academic proposal', () => {
    const form = new ScholarshipFormsStore().academicForm;

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
