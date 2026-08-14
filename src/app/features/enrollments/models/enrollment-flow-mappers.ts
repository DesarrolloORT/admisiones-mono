import { getAcademicProposalTypeByLevel } from '../../catalogs/models/academic-proposal';
import type { DegreeProgram } from '../../catalogs/models/catalog.interface';
import type {
  ApiPaymentMethod,
  EnrollmentInitialSurvey,
  EnrollmentInitialSurveyResponse,
  EnrollmentPaymentPayload,
  PaymentMethod,
} from './enrollment-flow';
import type { EnrollmentForms } from './enrollment-flow-forms';

const SCHOOL_PLACE_NATIONAL = '1';
const SCHOOL_PLACE_INTERNATIONAL = '2';
const OTHER_OPTION_VALUE = '0';

export interface BackendSurveyPatchContext {
  forms: EnrollmentForms;
  careers: readonly DegreeProgram[];
  /**
   * Si es `false`, NO se patchea la selección académica desde una encuesta previa.
   * Se usa en `nueva`, donde el Paso 1 debe quedar virgen.
   */
  includeAcademicSelection?: boolean;
}

export function patchBackendSurveyForms(
  survey: EnrollmentInitialSurvey,
  response: EnrollmentInitialSurveyResponse,
  context: BackendSurveyPatchContext
): string {
  const { forms } = context;
  const productId = toFormValue(survey.degreeProgramId);
  const processId = toFormValue(survey.intakeId);
  const levelId =
    survey.productLevelId ??
    context.careers.find(career => career.productId === survey.degreeProgramId)?.productLevelId;
  const proposalType =
    levelId === undefined
      ? forms.academicForm.controls.proposalType.value
      : (getAcademicProposalTypeByLevel(levelId)?.value ?? '');
  const schoolInstitution =
    survey.highSchoolInstitutionId ?? survey.highSchoolInstitutionName ?? '';

  if (context.includeAcademicSelection !== false) {
    forms.academicForm.patchValue(
      {
        proposalType,
        degreeProgram: productId,
        intake: processId,
      },
      { emitEvent: false }
    );
  }
  forms.educationForm.patchValue(
    {
      studiesHighSchool:
        survey.studiesHighSchool === true
          ? 'studying'
          : survey.studiesHighSchool === false
            ? 'not-studying'
            : '',
      highSchoolYear: toFormValue(survey.highSchoolYearId),
      orientation: toFormValue(survey.highSchoolOrientationId),
      repeatsHighSchoolYear: toYesNoValue(survey.repeatsHighSchoolYear),
      highSchoolYearRepeatCount: survey.highSchoolYearRepeatCount,
      highSchoolLocation: getSchoolPlaceValue(survey),
      educationalInstitution: schoolInstitution.toString(),
      higherEducationStatus: toFormValue(survey.priorHigherEducationStatusId),
      higherEducationUniversities: toSelectedOptionValues(response.higherEducationUniversities),
      otherHigherEducationUniversity: toFirstText(response.otherHigherEducationUniversities),
      motherEducation: toFormValue(survey.motherEducationLevelId),
      motherOrtDegree: toYesNoValue(survey.isMotherOrtGraduate),
      fatherEducation: toFormValue(survey.fatherEducationLevelId),
      fatherOrtDegree: toYesNoValue(survey.isFatherOrtGraduate),
    },
    { emitEvent: false }
  );
  forms.academicDecisionForm.patchValue(
    {
      degreeProgramDecisionYear: toFormValue(survey.degreeProgramDecisionYearId),
      decisionSupport: toFormValue(survey.decisionSupportId),
      ortDecisionYear: toFormValue(survey.ortDecisionYearId),
      otherUniversities: toYesNoValue(survey.researchedOtherUniversities),
      researchedUniversities: toSelectedOptionValues(response.consideredUniversities),
      otherResearchedUniversity: toFirstText(response.otherConsideredUniversities),
      decisionCertainty: toFormValue(survey.decisionLevelId),
      ortReasons: toSelectedOptionValues(response.selectedReasonOptions),
    },
    { emitEvent: false }
  );
  forms.ortExperienceForm.patchValue(
    {
      advisingMeeting: toYesNoValue(survey.hadOrtAdvising),
      advisingRating: survey.ortAdvisingRating,
      visitedWebsite: toYesNoValue(survey.visitedOrtWebsite),
      websiteRating: survey.ortWebsiteRating,
      visitedCampus: toYesNoValue(survey.visitedOrtCampus),
      campusRating: survey.ortCampusRating,
      recallsAdvertising: toYesNoValue(survey.recallsOrtAdvertising),
      advertisingChannels: toSelectedOptionValues(response.selectedAdvertisingOptions),
    },
    { emitEvent: false }
  );

  return proposalType;
}

