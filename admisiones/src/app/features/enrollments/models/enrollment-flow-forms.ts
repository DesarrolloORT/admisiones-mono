import { InjectionToken } from '@angular/core';
import { FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import type { OrtPreloadedFile } from '@desarrolloort/components';
import {
  buildFormErrorSummary,
  type FormErrorField,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';

import type { IdentityFiles, PaymentMethod, SurveySectionId } from './enrollment-flow';

export interface SectionConfig {
  label: string;
  icon: string;
  form: FormGroup;
  errorFields: FormErrorField[];
}

export type IdentityFileTarget = keyof IdentityFiles;

export type IdentityPreloadedFileMap = Record<IdentityFileTarget, OrtPreloadedFile | null>;

/**
 * Regla del contrato encuesta-inicial (código INS_EI_64): para carreras de nivel 1
 * (universitarias) el año de bachillerato no puede ser 4º ni 10º.
 */
export const UNIVERSITY_LEVEL = 1;
export const NON_UNIVERSITY_HIGH_SCHOOL_YEARS: readonly number[] = [4, 10];
export const NON_UNIVERSITY_HIGH_SCHOOL_YEAR_ERROR = 'nonUniversityHighSchoolYear';
export const NON_UNIVERSITY_HIGH_SCHOOL_YEAR_MESSAGE =
  'Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto año.';

/**
 * Bloquea años de bachillerato prohibidos cuando la carrera seleccionada es universitaria.
 * `isUniversity` lo provee la fachada (cruza la carrera seleccionada contra el catálogo).
 */
export function disallowedHighSchoolYearForUniversity(
  isUniversity: () => boolean,
  disallowedValues: readonly number[] = NON_UNIVERSITY_HIGH_SCHOOL_YEARS
): ValidatorFn {
  return control => {
    if (control.value === null || control.value === '' || !isUniversity()) return null;
    const value = Number(control.value);
    return disallowedValues.includes(value)
      ? { [NON_UNIVERSITY_HIGH_SCHOOL_YEAR_ERROR]: true }
      : null;
  };
}

export function createEnrollmentForms() {
  return {
    academicForm: new FormGroup({
      proposalType: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      degreeProgram: new FormControl('', { nonNullable: true, validators: Validators.required }),
      intake: new FormControl('', { nonNullable: true, validators: Validators.required }),
      shift: new FormControl('', { nonNullable: true, validators: Validators.required }),
      // Solo aplica a Actualización profesional
      seminars: new FormControl<string[]>([], { nonNullable: true }),
    }),
    educationForm: new FormGroup({
      studiesHighSchool: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      highSchoolYear: new FormControl('', { nonNullable: true }),
      orientation: new FormControl('', { nonNullable: true }),
      repeatsHighSchoolYear: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      // Los campos de escritura libre de la encuesta (este, `educationalInstitution` en el
      // exterior, `otherHigherEducationUniversity` y `otherResearchedUniversity`) actualizan al
      // salir del campo: el guardado se dispara con la seccion completa, y por tecla mandaria
      // un POST por letra. El check y el error de estos campos tambien llegan al blur.
      highSchoolYearRepeatCount: new FormControl<number | null>(null, { updateOn: 'blur' }),
      highSchoolLocation: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      state: new FormControl('', { nonNullable: true }),
      educationalInstitution: new FormControl('', { nonNullable: true, updateOn: 'blur' }),
      higherEducationStatus: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      higherEducationUniversities: new FormControl<string[]>([], { nonNullable: true }),
      otherHigherEducationUniversity: new FormControl('', {
        nonNullable: true,
        updateOn: 'blur',
      }),
      motherEducation: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      motherOrtDegree: new FormControl('', { nonNullable: true }),
      fatherEducation: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      fatherOrtDegree: new FormControl('', { nonNullable: true }),
    }),
    academicDecisionForm: new FormGroup({
      degreeProgramDecisionYear: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      decisionSupport: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      ortDecisionYear: new FormControl('', { nonNullable: true, validators: Validators.required }),
      otherUniversities: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      researchedUniversities: new FormControl<string[]>([], { nonNullable: true }),
      otherResearchedUniversity: new FormControl('', { nonNullable: true, updateOn: 'blur' }),
      decisionCertainty: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      ortReasons: new FormControl<string[]>([], {
        nonNullable: true,
        validators: Validators.required,
      }),
    }),
    ortExperienceForm: new FormGroup({
      advisingMeeting: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      advisingRating: new FormControl<number | null>(null),
      visitedWebsite: new FormControl('', { nonNullable: true, validators: Validators.required }),
      websiteRating: new FormControl<number | null>(null),
      visitedCampus: new FormControl('', { nonNullable: true, validators: Validators.required }),
      campusRating: new FormControl<number | null>(null),
      recallsAdvertising: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      advertisingChannels: new FormControl<string[]>([], { nonNullable: true }),
    }),
    workForm: new FormGroup({
      isCorporate: new FormControl<boolean | null>(null),
    }),
    identityForm: new FormGroup({
      documentExpiration: new FormControl<Date | null>(null, Validators.required),
      isIdentityCorrect: new FormControl(false, { nonNullable: true }),
    }),
    regulationForm: new FormGroup({
      acceptsRegulation: new FormControl(false, {
        nonNullable: true,
        validators: Validators.requiredTrue,
      }),
    }),
    paymentForm: new FormGroup({
      paymentMethod: new FormControl<PaymentMethod | ''>('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      bank: new FormControl('', { nonNullable: true }),
    }),
  };
}

export type EnrollmentForms = ReturnType<typeof createEnrollmentForms>;

export function createSectionConfig(
  forms: EnrollmentForms
): Record<SurveySectionId, SectionConfig> {
  return {
    education: {
      label: 'Educación',
      icon: 'menu_book',
      form: forms.educationForm,
      errorFields: [
        { controlName: 'studiesHighSchool', fieldId: '', label: 'Situación de secundaria' },
        {
          controlName: 'highSchoolYear',
          fieldId: '',
          label: 'Año en curso',
          messages: {
            [NON_UNIVERSITY_HIGH_SCHOOL_YEAR_ERROR]: NON_UNIVERSITY_HIGH_SCHOOL_YEAR_MESSAGE,
          },
        },
        { controlName: 'orientation', fieldId: '', label: 'Orientación' },
        { controlName: 'repeatsHighSchoolYear', fieldId: '', label: 'Recursado de bachillerato' },
        { controlName: 'highSchoolYearRepeatCount', fieldId: '', label: 'Veces de recursado' },
        { controlName: 'highSchoolLocation', fieldId: '', label: 'Lugar de secundaria' },
        { controlName: 'state', fieldId: '', label: 'Departamento' },
        { controlName: 'educationalInstitution', fieldId: '', label: 'Institución educativa' },
        {
          controlName: 'higherEducationStatus',
          fieldId: '',
          label: 'Estado de educación superior',
        },
        {
          controlName: 'higherEducationUniversities',
          fieldId: '',
          label: 'Universidades de educación superior',
        },
        {
          controlName: 'otherHigherEducationUniversity',
          fieldId: '',
          label: 'Otra universidad de educación superior',
        },
        { controlName: 'motherEducation', fieldId: '', label: 'Formación de madre o tutor' },
        { controlName: 'motherOrtDegree', fieldId: '', label: 'Título en ORT de madre o tutor' },
        { controlName: 'fatherEducation', fieldId: '', label: 'Formación de padre o tutor' },
        { controlName: 'fatherOrtDegree', fieldId: '', label: 'Título en ORT de padre o tutor' },
      ],
    },
    'academic-decision': {
      label: 'Decisión académica',
      icon: 'schema',
      form: forms.academicDecisionForm,
      errorFields: [
        {
          controlName: 'degreeProgramDecisionYear',
          fieldId: '',
          label: 'Año de decisión de carrera',
        },
        { controlName: 'decisionSupport', fieldId: '', label: 'Apoyo en la decisión' },
        { controlName: 'ortDecisionYear', fieldId: '', label: 'Año de decisión de ORT' },
        { controlName: 'otherUniversities', fieldId: '', label: 'Otras universidades' },
        {
          controlName: 'researchedUniversities',
          fieldId: '',
          label: 'Universidades consultadas',
        },
        {
          controlName: 'otherResearchedUniversity',
          fieldId: '',
          label: 'Otra universidad consultada',
        },
        { controlName: 'decisionCertainty', fieldId: '', label: 'Certeza de la decisión' },
        { controlName: 'ortReasons', fieldId: '', label: 'Motivos para elegir ORT' },
      ],
    },
    'ort-experience': {
      label: 'Experiencia con ORT',
      icon: 'domain',
      form: forms.ortExperienceForm,
      errorFields: [
        {
          controlName: 'advisingMeeting',
          fieldId: '',
          label: 'Reunión de asesoramiento',
        },
        {
          controlName: 'advisingRating',
          fieldId: '',
          label: 'Calificación del asesoramiento',
        },
        { controlName: 'visitedWebsite', fieldId: '', label: 'Visita al sitio web' },
        { controlName: 'websiteRating', fieldId: '', label: 'Calificación del sitio web' },
        { controlName: 'visitedCampus', fieldId: '', label: 'Visita a instalaciones' },
        { controlName: 'campusRating', fieldId: '', label: 'Calificación de instalaciones' },
        { controlName: 'recallsAdvertising', fieldId: '', label: 'Publicidad de ORT' },
        { controlName: 'advertisingChannels', fieldId: '', label: 'Origen de la publicidad' },
      ],
    },
    'work-situation': {
      label: 'Situación laboral',
      icon: 'business_center',
      form: forms.workForm,
      errorFields: [
        { controlName: 'isCorporate', fieldId: '', label: 'Titular de la inscripción' },
      ],
    },
    identity: {
      label: 'Verificación de identidad',
      icon: 'verified',
      form: forms.identityForm,
      errorFields: [
        {
          controlName: 'documentExpiration',
          fieldId: '',
          label: 'Vencimiento del documento',
        },
        {
          controlName: 'isIdentityCorrect',
          fieldId: '',
          label: 'Verificación de identidad correcta',
        },
      ],
    },
    regulation: {
      label: 'Reglamento estudiantil',
      icon: 'article',
      form: forms.regulationForm,
      errorFields: [
        { controlName: 'acceptsRegulation', fieldId: '', label: 'Aceptación del reglamento' },
      ],
    },
  };
}

export function buildFormErrors(form: FormGroup, fields: FormErrorField[]) {
  return buildFormErrorSummary(form, fields, ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED);
}

export interface EnrollmentFormsState {
  forms: EnrollmentForms;
  sectionConfig: Record<SurveySectionId, SectionConfig>;
}

/**
 * Crea los forms del flujo junto con la configuración de secciones que los
 * recorre. Es el estado de formularios que comparten las fachadas del paso a
 * paso: se provee a nivel de la página (ver `ENROLLMENT_FORMS`), así que cada
 * inscripción tiene el suyo.
 */
export function createEnrollmentFormsState(): EnrollmentFormsState {
  const forms = createEnrollmentForms();

  return { forms, sectionConfig: createSectionConfig(forms) };
}

export const ENROLLMENT_FORMS = new InjectionToken<EnrollmentFormsState>('ENROLLMENT_FORMS', {
  factory: createEnrollmentFormsState,
});
