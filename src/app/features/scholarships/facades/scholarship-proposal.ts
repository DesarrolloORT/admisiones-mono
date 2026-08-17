import { inject, signal } from '@angular/core';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { ScholarshipFormsStore } from '../store/scholarship-forms';
import { ScholarshipProcessStore } from '../store/scholarship-process';

/**
 * Fachada del paso "Información de postulación" (propuesta académica). Es el
 * espejo de `EnrollmentProposalFacade`, pero **sin llamada a la API**: los
 * endpoints de becas todavía no están definidos.
 *
 * Expone el formulario académico y el servicio de selección que consume el
 * `app-academic-proposal-select`, y decide el avance en `continue()`: valida el
 * form y, si está OK, llama a `this.process.flow.next()`. Cuando exista el
 * endpoint de registro de interés, se agrega la llamada antes del `next()`
 * (igual que hace inscripciones con `registerProductInterest`).
 */
export class ScholarshipProposalFacade {
  private readonly formsStore = inject(ScholarshipFormsStore);
  private readonly process = inject(ScholarshipProcessStore);

  public readonly selection = inject(AcademicProposalSelection);
  public readonly academicForm = this.formsStore.academicForm;

  /** `true` una vez que el usuario intentó continuar; habilita mostrar errores. */
  public readonly submitted = signal(false);

  public continue(): void {
    this.submitted.set(true);
    if (this.academicForm.invalid) {
      this.academicForm.markAllAsTouched();
      return;
    }
    // Punto de extensión: cuando exista el endpoint, registrar el interés por la
    // propuesta acá antes de avanzar (ver EnrollmentProposalFacade.continue).
    this.process.flow.next();
  }
}
