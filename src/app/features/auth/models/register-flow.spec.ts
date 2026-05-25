import {
  getRegisterPersonalMode,
  isRegisterContinuableFlow,
  type RegisterDocumentEvaluation,
  resolveRegisterFlow,
} from './register-flow';

describe('register flow', () => {
  const emptyEvaluation: RegisterDocumentEvaluation = {
    requiereAltaPersona: false,
    requiereAltaSolicitud: false,
    requiereVerificacion: false,
    solicitudAltaExistente: false,
    usuarioExistente: false,
  };

  it('should resolve an already registered user first', () => {
    expect(
      resolveRegisterFlow('CI', {
        ...emptyEvaluation,
        requiereAltaPersona: true,
        usuarioExistente: true,
      })
    ).toBe('user-exists');
  });

  it('should resolve an existing application before continuable flows', () => {
    expect(
      resolveRegisterFlow('PS', {
        ...emptyEvaluation,
        requiereAltaSolicitud: true,
        solicitudAltaExistente: true,
      })
    ).toBe('application-exists');
  });

  it('should resolve existing CI person verification', () => {
    expect(resolveRegisterFlow('CI', { ...emptyEvaluation, requiereVerificacion: true })).toBe(
      'existing-person'
    );
  });

  it('should resolve new CI person registration', () => {
    expect(resolveRegisterFlow('CI', { ...emptyEvaluation, requiereAltaPersona: true })).toBe(
      'new-person'
    );
  });

  it('should resolve new non-CI application registration', () => {
    expect(resolveRegisterFlow('PS', { ...emptyEvaluation, requiereAltaSolicitud: true })).toBe(
      'new-application'
    );
  });

  it('should return null when the backend flags do not map to a known flow', () => {
    expect(resolveRegisterFlow('PS', { ...emptyEvaluation, requiereAltaPersona: true })).toBeNull();
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
