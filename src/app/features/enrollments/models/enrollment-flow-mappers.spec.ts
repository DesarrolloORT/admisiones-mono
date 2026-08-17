import '@angular/compiler';

import type { EnrollmentInitialSurvey, PaymentMethod } from './enrollment-flow';
import { createEnrollmentForms } from './enrollment-flow-forms';
import {
  buildConfirmPreEnrollmentPayload,
  buildInitialSurveyPayload,
  buildPaymentPayload,
  fromApiPaymentMethod,
  hasCompleteUniversityEducation,
  parseDate,
  patchBackendSurveyForms,
  serializeDate,
  toNullableNumber,
} from './enrollment-flow-mappers';

const emptySurveyResponse = {
  isEligibleForSurvey: true,
  survey: null,
  consideredUniversities: [],
  otherConsideredUniversities: [],
  higherEducationUniversities: [],
  otherHigherEducationUniversities: [],
  selectedReasonOptions: [],
  selectedAdvertisingOptions: [],
};

const emptySurvey: EnrollmentInitialSurvey = {
  degreeProgramId: null,
  intakeId: null,
  shiftId: null,
  productLevelId: null,
  complete: false,
  activeSection: null,
  studiesHighSchool: null,
  highSchoolOrientationId: null,
  highSchoolYearId: null,
  repeatsHighSchoolYear: null,
  highSchoolYearRepeatCount: null,
  highSchoolInstitutionId: null,
  highSchoolLocationId: null,
  highSchoolInstitutionName: null,
  priorHigherEducationStatusId: null,
  motherEducationLevelId: null,
  fatherEducationLevelId: null,
  isMotherOrtGraduate: null,
  isFatherOrtGraduate: null,
  degreeProgramDecisionYearId: null,
  ortDecisionYearId: null,
  researchedOtherUniversities: null,
  decisionSupportId: null,
  decisionLevelId: null,
  hadOrtAdvising: null,
  ortAdvisingRating: null,
  visitedOrtWebsite: null,
  ortWebsiteRating: null,
  visitedOrtCampus: null,
  ortCampusRating: null,
  recallsOrtAdvertising: null,
};

