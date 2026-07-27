import { FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import type { OrtPreloadedFile } from '@desarrolloort/components';
import {
  buildFormErrorSummary,
  type FormErrorField,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';

import type { ArchivosIdentidad, MetodoPago, SeccionEncuestaId } from './inscription-flow';

export interface SectionConfig {
  label: string;
  icon: string;
  form: FormGroup;
  errorFields: FormErrorField[];
}

export type IdentityFileTarget = keyof ArchivosIdentidad;

export type IdentityPreloadedFileMap = Record<IdentityFileTarget, OrtPreloadedFile | null>;

/**
 * Regla del contrato encuesta-inicial (código INS_EI_64): para carreras de nivel 1
 * (universitarias) el año de bachillerato no puede ser 4º ni 10º.
 */
export const NIVEL_UNIVERSITARIO = 1;
export const ANIOS_BACHILLERATO_NO_UNIVERSITARIOS: readonly number[] = [4, 10];
export const BACHILLERATO_NO_UNIVERSITARIO = 'bachilleratoNoUniversitario';
export const BACHILLERATO_NO_UNIVERSITARIO_MESSAGE =
  'Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto año.';

/**
 * Bloquea años de bachillerato prohibidos cuando la carrera seleccionada es universitaria.
 * `isUniversity` lo provee el facade (cruza la carrera seleccionada contra el catálogo).
 */
export function disallowedBachilleratoForUniversity(
  isUniversity: () => boolean,
  disallowedValues: readonly number[] = ANIOS_BACHILLERATO_NO_UNIVERSITARIOS
): ValidatorFn {
  return control => {
    if (control.value === null || control.value === '' || !isUniversity()) return null;
    const value = Number(control.value);
    return disallowedValues.includes(value) ? { [BACHILLERATO_NO_UNIVERSITARIO]: true } : null;
  };
}

export function createInscripcionForms() {
  return {
    academicForm: new FormGroup({
      tipoPropuesta: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      carrera: new FormControl('', { nonNullable: true, validators: Validators.required }),
      comienzo: new FormControl('', { nonNullable: true, validators: Validators.required }),
      turno: new FormControl('', { nonNullable: true, validators: Validators.required }),
      // Solo aplica a Actualización profesional
      seminarios: new FormControl<string[]>([], { nonNullable: true }),
    }),
    educationForm: new FormGroup({
      cursaSecundaria: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      anioSecundaria: new FormControl('', { nonNullable: true }),
      orientacion: new FormControl('', { nonNullable: true }),
      recursaAnioBachillerato: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      vecesRecursaAnioBachillerato: new FormControl<number | null>(null),
      lugarSecundaria: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      departamento: new FormControl('', { nonNullable: true }),
      institucionEducativa: new FormControl('', { nonNullable: true }),
      estadoEducacionSuperior: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      universidadesEducacionSuperior: new FormControl<string[]>([], { nonNullable: true }),
      universidadEducacionSuperiorOtro: new FormControl('', { nonNullable: true }),
      formacionMadre: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      tituloOrtMadre: new FormControl('', { nonNullable: true }),
      formacionPadre: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      tituloOrtPadre: new FormControl('', { nonNullable: true }),
    }),
    academicDecisionForm: new FormGroup({
      anioDecisionCarrera: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      apoyoDecision: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      anioDecisionOrt: new FormControl('', { nonNullable: true, validators: Validators.required }),
      otrasUniversidades: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      universidadesInformadas: new FormControl<string[]>([], { nonNullable: true }),
      universidadInformadaOtro: new FormControl('', { nonNullable: true }),
      certezaDecision: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      motivosOrt: new FormControl<string[]>([], {
        nonNullable: true,
        validators: Validators.required,
      }),
    }),
    ortExperienceForm: new FormGroup({
      reunionAsesoramiento: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      calificacionAsesoramiento: new FormControl<number | null>(null),
      visitoWeb: new FormControl('', { nonNullable: true, validators: Validators.required }),
      calificacionWeb: new FormControl<number | null>(null),
      visitoSede: new FormControl('', { nonNullable: true, validators: Validators.required }),
      calificacionSede: new FormControl<number | null>(null),
      recuerdaPublicidad: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      mediosPublicidad: new FormControl<string[]>([], { nonNullable: true }),
    }),
    workForm: new FormGroup({
      isCorporate: new FormControl<boolean | null>(null),
      situacionLaboral: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      tipoJornadaLaboral: new FormControl('', { nonNullable: true }),
    }),
    identityForm: new FormGroup({
      vencimientoDocumento: new FormControl<Date | null>(null, Validators.required),
      identidadCorrecta: new FormControl(false, { nonNullable: true }),
    }),
    regulationForm: new FormGroup({
      aceptaReglamento: new FormControl(false, {
        nonNullable: true,
        validators: Validators.requiredTrue,
      }),
    }),
    paymentForm: new FormGroup({
      metodoPago: new FormControl<MetodoPago | ''>('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      banco: new FormControl('', { nonNullable: true }),
    }),
  };
}

export type InscripcionForms = ReturnType<typeof createInscripcionForms>;

export function createSectionConfig(
  forms: InscripcionForms
): Record<SeccionEncuestaId, SectionConfig> {
  return {
    educacion: {
      label: 'Educación',
      icon: 'menu_book',
      form: forms.educationForm,
      errorFields: [
        { controlName: 'cursaSecundaria', fieldId: '', label: 'Situación de secundaria' },
        {
          controlName: 'anioSecundaria',
          fieldId: '',
          label: 'Año en curso',
          messages: { [BACHILLERATO_NO_UNIVERSITARIO]: BACHILLERATO_NO_UNIVERSITARIO_MESSAGE },
        },
        { controlName: 'orientacion', fieldId: '', label: 'Orientación' },
        { controlName: 'recursaAnioBachillerato', fieldId: '', label: 'Recursado de bachillerato' },
        { controlName: 'vecesRecursaAnioBachillerato', fieldId: '', label: 'Veces de recursado' },
        { controlName: 'lugarSecundaria', fieldId: '', label: 'Lugar de secundaria' },
        { controlName: 'departamento', fieldId: '', label: 'Departamento' },
        { controlName: 'institucionEducativa', fieldId: '', label: 'Institución educativa' },
        {
          controlName: 'estadoEducacionSuperior',
          fieldId: '',
          label: 'Estado de educación superior',
        },
        {
          controlName: 'universidadesEducacionSuperior',
          fieldId: '',
          label: 'Universidades de educación superior',
        },
        {
          controlName: 'universidadEducacionSuperiorOtro',
          fieldId: '',
          label: 'Otra universidad de educación superior',
        },
        { controlName: 'formacionMadre', fieldId: '', label: 'Formación de madre o tutor' },
        { controlName: 'tituloOrtMadre', fieldId: '', label: 'Título en ORT de madre o tutor' },
        { controlName: 'formacionPadre', fieldId: '', label: 'Formación de padre o tutor' },
        { controlName: 'tituloOrtPadre', fieldId: '', label: 'Título en ORT de padre o tutor' },
      ],
    },
    'decision-academica': {
      label: 'Decisión académica',
      icon: 'schema',
      form: forms.academicDecisionForm,
      errorFields: [
        { controlName: 'anioDecisionCarrera', fieldId: '', label: 'Año de decisión de carrera' },
        { controlName: 'apoyoDecision', fieldId: '', label: 'Apoyo en la decisión' },
        { controlName: 'anioDecisionOrt', fieldId: '', label: 'Año de decisión de ORT' },
        { controlName: 'otrasUniversidades', fieldId: '', label: 'Otras universidades' },
        {
          controlName: 'universidadesInformadas',
          fieldId: '',
          label: 'Universidades consultadas',
        },
        {
          controlName: 'universidadInformadaOtro',
          fieldId: '',
          label: 'Otra universidad consultada',
        },
        { controlName: 'certezaDecision', fieldId: '', label: 'Certeza de la decisión' },
        { controlName: 'motivosOrt', fieldId: '', label: 'Motivos para elegir ORT' },
      ],
    },
    'experiencia-ort': {
      label: 'Experiencia con ORT',
      icon: 'domain',
      form: forms.ortExperienceForm,
      errorFields: [
        {
          controlName: 'reunionAsesoramiento',
          fieldId: '',
          label: 'Reunión de asesoramiento',
        },
        {
          controlName: 'calificacionAsesoramiento',
          fieldId: '',
          label: 'Calificación del asesoramiento',
        },
        { controlName: 'visitoWeb', fieldId: '', label: 'Visita al sitio web' },
        { controlName: 'calificacionWeb', fieldId: '', label: 'Calificación del sitio web' },
        { controlName: 'visitoSede', fieldId: '', label: 'Visita a instalaciones' },
        { controlName: 'calificacionSede', fieldId: '', label: 'Calificación de instalaciones' },
        { controlName: 'recuerdaPublicidad', fieldId: '', label: 'Publicidad de ORT' },
        { controlName: 'mediosPublicidad', fieldId: '', label: 'Origen de la publicidad' },
      ],
    },
    'situacion-laboral': {
      label: 'Situación laboral',
      icon: 'business_center',
      form: forms.workForm,
      errorFields: [
        { controlName: 'isCorporate', fieldId: '', label: 'Titular de la inscripción' },
        { controlName: 'situacionLaboral', fieldId: '', label: 'Situación laboral' },
        { controlName: 'tipoJornadaLaboral', fieldId: '', label: 'Tipo de jornada laboral' },
      ],
    },
    identidad: {
      label: 'Verificación de identidad',
      icon: 'verified',
      form: forms.identityForm,
      errorFields: [
        {
          controlName: 'vencimientoDocumento',
          fieldId: '',
          label: 'Vencimiento del documento',
        },
        {
          controlName: 'identidadCorrecta',
          fieldId: '',
          label: 'Verificación de identidad correcta',
        },
      ],
    },
    reglamento: {
      label: 'Reglamento estudiantil',
      icon: 'article',
      form: forms.regulationForm,
      errorFields: [
        { controlName: 'aceptaReglamento', fieldId: '', label: 'Aceptación del reglamento' },
      ],
    },
  };
}

export function buildFormErrors(form: FormGroup, fields: FormErrorField[]) {
  return buildFormErrorSummary(form, fields, ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED);
}
