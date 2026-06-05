import type { RegisterFlowKind } from '../../../src/app/features/auth/models/register-flow';

export interface RegisterDocumentEvaluationMock {
  requiereAltaPersona: boolean;
  requiereAltaSolicitud: boolean;
  requiereVerificacion: boolean;
  solicitudAltaExistente: boolean;
  usuarioExistente: boolean;
}

export interface RegisterScenario {
  kind: RegisterFlowKind;
  documentType: 'CI' | 'PS' | 'DE';
  documentNumber: string;
  flowId: string;
  evaluation: RegisterDocumentEvaluationMock;
  terminalMessage?: string;
}

export const REGISTER_SCENARIOS: Record<RegisterFlowKind, RegisterScenario> = {
  'new-person': {
    kind: 'new-person',
    documentType: 'CI',
    documentNumber: '12345672',
    flowId: 'flow-e2e-new-person',
    evaluation: {
      requiereAltaPersona: true,
      requiereAltaSolicitud: false,
      requiereVerificacion: false,
      solicitudAltaExistente: false,
      usuarioExistente: false,
    },
  },
  'existing-person': {
    kind: 'existing-person',
    documentType: 'CI',
    documentNumber: '12345672',
    flowId: 'flow-e2e-existing-person',
    evaluation: {
      requiereAltaPersona: false,
      requiereAltaSolicitud: false,
      requiereVerificacion: true,
      solicitudAltaExistente: false,
      usuarioExistente: false,
    },
  },
  'new-application': {
    kind: 'new-application',
    documentType: 'PS',
    documentNumber: 'PS-123456',
    flowId: 'flow-e2e-new-application',
    evaluation: {
      requiereAltaPersona: false,
      requiereAltaSolicitud: true,
      requiereVerificacion: false,
      solicitudAltaExistente: false,
      usuarioExistente: false,
    },
  },
  'user-exists': {
    kind: 'user-exists',
    documentType: 'CI',
    documentNumber: '12345672',
    flowId: 'flow-e2e-user-exists',
    terminalMessage: 'Ya existe un usuario registrado con este documento.',
    evaluation: {
      requiereAltaPersona: false,
      requiereAltaSolicitud: false,
      requiereVerificacion: false,
      solicitudAltaExistente: false,
      usuarioExistente: true,
    },
  },
  'application-exists': {
    kind: 'application-exists',
    documentType: 'CI',
    documentNumber: '12345672',
    flowId: 'flow-e2e-application-exists',
    terminalMessage: 'Ya existe una solicitud de alta pendiente para este documento.',
    evaluation: {
      requiereAltaPersona: false,
      requiereAltaSolicitud: false,
      requiereVerificacion: false,
      solicitudAltaExistente: true,
      usuarioExistente: false,
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
