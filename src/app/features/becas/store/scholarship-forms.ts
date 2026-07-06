import { FormControl, FormGroup, Validators } from '@angular/forms';

import { createScholarshipPersonalForms } from '../models/scholarship-personal-forms';

/**
 * Crea y guarda los `FormGroup` del flujo de becas. Equivale a
 * `InscripcionFormsStore`: las fachadas leen los forms desde acá.
 *
 * `inscriptionForm` es del paso 1 (inscripción + período de evaluación). Los
 * forms de la sección "Información personal" (paso 2) se crean con
 * `createScholarshipPersonalForms` en `models/scholarship-personal-forms.ts`,
 * igual que inscripciones agrupa sus forms en `createInscripcionForms`.
 */
export class ScholarshipFormsStore {
  public readonly inscriptionForm = new FormGroup({
    inscription: new FormGroup({
      selectedInscription: new FormControl<string | null>(null, Validators.required),
      applicationMode: new FormControl<string | null>(null),
    }),
    evaluation: new FormGroup({
      evaluationDate: new FormControl<string | null>(null),
    }),
  });

  private readonly personalForms = createScholarshipPersonalForms();

  public readonly personalDataForm = this.personalForms.personalDataForm;
  public readonly educationInfoForm = this.personalForms.educationInfoForm;
  public readonly educationInfoFbrForm = this.personalForms.educationInfoFbrForm;
  public readonly educationInfoFclForm = this.personalForms.educationInfoFclForm;
  public readonly workHistoryForm = this.personalForms.workHistoryForm;
  public readonly declarationForm = this.personalForms.declarationForm;
}
