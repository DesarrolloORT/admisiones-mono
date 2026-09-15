import { signal } from '@angular/core';

import { createSectionStatus } from './section-status';

describe('createSectionStatus', () => {
  it('is complete only while the section is valid', () => {
    const valid = signal(false);
    const status = createSectionStatus(valid, signal(false));

    expect(status.isComplete()).toBe(false);

    valid.set(true);

    expect(status.isComplete()).toBe(true);
  });

  it('does not report an error before the person tried to continue', () => {
    const status = createSectionStatus(() => false, signal(false));

    expect(status.hasError()).toBe(false);
  });

  it('reports an error once submitted while the section stays invalid', () => {
    const valid = signal(false);
    const submitted = signal(false);
    const status = createSectionStatus(valid, submitted);

    submitted.set(true);

    expect(status.hasError()).toBe(true);

    valid.set(true);

    expect(status.hasError()).toBe(false);
  });
});
