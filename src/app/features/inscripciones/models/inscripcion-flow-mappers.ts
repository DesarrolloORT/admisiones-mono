import type { Career } from '../../catalogs/models/catalog.interface';
import type {
  InscripcionBackendSurvey,
  InscripcionInitialSurveyResponse,
  OpcionInscripcion,
  SeccionEncuestaId,
  ValoresEncuesta,
} from './inscripcion-flow';
import type { InscripcionForms } from './inscripcion-flow-forms';
import { getOptionLabel, getProposalOptionByLevel } from './inscripcion-flow-options';

const SURVEY_SECTION_ALIASES: Readonly<Record<string, SeccionEncuestaId>> = {
  educacion: 'educacion',
  education: 'educacion',
  decision: 'decision-academica',
  decisionacademica: 'decision-academica',
  experiencia: 'experiencia-ort',
  experienciaort: 'experiencia-ort',
  identidad: 'identidad',
  verificacionidentidad: 'identidad',
  reglamento: 'reglamento',
};

export interface BackendSurveyPatchContext {
  forms: InscripcionForms;
  careers: readonly Career[];
  previousCareerOptions: readonly OpcionInscripcion[];
}

export interface InitialSurveyPayloadContext {
  forms: InscripcionForms;
  previousCareerOptions: readonly OpcionInscripcion[];
  motivesOptions: readonly OpcionInscripcion[];
}

export function patchBackendSurveyForms(
  survey: InscripcionBackendSurvey,
  response: InscripcionInitialSurveyResponse,
  context: BackendSurveyPatchContext
): string {
  const { forms } = context;
  const productId = survey.idProducto?.toString() ?? '';
  const processId = survey.idProceso?.toString() ?? '';
  const levelId =
    survey.producto?.idNivelProducto ??
    context.careers.find(career => career.idProducto === survey.idProducto)?.idNivelProducto;
  const proposalType =
    levelId === undefined
      ? forms.academicForm.controls.tipoPropuesta.value
      : (getProposalOptionByLevel(levelId)?.value ?? '');
  const hasHigherEducation = fromBackendBoolean(survey.tieneEducacionSuperiorEncuestaIni);

  forms.academicForm.patchValue(
    {
      tipoPropuesta: proposalType,
      carrera: productId,
      comienzo: processId,
    },
    { emitEvent: false }
  );
  forms.educationForm.patchValue(
    {
      cursaSecundaria:
        survey.ultimoanioSecundariaEncuestaIni === true
          ? 'cursando'
          : survey.ultimoanioSecundariaEncuestaIni === false
            ? 'no-cursando'
            : '',
      lugarSecundaria: survey.codigoInstitucionBac
        ? 'uruguay'
        : survey.nombreInstSecEncuestaIni
          ? 'exterior'
          : '',
      estadoEducacionSuperior: findHigherEducationOption(
        hasHigherEducation,
        context.previousCareerOptions
      ),
      formacionMadre: survey.instruccionMadreEncuestaIni ?? '',
      formacionPadre: survey.instruccionPadreEncuestaIni ?? '',
    },
    { emitEvent: false }
  );
  forms.academicDecisionForm.patchValue(
    {
      anioDecisionCarrera: survey.decisionCarreraEncuestaIni ?? '',
      anioDecisionOrt: survey.decisionUniverEncuestaIni ?? '',
      otrasUniversidades: toYesNoValue(survey.inforOtrasAntesEncuestaIni),
      certezaDecision:
        survey.nivelDecisionEncuestaIni === true
          ? 'decidido'
          : survey.nivelDecisionEncuestaIni === false
            ? 'con-dudas'
            : '',
      motivosOrt: response.opcionesMotivosSeleccionados?.[0]?.idMotivo?.toString() ?? '',
    },
    { emitEvent: false }
  );
  forms.ortExperienceForm.patchValue(
    {
      reunionAsesoramiento: toYesNoValue(survey.asesoramientoOrtEncuestaIni),
      visitoWeb: toYesNoValue(survey.vistaSitioWebOrtEncuestaIni),
      visitoSede: toYesNoValue(survey.vistaInstalacionesOrtEncuestaIni),
      recuerdaPublicidad: toYesNoValue(survey.publicidadOrtEncuestaIni),
    },
    { emitEvent: false }
  );

  return proposalType;
}

