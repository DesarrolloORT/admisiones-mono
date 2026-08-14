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
    proposalType: new FormControl('', { nonNullable: true, validators: Validators.required }),
    degreeProgram: new FormControl('', { nonNullable: true, validators: Validators.required }),
    intake: new FormControl('', { nonNullable: true, validators: Validators.required }),
    shift: new FormControl('', { nonNullable: true, validators: Validators.required }),
    seminars: new FormControl<string[]>([], { nonNullable: true }),
  });
}
