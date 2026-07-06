import { FormControl, FormGroup, Validators } from '@angular/forms';

import type { AcademicProposalForm } from '../../catalogs/models/academic-proposal';
import { createScholarshipPersonalForms } from '../models/scholarship-personal-forms';

/**
 * Crea y guarda los `FormGroup` del flujo de becas. Equivale a
 * `InscripcionFormsStore`: las fachadas leen los forms desde acá.
 *
 * `academicForm` es del paso 1 (propuesta académica). Los forms de la sección
 * "Información personal" (paso 2) se crean con `createScholarshipPersonalForms`
 * en `models/scholarship-personal-forms.ts`, igual que inscripciones agrupa sus
 * forms en `createInscripcionForms`.
 */
export class ScholarshipFormsStore {
  public readonly academicForm = new FormGroup<AcademicProposalForm>({
    tipoPropuesta: new FormControl('', { nonNullable: true, validators: Validators.required }),
    carrera: new FormControl('', { nonNullable: true, validators: Validators.required }),
    comienzo: new FormControl('', { nonNullable: true, validators: Validators.required }),
    turno: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });

  private readonly personalForms = createScholarshipPersonalForms();

  public readonly personalDataForm = this.personalForms.personalDataForm;
  public readonly educationInfoForm = this.personalForms.educationInfoForm;
  public readonly educationInfoFbrForm = this.personalForms.educationInfoFbrForm;
  public readonly educationInfoFclForm = this.personalForms.educationInfoFclForm;
  public readonly workHistoryForm = this.personalForms.workHistoryForm;
  public readonly declarationForm = this.personalForms.declarationForm;
}
