import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsApi } from '../../catalogs/api/catalogs.api';
import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { EnrollmentsApi } from '../api/enrollments.api';
import type { EnrollmentDetail } from '../models/enrollment-detail';
import {
  deriveInitialEnrollmentState,
  type EnrollmentInitialSurveyResolved,
} from '../models/enrollment-entry';
import type { EnrollmentInitialSurvey } from '../models/enrollment-flow';
import {
  createEnrollmentFormsState,
  ENROLLMENT_FORMS,
  type EnrollmentFormsState,
} from '../models/enrollment-flow-forms';
import {
  createEnrollmentProcessState,
  ENROLLMENT_PROCESS_STATE,
  type EnrollmentProcessState,
} from '../models/enrollment-process';
import { type EnrollmentIdentityPreload, Enrollments } from '../services/enrollments';
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
    saveInitialSurvey.mockReset().mockReturnValue(of(undefined));
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

  // `canAnswerSurvey` manda sobre el estado de la encuesta: aunque el backend la marque
  // completa, con derecho a responderla los campos siguen visibles y editables.
  it('keeps the survey editable when the backend marks it complete', () => {
    const { survey } = createFacade(
      createSurveyResponse({ survey: createInitialSurvey({ complete: true }) })
    );

    expect(survey.canAnswerSurvey()).toBe(true);
    expect(survey.visibleSections()).toEqual([
      'education',
      'academic-decision',
      'ort-experience',
      'identity',
      'regulation',
    ]);
    expect(survey.activeSection()).toBe('education');
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
    await expect(firstValueFrom(survey.savePartial())).resolves.toBeUndefined();
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it('asks only for the enrollment owner when AP has survey rights', () => {
    const { survey, forms } = createFacade(
      createSurveyResponse({ isEligibleForSurvey: true }),
      {},
      AP_CAREERS
    );

    expect(survey.activeSection()).toBe('education');

    forms.forms.academicForm.controls.proposalType.setValue('3');
    forms.forms.academicForm.controls.degreeProgram.setValue('30');
    TestBed.tick();

    expect(survey.visibleSections()).toEqual(['work-situation', 'identity', 'regulation']);
    expect(survey.activeSection()).toBe('work-situation');
    expect(forms.forms.workForm.controls.isCorporate.hasError('required')).toBe(true);
  });

  it('asks for a corporate enrollment before identity when AP has no survey rights', () => {
    const { survey, forms } = createFacade(
      createSurveyResponse({ isEligibleForSurvey: false }),
      {},
      AP_CAREERS
    );

    expect(survey.activeSection()).toBe('identity');

    forms.forms.academicForm.controls.proposalType.setValue('3');
    forms.forms.academicForm.controls.degreeProgram.setValue('30');
    TestBed.tick();

    expect(survey.visibleSections()).toEqual(['work-situation', 'identity', 'regulation']);
    expect(survey.activeSection()).toBe('work-situation');
  });

  it('does not persist the initial survey for AP even with survey rights', async () => {
    const { survey, forms } = createFacade(createSurveyResponse(), {}, AP_CAREERS);
    forms.forms.academicForm.controls.proposalType.setValue('3');

    await expect(firstValueFrom(survey.savePartial())).resolves.toBeUndefined();
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it('confirms a personal AP pre-enrollment and advances to payment', () => {
    const { survey, forms, process } = createFacade(createSurveyResponse(), {}, AP_CAREERS);
    forms.forms.academicForm.controls.proposalType.setValue('3');
    forms.forms.academicForm.controls.degreeProgram.setValue('30');
    forms.forms.academicForm.controls.seminars.setValue(['300']);
    process.flow.goTo('survey');
    TestBed.tick();

    expect(forms.forms.workForm.controls.isCorporate.hasError('required')).toBe(true);
    forms.forms.workForm.controls.isCorporate.setValue(false);
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
    forms.forms.academicForm.controls.proposalType.setValue('3');
    forms.forms.academicForm.controls.degreeProgram.setValue('30');
    forms.forms.academicForm.controls.seminars.setValue(['300']);
    forms.forms.workForm.controls.isCorporate.setValue(true);
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
    forms.forms.academicForm.controls.shift.setValue('300');

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
    forms.forms.academicForm.controls.shift.setValue('300');

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
    forms.forms.academicForm.controls.shift.setValue('300');

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
    forms.forms.academicForm.controls.shift.setValue('300');

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

  it('waits for document and photo before confirming the pre-enrollment', () => {
    const documentResult = new Subject<boolean>();
    const photoResult = new Subject<boolean>();
    uploadIdentityDocument.mockReturnValue(documentResult);
    uploadIdentityPhoto.mockReturnValue(photoResult);
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledOnce();
    expect(uploadIdentityPhoto).toHaveBeenCalledOnce();
    expect(confirmPreEnrollment).not.toHaveBeenCalled();

    documentResult.next(true);
    documentResult.complete();
    expect(confirmPreEnrollment).not.toHaveBeenCalled();

    photoResult.next(true);
    photoResult.complete();

    // La encuesta no se vuelve a postear: el guardado por check ya la persistió completa.
    expect(saveInitialSurvey).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('payment');
  });

  it('does not post again when nothing changed since the last save', async () => {
    const { survey } = prepareFinalizableSurvey();

    survey.continue();
    expect(confirmPreEnrollment).toHaveBeenCalledOnce();

    await expect(firstValueFrom(survey.savePartial())).resolves.toBeUndefined();
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  // `canAnswerSurvey` es el único gate: con derecho a responderla se guarda siempre, incluso
  // después de que la preinscripción quedó confirmada.
  it('keeps saving edits after the pre-enrollment was confirmed', async () => {
    const { survey } = prepareFinalizableSurvey();

    survey.continue();
    saveInitialSurvey.mockClear();
    survey.educationForm.controls.motherEducation.setValue('6');

    await expect(firstValueFrom(survey.savePartial())).resolves.toBeUndefined();
    expect(saveInitialSurvey).toHaveBeenCalledWith({ motherOrGuardianEducationLevelId: 6 });
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

  it('shows the backend message when the identity upload fails', () => {
    uploadIdentityPhoto.mockReturnValue(
      throwError(() => ({
        status: 400,
        message: 'La foto no cumple el formato requerido.',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('bad request'),
      }))
    );
    const { survey } = prepareFinalizableSurvey();

    survey.continue();

    expect(survey.activeSection()).toBe('identity');
    expect(survey.preEnrollmentError()).toBe('La foto no cumple el formato requerido.');
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

  it('does not confirm when the final survey save fails', () => {
    saveInitialSurvey.mockReturnValue(throwError(() => ({ status: 400 })));
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

  // La encuesta se hidrata UNA sola vez, con los catálogos todavía vacíos: ningún catálogo
  // que llegue después puede borrar lo que trajo el backend ni pisar al usuario.
  it('keeps the answered orientation when the survey catalogs arrive late', () => {
    const catalogs$ = new Subject<unknown>();
    const { survey } = createFacade(
      createSurveyResponse({
        survey: createInitialSurvey({
          activeSection: 'education',
          studiesHighSchool: true,
          highSchoolYearId: 11,
          highSchoolOrientationId: 20,
        }),
      }),
      {},
      [],
      { getInitialSurveyCatalogs: () => catalogs$ }
    );
    TestBed.tick();

    // Sin catálogo todavía: el valor prellenado sobrevive.
    expect(survey.educationForm.controls.orientation.value).toBe('20');

    catalogs$.next({
      education: {
        lastSecondaryYearLocations: [],
        previousHigherEducationOptions: [],
        universities: [],
        guardianEducationLevels: [],
        highSchoolYears: [
          {
            id: 11,
            label: '2 EMS',
            baccalaureates: [{ id: 20, label: 'Bachillerato A', orientation: 'Cientifico' }],
          },
        ],
      },
      academicDecision: {
        upperSecondaryYears: [],
        decisionSupports: [],
        decisionLevels: [],
        universities: [],
        ortChoiceReasons: [],
      },
      ortExperience: { ratings: [], ortAdvertisements: [] },
    });
    catalogs$.complete();

    expect(survey.options.orientationOptions()).toEqual([{ value: '20', label: 'Cientifico' }]);
    expect(survey.educationForm.controls.orientation.value).toBe('20');
  });

  it('keeps the answered orientation when the survey catalogs fail', () => {
    const { survey } = createFacade(
      createSurveyResponse({
        survey: createInitialSurvey({
          activeSection: 'education',
          studiesHighSchool: true,
          highSchoolYearId: 11,
          highSchoolOrientationId: 20,
        }),
      }),
      {},
      [],
      { getInitialSurveyCatalogs: () => throwError(() => new Error('network error')) }
    );
    TestBed.tick();

    expect(survey.catalogError()).toBe('No se pudieron cargar los catálogos de encuesta inicial.');
    expect(survey.educationForm.controls.orientation.value).toBe('20');
  });

  it('does not overwrite an answer the user changed while the catalogs were loading', () => {
    const catalogs$ = new Subject<unknown>();
    const { survey } = createFacade(
      createSurveyResponse({
        survey: createInitialSurvey({ activeSection: 'education', repeatsHighSchoolYear: false }),
      }),
      {},
      [],
      { getInitialSurveyCatalogs: () => catalogs$ }
    );
    TestBed.tick();

    expect(survey.educationForm.controls.repeatsHighSchoolYear.value).toBe('no');
    survey.educationForm.controls.repeatsHighSchoolYear.setValue('yes');

    catalogs$.next({
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
    });
    catalogs$.complete();

    expect(survey.educationForm.controls.repeatsHighSchoolYear.value).toBe('yes');
  });

  // El departamento no viaja en la encuesta: sin sembrar la institución respondida, Educación
  // quedaría inválida al retomar y el usuario tendría que rehacer los dos campos.
  // El departamento no se guarda: el backend lo deriva de la institución y lo devuelve de solo
  // lectura, y con él se arma la cascada departamento → instituciones al retomar.
  it('hydrates the department derived by the backend and loads its institutions', () => {
    const { survey } = createFacade(
      createSurveyResponse({
        survey: createInitialSurvey({
          activeSection: 'education',
          studiesHighSchool: false,
          repeatsHighSchoolYear: false,
          highSchoolLocationId: 1,
          highSchoolInstitutionId: 500,
          highSchoolInstitutionStateId: 5,
          priorHigherEducationStatusId: 3,
          motherEducationLevelId: 1,
          fatherEducationLevelId: 1,
        }),
      }),
      {},
      [],
      {
        countryLocations: [
          {
            countryCode: 1,
            name: 'Uruguay',
            states: [{ countryCode: 1, stateCode: 5, name: 'Montevideo' }],
          },
        ],
        institutions: [{ id: 500, label: 'Liceo Nº 1' }],
      }
    );
    TestBed.tick();

    expect(survey.educationForm.controls.state.value).toBe('5');
    expect(survey.options.institutionOptions()).toEqual([{ value: '500', label: 'Liceo Nº 1' }]);
    expect(survey.educationForm.controls.educationalInstitution.value).toBe('500');
    expect(survey.getSectionState('education')).toBe('complete');
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

    forms.forms.academicForm.controls.proposalType.setValue('1');
    forms.forms.academicForm.controls.degreeProgram.setValue('100');
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

    forms.forms.academicForm.controls.proposalType.setValue('2');
    forms.forms.academicForm.controls.degreeProgram.setValue('200');
    survey.educationForm.controls.studiesHighSchool.setValue('studying');
    survey.educationForm.controls.highSchoolYear.setValue('4');

    expect(survey.isUniversityDegreeProgram()).toBe(false);
    expect(
      survey.educationForm.controls.highSchoolYear.hasError('nonUniversityHighSchoolYear')
    ).toBe(false);
  });

  // Un 500/409/0 en confirm es indeterminado: el backend llama APIs externas y pudo haber
  // creado la inscripción. Se cierra el flujo en la pantalla de espera en vez de dejar al
  // usuario reintentando un confirm que quizá ya funcionó.
  it.each([{ status: 0 }, { status: 409 }, { status: 500 }])(
    'closes the flow on the waiting screen when the confirmation fails with $status',
    ({ status }) => {
      confirmPreEnrollment.mockReturnValue(throwError(() => ({ status })));
      const { survey } = prepareFinalizableSurvey();

      survey.continue();

      expect(payment.outcome()).toBe('enrollment-in-progress');
      expect(survey.confirmOutcomeUncertain()).toBe(true);
      expect(survey.preEnrollmentError()).toBeNull();
      expect(survey.finalizingPreEnrollment()).toBe(false);
    }
  );

  // Un fallo del guardado parcial no dice nada sobre la confirmación: nunca llegó a
  // llamarse. Es reintentable, así que no cierra el flujo en la pantalla de espera.
  it.each([{ status: 0 }, { status: 409 }, { status: 500 }])(
    'stays on the survey when the partial save fails with $status',
    ({ status }) => {
      saveInitialSurvey.mockReturnValue(throwError(() => ({ status })));
      const { survey } = prepareFinalizableSurvey();

      survey.continue();

      expect(confirmPreEnrollment).not.toHaveBeenCalled();
      expect(payment.outcome()).toBeNull();
      expect(survey.confirmOutcomeUncertain()).toBe(false);
      expect(survey.preEnrollmentError()).toBe(
        'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
      );
    }
  );

  it('shows the backend message when the partial save fails', () => {
    saveInitialSurvey.mockReturnValue(
      throwError(() => ({
        status: 409,
        message: 'La encuesta ya fue confirmada.',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('conflict'),
      }))
    );
    const { survey } = prepareFinalizableSurvey();

    survey.continue();

    expect(survey.preEnrollmentError()).toBe('La encuesta ya fue confirmada.');
  });

  it('sets the pre-enrollment error and stops the spinner when confirmation fails for good', () => {
    confirmPreEnrollment.mockReturnValue(throwError(() => ({ status: 400 })));
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

  it('shows the backend message when confirmation fails for good with a normalized error', () => {
    confirmPreEnrollment.mockReturnValue(
      throwError(() => ({
        status: 422,
        message: 'La oferta seleccionada ya no está disponible.',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('unprocessable'),
      }))
    );
    const { survey } = prepareFinalizableSurvey();

    survey.continue();

    expect(survey.preEnrollmentError()).toBe('La oferta seleccionada ya no está disponible.');
  });

  it('reports an error instead of confirming when no shift is selected', () => {
    const { survey, forms } = prepareFinalizableSurvey();
    forms.forms.academicForm.controls.shift.setValue('');

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
    expect(survey.canAnswerSurvey()).toBe(true);
    expect(survey.activeSection()).toBe('education');
    expect(survey.loadingSurveyState()).toBe(false);
  });

  it('shows the survey load error when the resolver reports a failure', () => {
    const { survey } = createFacade(null, {}, [], { loadFailed: true });

    expect(survey.surveyLoadError()).toBe(
      'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.'
    );
  });

  it('shows the backend message when the resolver carries one', () => {
    const { survey } = createFacade(null, {}, [], {
      loadFailed: true,
      loadFailedMessage: 'El servicio no está disponible.',
    });

    expect(survey.surveyLoadError()).toBe('El servicio no está disponible.');
  });

  it('fetchResolvedInitialSurvey maps errors to loadFailed and toggles the loading flag', async () => {
    const getInitialSurvey = vi.fn().mockReturnValue(throwError(() => ({ status: 500 })));
    const { survey } = createFacade(null, {}, [], { skipApply: true, getInitialSurvey });

    await expect(firstValueFrom(survey.fetchResolvedInitialSurvey())).resolves.toEqual({
      initialSurvey: null,
      loadFailed: true,
      loadFailedMessage: 'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.',
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

  // Guardado por delta: cada guardado manda SOLO lo que cambió contra lo último que el backend
  // confirmó. Lo dispara el check de la sección (su form quedó válido) y cada cambio posterior
  // mientras siga completa; el Continuar, el cierre del paso y la salida solo reintentan lo que
  // haya quedado pendiente. Nunca un timer.
  describe('survey delta save', () => {
    it('saves each section the moment it gets its check', () => {
      const { survey } = createFacade(createSurveyResponse());

      completeEducation(survey);

      // Sin pulsar Continuar: alcanza con que la sección quede válida.
      expect(saveInitialSurvey).toHaveBeenCalledOnce();
      expect(saveInitialSurvey).toHaveBeenCalledWith({
        // `completeEducation` responde "No curso secundaria": la respuesta viaja como `false`,
        // no se confunde con "sin responder".
        currentlyStudiesHighSchool: false,
        repeatsHighSchoolYear: false,
        finalHighSchoolYearLocationId: 1,
        priorHigherEducationStatusId: 2,
        motherOrGuardianEducationLevelId: 1,
        fatherOrGuardianEducationLevelId: 1,
      });
      expect(survey.getSectionState('education')).toBe('complete');

      saveInitialSurvey.mockClear();
      completeAcademicDecision(survey);

      // Nada de la sección anterior se repite.
      expect(saveInitialSurvey).toHaveBeenCalledWith({
        degreeProgramDecisionYearId: 1,
        ortDecisionYearId: 1,
        researchedOtherUniversities: false,
        decisionSupportId: 1,
        decisionLevelId: 1,
        ortChoiceReasonIds: [1],
      });

      saveInitialSurvey.mockClear();
      completeOrtExperience(survey);

      expect(saveInitialSurvey).toHaveBeenCalledWith({
        hadOrtAdvising: false,
        visitedOrtWebsite: false,
        visitedOrtCampus: false,
        recallsOrtAdvertising: false,
      });
    });

    it('does not post while the section is incomplete and sends the whole delta when it completes', () => {
      const { survey } = createFacade(createSurveyResponse());

      survey.educationForm.controls.motherEducation.setValue('1');

      expect(survey.getSectionState('education')).not.toBe('complete');
      expect(saveInitialSurvey).not.toHaveBeenCalled();

      completeEducation(survey);

      // Lo respondido antes del check no se pierde: viaja en el delta del primer guardado.
      expect(saveInitialSurvey).toHaveBeenCalledWith({
        // `completeEducation` responde "No curso secundaria": la respuesta viaja como `false`,
        // no se confunde con "sin responder".
        currentlyStudiesHighSchool: false,
        repeatsHighSchoolYear: false,
        finalHighSchoolYearLocationId: 1,
        priorHigherEducationStatusId: 2,
        motherOrGuardianEducationLevelId: 1,
        fatherOrGuardianEducationLevelId: 1,
      });
    });

    it('posts again on every change once the section keeps its check', () => {
      const { survey } = createFacade(createSurveyResponse());
      completeEducation(survey);
      saveInitialSurvey.mockClear();

      survey.educationForm.controls.motherEducation.setValue('2');

      expect(saveInitialSurvey).toHaveBeenCalledWith({ motherOrGuardianEducationLevelId: 2 });
    });

    it('ignores a change in an incomplete section even if another one is complete', () => {
      const { survey } = createFacade(createSurveyResponse());
      completeEducation(survey);
      saveInitialSurvey.mockClear();

      survey.academicDecisionForm.controls.decisionSupport.setValue('1');

      expect(saveInitialSurvey).not.toHaveBeenCalled();
    });

    it('sends the value again when it goes back to what it was before', () => {
      const { survey } = createFacade(createSurveyResponse());
      completeEducation(survey);
      survey.educationForm.controls.motherEducation.setValue('2');
      saveInitialSurvey.mockClear();

      survey.educationForm.controls.motherEducation.setValue('1');

      expect(saveInitialSurvey).toHaveBeenCalledWith({ motherOrGuardianEducationLevelId: 1 });
    });

    it('sends nulls when a conditional answer clears dependent fields', async () => {
      const { survey } = createFacade(createSurveyResponse());
      survey.educationForm.patchValue({ studiesHighSchool: 'studying', highSchoolYear: '11' });

      await expect(firstValueFrom(survey.savePartial())).resolves.toBeUndefined();
      expect(saveInitialSurvey).toHaveBeenCalledWith({
        currentlyStudiesHighSchool: true,
        highSchoolYear: 11,
      });
      saveInitialSurvey.mockClear();

      survey.educationForm.controls.studiesHighSchool.setValue('not-studying');
      await expect(firstValueFrom(survey.savePartial())).resolves.toBeUndefined();

      // El campo vaciado viaja en null: ausente sería "sin cambios" y el backend lo dejaría.
      expect(saveInitialSurvey).toHaveBeenCalledWith({
        currentlyStudiesHighSchool: false,
        highSchoolYear: null,
      });
    });

    it('keeps a failed change pending for the next delta', () => {
      const { survey } = createFacade(createSurveyResponse());
      saveInitialSurvey.mockReturnValue(throwError(() => ({ status: 500 })));

      completeEducation(survey);
      expect(saveInitialSurvey).toHaveBeenCalledOnce();
      // Fallo silencioso: es un guardado oportunista, no molesta al usuario.
      expect(survey.preEnrollmentError()).toBeNull();

      saveInitialSurvey.mockClear().mockReturnValue(of(undefined));
      survey.educationForm.controls.motherEducation.setValue('2');

      expect(saveInitialSurvey).toHaveBeenCalledWith({
        // `completeEducation` responde "No curso secundaria": la respuesta viaja como `false`,
        // no se confunde con "sin responder".
        currentlyStudiesHighSchool: false,
        repeatsHighSchoolYear: false,
        finalHighSchoolYearLocationId: 1,
        priorHigherEducationStatusId: 2,
        motherOrGuardianEducationLevelId: 2,
        fatherOrGuardianEducationLevelId: 1,
      });
    });

    it('retries the pending delta when the section closes with Continuar', () => {
      const { survey } = createFacade(createSurveyResponse());
      saveInitialSurvey.mockReturnValue(throwError(() => ({ status: 500 })));
      completeEducation(survey);
      saveInitialSurvey.mockClear().mockReturnValue(of(undefined));

      survey.continue();

      expect(saveInitialSurvey).toHaveBeenCalledWith({
        // `completeEducation` responde "No curso secundaria": la respuesta viaja como `false`,
        // no se confunde con "sin responder".
        currentlyStudiesHighSchool: false,
        repeatsHighSchoolYear: false,
        finalHighSchoolYearLocationId: 1,
        priorHigherEducationStatusId: 2,
        motherOrGuardianEducationLevelId: 1,
        fatherOrGuardianEducationLevelId: 1,
      });
      expect(survey.activeSection()).toBe('academic-decision');
    });

    it('does not post when the section closes with nothing pending', () => {
      const { survey } = createFacade(createSurveyResponse());
      completeEducation(survey);
      saveInitialSurvey.mockClear();

      survey.continue();

      expect(saveInitialSurvey).not.toHaveBeenCalled();
    });

    it('drops the change during an in-flight save and sends it in the next delta', () => {
      const { survey } = createFacade(createSurveyResponse());
      const inFlight = new Subject<void>();
      saveInitialSurvey.mockReturnValue(inFlight);

      completeEducation(survey);
      expect(saveInitialSurvey).toHaveBeenCalledOnce();

      survey.educationForm.controls.motherEducation.setValue('2');
      // El POST sigue en vuelo: exhaustMap descarta el trigger en lugar de cancelarlo.
      expect(saveInitialSurvey).toHaveBeenCalledOnce();

      saveInitialSurvey.mockClear().mockReturnValue(of(undefined));
      inFlight.next();
      inFlight.complete();
      survey.educationForm.controls.fatherEducation.setValue('2');

      // El cambio descartado no se perdió: entra en el delta siguiente.
      expect(saveInitialSurvey).toHaveBeenCalledWith({
        motherOrGuardianEducationLevelId: 2,
        fatherOrGuardianEducationLevelId: 2,
      });
    });

    it('does not save while the survey right is unknown after a load failure', async () => {
      const { survey } = createFacade(null, {}, [], { loadFailed: true });

      completeEducation(survey);
      await expect(firstValueFrom(survey.savePartial())).resolves.toBeUndefined();

      expect(survey.canAnswerSurvey()).toBe(false);
      expect(saveInitialSurvey).not.toHaveBeenCalled();
    });
  });

  // Valores elegidos para que ninguna pregunta condicional quede requerida con los
  // catálogos vacíos del test.
  function completeEducation(survey: EnrollmentSurveyFacade): void {
    survey.educationForm.patchValue({
      studiesHighSchool: 'not-studying',
      repeatsHighSchoolYear: 'no',
      highSchoolLocation: '1',
      higherEducationStatus: '2',
      motherEducation: '1',
      fatherEducation: '1',
    });
  }

  function completeAcademicDecision(survey: EnrollmentSurveyFacade): void {
    survey.academicDecisionForm.patchValue({
      degreeProgramDecisionYear: '1',
      decisionSupport: '1',
      ortDecisionYear: '1',
      otherUniversities: 'no',
      decisionCertainty: '1',
      ortReasons: ['1'],
    });
  }

  function completeOrtExperience(survey: EnrollmentSurveyFacade): void {
    survey.ortExperienceForm.patchValue({
      advisingMeeting: 'no',
      visitedWebsite: 'no',
      visitedCampus: 'no',
      recallsAdvertising: 'no',
    });
  }

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
    options: {
      loadFailed?: boolean;
      loadFailedMessage?: string;
      getInitialSurvey?: () => unknown;
      skipApply?: boolean;
      getInitialSurveyCatalogs?: () => unknown;
      countryLocations?: unknown[];
      institutions?: unknown[];
    } = {}
  ): {
    survey: EnrollmentSurveyFacade;
    process: EnrollmentProcessState;
    forms: EnrollmentFormsState;
  } {
    const getInitialSurvey = options.getInitialSurvey ?? (() => of(initialSurvey));
    const enrollmentsMock = {
      getStudentRegulationAcceptance,
      getIdentityPreload,
      getInitialSurvey,
      saveInitialSurvey,
      uploadIdentityDocument,
      uploadIdentityPhoto,
      confirmPreEnrollment,
      registerProductInterest: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        { provide: ENROLLMENT_FORMS, useFactory: createEnrollmentFormsState },
        { provide: ENROLLMENT_PROCESS_STATE, useFactory: createEnrollmentProcessState },
        EnrollmentProposalFacade,
        EnrollmentSurveyOptionsFacade,
        EnrollmentSurveyIdentityFacade,
        EnrollmentSurveyFacade,
        {
          provide: CatalogsApi,
          useValue: {
            getDegreePrograms: () => of(degreePrograms),
            getIntakes: () => of([]),
            getShifts: () => of([]),
            getSeminars: () =>
              of([
                { offeringId: 300, admissionProcessId: 200, name: 'Marco legal', startDate: null },
              ]),
            getCountryLocations: () => of(options.countryLocations ?? []),
            getInstitutions: () => of(options.institutions ?? []),
            getInitialSurveyCatalogs:
              options.getInitialSurveyCatalogs ??
              (() =>
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
                })),
          },
        },
        { provide: EnrollmentPaymentFacade, useValue: payment },
        { provide: Enrollments, useValue: enrollmentsMock },
        { provide: EnrollmentsApi, useValue: enrollmentsMock },
      ],
    });
    const survey = TestBed.inject(EnrollmentSurveyFacade);
    const process = TestBed.inject(ENROLLMENT_PROCESS_STATE);
    const forms = TestBed.inject(ENROLLMENT_FORMS);

    // Réplica de lo que hace EnrollmentProcessFacade.applyInitialState para el slice
    // de encuesta: deriva y aplica, posicionando el paso al final.
    if (!options.skipApply) {
      const resolved: EnrollmentInitialSurveyResolved = {
        initialSurvey: (initialSurvey ?? null) as EnrollmentInitialSurveyResolved['initialSurvey'],
        loadFailed: options.loadFailed ?? false,
        loadFailedMessage: options.loadFailedMessage,
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

  // Paso 2 listo para cerrar: con derecho a encuesta las cinco secciones son visibles, así
  // que las tres de respuestas se completan además de identidad y reglamento.
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
    completeEducation(result.survey);
    completeAcademicDecision(result.survey);
    completeOrtExperience(result.survey);
    const front = new File(['front'], 'front.png', { type: 'image/png' });
    const back = new File(['back'], 'back.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    result.survey.identityForm.controls.documentExpiration.setValue(new Date(2030, 1, 4));
    result.survey.identityForm.controls.documentExpiration.markAsDirty();
    result.survey.identity.updateIdentityFile('front', fileEvent(front));
    result.survey.identity.updateIdentityFile('back', fileEvent(back));
    result.survey.identity.updateIdentityFile('selfie', fileEvent(selfie));
    result.survey.regulationForm.controls.acceptsRegulation.setValue(true);
    result.forms.forms.academicForm.controls.shift.setValue('300');
    // Completar las secciones ya posteo el delta por check: los tests de cierre miden desde acá.
    saveInitialSurvey.mockClear();

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
      highSchoolInstitutionStateId: null,
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
