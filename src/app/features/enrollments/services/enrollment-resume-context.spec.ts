import { TestBed } from '@angular/core/testing';

import { EnrollmentResumeContextStore } from './enrollment-resume-context';

describe('EnrollmentResumeContextStore', () => {
  let store: EnrollmentResumeContextStore;

  beforeEach(() => {
    sessionStorage.clear();
    store = TestBed.inject(EnrollmentResumeContextStore);
  });

  it('stores only unique positive ids for the selected enrollment', () => {
    store.save({
      productId: 40,
      admissionProcessId: 210,
      offeringIds: [310, 311, 310, 0],
      enrollmentIds: [7010, 7011, 7010, -1],
    });

    expect(store.read(40, 210)).toEqual({
      productId: 40,
      admissionProcessId: 210,
      offeringIds: [310, 311],
      enrollmentIds: [7010, 7011],
    });
    expect(store.read(41, 210)).toBeNull();
  });

  it('ignores malformed session data', () => {
    sessionStorage.setItem('inscription-resume-context', '{invalid');

    expect(store.read(40, 210)).toBeNull();
  });
});
