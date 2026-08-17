import { ScholarshipProcessStore } from './scholarship-process';

describe('ScholarshipProcessStore', () => {
  it('navigates the scholarship steps', () => {
    const store = new ScholarshipProcessStore();

    expect(store.flow.currentStep()).toBe('application-info');
    expect(store.flow.next()).toBe(true);
    expect(store.flow.currentStep()).toBe('personal-info');
    expect(store.flow.previous()).toBe(true);
    expect(store.flow.currentStep()).toBe('application-info');
  });
});
