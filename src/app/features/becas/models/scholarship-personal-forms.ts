import { FormArray, FormControl, FormGroup, Validators } from '@angular/forms';

export type ScholarshipVariant = 'fbr' | 'fbc' | 'fcl' | 'fexaCon' | 'fexaSin';

export type ScholarshipPersonalSectionId =
  | 'datos-personales'
  | 'educacion'
  | 'educacion-fbr'
  | 'educacion-fcl'
  | 'antecedentes-laborales'
  | 'declaracion';

export interface FamilyMemberControls {
  id: FormControl<number>;
  relationship: FormControl<string | null>;
  name: FormControl<string | null>;
  age: FormControl<string | null>;
  percibeIngresos: FormControl<'si' | 'no' | null>;
  nominalIncome: FormControl<string | null>;
  liquidIncome: FormControl<string | null>;
  proofAttached: FormControl<boolean | null>;
}

export type FamilyMemberGroup = FormGroup<FamilyMemberControls>;

export function createFamilyMember(id: number): FamilyMemberGroup {
  return new FormGroup<FamilyMemberControls>({
    id: new FormControl(id, { nonNullable: true }),
    relationship: new FormControl<string | null>(null),
    name: new FormControl<string | null>(null, Validators.required),
    age: new FormControl<string | null>(null, Validators.required),
    percibeIngresos: new FormControl<'si' | 'no' | null>(null, Validators.required),
    nominalIncome: new FormControl<string | null>(null),
    liquidIncome: new FormControl<string | null>(null),
    proofAttached: new FormControl<boolean>(false),
  });
}

/**
 * Crea los `FormGroup` de la sección "Información personal" de la postulación a
 * becas. Equivale a `createInscripcionForms`: la fachada de la sección
 * (`ScholarshipPersonalFacade`) y el store (`ScholarshipFormsStore`) los leen
 * desde acá en vez de que cada componente hijo tenga su propio `FormGroup`.
 */
export function createScholarshipPersonalForms() {
  return {
    personalDataForm: new FormGroup({
      attendanceMode: new FormControl<string | null>(null, Validators.required),
    }),
    educationInfoForm: new FormGroup({
      averageSecondYear: new FormControl<number | null>(null, Validators.required),
      averageThirdYear: new FormControl<number | null>(null, Validators.required),
      certificateFile: new FormControl<boolean>(false, Validators.requiredTrue),
    }),
    educationInfoFbrForm: new FormGroup({
      schoolLocation: new FormControl<string | null>(null, Validators.required),
      lastYear: new FormControl<string | null>(null, Validators.required),
      universityLocation: new FormControl<string | null>(null, Validators.required),
      career: new FormControl<string | null>(null, Validators.required),
      approvedSubjects: new FormControl<number | null>(null, Validators.required),
      totalSubjects: new FormControl<number | null>(null, Validators.required),
      average: new FormControl<number | null>(null, Validators.required),
      averageRevalidation: new FormControl<number | null>(null, Validators.required),
      revalidationFormFile: new FormControl<boolean>(false, Validators.requiredTrue),
    }),
    educationInfoFclForm: new FormGroup({
      otherStudies: new FormControl<string | null>(null, Validators.required),
    }),
    workHistoryForm: new FormGroup({
      workHistory: new FormControl<string | null>(null, Validators.required),
    }),
    declarationForm: new FormGroup({
      familyMembers: new FormArray<FamilyMemberGroup>([createFamilyMember(1)]),
      housingType: new FormControl<string | null>(null, Validators.required),
      summerHouse: new FormControl<string | null>(null, Validators.required),
      vehicleOwn: new FormControl<string | null>(null, Validators.required),
      vehicleModel: new FormControl<string | null>(null),
      vehicleYear: new FormControl<string | null>(null),
      savingsAmount: new FormControl<number | null>(null, Validators.required),
      studyPlan: new FormControl<string | null>(null, Validators.required),
      housingExpenses: new FormControl<number | null>(null, Validators.required),
      otherServicesExpenses: new FormControl<number | null>(null, Validators.required),
      healthExpenses: new FormControl<number | null>(null, Validators.required),
      personalExpenses: new FormControl<number | null>(null, Validators.required),
      transportationExpenses: new FormControl<number | null>(null, Validators.required),
      observations: new FormControl<string | null>(null),
    }),
  };
}

export type ScholarshipPersonalForms = ReturnType<typeof createScholarshipPersonalForms>;

/**
 * Secciones del acordeón "Información personal" visibles para cada modalidad.
 * Equivale a `getSeccionesVisibles` de inscripciones.
 */
export function getVisiblePersonalSections(
  variant: ScholarshipVariant
): readonly ScholarshipPersonalSectionId[] {
  const sections: ScholarshipPersonalSectionId[] = [];

  if (variant !== 'fcl' && variant !== 'fexaSin') {
    sections.push('datos-personales');
  }

  if (variant === 'fbr') {
    sections.push('educacion-fbr');
  } else if (variant === 'fcl') {
    sections.push('educacion-fcl');
  } else {
    sections.push('educacion');
  }

  if (variant === 'fcl') {
    sections.push('antecedentes-laborales');
  }

  if (variant !== 'fexaSin') {
    sections.push('declaracion');
  }

  return sections;
}
