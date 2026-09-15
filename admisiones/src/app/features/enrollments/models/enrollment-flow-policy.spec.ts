import { getVisibleSections, parseForcedResult } from './enrollment-flow-policy';

describe('enrollment flow policy', () => {
  it('uses a safe default for unknown forced results', () => {
    expect(parseForcedResult('error')).toBeNull();
    expect(parseForcedResult('en-proceso')).toBe('in-progress');
  });

  it('does not show the work section outside professional update flows', () => {
    expect(getVisibleSections(true)).not.toContain('work-situation');
    expect(getVisibleSections(false)).not.toContain('work-situation');
  });

  it('shows the full survey while the person can still answer it', () => {
    expect(getVisibleSections(true)).toEqual([
      'education',
      'academic-decision',
      'ort-experience',
      'identity',
      'regulation',
    ]);
  });

  it('limits the step to identity and regulation once the survey cannot be answered', () => {
    expect(getVisibleSections(false)).toEqual(['identity', 'regulation']);
  });

  it('reduces professional update flows to work status, identity and regulation', () => {
    expect(getVisibleSections(true, true)).toEqual(['work-situation', 'identity', 'regulation']);
    expect(getVisibleSections(false, true)).toEqual(['work-situation', 'identity', 'regulation']);
  });
});
