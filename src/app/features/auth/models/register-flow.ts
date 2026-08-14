export type RegisterFlowKind =
  'new-person' | 'existing-person' | 'new-application' | 'user-exists' | 'application-exists';

export type RegisterContinuableFlowKind = Extract<
  RegisterFlowKind,
  'new-person' | 'existing-person' | 'new-application'
>;

export type RegisterPersonalMode = 'complete' | 'verification';

export interface RegisterDocumentEvaluation {
  requiresPersonCreation: boolean;
  requiresApplicationCreation: boolean;
  requiresVerification: boolean;
  hasExistingApplication: boolean;
  userExists: boolean;
}

export function resolveRegisterFlow(
  documentType: string,
  evaluation: RegisterDocumentEvaluation
): RegisterFlowKind | null {
  if (evaluation.userExists) {
    return 'user-exists';
  }

  if (evaluation.hasExistingApplication) {
    return 'application-exists';
  }

  if (documentType === 'CI') {
    if (evaluation.requiresVerification) {
      return 'existing-person';
    }

    if (evaluation.requiresPersonCreation) {
      return 'new-person';
    }
  }

  if (documentType !== 'CI' && evaluation.requiresApplicationCreation) {
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
