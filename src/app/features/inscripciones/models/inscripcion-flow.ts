import { FormControl } from '@angular/forms';

export type InscripcionStep = 'propuesta' | 'personal' | 'confirmacion' | 'finalizada';

export interface InscripcionOption {
  value: string;
  label: string;
  icon?: string;
  hint?: string;
}

export interface InscripcionStepMetadata {
  number: 1 | 2 | 3;
  supportLabel: string;
}

export interface AcademicForm {
  proposalType: FormControl<string>;
  career: FormControl<string>;
  start: FormControl<string>;
  turno: FormControl<string>;
}

export interface PersonalInfoForm {
  secondaryStatus: FormControl<string>;
  secondaryPlace: FormControl<string>;
  previousCareer: FormControl<string>;
  motherEducation: FormControl<string>;
  fatherEducation: FormControl<string>;
  academicDecision: FormControl<string>;
  ortExperience: FormControl<string>;
  workStatus: FormControl<string>;
  identityVerification: FormControl<string>;
  acceptedRules: FormControl<boolean>;
}

export interface PaymentForm {
  paymentMethod: FormControl<string>;
}

export interface InscripcionSummaryItem {
  icon: string;
  label: string;
  value: string;
}

export interface CoordinatorContact {
  role: string;
  name: string;
  email: string;
}

export const INSCRIPCION_STEP_METADATA: Record<
  Exclude<InscripcionStep, 'finalizada'>,
  InscripcionStepMetadata
> = {
  propuesta: {
    number: 1,
    supportLabel: 'Paso 1 de 3 - Propuesta académica',
  },
  personal: {
    number: 2,
    supportLabel: 'Paso 2 de 3 - Información personal',
  },
  confirmacion: {
    number: 3,
    supportLabel: 'Paso 3 de 3 - Confirmación',
  },
};

