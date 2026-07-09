import { createProcessFlow } from 'src/app/shared/process-flow/process-flow';

import { INSCRIPCION_STEPS } from './inscription-process';

describe('INSCRIPCION_STEPS', () => {
  it('declares the enrollment steps in order', () => {
    expect(INSCRIPCION_STEPS.map(step => step.id)).toEqual(['propuesta', 'encuesta', 'pago']);
    expect(INSCRIPCION_STEPS.map(step => step.overline)).toEqual(['Paso 1', 'Paso 2', 'Paso 3']);
    expect(INSCRIPCION_STEPS.map(step => step.title)).toEqual([
      'Propuesta académica',
      'Información personal',
      'Confirmación',
    ]);
  });
});

describe('createProcessFlow with INSCRIPCION_STEPS', () => {
  it('starts at the proposal step with derived state', () => {
    const flow = createProcessFlow(INSCRIPCION_STEPS, 'propuesta');

    expect(flow.currentStep()).toBe('propuesta');
    expect(flow.currentIndex()).toBe(0);
    expect(flow.canGoBack()).toBe(false);
    expect(flow.canGoNext()).toBe(true);
    expect(flow.stepItems().map(step => step.status)).toEqual(['current', 'pending', 'pending']);
  });

  it('moves through the steps with next, previous and goTo', () => {
    const flow = createProcessFlow(INSCRIPCION_STEPS, 'propuesta');

    expect(flow.next()).toBe(true);
    expect(flow.currentStep()).toBe('encuesta');
    expect(flow.stepItems().map(step => step.status)).toEqual(['completed', 'current', 'pending']);

    expect(flow.previous()).toBe(true);
    expect(flow.currentStep()).toBe('propuesta');

    expect(flow.goTo('pago')).toBe(true);
    expect(flow.currentIndex()).toBe(2);
    expect(flow.canGoNext()).toBe(false);
  });

  it('stays inside bounds and rejects no-op moves', () => {
    const flow = createProcessFlow(INSCRIPCION_STEPS, 'propuesta');

    expect(flow.previous()).toBe(false);
    expect(flow.currentStep()).toBe('propuesta');
    expect(flow.goTo('propuesta')).toBe(false);

    flow.goTo('pago');
    expect(flow.next()).toBe(false);
    expect(flow.currentStep()).toBe('pago');
  });

  it('resets back to the initial step', () => {
    const flow = createProcessFlow(INSCRIPCION_STEPS, 'propuesta');

    flow.goTo('pago');
    flow.reset();

    expect(flow.currentStep()).toBe('propuesta');
    expect(flow.currentIndex()).toBe(0);
    expect(flow.canGoBack()).toBe(false);
  });
});