export function buildInitialSurveyPayload(context: InitialSurveyPayloadContext) {
  const { forms } = context;
  const decisionUniversity = toNullableNumber(forms.academicDecisionForm.controls.motivosOrt.value);
  const higherEducation = hasHigherEducation(
    forms.educationForm.controls.estadoEducacionSuperior.value,
    context.previousCareerOptions
  );

  return {
    idProducto: toNullableNumber(forms.academicForm.controls.carrera.value),
    idProceso: toNullableNumber(forms.academicForm.controls.comienzo.value),
    ultimoAnioSecundaria: toSecondaryCurrentYear(
      forms.educationForm.controls.cursaSecundaria.value
    ),
    instruccionPadre: toNullableNumber(forms.educationForm.controls.formacionPadre.value),
    instruccionMadre: toNullableNumber(forms.educationForm.controls.formacionMadre.value),
    decisionCarrera: toNullableNumber(
      forms.academicDecisionForm.controls.anioDecisionCarrera.value
    ),
    decisionUniversidad: decisionUniversity,
    infoOtrasUniversidadesAntes: toBackendYesNo(
      forms.academicDecisionForm.controls.otrasUniversidades.value
    ),
    compartidoCon: toNullableNumber(forms.academicDecisionForm.controls.apoyoDecision.value),
    tieneEducacionSuperior: higherEducation,
    nivelDecision:
      forms.academicDecisionForm.controls.certezaDecision.value === 'decidido'
        ? 1
        : forms.academicDecisionForm.controls.certezaDecision.value === 'con-dudas'
          ? 0
          : null,
    asesoramientoOrt: toNullableBoolean(
      forms.ortExperienceForm.controls.reunionAsesoramiento.value
    ),
    vistaSitioWebOrt: toNullableBoolean(forms.ortExperienceForm.controls.visitoWeb.value),
    vistaInstalacionesOrt: toNullableBoolean(forms.ortExperienceForm.controls.visitoSede.value),
    publicidadOrt: toNullableBoolean(forms.ortExperienceForm.controls.recuerdaPublicidad.value),
    trabajaActualmente: forms.workForm.controls.situacionLaboral.value || null,
    opcionesMotivosSeleccionados:
      decisionUniversity === null
        ? null
        : [
            {
              idMotivo: decisionUniversity,
              nombreMotivo: getOptionLabel(
                context.motivesOptions,
                decisionUniversity.toString(),
                ''
              ),
            },
          ],
  };
}

export function buildConfirmPreEnrollmentPayload(forms: InscripcionForms) {
  const idOfertaSeleccionada = toNullableNumber(forms.academicForm.controls.turno.value);
  if (idOfertaSeleccionada === null) return null;

  return {
    aceptoReglamento: forms.regulationForm.controls.aceptaReglamento.value,
    idOfertaSeleccionada,
  };
}

export function getSurveyValues(forms: InscripcionForms): ValoresEncuesta {
  return {
    educacion: forms.educationForm.getRawValue(),
    decisionAcademica: forms.academicDecisionForm.getRawValue(),
    experienciaOrt: forms.ortExperienceForm.getRawValue(),
    situacionLaboral: forms.workForm.getRawValue(),
  };
}

export function serializeDate(value: Date | null): string {
  if (!value) return '';

  const year = value.getFullYear();
  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function parseDate(value: string | null | undefined): Date | null {
  if (!value) return null;

  const normalized = value.trim();
  const slashMatch = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(normalized);
  if (slashMatch) {
    return createValidDate(Number(slashMatch[3]), Number(slashMatch[2]), Number(slashMatch[1]));
  }

  const isoMatch = /^(\d{4})-(\d{2})-(\d{2})/.exec(normalized);
  if (!isoMatch) return null;

  return createValidDate(Number(isoMatch[1]), Number(isoMatch[2]), Number(isoMatch[3]));
}

export function isBackendSurveyComplete(survey: InscripcionBackendSurvey): boolean {
  const state = normalizeBackendState(survey.estadoEncuestaIniAdmision);
  return (
    state.includes('complet') || state.includes('finaliz') || !!survey.fechaProcesadoEncuestaIni
  );
}

export function resolveBackendSection(value: string | null | undefined): SeccionEncuestaId | null {
  const state = normalizeBackendState(value);
  return (
    Object.entries(SURVEY_SECTION_ALIASES).find(([alias]) => state.includes(alias))?.[1] ?? null
  );
}

export function normalizeBackendState(value: string | null | undefined): string {
  return (value ?? '')
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
}

export function fromBackendBoolean(value: string | boolean | null | undefined): boolean | null {
  if (typeof value === 'boolean') return value;
  const normalized = normalizeBackendState(value);
  if (['s', 'si', 'true', '1'].includes(normalized)) return true;
  if (['n', 'no', 'false', '0'].includes(normalized)) return false;
  return null;
}

export function toYesNoValue(value: string | boolean | null | undefined): string {
  const parsed = fromBackendBoolean(value);
  return parsed === null ? '' : parsed ? 'si' : 'no';
}

export function toNullableBoolean(value: string): boolean | null {
  return value === 'si' ? true : value === 'no' ? false : null;
}

export function toBackendYesNo(value: string): string | null {
  return value === 'si' ? 'S' : value === 'no' ? 'N' : null;
}

export function toSecondaryCurrentYear(value: string): number | null {
  if (value === 'cursando') return 1;
  if (value === 'no-cursando') return 0;
  return null;
}

export function toNullableNumber(value: string): number | null {
  if (!value) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function findHigherEducationOption(
  value: boolean | null,
  options: readonly OpcionInscripcion[]
): string {
  if (value === null) return '';
  const option = options.find(item => {
    const isNegative = item.label.trim().toLowerCase().startsWith('no ');
    return value ? !isNegative : isNegative;
  });
  return option?.value ?? '';
}

function hasHigherEducation(value: string, options: readonly OpcionInscripcion[]): boolean | null {
  if (!value) return null;
  const label = getOptionLabel(options, value, '').toLowerCase();
  return label ? !label.trim().startsWith('no ') : null;
}

function createValidDate(year: number, month: number, day: number): Date | null {
  const date = new Date(year, month - 1, day);

  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
    ? date
    : null;
}
