import { type EnrollmentScenario, SURVEY_SECTIONS } from './enrollment-flow';

describe('enrollment flow model', () => {
  it('keeps the supported enrollment scenarios explicit', () => {
    const scenarios: EnrollmentScenario[] = ['first-time', 'partial', 'survey-complete'];

    expect(scenarios).toEqual(['first-time', 'partial', 'survey-complete']);
  });

  it('lists the initial survey sections without the work section', () => {
    expect(SURVEY_SECTIONS).toEqual([
      'education',
      'academic-decision',
      'ort-experience',
      'identity',
      'regulation',
    ]);
  });
});
