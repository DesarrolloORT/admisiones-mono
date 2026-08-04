import { REGISTER_STEP_VIEW_MODELS } from './register-step';

describe('register step view models', () => {
  it('should define labels for each register step', () => {
    expect(REGISTER_STEP_VIEW_MODELS.identity.title).toBe('Crear cuenta');
    expect(REGISTER_STEP_VIEW_MODELS.personal.stepLabel).toBeNull();
  });
});