describe('enrollment flow mappers', () => {
  it('maps every initial survey contract field when building the payload', () => {
    const forms = createEnrollmentForms();
    forms.academicForm.patchValue({ degreeProgram: '20', intake: '200' });
    forms.educationForm.patchValue({
      studiesHighSchool: 'studying',
      highSchoolYear: '11',
      orientation: '12',
      repeatsHighSchoolYear: 'yes',
      highSchoolYearRepeatCount: 2,
      highSchoolLocation: '1',
      educationalInstitution: '99',
      higherEducationStatus: '1',
      higherEducationUniversities: ['10', '0'],
      otherHigherEducationUniversity: ' Universidad inventada ',
      motherEducation: '5',
      motherOrtDegree: 'yes',
      fatherEducation: '6',
      fatherOrtDegree: 'no',
    });
    forms.academicDecisionForm.patchValue({
      degreeProgramDecisionYear: '1',
      ortDecisionYear: '7',
      decisionSupport: '5',
      otherUniversities: 'yes',
      researchedUniversities: ['11', '0'],
      otherResearchedUniversity: ' Otra consultada ',
      decisionCertainty: '1',
      ortReasons: ['2'],
    });
    forms.ortExperienceForm.patchValue({
      advisingMeeting: 'yes',
      advisingRating: 4,
      visitedWebsite: 'no',
      websiteRating: null,
      visitedCampus: 'yes',
      campusRating: 5,
      recallsAdvertising: 'yes',
      advertisingChannels: ['9'],
    });
    const payload = buildInitialSurveyPayload(forms);

    expect(Object.keys(payload).sort()).toEqual(
      [
        'consideredUniversityIds',
        'currentlyStudiesHighSchool',
        'decisionLevelId',
        'decisionSupportId',
        'degreeProgramDecisionYearId',
        'degreeProgramId',
        'fatherOrGuardianEducationLevelId',
        'finalHighSchoolYearLocationId',
        'hadOrtAdvising',
        'highSchoolInstitutionId',
        'highSchoolInstitutionName',
        'highSchoolOrientationId',
        'highSchoolYear',
        'highSchoolYearRepeatCount',
        'higherEducationUniversityIds',
        'intakeId',
        'isFatherOrGuardianOrtGraduate',
        'isMotherOrGuardianOrtGraduate',
        'motherOrGuardianEducationLevelId',
        'ortAdvertisingIds',
        'ortAdvisingRatingId',
        'ortCampusRatingId',
        'ortChoiceReasonIds',
        'ortDecisionYearId',
        'ortWebsiteRatingId',
        'otherConsideredUniversities',
        'otherHigherEducationUniversities',
        'otherUniversitiesInfoLine1',
        'otherUniversitiesInfoLine2',
        'priorHigherEducationStatusId',
        'recallsOrtAdvertising',
        'repeatsHighSchoolYear',
        'researchedOtherUniversities',
        'visitedOrtCampus',
        'visitedOrtWebsite',
      ].sort()
    );
    expect(payload).toMatchObject({
      degreeProgramId: 20,
      intakeId: 200,
      highSchoolOrientationId: 12,
      highSchoolYear: 11,
      currentlyStudiesHighSchool: true,
      repeatsHighSchoolYear: true,
      highSchoolYearRepeatCount: 2,
      highSchoolInstitutionId: 99,
      highSchoolInstitutionName: null,
      motherOrGuardianEducationLevelId: 5,
      isMotherOrGuardianOrtGraduate: true,
      isFatherOrGuardianOrtGraduate: false,
      degreeProgramDecisionYearId: 1,
      ortDecisionYearId: 7,
      decisionSupportId: 5,
      researchedOtherUniversities: true,
      consideredUniversityIds: [11, 0],
      otherConsideredUniversities: ['Otra consultada'],
      priorHigherEducationStatusId: 1,
      higherEducationUniversityIds: [10, 0],
      otherHigherEducationUniversities: ['Universidad inventada'],
      decisionLevelId: 1,
      hadOrtAdvising: true,
      ortAdvisingRatingId: 4,
      ortWebsiteRatingId: null,
      visitedOrtCampus: true,
      ortCampusRatingId: 5,
      recallsOrtAdvertising: true,
      ortAdvertisingIds: [9],
      ortChoiceReasonIds: [2],
    });
  });

  it('does not send orientation when secondary school is already complete', () => {
    const forms = createEnrollmentForms();
    forms.educationForm.patchValue({
      studiesHighSchool: 'not-studying',
      highSchoolYear: '11',
      orientation: '12',
    });

    const payload = buildInitialSurveyPayload(forms);

    expect(payload.highSchoolOrientationId).toBeNull();
  });

  it('does not send other-university text unless option 0 is selected', () => {
    const forms = createEnrollmentForms();
    forms.educationForm.patchValue({
      higherEducationStatus: '1',
      higherEducationUniversities: ['10'],
      otherHigherEducationUniversity: 'Ignorada',
    });
    forms.academicDecisionForm.patchValue({
      otherUniversities: 'yes',
      researchedUniversities: ['11'],
      otherResearchedUniversity: 'Ignorada',
    });

    const payload = buildInitialSurveyPayload(forms);

    expect(payload.otherHigherEducationUniversities).toBeNull();
    expect(payload.otherConsideredUniversities).toBeNull();
  });

  it('detects complete university education by contract id', () => {
    expect(hasCompleteUniversityEducation('5')).toBe(true);
    expect(hasCompleteUniversityEducation('6')).toBe(true);
    expect(hasCompleteUniversityEducation('4')).toBe(false);
  });

  it('patches backend survey selections into the forms', () => {
    const forms = createEnrollmentForms();

    patchBackendSurveyForms(
      {
        ...emptySurvey,
        studiesHighSchool: true,
        highSchoolYearId: 6,
        highSchoolOrientationId: 2,
        repeatsHighSchoolYear: true,
        highSchoolYearRepeatCount: 2,
        highSchoolLocationId: 1,
        highSchoolInstitutionId: 99,
        researchedOtherUniversities: true,
        decisionLevelId: 2,
        hadOrtAdvising: true,
        visitedOrtCampus: true,
        recallsOrtAdvertising: true,
      },
      {
        ...emptySurveyResponse,
        consideredUniversities: [10, 0],
        otherConsideredUniversities: ['Otra consultada'],
        higherEducationUniversities: [11, 0],
        otherHigherEducationUniversities: ['Otra superior'],
        selectedReasonOptions: [8],
        selectedAdvertisingOptions: [9],
      },
      { forms, degreePrograms: [] }
    );

    expect(forms.educationForm.getRawValue()).toMatchObject({
      studiesHighSchool: 'studying',
      highSchoolYear: '6',
      orientation: '2',
      repeatsHighSchoolYear: 'yes',
      highSchoolYearRepeatCount: 2,
      highSchoolLocation: '1',
      educationalInstitution: '99',
      higherEducationUniversities: ['11', '0'],
      otherHigherEducationUniversity: 'Otra superior',
    });
    expect(forms.academicDecisionForm.getRawValue()).toMatchObject({
      otherUniversities: 'yes',
      researchedUniversities: ['10', '0'],
      otherResearchedUniversity: 'Otra consultada',
      decisionCertainty: '2',
      ortReasons: ['8'],
    });
    expect(forms.ortExperienceForm.getRawValue()).toMatchObject({
      advisingMeeting: 'yes',
      visitedCampus: 'yes',
      recallsAdvertising: 'yes',
      advertisingChannels: ['9'],
    });
  });

  it.each<{ api: string; method: PaymentMethod }>([
    { api: 'CUENTA_PERSONAL', method: 'personal-account' },
    { api: 'ABITAB', method: 'abitab' },
    { api: 'PAGANZA', method: 'paganza' },
    { api: 'BANRED', method: 'banred' },
    { api: 'GEOPAY', method: 'geopay' },
    { api: 'SISTARBANC', method: 'bank-account' },
  ])('maps the API payment method $api back to $method', ({ api, method }) => {
    expect(fromApiPaymentMethod(api)).toBe(method);
  });

  it('returns null for unknown API payment methods', () => {
    expect(fromApiPaymentMethod('TRANSFERENCIA')).toBeNull();
    expect(fromApiPaymentMethod('')).toBeNull();
    expect(fromApiPaymentMethod(null)).toBeNull();
  });

  it('includes the bank only for the bank account payment method', () => {
    expect(
      buildPaymentPayload({
        enrollmentIds: [7],
        paymentMethod: 'bank-account',
        sistarbancBankId: '110',
      })
    ).toEqual({ enrollmentIds: [7], paymentType: 'SISTARBANC', sistarbancBankId: '110' });

    expect(
      buildPaymentPayload({
        enrollmentIds: [7, 8],
        paymentMethod: 'abitab',
        sistarbancBankId: '110',
      })
    ).toEqual({ enrollmentIds: [7, 8], paymentType: 'ABITAB', sistarbancBankId: null });
  });

  it('does not build a confirmation payload without a selected shift', () => {
    const forms = createEnrollmentForms();

    expect(buildConfirmPreEnrollmentPayload(forms)).toBeNull();

    forms.academicForm.controls.shift.setValue('300');
    forms.regulationForm.controls.acceptsRegulation.setValue(true);

    expect(buildConfirmPreEnrollmentPayload(forms)).toEqual({
      acceptedRegulation: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });
  });

  it.each([false, true])('builds the AP confirmation payload with corporate=%s', isCorporate => {
    const forms = createEnrollmentForms();
    forms.regulationForm.controls.acceptsRegulation.setValue(true);

    expect(buildConfirmPreEnrollmentPayload(forms, true)).toBeNull();

    forms.academicForm.controls.seminars.setValue(['300', '301']);
    forms.workForm.controls.isCorporate.setValue(isCorporate);

    expect(buildConfirmPreEnrollmentPayload(forms, true)).toEqual({
      acceptedRegulation: true,
      isCorporateEnrollment: isCorporate,
      selectedOfferingIds: [300, 301],
    });
  });

  it('parses slash and ISO dates and rejects rolled-over or malformed values', () => {
    expect(parseDate('26/06/2027')).toEqual(new Date(2027, 5, 26));
    expect(parseDate('2026-06-26T16:29:20')).toEqual(new Date(2026, 5, 26));
    expect(parseDate('31/02/2027')).toBeNull();
    expect(parseDate('basura')).toBeNull();
    expect(parseDate('')).toBeNull();
    expect(parseDate(null)).toBeNull();
    expect(parseDate(undefined)).toBeNull();
  });

  it('serializes dates as yyyy-MM-dd and null as empty string', () => {
    expect(serializeDate(null)).toBe('');
    expect(serializeDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('parses nullable numbers defensively', () => {
    expect(toNullableNumber('')).toBeNull();
    expect(toNullableNumber('abc')).toBeNull();
    expect(toNullableNumber('Infinity')).toBeNull();
    expect(toNullableNumber('42')).toBe(42);
  });

  it('filters invalid ids out of number arrays and nulls empty ones', () => {
    const forms = createEnrollmentForms();

    expect(buildInitialSurveyPayload(forms).ortChoiceReasonIds).toBeNull();

    forms.academicDecisionForm.controls.ortReasons.setValue(['abc', '5', '']);
    expect(buildInitialSurveyPayload(forms).ortChoiceReasonIds).toEqual([5]);

    forms.academicDecisionForm.controls.ortReasons.setValue(['abc']);
    expect(buildInitialSurveyPayload(forms).ortChoiceReasonIds).toBeNull();
  });

  it('resolves the school place with ubicacion > institucion > nombre precedence', () => {
    expect(
      patchedSchoolPlace({
        highSchoolLocationId: 2,
        highSchoolInstitutionId: 99,
        highSchoolInstitutionName: 'Liceo X',
      })
    ).toBe('2');
    expect(
      patchedSchoolPlace({ highSchoolInstitutionId: 99, highSchoolInstitutionName: 'Liceo X' })
    ).toBe('1');
    expect(patchedSchoolPlace({ highSchoolInstitutionName: 'Liceo X' })).toBe('2');
    expect(patchedSchoolPlace({})).toBe('');
  });

  it('leaves the academic selection untouched when includeAcademicSelection is false', () => {
    const forms = createEnrollmentForms();

    patchBackendSurveyForms(
      {
        ...emptySurvey,
        degreeProgramId: 20,
        intakeId: 200,
        studiesHighSchool: true,
        highSchoolYearId: 6,
      },
      emptySurveyResponse,
      { forms, degreePrograms: [], includeAcademicSelection: false }
    );

    // El paso 1 queda virgen; el resto de la encuesta sí se patchea.
    expect(forms.academicForm.controls.degreeProgram.value).toBe('');
    expect(forms.academicForm.controls.intake.value).toBe('');
    expect(forms.academicForm.controls.proposalType.value).toBe('');
    expect(forms.educationForm.controls.highSchoolYear.value).toBe('6');
  });

  it('keeps the current proposal type when the survey level cannot be resolved', () => {
    const forms = createEnrollmentForms();
    forms.academicForm.controls.proposalType.setValue('3');

    const proposalType = patchBackendSurveyForms(
      { ...emptySurvey, degreeProgramId: 20 },
      emptySurveyResponse,
      { forms, degreePrograms: [] }
    );

    expect(proposalType).toBe('3');
    expect(forms.academicForm.controls.proposalType.value).toBe('3');
    expect(forms.academicForm.controls.degreeProgram.value).toBe('20');
  });

  it('derives the proposal type from the degreePrograms catalog when the survey has no level', () => {
    const forms = createEnrollmentForms();

    const proposalType = patchBackendSurveyForms(
      { ...emptySurvey, degreeProgramId: 20 },
      emptySurveyResponse,
      {
        forms,
        degreePrograms: [
          {
            productId: 20,
            productLevelId: 2,
            productName: 'Tecnicatura',
            productLevelName: 'Terciaria',
          },
        ],
      }
    );

    expect(proposalType).toBe('2');
    expect(forms.academicForm.controls.proposalType.value).toBe('2');
  });

  it('prefers the survey level over the degreePrograms catalog and blanks unknown levels', () => {
    const forms = createEnrollmentForms();
    const degreePrograms = [
      {
        productId: 20,
        productLevelId: 2,
        productName: 'Tecnicatura',
        productLevelName: 'Terciaria',
      },
    ];

    expect(
      patchBackendSurveyForms(
        { ...emptySurvey, degreeProgramId: 20, productLevelId: 1 },
        emptySurveyResponse,
        {
          forms,
          degreePrograms,
        }
      )
    ).toBe('1');

    expect(
      patchBackendSurveyForms(
        { ...emptySurvey, degreeProgramId: 20, productLevelId: 99 },
        emptySurveyResponse,
        {
          forms,
          degreePrograms,
        }
      )
    ).toBe('');
  });

  function patchedSchoolPlace(overrides: Partial<EnrollmentInitialSurvey>): string {
    const forms = createEnrollmentForms();
    patchBackendSurveyForms({ ...emptySurvey, ...overrides }, emptySurveyResponse, {
      forms,
      degreePrograms: [],
    });
    return forms.educationForm.controls.highSchoolLocation.value;
  }
});
