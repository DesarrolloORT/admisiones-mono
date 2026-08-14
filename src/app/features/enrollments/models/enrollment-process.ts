import type { ProcessStepDefinition } from 'src/app/shared/process-flow/process-flow';

export type EnrollmentStep = 'proposal' | 'survey' | 'payment';

export type EnrollmentOutcome =
  'reservation' | 'enrollment-confirmed' | 'enrollment-in-progress' | 'external-payment-pending';

export type EnrollmentPaymentView = 'editing' | 'confirming' | 'processing';

export const ENROLLMENT_STEPS: readonly ProcessStepDefinition<EnrollmentStep>[] = [
  { id: 'proposal', overline: 'Paso 1', title: 'Propuesta académica' },
  { id: 'survey', overline: 'Paso 2', title: 'Información personal' },
  { id: 'payment', overline: 'Paso 3', title: 'Confirmación' },
];
