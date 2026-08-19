import { FormControl, FormGroup, Validators } from '@angular/forms';
import type { ProcessStepDefinition } from 'src/app/shared/process-flow/process-flow';

import type { AcademicProposalForm } from '../../catalogs/models/academic-proposal';

/**
 * Identificadores de los pasos del flujo de postulación a becas.
 *
 * Es el equivalente de `EnrollmentStep` en inscripciones. Cada id se usa como
 * `currentStepId` del `app-process-layout` y como discriminante del `@switch`
 * que decide qué step component se muestra en `fbr.html`.
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
 * Crea el `FormGroup` de la propuesta académica del paso 1. La fachada de
 * sección lo invoca y es su dueña: no hay una capa `store/` intermedia.
 */
export function createScholarshipAcademicForm(): FormGroup<AcademicProposalForm> {
  return new FormGroup<AcademicProposalForm>({
    proposalType: new FormControl('', { nonNullable: true, validators: Validators.required }),
    degreeProgram: new FormControl('', { nonNullable: true, validators: Validators.required }),
    intake: new FormControl('', { nonNullable: true, validators: Validators.required }),
    shift: new FormControl('', { nonNullable: true, validators: Validators.required }),
    seminars: new FormControl<string[]>([], { nonNullable: true }),
  });
}
