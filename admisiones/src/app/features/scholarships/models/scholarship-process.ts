import { FormControl, FormGroup, Validators } from '@angular/forms';
import type { ProcessStepDefinition } from 'src/app/shared/process-flow/process-flow';

/**
 * Identificadores de los pasos del flujo de postulación a becas.
 *
 * Es el equivalente de `EnrollmentStep` en inscripciones. Cada id se usa como
 * `currentStepId` del `app-process-layout` y como discriminante del `@switch`
 * que decide qué step component se muestra en `scholarship-process.html`.
 */
export type ScholarshipStep = 'application-info' | 'personal-info' | 'confirmation';

/**
 * Definición ordenada de los pasos. El orden de este array ES el orden del
 * proceso: `createProcessFlow` avanza/retrocede por índice sobre esta lista.
 * Para agregar, quitar o reordenar pasos, editá únicamente este array.
 */
export const SCHOLARSHIP_STEPS: readonly ProcessStepDefinition<ScholarshipStep>[] = [
  { id: 'application-info', overline: 'Paso 1', title: 'Información de postulación' },
  { id: 'personal-info', overline: 'Paso 2', title: 'Información personal' },
  { id: 'confirmation', overline: 'Paso 3', title: 'Confirmación' },
];

/**
 * Crea el `FormGroup` del paso 1: la inscripción sobre la que se postula y, en
 * las becas que lo piden, el período de evaluación. La fachada de proceso es su
 * dueña porque tiene que sobrevivir al cambio de paso.
 */
export function createScholarshipApplicationForm() {
  return new FormGroup({
    inscription: new FormGroup({
      selectedInscription: new FormControl<string | null>(null, Validators.required),
      applicationMode: new FormControl<string | null>(null),
    }),
    evaluation: new FormGroup({
      evaluationDate: new FormControl<string | null>(null),
    }),
  });
}

export type ScholarshipApplicationForm = ReturnType<typeof createScholarshipApplicationForm>;