export function buildInitialSurveyPayload(forms: EnrollmentForms) {
  const education = forms.educationForm.controls;
  const decision = forms.academicDecisionForm.controls;
  const experience = forms.ortExperienceForm.controls;
  const currentlyInSchool = education.studiesHighSchool.value === 'studying';
  const recursedBaccalaureate = education.repeatsHighSchoolYear.value === 'yes';
  const nationalSchoolPlace = education.highSchoolLocation.value === SCHOOL_PLACE_NATIONAL;
  const motherHasCompleteUniversity = hasCompleteUniversityEducation(
    education.motherEducation.value
  );
  const fatherHasCompleteUniversity = hasCompleteUniversityEducation(
    education.fatherEducation.value
  );
  const informedOtherUniversities = decision.otherUniversities.value === 'yes';
  const hasPreviousHigherEducation = education.higherEducationStatus.value === '1';
  const remembersAdvertising = experience.recallsAdvertising.value === 'yes';

  return {
    degreeProgramId: toNullableNumber(forms.academicForm.controls.degreeProgram.value),
    intakeId: toNullableNumber(forms.academicForm.controls.intake.value),
    highSchoolOrientationId: currentlyInSchool
      ? toNullableNumber(education.orientation.value)
      : null,
    highSchoolYear: currentlyInSchool ? toNullableNumber(education.highSchoolYear.value) : null,
    currentlyStudiesHighSchool: currentlyInSchool,
    highSchoolYearRepeatCount: recursedBaccalaureate
      ? education.highSchoolYearRepeatCount.value
      : null,
    repeatsHighSchoolYear: toNullableBoolean(education.repeatsHighSchoolYear.value),
    fatherOrGuardianEducationLevelId: toNullableNumber(education.fatherEducation.value),
    motherOrGuardianEducationLevelId: toNullableNumber(education.motherEducation.value),
    degreeProgramDecisionYearId: toNullableNumber(decision.degreeProgramDecisionYear.value),
    ortDecisionYearId: toNullableNumber(decision.ortDecisionYear.value),
    researchedOtherUniversities: toNullableBoolean(decision.otherUniversities.value),
    otherUniversitiesInfoLine1: null,
    otherUniversitiesInfoLine2: null,
    decisionSupportId: toNullableNumber(decision.decisionSupport.value),
    highSchoolInstitutionId: nationalSchoolPlace
      ? toNullableNumber(education.educationalInstitution.value)
      : null,
    highSchoolInstitutionName: nationalSchoolPlace
      ? null
      : toNullableText(education.educationalInstitution.value),
    finalHighSchoolYearLocationId: toNullableNumber(education.highSchoolLocation.value),
    priorHigherEducationStatusId: toNullableNumber(education.higherEducationStatus.value),
    decisionLevelId: toNullableNumber(decision.decisionCertainty.value),
    hadOrtAdvising: toNullableBoolean(experience.advisingMeeting.value),
    ortAdvisingRatingId:
      experience.advisingMeeting.value === 'yes' ? experience.advisingRating.value : null,
    visitedOrtWebsite: toNullableBoolean(experience.visitedWebsite.value),
    ortWebsiteRatingId:
      experience.visitedWebsite.value === 'yes' ? experience.websiteRating.value : null,
    visitedOrtCampus: toNullableBoolean(experience.visitedCampus.value),
    ortCampusRatingId:
      experience.visitedCampus.value === 'yes' ? experience.campusRating.value : null,
    recallsOrtAdvertising: toNullableBoolean(experience.recallsAdvertising.value),
    isMotherOrGuardianOrtGraduate: motherHasCompleteUniversity
      ? toNullableBoolean(education.motherOrtDegree.value)
      : null,
    isFatherOrGuardianOrtGraduate: fatherHasCompleteUniversity
      ? toNullableBoolean(education.fatherOrtDegree.value)
      : null,
    consideredUniversityIds: informedOtherUniversities
      ? toNumberArray(decision.researchedUniversities.value)
      : null,
    otherConsideredUniversities: toOtherOptionTextArray(
      informedOtherUniversities,
      decision.researchedUniversities.value,
      decision.otherResearchedUniversity.value
    ),
    higherEducationUniversityIds: hasPreviousHigherEducation
      ? toNumberArray(education.higherEducationUniversities.value)
      : null,
    otherHigherEducationUniversities: toOtherOptionTextArray(
      hasPreviousHigherEducation,
      education.higherEducationUniversities.value,
      education.otherHigherEducationUniversity.value
    ),
    ortAdvertisingIds: remembersAdvertising
      ? toNumberArray(experience.advertisingChannels.value)
      : null,
    ortChoiceReasonIds: toNumberArray(decision.ortReasons.value),
  };
}

