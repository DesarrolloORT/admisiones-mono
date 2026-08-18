import { getVisibleSections, parseForcedResult } from './enrollment-flow-policy';

describe('enrollment flow policy', () => {
  it('uses a safe default for unknown forced results', () => {
    expect(parseForcedResult('error')).toBeNull();
    expect(parseForcedResult('en-proceso')).toBe('in-progress');
  });

  it('does not show the work section outside professional update flows', () => {
    expect(getVisibleSections('first-time')).not.toContain('work-situation');
    expect(getVisibleSections('partial')).not.toContain('work-situation');
  });

  it('limits a completed survey to identity and regulation', () => {
    expect(getVisibleSections('survey-complete')).toEqual(['identity', 'regulation']);
  });

  it('reduces professional update flows to work status, identity and regulation', () => {
    expect(getVisibleSections('first-time', true)).toEqual([
      'work-situation',
      'identity',
      'regulation',
    ]);
    expect(getVisibleSections('partial', true)).toEqual([
      'work-situation',
      'identity',
      'regulation',
    ]);
    expect(getVisibleSections('survey-complete', true)).toEqual([
      'work-situation',
      'identity',
      'regulation',
    ]);
  });
});
