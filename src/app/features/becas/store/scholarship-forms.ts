import { FormControl, FormGroup, Validators } from '@angular/forms';

import type { AcademicProposalForm } from '../../catalogs/models/academic-proposal';

/**
 * Crea y guarda los `FormGroup` del flujo de becas. Equivale a
 * `InscripcionFormsStore`: las fachadas leen los forms desde acá.
 *
 * Por ahora solo tiene el formulario de propuesta académica (paso 1). A medida
 * que se sumen pasos con formulario, agregalos acá igual que inscripciones suma
 * `educationForm`, `identityForm`, etc.
 */
export class ScholarshipFormsStore {
  public readonly academicForm = new FormGroup<AcademicProposalForm>({
    tipoPropuesta: new FormControl('', { nonNullable: true, validators: Validators.required }),
    carrera: new FormControl('', { nonNullable: true, validators: Validators.required }),
    comienzo: new FormControl('', { nonNullable: true, validators: Validators.required }),
    turno: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });
}
