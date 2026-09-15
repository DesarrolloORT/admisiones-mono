import { InjectionToken, signal, type WritableSignal } from '@angular/core';
import {
  createProcessFlow,
  type ProcessFlow,
  type ProcessStepDefinition,
} from 'src/app/shared/process-flow/process-flow';

import type { EnrollmentPreEnrollmentResponse } from './enrollment-flow';

export type EnrollmentStep = 'proposal' | 'survey' | 'payment';

export type EnrollmentOutcome =
  'reservation' | 'enrollment-confirmed' | 'enrollment-in-progress' | 'external-payment-pending';

export type EnrollmentPaymentView = 'editing' | 'processing';

export const ENROLLMENT_STEPS: readonly ProcessStepDefinition<EnrollmentStep>[] = [
  { id: 'proposal', overline: 'Paso 1', title: 'Propuesta académica' },
  { id: 'survey', overline: 'Paso 2', title: 'Información personal' },
  { id: 'payment', overline: 'Paso 3', title: 'Confirmación' },
];

export interface EnrollmentProcessState {
  flow: ProcessFlow<EnrollmentStep>;
  preEnrollmentResponse: WritableSignal<EnrollmentPreEnrollmentResponse | null>;
}

/**
 * Estado del proceso de inscripción: el paso actual y la respuesta de
 * preinscripción que comparten las fachadas del flujo. Se provee a nivel de la
 * página (ver `ENROLLMENT_PROCESS_STATE`), así que cada inscripción tiene el
 * suyo. No hay una capa `store/`: el estado es una factory de `models/`.
 */
export function createEnrollmentProcessState(): EnrollmentProcessState {
  return {
    flow: createProcessFlow(ENROLLMENT_STEPS, 'proposal'),
    preEnrollmentResponse: signal<EnrollmentPreEnrollmentResponse | null>(null),
  };
}

export const ENROLLMENT_PROCESS_STATE = new InjectionToken<EnrollmentProcessState>(
  'ENROLLMENT_PROCESS_STATE',
  { factory: createEnrollmentProcessState }
);
