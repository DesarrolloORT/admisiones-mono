import { FormControl, FormGroup, Validators } from '@angular/forms';
import type { OrtPreloadedFile } from '@desarrolloort/components';
import {
  buildFormErrorSummary,
  type FormErrorField,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';

import type {
  ArchivosIdentidad,
  FormularioDecisionAcademica,
  FormularioEducacion,
  FormularioExperienciaOrt,
  FormularioIdentidad,
  FormularioPago,
  FormularioPropuesta,
  FormularioReglamento,
  FormularioSituacionLaboral,
  MetodoPago,
  SeccionEncuestaId,
} from './inscripcion-flow';

export interface InscripcionForms {
  academicForm: FormGroup<FormularioPropuesta>;
  educationForm: FormGroup<FormularioEducacion>;
  academicDecisionForm: FormGroup<FormularioDecisionAcademica>;
  ortExperienceForm: FormGroup<FormularioExperienciaOrt>;
  workForm: FormGroup<FormularioSituacionLaboral>;
  identityForm: FormGroup<FormularioIdentidad>;
  regulationForm: FormGroup<FormularioReglamento>;
  paymentForm: FormGroup<FormularioPago>;
}

export interface SectionConfig {
  label: string;
  icon: string;
  form: FormGroup;
  errorFields: FormErrorField[];
}

export type IdentityFileTarget = keyof ArchivosIdentidad;

export type IdentityPreloadedFileMap = Record<IdentityFileTarget, OrtPreloadedFile | null>;

export function createInscripcionForms(): InscripcionForms {
  return {
    academicForm: new FormGroup<FormularioPropuesta>({
      tipoPropuesta: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      carrera: new FormControl('', { nonNullable: true, validators: Validators.required }),
      comienzo: new FormControl('', { nonNullable: true, validators: Validators.required }),
      turno: new FormControl('', { nonNullable: true, validators: Validators.required }),
    }),
    educationForm: new FormGroup<FormularioEducacion>({
      cursaSecundaria: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      anioSecundaria: new FormControl('', { nonNullable: true }),
      tipoBachillerato: new FormControl('', { nonNullable: true }),
      orientacion: new FormControl('', { nonNullable: true }),
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
    academicDecisionForm: new FormGroup<FormularioDecisionAcademica>({
      anioDecisionCarrera: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      apoyoDecision: new FormControl<string[]>([], {
        nonNullable: true,
        validators: Validators.required,
      }),
      anioDecisionOrt: new FormControl('', { nonNullable: true, validators: Validators.required }),
      otrasUniversidades: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      universidadesInformadas: new FormControl<string[]>([], { nonNullable: true }),
      certezaDecision: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      motivosOrt: new FormControl<string[]>([], {
        nonNullable: true,
        validators: Validators.required,
      }),
    }),
    ortExperienceForm: new FormGroup<FormularioExperienciaOrt>({
      reunionAsesoramiento: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      calificacionAsesoramiento: new FormControl<number | null>(null),
      visitoWeb: new FormControl('', { nonNullable: true, validators: Validators.required }),
      calificacionWeb: new FormControl<number | null>(null),
      visitoSede: new FormControl('', { nonNullable: true, validators: Validators.required }),
      recuerdaPublicidad: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      mediosPublicidad: new FormControl<string[]>([], { nonNullable: true }),
    }),
    workForm: new FormGroup<FormularioSituacionLaboral>({
      situacionLaboral: new FormControl('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      tipoJornadaLaboral: new FormControl('', { nonNullable: true }),
    }),
    identityForm: new FormGroup<FormularioIdentidad>({
      vencimientoDocumento: new FormControl<Date | null>(null, Validators.required),
    }),
    regulationForm: new FormGroup<FormularioReglamento>({
      aceptaReglamento: new FormControl(false, {
        nonNullable: true,
        validators: Validators.requiredTrue,
      }),
    }),
    paymentForm: new FormGroup<FormularioPago>({
      metodoPago: new FormControl<MetodoPago | ''>('', {
        nonNullable: true,
        validators: Validators.required,
      }),
      banco: new FormControl('', { nonNullable: true }),
    }),
  };
}

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
        { controlName: 'anioSecundaria', fieldId: '', label: 'Año en curso' },
        { controlName: 'tipoBachillerato', fieldId: '', label: 'Tipo de bachillerato' },
        { controlName: 'orientacion', fieldId: '', label: 'Orientación' },
        { controlName: 'lugarSecundaria', fieldId: '', label: 'Lugar de secundaria' },
        { controlName: 'departamento', fieldId: '', label: 'Departamento' },
        { controlName: 'institucionEducativa', fieldId: '', label: 'Institución educativa' },
        {
          controlName: 'estadoEducacionSuperior',
          fieldId: '',
          label: 'Estado de educación superior',
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
        { controlName: 'recuerdaPublicidad', fieldId: '', label: 'Publicidad de ORT' },
        { controlName: 'mediosPublicidad', fieldId: '', label: 'Origen de la publicidad' },
      ],
    },
    'situacion-laboral': {
      label: 'Situación laboral',
      icon: 'business_center',
      form: forms.workForm,
      errorFields: [
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
