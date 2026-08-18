import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { EnrollmentsEndpoint } from '../endpoints/enrollments.endpoint';
import { Enrollments } from './enrollments';

describe('Enrollments', () => {
  let service: Enrollments;
  let endpointMock: {
    confirmPreEnrollment: ReturnType<typeof vi.fn>;
    reactivate: ReturnType<typeof vi.fn>;
    getDetail: ReturnType<typeof vi.fn>;
    getIdentityDocument: ReturnType<typeof vi.fn>;
    getIdentityPhoto: ReturnType<typeof vi.fn>;
    uploadIdentityDocument: ReturnType<typeof vi.fn>;
    uploadIdentityPhoto: ReturnType<typeof vi.fn>;
    getInitialSurvey: ReturnType<typeof vi.fn>;
    getStudentRegulationAcceptance: ReturnType<typeof vi.fn>;
    saveInitialSurvey: ReturnType<typeof vi.fn>;
    registerProductInterest: ReturnType<typeof vi.fn>;
    pay: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      confirmPreEnrollment: vi.fn().mockReturnValue(
        of({
          confirmed: true,
          paymentDueDate: null,
          enrollmentDeposit: null,
          accountBalance: null,
          summary: null,
        })
      ),
      reactivate: vi.fn().mockReturnValue(
        of({
          confirmed: false,
          paymentDueDate: null,
          enrollmentDeposit: 15500,
          accountBalance: null,
          summary: null,
        })
      ),
      getDetail: vi
        .fn()
        .mockReturnValue(
          of({ status: 'A la espera', summary: null, pendingPayment: null, confirmed: null })
        ),
      getIdentityDocument: vi.fn().mockReturnValue(of({})),
      getIdentityPhoto: vi.fn().mockReturnValue(of(new Blob())),
      uploadIdentityDocument: vi.fn().mockReturnValue(of(true)),
      uploadIdentityPhoto: vi.fn().mockReturnValue(of(true)),
      getInitialSurvey: vi.fn().mockReturnValue(
        of({
          isEligibleForSurvey: true,
          survey: null,
          consideredUniversities: [],
          otherConsideredUniversities: [],
          higherEducationUniversities: [],
          otherHigherEducationUniversities: [],
          selectedReasonOptions: [],
          selectedAdvertisingOptions: [],
        })
      ),
      getStudentRegulationAcceptance: vi
        .fn()
        .mockReturnValue(of({ acceptedStudentRegulation: false, acceptanceDate: null })),
      saveInitialSurvey: vi.fn().mockReturnValue(of(true)),
      registerProductInterest: vi.fn().mockReturnValue(of(true)),
      pay: vi.fn().mockReturnValue(
        of({
          success: true,
          result: null,
          paymentUrl: null,
          encryptedParameters: null,
          messages: [],
          confirmed: null,
          message: null,
          errorCode: null,
        })
      ),
    };

    TestBed.configureTestingModule({
      providers: [Enrollments, { provide: EnrollmentsEndpoint, useValue: endpointMock }],
    });
    service = TestBed.inject(Enrollments);
  });

  it('maps identity document and photo responses to preload files', async () => {
    endpointMock.getIdentityDocument.mockReturnValueOnce(
      of({
        front: { fileName: 'front.png', content: 'aGVsbG8=' },
        back: {
          fileName: 'folder\\back.jpg',
          content: 'data:image/jpeg;base64,d29ybGQ=',
        },
        expirationDate: '2030-02-04',
      })
    );
    endpointMock.getIdentityPhoto.mockReturnValueOnce(
      of(new Blob(['photo'], { type: 'image/png' }))
    );

    const preload = await firstValueFrom(service.getIdentityPreload());

    expect(preload.front).toEqual(
      expect.objectContaining({ name: 'front.png', size: 5, type: 'image/png' })
    );
    expect(preload.back).toEqual(
      expect.objectContaining({ name: 'back.jpg', size: 5, type: 'image/jpeg' })
    );
    expect(preload.selfie).toEqual(
      expect.objectContaining({ name: 'identity-photo.png', size: 5, type: 'image/png' })
    );
    expect(preload.expirationDate).toBe('2030-02-04');
  });

  it('returns an empty identity preload when person files are unavailable', async () => {
    endpointMock.getIdentityDocument.mockReturnValueOnce(
      throwError(() => new Error('document unavailable'))
    );
    endpointMock.getIdentityPhoto.mockReturnValueOnce(
      throwError(() => new Error('photo unavailable'))
    );

    await expect(firstValueFrom(service.getIdentityPreload())).resolves.toEqual({
      front: null,
      back: null,
      selfie: null,
      expirationDate: null,
    });
  });

  it('uploads identity document files as base64', async () => {
    const front = new File(['front'], 'front.png', { type: 'image/png' });
    const back = new File(['back'], 'back.jpg', { type: 'image/jpeg' });

    await expect(
      firstValueFrom(service.uploadIdentityDocument({ date: '2030-02-04', front, back }))
    ).resolves.toBe(true);

    expect(endpointMock.uploadIdentityDocument).toHaveBeenCalledWith({
      date: '2030-02-04',
      front: { fileName: 'front.png', content: 'ZnJvbnQ=' },
      back: { fileName: 'back.jpg', content: 'YmFjaw==' },
    });
  });

  it('uploads identity photo as base64', async () => {
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    await expect(firstValueFrom(service.uploadIdentityPhoto(selfie))).resolves.toBe(true);

    expect(endpointMock.uploadIdentityPhoto).toHaveBeenCalledWith({
      attachedFile: { fileName: 'selfie.png', content: 'cGhvdG8=' },
    });
  });
  it('delegates initial survey loading and saving', () => {
    const payload = {
      degreeProgramId: 20,
      intakeId: 200,
      highSchoolOrientationId: null,
      highSchoolYear: null,
      currentlyStudiesHighSchool: null,
      highSchoolYearRepeatCount: null,
      repeatsHighSchoolYear: null,
      fatherOrGuardianEducationLevelId: null,
      motherOrGuardianEducationLevelId: null,
      degreeProgramDecisionYearId: null,
      ortDecisionYearId: null,
      researchedOtherUniversities: null,
      otherUniversitiesInfoLine1: null,
      otherUniversitiesInfoLine2: null,
      decisionSupportId: null,
      highSchoolInstitutionId: null,
      highSchoolInstitutionName: null,
      finalHighSchoolYearLocationId: null,
      priorHigherEducationStatusId: null,
      decisionLevelId: null,
      hadOrtAdvising: null,
      ortAdvisingRatingId: null,
      visitedOrtWebsite: null,
      ortWebsiteRatingId: null,
      visitedOrtCampus: null,
      ortCampusRatingId: null,
      recallsOrtAdvertising: null,
      isMotherOrGuardianOrtGraduate: null,
      isFatherOrGuardianOrtGraduate: null,
      consideredUniversityIds: null,
      otherConsideredUniversities: null,
      higherEducationUniversityIds: null,
      otherHigherEducationUniversities: null,
      ortAdvertisingIds: null,
      ortChoiceReasonIds: null,
    };

    service.getInitialSurvey().subscribe();
    service.saveInitialSurvey(payload).subscribe();

    expect(endpointMock.getInitialSurvey).toHaveBeenCalledOnce();
    expect(endpointMock.saveInitialSurvey).toHaveBeenCalledWith(payload);
  });

  it('delegates enrollment detail loading', () => {
    service.getDetail(20, 200, 'Pago pendiente').subscribe();

    expect(endpointMock.getDetail).toHaveBeenCalledWith(20, 200, 'Pago pendiente');
  });

  it('delegates student regulation acceptance loading', () => {
    service.getStudentRegulationAcceptance().subscribe();

    expect(endpointMock.getStudentRegulationAcceptance).toHaveBeenCalledOnce();
  });

  it('delegates pre-enrollment confirmation', () => {
    const payload = {
      acceptedRegulation: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    };

    service.confirmPreEnrollment(payload).subscribe();

    expect(endpointMock.confirmPreEnrollment).toHaveBeenCalledWith(payload);
  });

  it('delegates enrollment reactivation', () => {
    service.reactivate([100, 101]).subscribe();

    expect(endpointMock.reactivate).toHaveBeenCalledWith([100, 101]);
  });

  it('delegates payment', () => {
    const payload = {
      enrollmentIds: [1072704],
      paymentMethod: 'bank-account' as const,
      sistarbancBankId: 'brou',
    };

    service.pay(payload).subscribe();

    expect(endpointMock.pay).toHaveBeenCalledWith(payload);
  });
});
