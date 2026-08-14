import { createProcessFlow } from 'src/app/shared/process-flow/process-flow';

import { ENROLLMENT_STEPS } from './enrollment-process';

describe('ENROLLMENT_STEPS', () => {
  it('declares the enrollment steps in order', () => {
    expect(ENROLLMENT_STEPS.map(step => step.id)).toEqual(['proposal', 'survey', 'payment']);
    expect(ENROLLMENT_STEPS.map(step => step.overline)).toEqual(['Paso 1', 'Paso 2', 'Paso 3']);
    expect(ENROLLMENT_STEPS.map(step => step.title)).toEqual([
      'Propuesta académica',
      'Información personal',
      'Confirmación',
    ]);
  });
});

describe('createProcessFlow with ENROLLMENT_STEPS', () => {
  it('starts at the proposal step with derived state', () => {
    const flow = createProcessFlow(ENROLLMENT_STEPS, 'proposal');

    expect(flow.currentStep()).toBe('proposal');
    expect(flow.currentIndex()).toBe(0);
    expect(flow.canGoBack()).toBe(false);
    expect(flow.canGoNext()).toBe(true);
    expect(flow.stepItems().map(step => step.status)).toEqual(['current', 'pending', 'pending']);
  });

  it('moves through the steps with next, previous and goTo', () => {
    const flow = createProcessFlow(ENROLLMENT_STEPS, 'proposal');

    expect(flow.next()).toBe(true);
    expect(flow.currentStep()).toBe('survey');
    expect(flow.stepItems().map(step => step.status)).toEqual(['completed', 'current', 'pending']);

    expect(flow.previous()).toBe(true);
    expect(flow.currentStep()).toBe('proposal');

    expect(flow.goTo('payment')).toBe(true);
    expect(flow.currentIndex()).toBe(2);
    expect(flow.canGoNext()).toBe(false);
  });

  it('stays inside bounds and rejects no-op moves', () => {
    const flow = createProcessFlow(ENROLLMENT_STEPS, 'proposal');

    expect(flow.previous()).toBe(false);
    expect(flow.currentStep()).toBe('proposal');
    expect(flow.goTo('proposal')).toBe(false);

    flow.goTo('payment');
    expect(flow.next()).toBe(false);
    expect(flow.currentStep()).toBe('payment');
  });

  it('resets back to the initial step', () => {
    const flow = createProcessFlow(ENROLLMENT_STEPS, 'proposal');

    flow.goTo('payment');
    flow.reset();

    expect(flow.currentStep()).toBe('proposal');
    expect(flow.currentIndex()).toBe(0);
    expect(flow.canGoBack()).toBe(false);
  });
});
