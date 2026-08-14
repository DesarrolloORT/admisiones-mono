import {
  getRegisterPersonalMode,
  isRegisterContinuableFlow,
  type RegisterDocumentEvaluation,
  resolveRegisterFlow,
} from './register-flow';

describe('register flow', () => {
  const emptyEvaluation: RegisterDocumentEvaluation = {
    requiresPersonCreation: false,
    requiresApplicationCreation: false,
    requiresVerification: false,
    hasExistingApplication: false,
    userExists: false,
  };

  it('should resolve an already registered user first', () => {
    expect(
      resolveRegisterFlow('CI', {
        ...emptyEvaluation,
        requiresPersonCreation: true,
        userExists: true,
      })
    ).toBe('user-exists');
  });

  it('should resolve an existing application before continuable flows', () => {
    expect(
      resolveRegisterFlow('PS', {
        ...emptyEvaluation,
        requiresApplicationCreation: true,
        hasExistingApplication: true,
      })
    ).toBe('application-exists');
  });

  it('should resolve existing CI person verification', () => {
    expect(resolveRegisterFlow('CI', { ...emptyEvaluation, requiresVerification: true })).toBe(
      'existing-person'
    );
  });

  it('should resolve new CI person registration', () => {
    expect(resolveRegisterFlow('CI', { ...emptyEvaluation, requiresPersonCreation: true })).toBe(
      'new-person'
    );
  });

  it('should resolve new non-CI application registration', () => {
    expect(
      resolveRegisterFlow('PS', { ...emptyEvaluation, requiresApplicationCreation: true })
    ).toBe('new-application');
  });

  it('should return null when the backend flags do not map to a known flow', () => {
    expect(
      resolveRegisterFlow('PS', { ...emptyEvaluation, requiresPersonCreation: true })
    ).toBeNull();
  });

  it('should expose continuable flows and personal form mode', () => {
    expect(isRegisterContinuableFlow('new-person')).toBe(true);
    expect(isRegisterContinuableFlow('existing-person')).toBe(true);
    expect(isRegisterContinuableFlow('new-application')).toBe(true);
    expect(isRegisterContinuableFlow('user-exists')).toBe(false);
    expect(getRegisterPersonalMode('existing-person')).toBe('verification');
    expect(getRegisterPersonalMode('new-person')).toBe('complete');
    expect(getRegisterPersonalMode(null)).toBe('complete');
  });
});
