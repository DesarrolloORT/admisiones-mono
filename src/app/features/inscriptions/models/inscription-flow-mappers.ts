import { getAcademicProposalTypeByLevel } from '../../catalogs/models/academic-proposal';
import type { Career } from '../../catalogs/models/catalog.interface';
import type {
  InscripcionInitialSurvey,
  InscripcionInitialSurveyResponse,
  InscripcionPaymentPayload,
  MetodoPago,
  MetodoPagoApi,
} from './inscription-flow';
import type { InscripcionForms } from './inscription-flow-forms';

const SCHOOL_PLACE_NATIONAL = '1';
const SCHOOL_PLACE_INTERNATIONAL = '2';
const OTHER_OPTION_VALUE = '0';

export interface BackendSurveyPatchContext {
  forms: InscripcionForms;
  careers: readonly Career[];
  /**
   * Si es `false`, NO se patchea la selección académica desde una encuesta previa.
   * Se usa en `nueva`, donde el Paso 1 debe quedar virgen.
   */
  includeAcademicSelection?: boolean;
}

export function patchBackendSurveyForms(
  survey: InscripcionInitialSurvey,
  response: InscripcionInitialSurveyResponse,
  context: BackendSurveyPatchContext
): string {
  const { forms } = context;
  const productId = toFormValue(survey.carreraId);
  const processId = toFormValue(survey.comienzoId);
  const levelId =
    survey.nivelProductoId ??
    context.careers.find(career => career.idProducto === survey.carreraId)?.idNivelProducto;
  const proposalType =
    levelId === undefined
      ? forms.academicForm.controls.tipoPropuesta.value
      : (getAcademicProposalTypeByLevel(levelId)?.value ?? '');
  const schoolInstitution =
    survey.institucionSecundariaId ?? survey.nombreInstitucionSecundaria ?? '';

  if (context.includeAcademicSelection !== false) {
    forms.academicForm.patchValue(
      {
        tipoPropuesta: proposalType,
        carrera: productId,
        comienzo: processId,
      },
      { emitEvent: false }
    );
  }
  forms.educationForm.patchValue(
    {
      cursaSecundaria:
        survey.cursaSecundaria === true
          ? 'cursando'
          : survey.cursaSecundaria === false
            ? 'no-cursando'
            : '',
      anioSecundaria: toFormValue(survey.anioBachilleratoId),
      orientacion: toFormValue(survey.orientacionBachilleratoId),
      recursaAnioBachillerato: toYesNoValue(survey.recursaAnioBachillerato),
      vecesRecursaAnioBachillerato: survey.vecesRecursaAnioBachillerato,
      lugarSecundaria: getSchoolPlaceValue(survey),
      institucionEducativa: schoolInstitution.toString(),
      estadoEducacionSuperior: toFormValue(survey.estadoEducacionSuperiorPreviaId),
      universidadesEducacionSuperior: toSelectedOptionValues(
        response.universidadesEducacionSuperior
      ),
      universidadEducacionSuperiorOtro: toFirstText(response.universidadesEducacionSuperiorOtros),
      formacionMadre: toFormValue(survey.nivelFormacionMadreId),
      tituloOrtMadre: toYesNoValue(survey.madreEgresadaOrt),
      formacionPadre: toFormValue(survey.nivelFormacionPadreId),
      tituloOrtPadre: toYesNoValue(survey.padreEgresadoOrt),
    },
    { emitEvent: false }
  );
  forms.academicDecisionForm.patchValue(
    {
      anioDecisionCarrera: toFormValue(survey.anioDecisionCarreraId),
      apoyoDecision: toFormValue(survey.apoyoDecisionId),
      anioDecisionOrt: toFormValue(survey.anioDecisionOrtId),
      otrasUniversidades: toYesNoValue(survey.seInformoEnOtrasUniversidades),
      universidadesInformadas: toSelectedOptionValues(response.universidadesConsideradas),
      universidadInformadaOtro: toFirstText(response.universidadesConsideradasOtros),
      certezaDecision: toFormValue(survey.nivelDecisionId),
      motivosOrt: toSelectedOptionValues(response.opcionesMotivosSeleccionados),
    },
    { emitEvent: false }
  );
  forms.ortExperienceForm.patchValue(
    {
      reunionAsesoramiento: toYesNoValue(survey.tuvoAsesoramientoOrt),
      calificacionAsesoramiento: survey.valoracionAsesoramientoOrt,
      visitoWeb: toYesNoValue(survey.visitoSitioWebOrt),
      calificacionWeb: survey.valoracionSitioWebOrt,
      visitoSede: toYesNoValue(survey.visitoInstalacionesOrt),
      calificacionSede: survey.valoracionInstalacionesOrt,
      recuerdaPublicidad: toYesNoValue(survey.recuerdaPublicidadOrt),
      mediosPublicidad: toSelectedOptionValues(response.opcionesPublicidadSeleccionadas),
    },
    { emitEvent: false }
  );

  return proposalType;
}

