export type RegisterFlowKind =
  | 'new-person'
  | 'existing-person'
  | 'new-application'
  | 'user-exists'
  | 'application-exists';

export type RegisterContinuableFlowKind = Extract<
  RegisterFlowKind,
  'new-person' | 'existing-person' | 'new-application'
>;

export type RegisterPersonalMode = 'complete' | 'verification';

export interface RegisterDocumentEvaluation {
  requiereAltaPersona: boolean;
  requiereAltaSolicitud: boolean;
  requiereVerificacion: boolean;
  solicitudAltaExistente: boolean;
  usuarioExistente: boolean;
}

export function resolveRegisterFlow(
  documentType: string,
  evaluation: RegisterDocumentEvaluation
): RegisterFlowKind | null {
  if (evaluation.usuarioExistente) {
    return 'user-exists';
  }

  if (evaluation.solicitudAltaExistente) {
    return 'application-exists';
  }

  if (documentType === 'CI') {
    if (evaluation.requiereVerificacion) {
      return 'existing-person';
    }

    if (evaluation.requiereAltaPersona) {
      return 'new-person';
    }
  }

  if (documentType !== 'CI' && evaluation.requiereAltaSolicitud) {
    return 'new-application';
  }

  return null;
}

export function isRegisterContinuableFlow(
  flow: RegisterFlowKind | null
): flow is RegisterContinuableFlowKind {
  return flow === 'new-person' || flow === 'existing-person' || flow === 'new-application';
}

export function getRegisterPersonalMode(flow: RegisterFlowKind | null): RegisterPersonalMode {
  return flow === 'existing-person' ? 'verification' : 'complete';
}
