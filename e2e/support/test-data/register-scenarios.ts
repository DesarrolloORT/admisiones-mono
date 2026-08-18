import type { RegisterFlowKind } from '../../../src/app/features/auth/models/register-flow';

export interface RegisterDocumentEvaluationMock {
  requiresPersonCreation: boolean;
  requiresApplicationCreation: boolean;
  requiresVerification: boolean;
  hasExistingApplication: boolean;
  userExists: boolean;
}

/** Cuerpo de `RegistrationFlowResult` que devuelven los endpoints de confirmacion. */
export interface RegisterConfirmationMock {
  mailSent: boolean;
  pendingReview: boolean;
}

export interface RegisterScenario {
  kind: RegisterFlowKind;
  documentType: 'CI' | 'PS' | 'DE';
  documentNumber: string;
  flowId: string;
  evaluation: RegisterDocumentEvaluationMock;
  confirmation?: RegisterConfirmationMock;
  terminalMessage?: string;
}

export const REGISTER_SCENARIOS: Record<RegisterFlowKind, RegisterScenario> = {
  'new-person': {
    kind: 'new-person',
    documentType: 'CI',
    documentNumber: '12345672',
    flowId: 'flow-e2e-new-person',
    evaluation: {
      requiresPersonCreation: true,
      requiresApplicationCreation: false,
      requiresVerification: false,
      hasExistingApplication: false,
      userExists: false,
    },
    confirmation: { mailSent: true, pendingReview: false },
  },
  'existing-person': {
    kind: 'existing-person',
    documentType: 'CI',
    documentNumber: '12345672',
    flowId: 'flow-e2e-existing-person',
    evaluation: {
      requiresPersonCreation: false,
      requiresApplicationCreation: false,
      requiresVerification: true,
      hasExistingApplication: false,
      userExists: false,
    },
    confirmation: { mailSent: true, pendingReview: false },
  },
  'new-application': {
    kind: 'new-application',
    documentType: 'PS',
    documentNumber: 'PS-123456',
    flowId: 'flow-e2e-new-application',
    evaluation: {
      requiresPersonCreation: false,
      requiresApplicationCreation: true,
      requiresVerification: false,
      hasExistingApplication: false,
      userExists: false,
    },
    confirmation: { mailSent: false, pendingReview: true },
  },
  'user-exists': {
    kind: 'user-exists',
    documentType: 'CI',
    documentNumber: '12345672',
    flowId: 'flow-e2e-user-exists',
    terminalMessage: 'Ya existe un usuario registrado con este documento.',
    evaluation: {
      requiresPersonCreation: false,
      requiresApplicationCreation: false,
      requiresVerification: false,
      hasExistingApplication: false,
      userExists: true,
    },
  },
  // Solo puede darse con documento no CI: una CI con solicitud previa no existe
  // en el backend (T_SOLICITUD_ALTA solo recibe documentos extranjeros).
  'application-exists': {
    kind: 'application-exists',
    documentType: 'PS',
    documentNumber: 'PS-654321',
    flowId: 'flow-e2e-application-exists',
    terminalMessage: 'Ya existe una solicitud de alta pendiente para este documento.',
    evaluation: {
      requiresPersonCreation: false,
      requiresApplicationCreation: false,
      requiresVerification: false,
      hasExistingApplication: true,
      userExists: false,
    },
  },
};

export const personalData = {
  firstName: 'Ana',
  secondName: 'Laura',
  firstLastName: 'Pereira',
  secondLastName: 'Silva',
  birthDate: '1999-05-21',
  sex: 'Femenino',
  country: 'Uruguay',
  state: 'Montevideo',
  city: 'Montevideo',
  address: 'Bulevar España 2633',
  phone: '99123456',
  email: 'ana.pereira@example.com',
};
