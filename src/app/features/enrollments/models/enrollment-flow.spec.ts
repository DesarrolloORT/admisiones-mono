import { SURVEY_SECTIONS } from './enrollment-flow';

describe('enrollment flow model', () => {
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
