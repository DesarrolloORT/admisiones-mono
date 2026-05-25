import { REGISTER_STEP_VIEW_MODELS } from './register-step';

describe('register step view models', () => {
  it('should define labels for each register step', () => {
    expect(REGISTER_STEP_VIEW_MODELS.identity.title).toBe('Crear cuenta');
    expect(REGISTER_STEP_VIEW_MODELS.personal.stepLabel).toBe('Paso 1 de 2');
    expect(REGISTER_STEP_VIEW_MODELS.career.stepLabel).toBe('Paso 2 de 2');
  });
});
