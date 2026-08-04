import { createInscripcionForms, createSectionConfig } from '../models/inscription-flow-forms';

export class InscripcionFormsStore {
  public readonly forms = createInscripcionForms();

  public readonly academicForm = this.forms.academicForm;
  public readonly educationForm = this.forms.educationForm;
  public readonly academicDecisionForm = this.forms.academicDecisionForm;
  public readonly ortExperienceForm = this.forms.ortExperienceForm;
  public readonly workForm = this.forms.workForm;
  public readonly identityForm = this.forms.identityForm;
  public readonly regulationForm = this.forms.regulationForm;
  public readonly paymentForm = this.forms.paymentForm;
  public readonly sectionConfig = createSectionConfig(this.forms);
}
