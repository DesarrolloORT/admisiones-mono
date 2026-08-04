import { ScholarshipProcessStore } from './scholarship-process';

describe('ScholarshipProcessStore', () => {
  it('navigates the scholarship steps', () => {
    const store = new ScholarshipProcessStore();

    expect(store.flow.currentStep()).toBe('info-postulacion');
    expect(store.flow.next()).toBe(true);
    expect(store.flow.currentStep()).toBe('info-personal');
    expect(store.flow.previous()).toBe(true);
    expect(store.flow.currentStep()).toBe('info-postulacion');
  });
});
