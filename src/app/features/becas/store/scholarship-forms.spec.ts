import { ScholarshipFormsStore } from './scholarship-forms';

describe('ScholarshipFormsStore', () => {
  it('requires a complete academic proposal', () => {
    const form = new ScholarshipFormsStore().academicForm;

    expect(form.invalid).toBe(true);

    form.setValue({
      tipoPropuesta: '1',
      carrera: '20',
      comienzo: '200',
      turno: '300',
      seminarios: [],
    });

    expect(form.valid).toBe(true);
  });
});