export function buildInitialSurveyPayload(forms: InscripcionForms) {
  const education = forms.educationForm.controls;
  const decision = forms.academicDecisionForm.controls;
  const experience = forms.ortExperienceForm.controls;
  const work = forms.workForm.controls;
  const currentlyInSchool = education.cursaSecundaria.value === 'cursando';
  const recursedBaccalaureate = education.recursaAnioBachillerato.value === 'si';
  const nationalSchoolPlace = education.lugarSecundaria.value === SCHOOL_PLACE_NATIONAL;
  const motherHasCompleteUniversity = hasCompleteUniversityEducation(
    education.formacionMadre.value
  );
  const fatherHasCompleteUniversity = hasCompleteUniversityEducation(
    education.formacionPadre.value
  );
  const works = work.situacionLaboral.value === 'trabaja';
  const informedOtherUniversities = decision.otrasUniversidades.value === 'si';
  const hasPreviousHigherEducation = education.estadoEducacionSuperior.value === '1';
  const remembersAdvertising = experience.recuerdaPublicidad.value === 'si';

  return {
    carreraId: toNullableNumber(forms.academicForm.controls.carrera.value),
    comienzoId: toNullableNumber(forms.academicForm.controls.comienzo.value),
    orientacionBachilleratoId: currentlyInSchool
      ? toNullableNumber(education.orientacion.value)
      : null,
    anioBachillerato: currentlyInSchool ? toNullableNumber(education.anioSecundaria.value) : null,
    cursaSecundariaActualmente: currentlyInSchool,
    vecesRecursaAnioBachillerato: recursedBaccalaureate
      ? education.vecesRecursaAnioBachillerato.value
      : null,
    recursaAnioBachillerato: toNullableBoolean(education.recursaAnioBachillerato.value),
    nivelFormacionPadreTutorId: toNullableNumber(education.formacionPadre.value),
    nivelFormacionMadreTutorId: toNullableNumber(education.formacionMadre.value),
    anioDecisionCarreraId: toNullableNumber(decision.anioDecisionCarrera.value),
    anioDecisionOrtId: toNullableNumber(decision.anioDecisionOrt.value),
    seInformoEnOtrasUniversidades: toNullableBoolean(decision.otrasUniversidades.value),
    informacionOtrasUniversidadesLinea1: null,
    informacionOtrasUniversidadesLinea2: null,
    apoyoDecisionId: toNullableNumber(decision.apoyoDecision.value),
    institucionSecundariaId: nationalSchoolPlace
      ? toNullableNumber(education.institucionEducativa.value)
      : null,
    nombreInstitucionSecundaria: nationalSchoolPlace
      ? null
      : toNullableText(education.institucionEducativa.value),
    ubicacionUltimoAnioSecundariaId: toNullableNumber(education.lugarSecundaria.value),
    estadoEducacionSuperiorPreviaId: toNullableNumber(education.estadoEducacionSuperior.value),
    nivelDecisionId: toNullableNumber(decision.certezaDecision.value),
    tuvoAsesoramientoOrt: toNullableBoolean(experience.reunionAsesoramiento.value),
    valoracionAsesoramientoOrtId:
      experience.reunionAsesoramiento.value === 'si'
        ? experience.calificacionAsesoramiento.value
        : null,
    visitoSitioWebOrt: toNullableBoolean(experience.visitoWeb.value),
    valoracionSitioWebOrtId:
      experience.visitoWeb.value === 'si' ? experience.calificacionWeb.value : null,
    visitoInstalacionesOrt: toNullableBoolean(experience.visitoSede.value),
    valoracionInstalacionesOrtId:
      experience.visitoSede.value === 'si' ? experience.calificacionSede.value : null,
    recuerdaPublicidadOrt: toNullableBoolean(experience.recuerdaPublicidad.value),
    madreTutorEgresadoOrt: motherHasCompleteUniversity
      ? toNullableBoolean(education.tituloOrtMadre.value)
      : null,
    padreTutorEgresadoOrt: fatherHasCompleteUniversity
      ? toNullableBoolean(education.tituloOrtPadre.value)
      : null,
    trabajaActualmente: toWorkStatusFlag(work.situacionLaboral.value),
    tipoJornadaId: works ? toNullableNumber(work.tipoJornadaLaboral.value) : null,
    universidadConsideradaIds: informedOtherUniversities
      ? toNumberArray(decision.universidadesInformadas.value)
      : null,
    universidadConsideradaOtros:
      informedOtherUniversities && hasOtherOption(decision.universidadesInformadas.value)
        ? toSingleTextArray(decision.universidadInformadaOtro.value)
        : null,
    universidadEducacionSuperiorIds: hasPreviousHigherEducation
      ? toNumberArray(education.universidadesEducacionSuperior.value)
      : null,
    universidadEducacionSuperiorOtros:
      hasPreviousHigherEducation && hasOtherOption(education.universidadesEducacionSuperior.value)
        ? toSingleTextArray(education.universidadEducacionSuperiorOtro.value)
        : null,
    publicidadOrtIds: remembersAdvertising
      ? toNumberArray(experience.mediosPublicidad.value)
      : null,
    motivoEleccionOrtIds: toNumberArray(decision.motivosOrt.value),
  };
}

