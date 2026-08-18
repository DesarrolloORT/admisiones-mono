import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import type { EnrollmentDetail } from '../models/enrollment-detail';
import {
  deriveInitialEnrollmentState,
  type EnrollmentInitialSurveyResolved,
} from '../models/enrollment-entry';
import type { EnrollmentInitialSurvey } from '../models/enrollment-flow';
import { type EnrollmentIdentityPreload, Enrollments } from '../services/enrollments';
import { EnrollmentFormsStore } from '../store/enrollment-forms';
import { EnrollmentProcessStore } from '../store/enrollment-process';
import { EnrollmentPaymentFacade } from './enrollment-payment';
import { EnrollmentProposalFacade } from './enrollment-proposal';
import { EnrollmentSurveyFacade } from './enrollment-survey';
import { EnrollmentSurveyIdentityFacade } from './enrollment-survey-identity';
import { EnrollmentSurveyOptionsFacade } from './enrollment-survey-options';

const AP_CAREERS = [
  {
    productId: 30,
    productLevelId: 3,
    productName: 'Programa de Asesoramiento Financiero',
    productLevelName: 'Actualización profesional',
  },
];

describe('EnrollmentSurveyFacade', () => {
  const saveInitialSurvey = vi.fn();
  const confirmPreEnrollment = vi.fn();
  const uploadIdentityDocument = vi.fn();
  const uploadIdentityPhoto = vi.fn();
  const getIdentityPreload = vi.fn();
  const getStudentRegulationAcceptance = vi.fn();
  const payment = { outcome: signal(null) };

  beforeEach(() => {
    payment.outcome.set(null);
    saveInitialSurvey.mockReset().mockReturnValue(of(true));
    confirmPreEnrollment.mockReset().mockReturnValue(
      of({
        confirmed: true,
        isWaiting: false,
        paymentDueDate: null,
        enrollmentDeposit: null,
        accountBalance: null,
        summary: null,
      })
    );
    uploadIdentityDocument.mockReset().mockReturnValue(of(true));
    uploadIdentityPhoto.mockReset().mockReturnValue(of(true));
    getIdentityPreload
      .mockReset()
      .mockReturnValue(of({ front: null, back: null, selfie: null, expirationDate: null }));
    getStudentRegulationAcceptance
      .mockReset()
      .mockReturnValue(of({ acceptedStudentRegulation: false, acceptanceDate: null }));
  });

  it('resumes an incomplete backend survey at its active section', () => {
    const { survey, process } = createFacade({
      isEligibleForSurvey: true,
      survey: createInitialSurvey({ activeSection: 'academic-decision' }),
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });

    expect(process.flow.currentStep()).toBe('survey');
    expect(survey.activeSection()).toBe('academic-decision');
  });

  it('keeps only identity and regulation when the survey is not required', async () => {
    const { survey } = createFacade({
      isEligibleForSurvey: false,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });

    expect(survey.visibleSections()).toEqual(['identity', 'regulation']);
    await expect(firstValueFrom(survey.savePartial())).resolves.toBe(true);
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it('asks only for the enrollment owner when AP has survey rights', () => {
    const { survey, forms } = createFacade(
      createSurveyResponse({ isEligibleForSurvey: true }),
      {},
      AP_CAREERS
    );

    expect(survey.activeSection()).toBe('education');

    forms.academicForm.controls.proposalType.setValue('3');
    forms.academicForm.controls.degreeProgram.setValue('30');
    TestBed.tick();

    expect(survey.visibleSections()).toEqual(['work-situation', 'identity', 'regulation']);
    expect(survey.activeSection()).toBe('work-situation');
    expect(forms.workForm.controls.isCorporate.hasError('required')).toBe(true);
  });

  it('asks for a corporate enrollment before identity when AP has no survey rights', () => {
    const { survey, forms } = createFacade(
      createSurveyResponse({ isEligibleForSurvey: false }),
      {},
      AP_CAREERS
    );

    expect(survey.activeSection()).toBe('identity');

    forms.academicForm.controls.proposalType.setValue('3');
    forms.academicForm.controls.degreeProgram.setValue('30');
    TestBed.tick();

    expect(survey.visibleSections()).toEqual(['work-situation', 'identity', 'regulation']);
    expect(survey.activeSection()).toBe('work-situation');
  });

  it('does not persist the initial survey for AP even with survey rights', async () => {
    const { survey, forms } = createFacade(createSurveyResponse(), {}, AP_CAREERS);
    forms.academicForm.controls.proposalType.setValue('3');

    await expect(firstValueFrom(survey.savePartial())).resolves.toBe(true);
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it('confirms a personal AP pre-enrollment and advances to payment', () => {
    const { survey, forms, process } = createFacade(createSurveyResponse(), {}, AP_CAREERS);
    forms.academicForm.controls.proposalType.setValue('3');
    forms.academicForm.controls.degreeProgram.setValue('30');
    forms.academicForm.controls.seminars.setValue(['300']);
    process.flow.goTo('survey');
    TestBed.tick();

    expect(forms.workForm.controls.isCorporate.hasError('required')).toBe(true);
    forms.workForm.controls.isCorporate.setValue(false);
    const front = preloadFile('front.png');
    const back = preloadFile('back.png');
    const selfie = preloadFile('selfie.png');
    applyIdentityPreload(survey, {
      front,
      back,
      selfie,
      expirationDate: '2030-02-04',
    });
    survey.identityForm.controls.isIdentityCorrect.setValue(true);
    survey.regulationForm.controls.acceptsRegulation.setValue(true);

    survey.continue();

    expect(saveInitialSurvey).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      acceptedRegulation: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });
    expect(process.flow.currentStep()).toBe('payment');
    expect(payment.outcome()).toBeNull();
  });

  it('finishes a corporate AP pre-enrollment without opening payment', () => {
    // La rama corporativa responde sin datos de pago (seña 0): la pantalla de espera
    // manda sobre la de reserva por seña 0.
    confirmPreEnrollment.mockReturnValue(
      of({
        confirmed: false,
        isWaiting: true,
        paymentDueDate: null,
        enrollmentDeposit: 0,
        accountBalance: null,
        summary: null,
      })
    );
    const { survey, forms, process } = createFacade(createSurveyResponse(), {}, AP_CAREERS);
    forms.academicForm.controls.proposalType.setValue('3');
    forms.academicForm.controls.degreeProgram.setValue('30');
    forms.academicForm.controls.seminars.setValue(['300']);
    forms.workForm.controls.isCorporate.setValue(true);
    process.flow.goTo('survey');
    TestBed.tick();

    const identity = preloadFile('identidad.png');
    applyIdentityPreload(survey, {
      front: identity,
      back: identity,
      selfie: identity,
      expirationDate: '2030-02-04',
    });
    survey.identityForm.controls.isIdentityCorrect.setValue(true);
    survey.regulationForm.controls.acceptsRegulation.setValue(true);

    survey.continue();

    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      acceptedRegulation: true,
      isCorporateEnrollment: true,
      selectedOfferingIds: [300],
    });
    expect(process.flow.currentStep()).toBe('survey');
    expect(payment.outcome()).toBe('enrollment-in-progress');
  });

  it('completes identity and advances when confirming a complete backend preload', () => {
    const { survey } = createFacade({
      isEligibleForSurvey: false,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });
    const file = preloadFile('identidad.png');

    applyIdentityPreload(survey, {
      front: file,
      back: file,
      selfie: file,
      expirationDate: '2030-02-04',
    });

    expect(survey.identity.requiresIdentityConfirmation()).toBe(true);
    expect(survey.identityForm.controls.isIdentityCorrect.hasError('required')).toBe(true);

    survey.identityForm.controls.isIdentityCorrect.setValue(true);

    expect(survey.getSectionState('identity')).toBe('complete');
    expect(survey.activeSection()).toBe('regulation');
  });

  it('does not upload preloaded identity files when only confirmation changes', () => {
    const { survey, forms } = createFacade({
      isEligibleForSurvey: false,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });
    const front = preloadFile('front.png');
    const back = preloadFile('back.png');
    const selfie = preloadFile('selfie.png');

    applyIdentityPreload(survey, {
      front,
      back,
      selfie,
      expirationDate: '2030-02-04',
    });
    survey.identity.updateIdentityFile('front', preloadedFileEvent(front));
    survey.identity.updateIdentityFile('back', preloadedFileEvent(back));
    survey.identity.updateIdentityFile('selfie', preloadedFileEvent(selfie));
    survey.identityForm.controls.isIdentityCorrect.setValue(true);
    survey.regulationForm.controls.acceptsRegulation.setValue(true);
    forms.academicForm.controls.shift.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).not.toHaveBeenCalled();
    expect(uploadIdentityPhoto).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      acceptedRegulation: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });
  });

  it('does not upload preloaded identity document when the expiration control is dirty but unchanged', () => {
    const { survey, forms } = createFacade({
      isEligibleForSurvey: false,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });
    const front = preloadFile('front.png');
    const back = preloadFile('back.png');
    const selfie = preloadFile('selfie.png');

    applyIdentityPreload(survey, {
      front,
      back,
      selfie,
      expirationDate: '2030-02-04',
    });
    survey.identity.updateIdentityFile('front', preloadedFileEvent(front));
    survey.identity.updateIdentityFile('back', preloadedFileEvent(back));
    survey.identity.updateIdentityFile('selfie', preloadedFileEvent(selfie));
    survey.identityForm.controls.documentExpiration.markAsDirty();
    survey.identityForm.controls.isIdentityCorrect.setValue(true);
    survey.regulationForm.controls.acceptsRegulation.setValue(true);
    forms.academicForm.controls.shift.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).not.toHaveBeenCalled();
    expect(uploadIdentityPhoto).not.toHaveBeenCalled();
  });

  it('uploads preloaded identity document when the expiration changes', () => {
    const { survey, forms } = createFacade({
      isEligibleForSurvey: false,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });
    const front = preloadFile('front.png');
    const back = preloadFile('back.png');
    const selfie = preloadFile('selfie.png');

    applyIdentityPreload(survey, {
      front,
      back,
      selfie,
      expirationDate: '2030-02-04',
    });
    survey.identity.updateIdentityFile('front', preloadedFileEvent(front));
    survey.identity.updateIdentityFile('back', preloadedFileEvent(back));
    survey.identity.updateIdentityFile('selfie', preloadedFileEvent(selfie));
    survey.identityForm.controls.documentExpiration.setValue(new Date(2031, 1, 4));
    survey.identityForm.controls.documentExpiration.markAsDirty();
    survey.identityForm.controls.isIdentityCorrect.setValue(true);
    survey.regulationForm.controls.acceptsRegulation.setValue(true);
    forms.academicForm.controls.shift.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledWith({
      date: '2031-02-04',
      front,
      back,
    });
    expect(uploadIdentityPhoto).not.toHaveBeenCalled();
  });

  it('does not confirm when a previous visible section is invalid', () => {
    const { survey } = createFacade({
      isEligibleForSurvey: false,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });
    survey.identityForm.controls.documentExpiration.setValue(new Date(2030, 1, 4));
    survey.regulationForm.controls.acceptsRegulation.setValue(true);
    survey.activeSection.set('regulation');

    survey.continue();

    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(survey.activeSection()).toBe('identity');
    expect(survey.preEnrollmentError()).toBe(
      'Completá la información pendiente antes de confirmar la preinscripción.'
    );
  });

  it('uploads touched identity files before confirming pre-enrollment', () => {
    const { survey, forms } = createFacade({
      isEligibleForSurvey: false,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });
    const front = new File(['front'], 'front.png', { type: 'image/png' });
    const back = new File(['back'], 'back.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    survey.identityForm.controls.documentExpiration.setValue(new Date(2030, 1, 4));
    survey.identityForm.controls.documentExpiration.markAsDirty();
    survey.identity.updateIdentityFile('front', fileEvent(front));
    survey.identity.updateIdentityFile('back', fileEvent(back));
    survey.identity.updateIdentityFile('selfie', fileEvent(selfie));
    survey.regulationForm.controls.acceptsRegulation.setValue(true);
    forms.academicForm.controls.shift.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledWith({
      date: '2030-02-04',
      front,
      back,
    });
    expect(uploadIdentityPhoto).toHaveBeenCalledWith(selfie);
    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      acceptedRegulation: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });
  });

  it('waits for document and photo before saving the survey and confirming', () => {
    const documentResult = new Subject<boolean>();
    const photoResult = new Subject<boolean>();
    uploadIdentityDocument.mockReturnValue(documentResult);
    uploadIdentityPhoto.mockReturnValue(photoResult);
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledOnce();
    expect(uploadIdentityPhoto).toHaveBeenCalledOnce();
    expect(saveInitialSurvey).not.toHaveBeenCalled();

    documentResult.next(true);
    documentResult.complete();
    expect(saveInitialSurvey).not.toHaveBeenCalled();

    photoResult.next(true);
    photoResult.complete();

    expect(saveInitialSurvey).toHaveBeenCalledOnce();
    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('payment');
  });

  it('does not re-save the survey when exiting after pre-enrollment was already confirmed', async () => {
    const { survey } = prepareFinalizableSurvey();

    survey.continue();
    expect(saveInitialSurvey).toHaveBeenCalledOnce();
    saveInitialSurvey.mockClear();

    await expect(firstValueFrom(survey.savePartial())).resolves.toBe(true);
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it.each([
    { failure: 'data false', result: of(false) },
    { failure: 'HTTP 400', result: throwError(() => ({ status: 400 })) },
  ])('reopens identity and stops the chain when photo returns $failure', ({ result }) => {
    uploadIdentityPhoto.mockReturnValue(result);
    const { survey, front, back, selfie } = prepareFinalizableSurvey();

    survey.continue();

    expect(saveInitialSurvey).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(survey.activeSection()).toBe('identity');
    expect(survey.getSectionState('identity')).toBe('active');
    expect(survey.identity.identityFiles()).toEqual({ front, back, selfie });
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo guardar la verificación de identidad. Intentá nuevamente.'
    );
  });

  it('marks identity complete reactively once files and expiry are set, without pressing Continuar', () => {
    const { survey } = createFacade(createSurveyResponse({ isEligibleForSurvey: false }));
    const front = new File(['front'], 'front.png', { type: 'image/png' });
    const back = new File(['back'], 'back.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    survey.identityForm.controls.documentExpiration.setValue(new Date(2030, 1, 4));
    survey.identity.updateIdentityFile('front', fileEvent(front));
    survey.identity.updateIdentityFile('back', fileEvent(back));
    survey.identity.updateIdentityFile('selfie', fileEvent(selfie));

    expect(survey.getSectionState('identity')).toBe('complete');
    // El check aparece sin avanzar de sección (eso sigue siendo tarea de Continuar).
    expect(survey.activeSection()).toBe('identity');
  });

  it.each([
    {
      name: 'changing an identity file',
      mutate: (survey: EnrollmentSurveyFacade) =>
        survey.identity.updateIdentityFile(
          'selfie',
          fileEvent(new File(['new'], 'selfie-2.png', { type: 'image/png' }))
        ),
    },
    {
      name: 'changing the document expiry',
      mutate: (survey: EnrollmentSurveyFacade) =>
        survey.identityForm.controls.documentExpiration.setValue(new Date(2031, 0, 1)),
    },
  ])(
    'restores reactive identity completion after an upload failure when $name',
    ({ mutate }: { name: string; mutate: (survey: EnrollmentSurveyFacade) => void }) => {
      uploadIdentityPhoto.mockReturnValue(of(false));
      const { survey } = prepareFinalizableSurvey();

      survey.continue();
      expect(survey.getSectionState('identity')).toBe('active');

      mutate(survey);

      expect(survey.getSectionState('identity')).toBe('complete');
    }
  );

  it.each([
    { failure: 'data false', result: of(false) },
    { failure: 'HTTP 400', result: throwError(() => ({ status: 400 })) },
  ])('does not confirm when the final survey save returns $failure', ({ result }) => {
    saveInitialSurvey.mockReturnValue(result);
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('survey');
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
    );
  });

  it('uses only enEspera to decide between payment and manual review screens', () => {
    confirmPreEnrollment.mockReturnValue(
      of({
        confirmed: false,
        isWaiting: false,
        paymentDueDate: null,
        enrollmentDeposit: 15500,
        accountBalance: null,
        summary: null,
      })
    );
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('payment');
    expect(payment.outcome()).toBeNull();
    expect(survey.preEnrollmentError()).toBeNull();
  });

  it('sets the reserva outcome and skips payment method selection when the deposit is 0', () => {
    confirmPreEnrollment.mockReturnValue(
      of({
        confirmed: false,
        isWaiting: false,
        paymentDueDate: null,
        enrollmentDeposit: 0,
        accountBalance: null,
        summary: null,
      })
    );
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(process.flow.currentStep()).toBe('survey');
    expect(payment.outcome()).toBe('reservation');
  });

  it('shows the in-process outcome when pre-enrollment is waiting for manual review', () => {
    confirmPreEnrollment.mockReturnValue(
      of({
        confirmed: true,
        isWaiting: true,
        enrollmentId: null,
        paymentDueDate: null,
        enrollmentDeposit: 15500,
        accountBalance: null,
        summary: { degreeProgram: 'Sistemas', intake: 'Marzo', shift: 'Noche' },
      })
    );
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(process.flow.currentStep()).toBe('survey');
    expect(payment.outcome()).toBe('enrollment-in-progress');
  });

  it('uses school year orientations directly from the selected year catalog', () => {
    const { survey } = createFacade(
      {
        isEligibleForSurvey: true,
        survey: createInitialSurvey({ activeSection: 'education' }),
        consideredUniversities: [],
        otherConsideredUniversities: [],
        higherEducationUniversities: [],
        otherHigherEducationUniversities: [],
        selectedReasonOptions: [],
        selectedAdvertisingOptions: [],
      },
      {
        education: {
          lastSecondaryYearLocations: [],
          previousHigherEducationOptions: [],
          universities: [],
          guardianEducationLevels: [],
          highSchoolYears: [
            { id: 10, label: '1 EMS', baccalaureates: [] },
            {
              id: 11,
              label: '2 EMS',
              baccalaureates: [{ id: 20, label: 'Bachillerato A', orientation: 'Cientifico' }],
            },
            {
              id: 12,
              label: '3 EMS',
              baccalaureates: [{ id: 30, label: 'Bachillerato B', orientation: 'Economia' }],
            },
          ],
        },
      }
    );
    TestBed.tick();

    expect(survey.options.schoolYearOptions()).toEqual([
      { value: '10', label: '1 EMS' },
      { value: '11', label: '2 EMS' },
      { value: '12', label: '3 EMS' },
    ]);

    survey.educationForm.patchValue({ studiesHighSchool: 'studying', highSchoolYear: '10' });

    expect(survey.shouldAskHighSchoolOrientation()).toBe(false);
    expect(survey.options.orientationOptions()).toEqual([]);

    survey.educationForm.controls.highSchoolYear.setValue('11');

    expect(survey.shouldAskHighSchoolOrientation()).toBe(true);
    expect(survey.options.orientationOptions()).toEqual([{ value: '20', label: 'Cientifico' }]);

    survey.educationForm.controls.orientation.setValue('20');
    survey.educationForm.controls.highSchoolYear.setValue('12');

    expect(survey.options.orientationOptions()).toEqual([{ value: '30', label: 'Economia' }]);
    expect(survey.educationForm.controls.orientation.value).toBe('');
  });

  it('requires free-text details for recursado and Otro university options', () => {
    const { survey } = createFacade(
      {
        isEligibleForSurvey: true,
        survey: null,
        consideredUniversities: [],
        otherConsideredUniversities: [],
        higherEducationUniversities: [],
        otherHigherEducationUniversities: [],
        selectedReasonOptions: [],
        selectedAdvertisingOptions: [],
      },
      {
        education: {
          lastSecondaryYearLocations: [],
          highSchoolYears: [],
          previousHigherEducationOptions: [],
          universities: [
            { id: 0, label: 'Otra' },
            { id: 10, label: 'Udelar' },
          ],
          guardianEducationLevels: [],
        },
        academicDecision: {
          upperSecondaryYears: [],
          decisionSupports: [],
          decisionLevels: [],
          universities: [
            { id: 0, label: 'Otra' },
            { id: 11, label: 'UCU' },
          ],
          ortChoiceReasons: [],
        },
      }
    );

    survey.educationForm.controls.repeatsHighSchoolYear.setValue('yes');
    expect(survey.shouldAskHighSchoolRepeatCount()).toBe(true);
    expect(survey.educationForm.controls.highSchoolYearRepeatCount.hasError('required')).toBe(true);
    survey.educationForm.controls.highSchoolYearRepeatCount.setValue(0);
    expect(survey.educationForm.controls.highSchoolYearRepeatCount.hasError('min')).toBe(true);
    survey.educationForm.controls.highSchoolYearRepeatCount.setValue(2);
    expect(survey.educationForm.controls.highSchoolYearRepeatCount.valid).toBe(true);

    survey.educationForm.patchValue({
      higherEducationStatus: '1',
      higherEducationUniversities: ['0'],
    });
    expect(survey.shouldAskHigherEducationOtherUniversity()).toBe(true);
    expect(survey.educationForm.controls.otherHigherEducationUniversity.hasError('required')).toBe(
      true
    );
    survey.educationForm.controls.otherHigherEducationUniversity.setValue('Otra superior');
    expect(survey.educationForm.controls.otherHigherEducationUniversity.valid).toBe(true);

    survey.academicDecisionForm.patchValue({
      otherUniversities: 'yes',
      researchedUniversities: ['0'],
    });
    expect(survey.shouldAskInformedOtherUniversity()).toBe(true);
    expect(
      survey.academicDecisionForm.controls.otherResearchedUniversity.hasError('required')
    ).toBe(true);
    survey.academicDecisionForm.controls.otherResearchedUniversity.setValue('Otra consultada');
    expect(survey.academicDecisionForm.controls.otherResearchedUniversity.valid).toBe(true);
  });
  it('blocks pre-enrollment when a university degreeProgram has a disallowed baccalaureate year', () => {
    const { survey, forms } = createFacade(
      {
        isEligibleForSurvey: true,
        survey: null,
        consideredUniversities: [],
        otherConsideredUniversities: [],
        higherEducationUniversities: [],
        otherHigherEducationUniversities: [],
        selectedReasonOptions: [],
        selectedAdvertisingOptions: [],
      },
      {
        education: {
          lastSecondaryYearLocations: [],
          previousHigherEducationOptions: [],
          universities: [],
          guardianEducationLevels: [],
          highSchoolYears: [
            { id: 4, label: '4º año', baccalaureates: [] },
            { id: 5, label: '5º año', baccalaureates: [] },
          ],
        },
      },
      [
        {
          productId: 100,
          productLevelId: 1,
          productName: 'Ingeniería',
          productLevelName: 'Universitaria',
        },
      ]
    );

    forms.academicForm.controls.proposalType.setValue('1');
    forms.academicForm.controls.degreeProgram.setValue('100');
    survey.educationForm.controls.studiesHighSchool.setValue('studying');
    survey.educationForm.controls.highSchoolYear.setValue('4');

    expect(survey.isUniversityDegreeProgram()).toBe(true);
    expect(
      survey.educationForm.controls.highSchoolYear.hasError('nonUniversityHighSchoolYear')
    ).toBe(true);

    survey.educationForm.controls.highSchoolYear.setValue('5');
    expect(
      survey.educationForm.controls.highSchoolYear.hasError('nonUniversityHighSchoolYear')
    ).toBe(false);
  });

  it('does not flag disallowed years for non-university degreePrograms', () => {
    const { survey, forms } = createFacade(
      {
        isEligibleForSurvey: true,
        survey: null,
        consideredUniversities: [],
        otherConsideredUniversities: [],
        higherEducationUniversities: [],
        otherHigherEducationUniversities: [],
        selectedReasonOptions: [],
        selectedAdvertisingOptions: [],
      },
      {
        education: {
          lastSecondaryYearLocations: [],
          previousHigherEducationOptions: [],
          universities: [],
          guardianEducationLevels: [],
          highSchoolYears: [{ id: 4, label: '4º año', baccalaureates: [] }],
        },
      },
      [
        {
          productId: 200,
          productLevelId: 2,
          productName: 'Tecnicatura',
          productLevelName: 'Terciaria',
        },
      ]
    );

    forms.academicForm.controls.proposalType.setValue('2');
    forms.academicForm.controls.degreeProgram.setValue('200');
    survey.educationForm.controls.studiesHighSchool.setValue('studying');
    survey.educationForm.controls.highSchoolYear.setValue('4');

    expect(survey.isUniversityDegreeProgram()).toBe(false);
    expect(
      survey.educationForm.controls.highSchoolYear.hasError('nonUniversityHighSchoolYear')
    ).toBe(false);
  });

  it('sets the pre-enrollment error and stops the spinner when confirmation fails', () => {
    confirmPreEnrollment.mockReturnValue(throwError(() => ({ status: 500 })));
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
    );
    expect(survey.finalizingPreEnrollment()).toBe(false);
    expect(process.flow.currentStep()).toBe('survey');
    expect(payment.outcome()).toBeNull();
  });

  it('reports an error instead of confirming when no shift is selected', () => {
    const { survey, forms } = prepareFinalizableSurvey();
    forms.academicForm.controls.shift.setValue('');

    survey.continue();

    expect(uploadIdentityDocument).not.toHaveBeenCalled();
    expect(saveInitialSurvey).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(survey.preEnrollmentError()).toBe('No se pudo confirmar la preinscripción.');
    expect(survey.finalizingPreEnrollment()).toBe(false);
  });

  it('defers the regulation acceptance lookup until the regulation section is reached', () => {
    getStudentRegulationAcceptance.mockReturnValue(
      of({ acceptedStudentRegulation: true, acceptanceDate: '10/05/2026' })
    );
    const { survey, process } = createFacade(createSurveyResponse());

    expect(getStudentRegulationAcceptance).not.toHaveBeenCalled();

    process.flow.goTo('survey');
    survey.activeSection.set('regulation');
    TestBed.tick();

    expect(getStudentRegulationAcceptance).toHaveBeenCalledOnce();
  });

  it('does not repeat the regulation lookup when navigating back and forth', () => {
    getStudentRegulationAcceptance.mockReturnValue(
      of({ acceptedStudentRegulation: false, acceptanceDate: null })
    );
    const { survey, process } = createFacade(createSurveyResponse());

    process.flow.goTo('survey');
    survey.activeSection.set('regulation');
    TestBed.tick();
    survey.activeSection.set('education');
    TestBed.tick();
    survey.activeSection.set('regulation');
    TestBed.tick();

    expect(getStudentRegulationAcceptance).toHaveBeenCalledOnce();
  });

  it('prefills the regulation section when the student already accepted it', () => {
    getStudentRegulationAcceptance.mockReturnValue(
      of({ acceptedStudentRegulation: true, acceptanceDate: '10/05/2026' })
    );
    const { survey, process } = createFacade(createSurveyResponse());
    process.flow.goTo('survey');
    survey.activeSection.set('regulation');
    TestBed.tick();

    expect(survey.hasAcceptedStudentRegulation()).toBe(true);
    expect(survey.regulationForm.controls.acceptsRegulation.value).toBe(true);
    expect(survey.submittedAcceptanceDate()).toEqual(new Date(2026, 4, 10));
    expect(survey.getSectionState('regulation')).toBe('complete');
  });

  it('keeps the regulation unaccepted when the acceptance lookup fails', () => {
    getStudentRegulationAcceptance.mockReturnValue(throwError(() => ({ status: 500 })));
    const { survey, process } = createFacade(createSurveyResponse());
    process.flow.goTo('survey');
    survey.activeSection.set('regulation');
    TestBed.tick();

    expect(survey.hasAcceptedStudentRegulation()).toBe(false);
    expect(survey.regulationForm.controls.acceptsRegulation.value).toBe(false);
    expect(survey.submittedAcceptanceDate()).toBeNull();
  });

  it('marks the regulation as accepted from the reader', () => {
    const { survey } = createFacade(createSurveyResponse());
    survey.openRegulationReader();
    expect(survey.readerOpen()).toBe(true);

    survey.acceptRegulation();

    expect(survey.regulationForm.controls.acceptsRegulation.value).toBe(true);
    expect(survey.readerOpen()).toBe(false);
    expect(survey.activeSection()).toBe('regulation');
    expect(survey.getSectionState('regulation')).toBe('complete');
  });

  it('closes the reader on back, retreats sections and never leaves the step', () => {
    const { survey, process } = createFacade(createSurveyResponse());
    process.flow.goTo('survey');
    survey.activeSection.set('academic-decision');
    survey.openRegulationReader();

    expect(survey.canGoBack()).toBe(true);
    survey.back();

    expect(survey.readerOpen()).toBe(false);
    expect(survey.activeSection()).toBe('academic-decision');
    expect(process.flow.currentStep()).toBe('survey');

    survey.back();

    expect(survey.activeSection()).toBe('education');
    expect(process.flow.currentStep()).toBe('survey');

    // Primera sección visible: no hay a dónde volver, el paso 1 queda inalcanzable.
    expect(survey.canGoBack()).toBe(false);
    survey.back();

    expect(survey.activeSection()).toBe('education');
    expect(process.flow.currentStep()).toBe('survey');
  });

  it('starts an empty survey when the backend has no survey yet', () => {
    const { survey } = createFacade(null);

    expect(survey.surveyLoadError()).toBeNull();
    expect(survey.hasInitialSurveyRight()).toBe(true);
    expect(survey.scenario()).toBe('first-time');
    expect(survey.activeSection()).toBe('education');
    expect(survey.loadingSurveyState()).toBe(false);
  });

  it('shows the survey load error when the resolver reports a failure', () => {
    const { survey } = createFacade(null, {}, [], { loadFailed: true });

    expect(survey.surveyLoadError()).toBe(
      'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.'
    );
  });

  it('fetchResolvedInitialSurvey maps errors to loadFailed and toggles the loading flag', async () => {
    const getInitialSurvey = vi.fn().mockReturnValue(throwError(() => ({ status: 500 })));
    const { survey } = createFacade(null, {}, [], { skipApply: true, getInitialSurvey });

    await expect(firstValueFrom(survey.fetchResolvedInitialSurvey())).resolves.toEqual({
      initialSurvey: null,
      loadFailed: true,
    });
    expect(survey.loadingSurveyState()).toBe(false);
  });

  it('fetchResolvedInitialSurvey maps a 404 to an empty survey with right', async () => {
    const getInitialSurvey = vi.fn().mockReturnValue(throwError(() => ({ status: 404 })));
    const { survey } = createFacade(null, {}, [], { skipApply: true, getInitialSurvey });

    await expect(firstValueFrom(survey.fetchResolvedInitialSurvey())).resolves.toEqual({
      initialSurvey: expect.objectContaining({ isEligibleForSurvey: true, survey: null }),
      loadFailed: false,
    });
    expect(survey.loadingSurveyState()).toBe(false);
  });

  function createSurveyResponse(overrides: Record<string, unknown> = {}) {
    return {
      isEligibleForSurvey: true,
      survey: null,
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
      ...overrides,
    };
  }

  // Detalle mínimo "En proceso" sin bloque `detalle` ni ofertas de interés: no hay
  // precarga posible, así que la encuesta prellena el paso 1 y el flujo arranca en el
  // paso 2. El posicionamiento del flujo y la selección de slice los deriva la función
  // pura (testeada en enrollment-entry.spec); acá solo se aplican.
  const RESUME_DETAIL: EnrollmentDetail = {
    status: 'En proceso',
    summary: null,
    interests: [],
    pendingPayment: null,
    minimumDeposit: null,
    confirmed: null,
  };

  function createFacade(
    initialSurvey: unknown,
    catalogOverrides: Record<string, unknown> = {},
    degreePrograms: unknown[] = [],
    options: { loadFailed?: boolean; getInitialSurvey?: () => unknown; skipApply?: boolean } = {}
  ): {
    survey: EnrollmentSurveyFacade;
    process: EnrollmentProcessStore;
    forms: EnrollmentFormsStore;
  } {
    const getInitialSurvey = options.getInitialSurvey ?? (() => of(initialSurvey));
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        EnrollmentFormsStore,
        EnrollmentProcessStore,
        EnrollmentProposalFacade,
        EnrollmentSurveyOptionsFacade,
        EnrollmentSurveyIdentityFacade,
        EnrollmentSurveyFacade,
        {
          provide: Catalogs,
          useValue: {
            getDegreePrograms: () => of(degreePrograms),
            getIntakes: () => of([]),
            getShifts: () => of([]),
            getSeminars: () =>
              of([
                { offeringId: 300, admissionProcessId: 200, name: 'Marco legal', startDate: null },
              ]),
            getCountryLocations: () => of([]),
            getInstitutions: () => of([]),
            getInitialSurveyCatalogs: () =>
              of({
                education: {
                  lastSecondaryYearLocations: [],
                  highSchoolYears: [],
                  previousHigherEducationOptions: [],
                  universities: [],
                  guardianEducationLevels: [],
                },
                academicDecision: {
                  upperSecondaryYears: [],
                  decisionSupports: [],
                  decisionLevels: [],
                  universities: [],
                  ortChoiceReasons: [],
                },
                ortExperience: { ratings: [], ortAdvertisements: [] },
                ...catalogOverrides,
              }),
          },
        },
        { provide: EnrollmentPaymentFacade, useValue: payment },
        {
          provide: Enrollments,
          useValue: {
            getStudentRegulationAcceptance,
            getIdentityPreload,
            getInitialSurvey,
            saveInitialSurvey,
            uploadIdentityDocument,
            uploadIdentityPhoto,
            confirmPreEnrollment,
            registerProductInterest: vi.fn(),
          },
        },
      ],
    });
    const survey = TestBed.inject(EnrollmentSurveyFacade);
    const process = TestBed.inject(EnrollmentProcessStore);
    const forms = TestBed.inject(EnrollmentFormsStore);

    // Réplica de lo que hace EnrollmentProcessFacade.applyInitialState para el slice
    // de encuesta: deriva y aplica, posicionando el paso al final.
    if (!options.skipApply) {
      const resolved: EnrollmentInitialSurveyResolved = {
        initialSurvey: (initialSurvey ?? null) as EnrollmentInitialSurveyResolved['initialSurvey'],
        loadFailed: options.loadFailed ?? false,
      };
      const state = deriveInitialEnrollmentState({
        entry: {
          intent: 'resume',
          detail: RESUME_DETAIL,
          productId: 20,
          admissionProcessId: 200,
          offeringIds: [],
          productLevelId: null,
        },
        survey: resolved,
      });
      survey.applyInitialState(state.survey);
      process.flow.goTo(state.step);
    }

    return { survey, process, forms };
  }

  function prepareFinalizableSurvey() {
    const result = createFacade({
      isEligibleForSurvey: true,
      survey: createInitialSurvey({ complete: true }),
      consideredUniversities: [],
      otherConsideredUniversities: [],
      higherEducationUniversities: [],
      otherHigherEducationUniversities: [],
      selectedReasonOptions: [],
      selectedAdvertisingOptions: [],
    });
    const front = new File(['front'], 'front.png', { type: 'image/png' });
    const back = new File(['back'], 'back.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    result.survey.identityForm.controls.documentExpiration.setValue(new Date(2030, 1, 4));
    result.survey.identityForm.controls.documentExpiration.markAsDirty();
    result.survey.identity.updateIdentityFile('front', fileEvent(front));
    result.survey.identity.updateIdentityFile('back', fileEvent(back));
    result.survey.identity.updateIdentityFile('selfie', fileEvent(selfie));
    result.survey.regulationForm.controls.acceptsRegulation.setValue(true);
    result.forms.academicForm.controls.shift.setValue('300');

    return { ...result, front, back, selfie };
  }

  function preloadFile(name: string): File {
    const bytes = new Uint8Array([1, 2, 3]).buffer;
    return {
      name,
      size: 3,
      type: 'image/png',
      arrayBuffer: () => Promise.resolve(bytes),
    } as File;
  }
  function applyIdentityPreload(
    survey: EnrollmentSurveyFacade,
    preload: EnrollmentIdentityPreload
  ): void {
    (
      survey.identity as unknown as {
        applyIdentityPreload(preload: EnrollmentIdentityPreload): void;
      }
    ).applyIdentityPreload(preload);
  }
  function preloadedFileEvent(
    file: File
  ): Parameters<EnrollmentSurveyIdentityFacade['updateIdentityFile']>[1] {
    return { value: [{ isValid: true, isPreloaded: true, file }] } as Parameters<
      EnrollmentSurveyIdentityFacade['updateIdentityFile']
    >[1];
  }
  function fileEvent(
    file: File
  ): Parameters<EnrollmentSurveyIdentityFacade['updateIdentityFile']>[1] {
    return { value: [{ isValid: true, file }] } as Parameters<
      EnrollmentSurveyIdentityFacade['updateIdentityFile']
    >[1];
  }

  function createInitialSurvey(
    values: Partial<EnrollmentInitialSurvey> = {}
  ): EnrollmentInitialSurvey {
    return {
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
      ...values,
    };
  }
});
