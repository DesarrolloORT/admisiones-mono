import { SCHOLARSHIP_REQUIREMENTS_CONFIG } from './scholarship-requirements-card';

describe('SCHOLARSHIP_REQUIREMENTS_CONFIG', () => {
  it('defines requirements for every scholarship type', () => {
    expect(Object.keys(SCHOLARSHIP_REQUIREMENTS_CONFIG)).toEqual([
      'revalidation',
      'academic',
      'socioeconomic',
    ]);
    expect(
      Object.values(SCHOLARSHIP_REQUIREMENTS_CONFIG).every(config => config.requirements.length > 0)
    ).toBe(true);
  });
});
