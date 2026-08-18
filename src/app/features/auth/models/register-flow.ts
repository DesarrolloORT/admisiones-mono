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

export const REGISTER_CONFIRMATION_ROUTE = '/confirmacion-correo/registro';
export const REGISTER_REQUEST_CONFIRMATION_ROUTE = '/confirmacion-correo/solicitud-registro';

export interface RegistrationOutcome {
  pendingReview: boolean;
  mailSent: boolean;
}

export interface RegistrationEnding {
  route: string;
  missingActivationEmail: boolean;
}

/**
 * `pendingReview` es la unica fuente de verdad del final del registro: el
 * backend ya resolvio si la solicitud queda esperando revision manual, asi que
 * el tipo de documento no participa. `mailSent` solo importa cuando hubo un
 * correo de activacion que enviar; con `pendingReview` nunca lo hay.
 */
export function resolveRegistrationEnding({
  pendingReview,
  mailSent,
}: RegistrationOutcome): RegistrationEnding {
  return pendingReview
    ? { route: REGISTER_REQUEST_CONFIRMATION_ROUTE, missingActivationEmail: false }
    : { route: REGISTER_CONFIRMATION_ROUTE, missingActivationEmail: !mailSent };
}
