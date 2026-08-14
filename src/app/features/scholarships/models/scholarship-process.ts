import type { ProcessStepDefinition } from 'src/app/shared/process-flow/process-flow';

/**
 * Identificadores de los pasos del flujo de postulación a becas.
 *
 * Es el equivalente de `InscripcionStep` en inscripciones. Cada id se usa como
 * `currentStepId` del `app-process-layout` y como discriminante del `@switch`
 * que decide qué step component se muestra en `fbr.html`.
 */
export type ScholarshipStep = 'info-postulacion' | 'info-personal' | 'confirmacion';

/**
 * Definición ordenada de los pasos. El orden de este array ES el orden del
 * proceso: `createProcessFlow` avanza/retrocede por índice sobre esta lista.
 * Para agregar, quitar o reordenar pasos, editá únicamente este array.
 */
export const SCHOLARSHIP_STEPS: readonly ProcessStepDefinition<ScholarshipStep>[] = [
  { id: 'info-postulacion', overline: 'Paso 1', title: 'Información de postulación' },
  { id: 'info-personal', overline: 'Paso 2', title: 'Información personal' },
  { id: 'confirmacion', overline: 'Paso 3', title: 'Confirmación' },
];
