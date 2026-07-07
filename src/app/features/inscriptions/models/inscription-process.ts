import type { ProcessStepDefinition } from 'src/app/shared/process-flow/process-flow';

export type InscripcionStep = 'propuesta' | 'encuesta' | 'pago';

export type InscripcionOutcome =
  | 'reserva'
  | 'inscription-confirmada'
  | 'inscription-en-proceso'
  | 'pago-pendiente-externo';

export type InscripcionPaymentView = 'editing' | 'confirming' | 'processing';

export const INSCRIPCION_STEPS: readonly ProcessStepDefinition<InscripcionStep>[] = [
  { id: 'propuesta', overline: 'Paso 1', title: 'Propuesta académica' },
  { id: 'encuesta', overline: 'Paso 2', title: 'Información personal' },
  { id: 'pago', overline: 'Paso 3', title: 'Confirmación' },
];