export function buildConfirmPreEnrollmentPayload(
  forms: InscripcionForms,
  actualizacionProfesional = false
) {
  const idOfertasSeleccionadas = actualizacionProfesional
    ? (toNumberArray(forms.academicForm.controls.seminarios.value) ?? [])
    : [toNullableNumber(forms.academicForm.controls.turno.value)].filter(
        (oferta): oferta is number => oferta !== null
      );
  if (idOfertasSeleccionadas.length === 0) return null;

  return {
    aceptoReglamento: forms.regulationForm.controls.aceptaReglamento.value,
    esInscripcionCorporativa:
      actualizacionProfesional && forms.workForm.controls.isCorporate.value === true,
    idOfertasSeleccionadas,
  };
}

export function buildPaymentPayload(payload: InscripcionPaymentPayload) {
  return {
    idInscripto: payload.idInscripcion,
    tipoPago: toApiPaymentMethod(payload.metodoPago),
    idBancoSistarbanc: payload.metodoPago === 'cuenta-bancaria' ? payload.idBancoSistarbanc : null,
  };
}

function toApiPaymentMethod(method: MetodoPago): MetodoPagoApi {
  switch (method) {
    case 'cuenta-personal':
      return 'CUENTA_PERSONAL';
    case 'abitab':
      return 'ABITAB';
    case 'paganza':
      return 'PAGANZA';
    case 'banred':
      return 'BANRED';
    case 'geopay':
      return 'GEOPAY';
    case 'cuenta-bancaria':
      return 'SISTARBANC';
  }
}

// Inverso de toApiPaymentMethod: el bloque seniaMinima trae el método ya elegido
// como string de API (p.ej. ABITAB/PAGANZA). Lo mapeamos al MetodoPago interno para
// reutilizar la pantalla de referencias de pago. Un valor desconocido devuelve null.
export function fromApiPaymentMethod(value: string | null): MetodoPago | null {
  switch (value) {
    case 'CUENTA_PERSONAL':
      return 'cuenta-personal';
    case 'ABITAB':
      return 'abitab';
    case 'PAGANZA':
      return 'paganza';
    case 'BANRED':
      return 'banred';
    case 'GEOPAY':
      return 'geopay';
    case 'SISTARBANC':
      return 'cuenta-bancaria';
    default:
      return null;
  }
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

export function toYesNoValue(value: boolean | null): string {
  return value === null ? '' : value ? 'si' : 'no';
}

export function toNullableBoolean(value: string): boolean | null {
  return value === 'si' ? true : value === 'no' ? false : null;
}

export function toWorkStatusFlag(value: string): boolean | null {
  return value ? value === 'trabaja' : null;
}

export function toNullableNumber(value: string): number | null {
  if (!value) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function getSchoolPlaceValue(survey: InscripcionInitialSurvey): string {
  if (survey.ubicacionSecundariaId) return survey.ubicacionSecundariaId.toString();
  if (survey.institucionSecundariaId) return SCHOOL_PLACE_NATIONAL;
  if (survey.nombreInstitucionSecundaria) return SCHOOL_PLACE_INTERNATIONAL;
  return '';
}

export function hasCompleteUniversityEducation(value: string): boolean {
  return value === '5' || value === '6';
}

function toNullableText(value: string): string | null {
  const trimmed = value.trim();
  return trimmed || null;
}

function toNumberArray(values: readonly string[]): number[] | null {
  const numbers = values.flatMap(value => {
    const parsed = toNullableNumber(value);
    return parsed === null ? [] : [parsed];
  });
  return numbers.length > 0 ? numbers : null;
}

function toFirstText(values: readonly string[]): string {
  return values[0] ?? '';
}

function toSingleTextArray(value: string): string[] | null {
  const trimmed = value.trim();
  return trimmed ? [trimmed] : null;
}

function hasOtherOption(values: readonly string[]): boolean {
  return values.includes(OTHER_OPTION_VALUE);
}

function toSelectedOptionValues(ids: readonly number[]): string[] {
  return ids.map(String);
}

function toFormValue(value: number | null): string {
  return value?.toString() ?? '';
}

function createValidDate(year: number, month: number, day: number): Date | null {
  const date = new Date(year, month - 1, day);

  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
    ? date
    : null;
}
