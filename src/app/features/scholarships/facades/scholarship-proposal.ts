import { inject, signal } from '@angular/core';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { createScholarshipAcademicForm } from '../models/scholarship-process';

/**
 * Fachada del paso "Información de postulación" (propuesta académica). Es el
 * espejo de `EnrollmentProposalFacade`, pero **sin llamada a la API**: los
 * endpoints de becas todavía no están definidos.
 *
 * Expone el formulario académico y el servicio de selección que consume el
 * `app-academic-proposal-select`. No avanza el flujo: `canContinue()` sólo
 * responde si la sección está en condiciones de avanzar y
 * `ScholarshipProcessFacade` decide. Esa dirección (proceso → sección) es lo
 * que permite que el estado del proceso viva en la fachada de proceso.
 */
export class ScholarshipProposalFacade {
  public readonly selection = inject(AcademicProposalSelection);
  public readonly academicForm = createScholarshipAcademicForm();

  /** `true` una vez que el usuario intentó continuar; habilita mostrar errores. */
  public readonly submitted = signal(false);

  /**
   * Valida la sección. Devuelve `true` cuando el proceso puede avanzar.
   * Punto de extensión: cuando exista el endpoint, registrar el interés por la
   * propuesta acá antes de devolver `true`.
   */
  public canContinue(): boolean {
    this.submitted.set(true);
    if (this.academicForm.invalid) {
      this.academicForm.markAllAsTouched();
      return false;
    }
    return true;
  }
}
