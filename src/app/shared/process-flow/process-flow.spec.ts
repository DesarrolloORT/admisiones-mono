import { createProcessFlow } from './process-flow';

describe('createProcessFlow', () => {
  const steps = [
    { id: 'one', overline: 'Paso 1', title: 'Uno' },
    { id: 'two', overline: 'Paso 2', title: 'Dos' },
    { id: 'three', overline: 'Paso 3', title: 'Tres' },
  ] as const;

  it('derives navigation and step status from the active step', () => {
    const flow = createProcessFlow(steps, 'one');

    expect(flow.currentStep()).toBe('one');
    expect(flow.canGoBack()).toBe(false);
    expect(flow.canGoNext()).toBe(true);
    expect(flow.stepItems().map(step => step.status)).toEqual(['current', 'pending', 'pending']);

    expect(flow.next()).toBe(true);
    expect(flow.currentStep()).toBe('two');
    expect(flow.stepItems().map(step => step.status)).toEqual(['completed', 'current', 'pending']);
  });

  it('keeps navigation inside the declared bounds', () => {
    const flow = createProcessFlow(steps, 'one');

    expect(flow.previous()).toBe(false);
    expect(flow.goTo('three')).toBe(true);
    expect(flow.next()).toBe(false);
    expect(flow.currentStep()).toBe('three');

    flow.reset();
    expect(flow.currentStep()).toBe('one');
  });

  it('rejects invalid definitions', () => {
    expect(() => createProcessFlow([], 'one')).toThrow();
    expect(() => createProcessFlow(steps, 'missing')).toThrow();
    expect(() => createProcessFlow([steps[0], steps[0]], 'one')).toThrow();
  });
});
