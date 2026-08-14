import type { EnrollmentPreEnrollmentResponse } from '../models/enrollment-flow';
import { EnrollmentProcessStore } from './enrollment-process';

describe('EnrollmentProcessStore', () => {
  let store: EnrollmentProcessStore;

  beforeEach(() => {
    store = new EnrollmentProcessStore();
  });

  it('starts the flow at the proposal step', () => {
    expect(store.flow.currentStep()).toBe('proposal');
    expect(store.flow.currentIndex()).toBe(0);
    expect(store.flow.canGoBack()).toBe(false);
  });

  it('navigates through the enrollment steps', () => {
    expect(store.flow.next()).toBe(true);
    expect(store.flow.currentStep()).toBe('survey');
    expect(store.flow.currentIndex()).toBe(1);
    expect(store.flow.canGoBack()).toBe(true);

    expect(store.flow.next()).toBe(true);
    expect(store.flow.currentStep()).toBe('payment');
    expect(store.flow.next()).toBe(false);

    expect(store.flow.previous()).toBe(true);
    expect(store.flow.currentStep()).toBe('survey');
  });

  it('jumps to a step and resets to the beginning', () => {
    expect(store.flow.goTo('payment')).toBe(true);
    expect(store.flow.currentIndex()).toBe(2);

    store.flow.reset();

    expect(store.flow.currentStep()).toBe('proposal');
    expect(store.flow.canGoBack()).toBe(false);
  });

  it('holds the pre-enrollment response, starting empty', () => {
    expect(store.preEnrollmentResponse()).toBeNull();

    const response: EnrollmentPreEnrollmentResponse = {
      idEnrollment: 1072704,
      confirmed: false,
      paymentDueDate: '2027-03-04',
      enrollmentDeposit: 15500,
      accountBalance: 1200,
      summary: { degreeProgram: 'Sistemas', intake: 'Marzo 2027', shift: 'Noche' },
    };
    store.preEnrollmentResponse.set(response);

    expect(store.preEnrollmentResponse()).toBe(response);
  });
});
