import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { ApiHttpClient } from '../../../shared/api/core/api-http-client';
import {
  getEnrollmentsDetailsEndpoint,
  getEnrollmentsInitialSurveyEndpoint,
  getEnrollmentsStudentRegulationsEndpoint,
  postEnrollmentsConfirmPreEnrollmentEndpoint,
  postEnrollmentsInitialSurveyEndpoint,
  postEnrollmentsProductInterestEndpoint,
  postEnrollmentsReactivateEndpoint,
  postEnrollmentsStartPaymentEndpoint,
} from '../../../shared/api/generated/endpoints/enrollments.endpoints';
import {
  getPersonIdentityDocumentEndpoint,
  getPersonPhotoEndpoint,
  postPersonIdentityDocumentEndpoint,
  postPersonPhotoEndpoint,
} from '../../../shared/api/generated/endpoints/person.endpoints';
import type { EnrollmentInitialSurveyPayload } from '../models/enrollment-flow';
import { EnrollmentsEndpoint } from './enrollments.endpoint';

describe('EnrollmentsEndpoint', () => {
  let endpoint: EnrollmentsEndpoint;
  let apiMock: {
    request: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    apiMock = {
      request: vi.fn().mockReturnValue(of(true)),
    };

    TestBed.configureTestingModule({
      providers: [EnrollmentsEndpoint, { provide: ApiHttpClient, useValue: apiMock }],
    });
    endpoint = TestBed.inject(EnrollmentsEndpoint);
  });

  it('maps enrollment detail without exposing generated contracts', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Confirmada',
        confirmed: {
          personId: 397654,
          productId: 20,
          degreeProgram: 'Sistemas',
          academicCoordinator: { name: 'Ana Coordinadora', email: 'ana@example.com' },
          enrollments: [{ firstSemesterSubjects: [{ subjectId: 1, name: 'Programación' }, {}] }],
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(20, 200, 'Confirmada'))).resolves.toEqual({
      status: 'Confirmada',
      summary: null,
      interests: [],
      pendingPayment: null,
      minimumDeposit: null,
      confirmed: {
        studentNumber: 397654,
        summary: {
          offeringId: null,
          productId: 20,
          degreeProgram: 'Sistemas',
          intake: null,
          shift: null,
        },
        academicCoordinator: { name: 'Ana Coordinadora', email: 'ana@example.com' },
        courseCoordinator: null,
        enrollments: [
          {
            enrollmentId: null,
            offeringId: null,
            intake: null,
            shift: null,
            firstSemesterSubjects: [
              { subjectId: 1, name: 'Programación' },
              { subjectId: null, name: null },
            ],
          },
        ],
      },
    });
    expect(apiMock.request).toHaveBeenCalledWith(getEnrollmentsDetailsEndpoint, {
      queryParams: { productId: 20, admissionProcessId: 200, status: 'Confirmada' },
      showLoader: true,
    });
  });

  it('omits the status query param when no estado is known', async () => {
    apiMock.request.mockReturnValueOnce(of({}));

    await firstValueFrom(endpoint.getDetail(20, 200));

    expect(apiMock.request).toHaveBeenCalledWith(getEnrollmentsDetailsEndpoint, {
      queryParams: { productId: 20, admissionProcessId: 200 },
      showLoader: true,
    });
  });

  it('maps every interest offering of an in-progress detail', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'En proceso',
        inProgress: {
          summary: { productId: 40, degreeProgram: 'Asesoramiento financiero' },
          interests: [
            {
              offeringId: 310,
              offeringDescription: 'Marco legal',
              intake: 'Abril',
              shift: 'Noche',
            },
            { offeringId: 311, offeringDescription: 'Renta fija' },
          ],
        },
      })
    );

    const detail = await firstValueFrom(endpoint.getDetail(40, 210));

    // El resumen toma la primera oferta; `interests` conserva todas (una por seminario).
    expect(detail.summary).toEqual({
      offeringId: 310,
      productId: 40,
      degreeProgram: 'Asesoramiento financiero',
      intake: 'Abril',
      shift: 'Noche',
    });
    expect(detail.interests).toEqual([
      {
        enrollmentId: null,
        offeringId: 310,
        name: 'Marco legal',
        intake: 'Abril',
        shift: 'Noche',
      },
      { enrollmentId: null, offeringId: 311, name: 'Renta fija', intake: null, shift: null },
    ]);
  });

  it('maps the course coordinator alongside the academic coordinator', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Confirmada',
        confirmed: {
          personId: 397654,
          academicCoordinator: { name: 'Ana Coordinadora', email: 'ana@example.com' },
          courseCoordinator: { name: 'Beto Cursos', email: 'beto@example.com' },
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(20, 200))).resolves.toEqual(
      expect.objectContaining({
        confirmed: expect.objectContaining({
          academicCoordinator: { name: 'Ana Coordinadora', email: 'ana@example.com' },
          courseCoordinator: { name: 'Beto Cursos', email: 'beto@example.com' },
        }),
      })
    );
  });

  it('maps the seniaMinima block when the payment method was already chosen', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Pago pendiente',
        minimumDeposit: {
          paymentType: 'ABITAB',
          documentNumber: '12345678',
          personId: 555,
          depositAmount: 3339,
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(719, 1398))).resolves.toEqual(
      expect.objectContaining({
        status: 'Pago pendiente',
        pendingPayment: null,
        minimumDeposit: {
          paymentMethod: 'ABITAB',
          documentNumber: '12345678',
          personCode: 555,
          deposit: 3339,
        },
      })
    );
  });

  it('maps pending payment account balance from enrollment detail', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Pago pendiente',
        pendingPayment: {
          enrollments: [{ enrollmentId: 1072704, paymentDueDate: '2026-06-26T16:29:20' }],
          depositAmount: 3339,
          currentAccount: { currentBalance: 70000 },
          summary: { degreeProgram: 'Arquitectura' },
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(719, 1398))).resolves.toEqual(
      expect.objectContaining({
        pendingPayment: expect.objectContaining({
          deposit: 3339,
          accountBalance: 70000,
        }),
      })
    );
  });

  it('maps the Actualización profesional seminarios array from a pending payment detail', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Pago pendiente',
        pendingPayment: {
          enrollments: [
            {
              enrollmentId: 1072704,
              offeringId: 58563,
              intake: 'Marzo',
              shift: 'Matutino',
              offeringDescription: 'Seminario de Liderazgo',
            },
            {
              enrollmentId: 1072705,
              offeringId: 58564,
              intake: 'Abril',
              shift: 'Nocturno',
              offeringDescription: 'Seminario de Finanzas',
            },
          ],
          depositAmount: 3339,
          currentAccount: { currentBalance: 70000 },
          summary: { degreeProgram: 'Actualización profesional' },
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(719, 1398))).resolves.toEqual(
      expect.objectContaining({
        pendingPayment: expect.objectContaining({
          seminars: [
            {
              enrollmentId: 1072704,
              offeringId: 58563,
              name: 'Seminario de Liderazgo',
              intake: 'Marzo',
              shift: 'Matutino',
            },
            {
              enrollmentId: 1072705,
              offeringId: 58564,
              name: 'Seminario de Finanzas',
              intake: 'Abril',
              shift: 'Nocturno',
            },
          ],
        }),
      })
    );
  });
  it('maps identity document fields to the feature contract', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        front: { content: 'front', fileName: 'front.png' },
        back: {},
        expirationDate: '2030-02-04',
      })
    );

    await expect(firstValueFrom(endpoint.getIdentityDocument())).resolves.toEqual({
      front: { content: 'front', fileName: 'front.png' },
      back: { content: null, fileName: null },
      expirationDate: '2030-02-04',
    });
    expect(apiMock.request).toHaveBeenCalledWith(getPersonIdentityDocumentEndpoint);
  });

  it('loads the identity photo as a blob', () => {
    endpoint.getIdentityPhoto().subscribe();

    expect(apiMock.request).toHaveBeenCalledWith(getPersonPhotoEndpoint, {
      responseType: 'blob',
    });
  });

  it('uploads identity document files', async () => {
    const payload = {
      date: '2030-02-04',
      front: { fileName: 'front.png', content: 'front' },
      back: { fileName: 'back.png', content: 'back' },
    };

    await expect(firstValueFrom(endpoint.uploadIdentityDocument(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postPersonIdentityDocumentEndpoint, {
      body: {
        expirationDate: '2030-02-04',
        front: { fileName: 'front.png', content: 'front' },
        back: { fileName: 'back.png', content: 'back' },
      },
      showLoader: true,
    });
  });

  it('uploads identity photo', async () => {
    const payload = { attachedFile: { fileName: 'selfie.png', content: 'photo' } };

    await expect(firstValueFrom(endpoint.uploadIdentityPhoto(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postPersonPhotoEndpoint, {
      body: { file: { fileName: 'selfie.png', content: 'photo' } },
      showLoader: true,
    });
  });
  it('maps the initial survey to the feature contract', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        canAnswerSurvey: true,
        survey: {
          surveyId: 1,
          degreeProgramId: 20,
          admissionProcessId: 200,
          status: 'completa',
          currentlyInSecondary: true,
          repeatsHighSchoolYear: true,
          highSchoolYearRepeatCount: 2,
          previousHigherEducationId: 1,
          decisionLevelId: 1,
          consideredUniversityIds: [10],
          consideredUniversityOthers: ['Otra consultada'],
          higherEducationUniversityIds: [20],
          higherEducationUniversityOthers: ['Otra superior'],
          ortChoiceReasonIds: [5],
          ortAdvertisingIds: [7],
        },
      })
    );

    const result = await firstValueFrom(endpoint.getInitialSurvey());

    expect(result).toEqual(
      expect.objectContaining({
        isEligibleForSurvey: true,
        survey: expect.objectContaining({
          degreeProgramId: 20,
          intakeId: 200,
          complete: true,
          studiesHighSchool: true,
          repeatsHighSchoolYear: true,
          highSchoolYearRepeatCount: 2,
          priorHigherEducationStatusId: 1,
          decisionLevelId: 1,
        }),
        consideredUniversities: [10],
        otherConsideredUniversities: ['Otra consultada'],
        higherEducationUniversities: [20],
        otherHigherEducationUniversities: ['Otra superior'],
        selectedReasonOptions: [5],
        selectedAdvertisingOptions: [7],
      })
    );
    expect(apiMock.request).toHaveBeenCalledWith(getEnrollmentsInitialSurveyEndpoint);
  });

  it('preserves the regulation acceptance date and normalizes missing values', async () => {
    apiMock.request.mockReturnValueOnce(
      of({ acceptedStudentRegulations: true, acceptanceDate: '2026-06-01' })
    );

    await expect(firstValueFrom(endpoint.getStudentRegulationAcceptance())).resolves.toEqual({
      acceptedStudentRegulation: true,
      acceptanceDate: '2026-06-01',
    });

    apiMock.request.mockReturnValueOnce(of({}));
    await expect(firstValueFrom(endpoint.getStudentRegulationAcceptance())).resolves.toEqual({
      acceptedStudentRegulation: false,
      acceptanceDate: null,
    });
    expect(apiMock.request).toHaveBeenCalledWith(getEnrollmentsStudentRegulationsEndpoint);
  });

  it('maps the survey payload', async () => {
    const payload: EnrollmentInitialSurveyPayload = {
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

    await expect(firstValueFrom(endpoint.saveInitialSurvey(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsInitialSurveyEndpoint, {
      body: expect.objectContaining({
        degreeProgramId: 20,
        admissionProcessId: 200,
        currentlyInSecondary: null,
        consideredUniversityOthers: null,
        higherEducationUniversityOthers: null,
      }),
      showLoader: true,
    });
  });

  it('maps pre-enrollment response', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        confirmed: true,
        waiting: true,
        enrollmentId: null,
        paymentDueDate: null,
        depositAmount: 0,
        currentAccount: { currentBalance: 70000 },
        summary: { degreeProgram: 'Sistemas' },
        enrollments: [{ intake: 'Marzo', shift: 'Matutino' }],
      })
    );
    const payload = {
      acceptedRegulation: true,
      isCorporateEnrollment: true,
      selectedOfferingIds: [300],
    };

    await expect(firstValueFrom(endpoint.confirmPreEnrollment(payload))).resolves.toEqual({
      confirmed: true,
      isWaiting: true,
      enrollmentId: null,
      paymentDueDate: null,
      enrollmentDeposit: 0,
      accountBalance: 70000,
      summary: { degreeProgram: 'Sistemas', intake: 'Marzo', shift: 'Matutino' },
      seminars: [
        { enrollmentId: null, offeringId: null, name: null, intake: 'Marzo', shift: 'Matutino' },
      ],
    });
    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsConfirmPreEnrollmentEndpoint, {
      body: {
        acceptedRegulations: true,
        isCorporateEnrollment: true,
        selectedOfferingIds: [300],
      },
      showLoader: true,
    });
  });

  it('maps the Actualización profesional seminarios array from confirm-pre-enrollment', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        confirmed: false,
        summary: { degreeProgram: 'Actualización profesional' },
        enrollments: [
          {
            enrollmentId: 1072704,
            offeringId: 58563,
            intake: 'Marzo',
            shift: 'Matutino',
            offeringDescription: 'Seminario de Liderazgo',
          },
          {
            enrollmentId: 1072705,
            offeringId: 58564,
            intake: 'Abril',
            shift: 'Nocturno',
            offeringDescription: 'Seminario de Finanzas',
          },
        ],
      })
    );

    const response = await firstValueFrom(
      endpoint.confirmPreEnrollment({
        acceptedRegulation: true,
        isCorporateEnrollment: false,
        selectedOfferingIds: [58563, 58564],
      })
    );

    expect(response.seminars).toEqual([
      {
        enrollmentId: 1072704,
        offeringId: 58563,
        name: 'Seminario de Liderazgo',
        intake: 'Marzo',
        shift: 'Matutino',
      },
      {
        enrollmentId: 1072705,
        offeringId: 58564,
        name: 'Seminario de Finanzas',
        intake: 'Abril',
        shift: 'Nocturno',
      },
    ]);
  });

  it('maps reactivation with the pre-enrollment contract', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        confirmed: false,
        waiting: false,
        depositAmount: 15500,
        currentAccount: { currentBalance: 1200 },
        summary: { degreeProgram: 'Actualización profesional', paymentDueDate: '2027-03-04' },
        enrollments: [
          {
            enrollmentId: 1072704,
            offeringId: 58563,
            intake: 'Marzo',
            shift: 'Matutino',
            offeringDescription: 'Seminario de Liderazgo',
          },
        ],
      })
    );

    await expect(firstValueFrom(endpoint.reactivate([100, 101]))).resolves.toEqual({
      confirmed: false,
      isWaiting: false,
      enrollmentId: 1072704,
      paymentDueDate: '2027-03-04',
      enrollmentDeposit: 15500,
      accountBalance: 1200,
      summary: {
        degreeProgram: 'Actualización profesional',
        intake: 'Marzo',
        shift: 'Matutino',
      },
      seminars: [
        {
          enrollmentId: 1072704,
          offeringId: 58563,
          name: 'Seminario de Liderazgo',
          intake: 'Marzo',
          shift: 'Matutino',
        },
      ],
    });
    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsReactivateEndpoint, {
      body: { enrollmentIds: [100, 101] },
      showLoader: true,
    });
  });

  it('maps bank account payment to Sistarbanc payload', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        result: 'pendiente',
        paymentUrl: 'https://pagos.example/sistarbanc',
        encryptedParameters: 'token-encriptado',
        messages: [{ key: 'factura', value: 'Creada' }],
      })
    );

    await expect(
      firstValueFrom(
        endpoint.pay({
          enrollmentIds: [1072704, 1072705],
          paymentMethod: 'bank-account',
          sistarbancBankId: 'brou',
        })
      )
    ).resolves.toEqual({
      success: true,
      result: 'pendiente',
      paymentUrl: 'https://pagos.example/sistarbanc',
      encryptedParameters: 'token-encriptado',
      messages: [{ key: 'factura', value: 'Creada' }],
      confirmed: null,
      message: null,
      errorCode: null,
    });
    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsStartPaymentEndpoint, {
      body: {
        enrollmentIds: [1072704, 1072705],
        paymentType: 'SISTARBANC',
        sistarbancBankId: 'brou',
      },
    });
  });

  it('maps the confirmada block when the backend confirms the payment inline', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        result: 'confirmada',
        paymentUrl: null,
        encryptedParameters: null,
        messages: [],
        // La confirmada trae producto/carrera en la cabecera y comienzo/turno/materias
        // en cada inscripción confirmada.
        confirmed: {
          personId: 34692671,
          productId: 20,
          degreeProgram: 'Sistemas',
          academicCoordinator: { name: 'Ana', email: 'ana@ort.edu.uy' },
          courseCoordinator: null,
          enrollments: [
            {
              enrollmentId: 1072704,
              offeringId: 300,
              intake: 'Marzo',
              shift: 'Matutino',
              firstSemesterSubjects: [{ subjectId: 1, name: 'Cálculo' }],
            },
          ],
        },
      })
    );

    const response = await firstValueFrom(
      endpoint.pay({
        enrollmentIds: [1],
        paymentMethod: 'personal-account',
        sistarbancBankId: null,
      })
    );

    expect(response.confirmed).toEqual({
      studentNumber: 34692671,
      summary: {
        offeringId: 300,
        productId: 20,
        degreeProgram: 'Sistemas',
        intake: 'Marzo',
        shift: 'Matutino',
      },
      academicCoordinator: { name: 'Ana', email: 'ana@ort.edu.uy' },
      courseCoordinator: null,
      enrollments: [
        {
          enrollmentId: 1072704,
          offeringId: 300,
          intake: 'Marzo',
          shift: 'Matutino',
          firstSemesterSubjects: [{ subjectId: 1, name: 'Cálculo' }],
        },
      ],
    });
  });

  it('maps each payment method without leaking generated contracts', async () => {
    apiMock.request.mockReturnValue(of({}));

    await firstValueFrom(
      endpoint.pay({
        enrollmentIds: [1],
        paymentMethod: 'personal-account',
        sistarbancBankId: null,
      })
    );
    await firstValueFrom(
      endpoint.pay({ enrollmentIds: [1], paymentMethod: 'abitab', sistarbancBankId: null })
    );
    await firstValueFrom(
      endpoint.pay({ enrollmentIds: [1], paymentMethod: 'paganza', sistarbancBankId: null })
    );
    await firstValueFrom(
      endpoint.pay({ enrollmentIds: [1], paymentMethod: 'banred', sistarbancBankId: null })
    );
    await firstValueFrom(
      endpoint.pay({ enrollmentIds: [1], paymentMethod: 'geopay', sistarbancBankId: null })
    );

    expect(apiMock.request).toHaveBeenNthCalledWith(
      1,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'CUENTA_PERSONAL' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      2,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'ABITAB' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      3,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'PAGANZA' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      4,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'BANRED' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      5,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'GEOPAY' }) })
    );
  });
  it('maps product interest payload and boolean response', async () => {
    const payload = { offeringIds: [300], selectedAdmissionProcessId: 200, productId: 20 };

    await expect(firstValueFrom(endpoint.registerProductInterest(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsProductInterestEndpoint, {
      body: { offeringIds: [300], admissionProcessId: 200, productId: 20 },
      showLoader: true,
    });
  });

  it('propagates HTTP errors without catching them silently', async () => {
    const failure = new HttpErrorResponse({ status: 500 });
    const operations: readonly (() => Observable<unknown>)[] = [
      () => endpoint.getDetail(20, 200),
      () => endpoint.saveInitialSurvey(createSurveyPayload()),
      () =>
        endpoint.confirmPreEnrollment({
          acceptedRegulation: true,
          isCorporateEnrollment: false,
          selectedOfferingIds: [300],
        }),
      () => endpoint.reactivate([100]),
      () => endpoint.pay({ enrollmentIds: [1], paymentMethod: 'abitab', sistarbancBankId: null }),
    ];

    for (const operation of operations) {
      apiMock.request.mockReturnValueOnce(throwError(() => failure));
      await expect(firstValueFrom(operation())).rejects.toBe(failure);
    }
  });

  it('maps the complete survey payload to the generated request', async () => {
    await expect(firstValueFrom(endpoint.saveInitialSurvey(createSurveyPayload()))).resolves.toBe(
      true
    );

    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsInitialSurveyEndpoint, {
      body: {
        degreeProgramId: 20,
        admissionProcessId: 200,
        highSchoolTrackId: 3,
        highSchoolYear: 2025,
        currentlyInSecondary: false,
        highSchoolYearRepeatCount: 1,
        repeatsHighSchoolYear: true,
        fatherEducationLevelId: 4,
        motherEducationLevelId: 5,
        careerDecisionYearId: 6,
        ortDecisionYearId: 7,
        researchedOtherUniversities: true,
        otherUniversitiesInfoLine1: 'UCU',
        otherUniversitiesInfoLine2: 'UM',
        decisionSupportId: 8,
        secondaryInstitutionId: 9,
        secondaryInstitutionName: 'Liceo 1',
        lastSecondaryYearLocationId: 10,
        previousHigherEducationId: 11,
        decisionLevelId: 12,
        hadOrtAdvisory: true,
        ortAdvisoryRatingId: 13,
        visitedOrtWebsite: true,
        ortWebsiteRatingId: 14,
        visitedOrtFacilities: false,
        ortFacilitiesRatingId: 15,
        recallsOrtAdvertising: true,
        motherIsOrtGraduate: false,
        fatherIsOrtGraduate: true,
        consideredUniversityIds: [10, 11],
        consideredUniversityOthers: ['Otra consultada'],
        higherEducationUniversityIds: [20],
        higherEducationUniversityOthers: ['Otra superior'],
        ortAdvertisingIds: [7],
        ortChoiceReasonIds: [5],
      },
      showLoader: true,
    });
  });

  it('normalizes a detail response without estado to nulls', async () => {
    apiMock.request.mockReturnValueOnce(of({}));

    await expect(firstValueFrom(endpoint.getDetail(20, 200))).resolves.toEqual({
      status: null,
      summary: null,
      interests: [],
      pendingPayment: null,
      minimumDeposit: null,
      confirmed: null,
    });
  });

  it('drops an unknown survey estado to a null active section', async () => {
    apiMock.request.mockReturnValueOnce(
      of({ canAnswerSurvey: true, survey: { status: 'en-revision' } })
    );
    const unknown = await firstValueFrom(endpoint.getInitialSurvey());

    expect(unknown.survey?.activeSection).toBeNull();
    expect(unknown.survey?.complete).toBe(false);

    apiMock.request.mockReturnValueOnce(
      of({ canAnswerSurvey: true, survey: { status: 'identidad' } })
    );
    const known = await firstValueFrom(endpoint.getInitialSurvey());

    expect(known.survey?.activeSection).toBe('identity');
  });
});

