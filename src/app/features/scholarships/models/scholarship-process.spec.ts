import { SCHOLARSHIP_STEPS } from './scholarship-process';

describe('SCHOLARSHIP_STEPS', () => {
  it('defines the three ordered scholarship steps', () => {
    expect(SCHOLARSHIP_STEPS.map(step => step.id)).toEqual([
      'application-info',
      'personal-info',
      'confirmation',
    ]);
  });
});