export function buildConfirmPreEnrollmentPayload(
  forms: EnrollmentForms,
  isProfessionalUpdate = false
) {
  const selectedOfferingIds = isProfessionalUpdate
    ? (toNumberArray(forms.academicForm.controls.seminars.value) ?? [])
    : [toNullableNumber(forms.academicForm.controls.shift.value)].filter(
        (offering): offering is number => offering !== null
      );
  if (selectedOfferingIds.length === 0) return null;

  return {
    acceptedRegulation: forms.regulationForm.controls.acceptsRegulation.value,
    isCorporateEnrollment:
      isProfessionalUpdate && forms.workForm.controls.isCorporate.value === true,
    selectedOfferingIds,
  };
}

export function buildPaymentPayload(payload: EnrollmentPaymentPayload) {
  return {
    enrollmentIds: payload.enrollmentIds,
    paymentType: toApiPaymentMethod(payload.paymentMethod),
    sistarbancBankId: payload.paymentMethod === 'bank-account' ? payload.sistarbancBankId : null,
  };
}

function toApiPaymentMethod(method: PaymentMethod): ApiPaymentMethod {
  switch (method) {
    case 'personal-account':
      return 'CUENTA_PERSONAL';
    case 'abitab':
      return 'ABITAB';
    case 'paganza':
      return 'PAGANZA';
    case 'banred':
      return 'BANRED';
    case 'geopay':
      return 'GEOPAY';
    case 'bank-account':
      return 'SISTARBANC';
  }
}

// Inverso de toApiPaymentMethod: el bloque seniaMinima trae el método ya elegido
// como string de API (p.ej. ABITAB/PAGANZA). Lo mapeamos al PaymentMethod interno para
// reutilizar la pantalla de referencias de pago. Un valor desconocido devuelve null.
export function fromApiPaymentMethod(value: string | null): PaymentMethod | null {
  switch (value) {
    case 'CUENTA_PERSONAL':
      return 'personal-account';
    case 'ABITAB':
      return 'abitab';
    case 'PAGANZA':
      return 'paganza';
    case 'BANRED':
      return 'banred';
    case 'GEOPAY':
      return 'geopay';
    case 'SISTARBANC':
      return 'bank-account';
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
  return value === null ? '' : value ? 'yes' : 'no';
}

export function toNullableBoolean(value: string): boolean | null {
  return value === 'yes' ? true : value === 'no' ? false : null;
}

export function toNullableNumber(value: string): number | null {
  if (!value) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function getSchoolPlaceValue(survey: EnrollmentInitialSurvey): string {
  if (survey.highSchoolLocationId) return survey.highSchoolLocationId.toString();
  if (survey.highSchoolInstitutionId) return SCHOOL_PLACE_NATIONAL;
  if (survey.highSchoolInstitutionName) return SCHOOL_PLACE_INTERNATIONAL;
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

function toOtherOptionTextArray(
  enabled: boolean,
  selectedValues: readonly string[],
  otherValue: string
): string[] | null {
  return enabled && hasOtherOption(selectedValues) ? toSingleTextArray(otherValue) : null;
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