function createSurveyPayload(): EnrollmentInitialSurveyPayload {
  return {
    degreeProgramId: 20,
    intakeId: 200,
    highSchoolOrientationId: 3,
    highSchoolYear: 2025,
    currentlyStudiesHighSchool: false,
    highSchoolYearRepeatCount: 1,
    repeatsHighSchoolYear: true,
    fatherOrGuardianEducationLevelId: 4,
    motherOrGuardianEducationLevelId: 5,
    degreeProgramDecisionYearId: 6,
    ortDecisionYearId: 7,
    researchedOtherUniversities: true,
    otherUniversitiesInfoLine1: 'UCU',
    otherUniversitiesInfoLine2: 'UM',
    decisionSupportId: 8,
    highSchoolInstitutionId: 9,
    highSchoolInstitutionName: 'Liceo 1',
    finalHighSchoolYearLocationId: 10,
    priorHigherEducationStatusId: 11,
    decisionLevelId: 12,
    hadOrtAdvising: true,
    ortAdvisingRatingId: 13,
    visitedOrtWebsite: true,
    ortWebsiteRatingId: 14,
    visitedOrtCampus: false,
    ortCampusRatingId: 15,
    recallsOrtAdvertising: true,
    isMotherOrGuardianOrtGraduate: false,
    isFatherOrGuardianOrtGraduate: true,
    consideredUniversityIds: [10, 11],
    otherConsideredUniversities: ['Otra consultada'],
    higherEducationUniversityIds: [20],
    otherHigherEducationUniversities: ['Otra superior'],
    ortAdvertisingIds: [7],
    ortChoiceReasonIds: [5],
  };
}
