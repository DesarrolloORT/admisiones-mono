import { createProcessFlow } from 'src/app/shared/process-flow/process-flow';

import { SCHOLARSHIP_STEPS } from '../models/scholarship-process';

/**
 * Estado del proceso de postulación a becas. Equivale a `InscripcionProcessStore`.
 *
 * Es la fuente de verdad del paso actual: envuelve el motor genérico
 * `createProcessFlow` y expone sus señales (`currentStep`, `stepItems`,
 * `canGoBack`, …) más las acciones de navegación (`next`, `previous`, `goTo`).
 *
 * Se provee a nivel de la página (no en root) para que cada postulación tenga
 * su propio estado. A medida que el flujo crezca, agregá acá las señales de
 * estado compartido entre pasos (p. ej. la respuesta de la API de postulación),
 * igual que `preEnrollmentResponse` en inscripciones.
 */
export class ScholarshipProcessStore {
  public readonly flow = createProcessFlow(SCHOLARSHIP_STEPS, 'info-postulacion');
}
