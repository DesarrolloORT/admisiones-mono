import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getEnrollmentsDetailsEndpoint,
  getEnrollmentsInitialSurveyEndpoint,
  getEnrollmentsStudentRegulationsEndpoint,
  postEnrollmentsConfirmPreEnrollmentEndpoint,
  postEnrollmentsInitialSurveyEndpoint,
  postEnrollmentsProductInterestEndpoint,
  postEnrollmentsReactivateEndpoint,
  postEnrollmentsStartPaymentEndpoint,
} from 'src/app/shared/api/generated/endpoints/enrollments.endpoints';
import {
  getPersonIdentityDocumentEndpoint,
  getPersonPhotoEndpoint,
  postPersonIdentityDocumentEndpoint,
  postPersonPhotoEndpoint,
} from 'src/app/shared/api/generated/endpoints/person.endpoints';
import type { ConfirmedEnrollmentDetailsResponse } from 'src/app/shared/api/generated/models/confirmedEnrollmentDetailsResponse';
import type { ConfirmPreEnrollmentResponse } from 'src/app/shared/api/generated/models/confirmPreEnrollmentResponse';
import type { EnrollmentOffering } from 'src/app/shared/api/generated/models/enrollmentOffering';
import type { InitialSurveyDetails } from 'src/app/shared/api/generated/models/initialSurveyDetails';

import type {
  EnrollmentConfirmedDetail,
  EnrollmentCoordinator,
  EnrollmentDetail,
  EnrollmentSummary,
} from '../models/enrollment-detail';
import type {
  EnrollmentConfirmPreEnrollmentPayload,
  EnrollmentIdentityDocument,
  EnrollmentIdentityDocumentUploadPayload,
  EnrollmentIdentityPhotoUploadPayload,
  EnrollmentInitialSurvey,
  EnrollmentInitialSurveyPayload,
  EnrollmentInitialSurveyResponse,
  EnrollmentOfferingSummary,
  EnrollmentPaymentPayload,
  EnrollmentPaymentResponse,
  EnrollmentPreEnrollmentResponse,
  EnrollmentProductInterestPayload,
  EnrollmentStudentRegulationAcceptance,
  InitialSurveyStatus,
  SurveySectionId,
} from '../models/enrollment-flow';
import { buildPaymentPayload } from '../models/enrollment-flow-mappers';

@Injectable({
  providedIn: 'root',
})
export class EnrollmentsApi {
  private readonly api = inject(ApiHttpClient);

  public getDetail(
    productId: number,
    admissionProcessId: number,
    status?: string | null
  ): Observable<EnrollmentDetail> {
    return this.api
      .request(getEnrollmentsDetailsEndpoint, {
        queryParams: {
          productId: productId,
          admissionProcessId: admissionProcessId,
          ...(status ? { status: status } : {}),
        },
        showLoader: true,
      })
      .pipe(
        map(response => ({
          status: response.status ?? null,
          summary: this.toSummary(
            response.inProgress?.summary,
            response.inProgress?.interests?.[0]
          ),
          interests: this.toSeminars(response.inProgress?.interests),
          pendingPayment: response.pendingPayment
            ? {
                enrollmentId: response.pendingPayment.enrollments?.[0]?.enrollmentId ?? null,
                deposit: response.pendingPayment.depositAmount ?? null,
                accountBalance: response.pendingPayment.currentAccount?.currentBalance ?? null,
                paymentDueDate: response.pendingPayment.summary?.paymentDueDate ?? null,
                summary: this.toSummary(
                  response.pendingPayment.summary,
                  response.pendingPayment.enrollments?.[0]
                ),
                seminars: this.toSeminars(response.pendingPayment.enrollments),
              }
            : null,
          minimumDeposit: response.minimumDeposit
            ? {
                paymentMethod: response.minimumDeposit.paymentType ?? null,
                documentNumber: response.minimumDeposit.documentNumber ?? null,
                personCode: response.minimumDeposit.personId ?? null,
                deposit: response.minimumDeposit.depositAmount ?? null,
              }
            : null,
          confirmed: this.toConfirmedDetail(response.confirmed),
        }))
      );
  }

  public getIdentityDocument(): Observable<EnrollmentIdentityDocument> {
    return this.api.request(getPersonIdentityDocumentEndpoint).pipe(
      map(document => ({
        front: document.front
          ? {
              content: document.front.content ?? null,
              fileName: document.front.fileName ?? null,
            }
          : null,
        back: document.back
          ? {
              content: document.back.content ?? null,
              fileName: document.back.fileName ?? null,
            }
          : null,
        expirationDate: document.expirationDate ?? null,
      }))
    );
  }

  public getIdentityPhoto(): Observable<Blob> {
    return this.api.request(getPersonPhotoEndpoint, { responseType: 'blob' });
  }

  public uploadIdentityDocument(
    payload: EnrollmentIdentityDocumentUploadPayload
  ): Observable<boolean> {
    return this.api.request(postPersonIdentityDocumentEndpoint, {
      body: {
        expirationDate: payload.date,
        front: { fileName: payload.front.fileName, content: payload.front.content },
        back: { fileName: payload.back.fileName, content: payload.back.content },
      },
      showLoader: true,
    });
  }

  public uploadIdentityPhoto(payload: EnrollmentIdentityPhotoUploadPayload): Observable<boolean> {
    return this.api.request(postPersonPhotoEndpoint, {
      body: {
        file: {
          fileName: payload.attachedFile.fileName,
          content: payload.attachedFile.content,
        },
      },
      showLoader: true,
    });
  }

  public getInitialSurvey(): Observable<EnrollmentInitialSurveyResponse> {
    return this.api.request(getEnrollmentsInitialSurveyEndpoint).pipe(
      map(response => {
        const survey = response.survey;

        return {
          isEligibleForSurvey: response.canAnswerSurvey === true,
          survey: survey ? this.toInitialSurvey(survey) : null,
          consideredUniversities: survey?.consideredUniversityIds ?? [],
          otherConsideredUniversities: survey?.consideredUniversityOthers ?? [],
          higherEducationUniversities: survey?.higherEducationUniversityIds ?? [],
          otherHigherEducationUniversities: survey?.higherEducationUniversityOthers ?? [],
          selectedReasonOptions: survey?.ortChoiceReasonIds ?? [],
          selectedAdvertisingOptions: survey?.ortAdvertisingIds ?? [],
        };
      })
    );
  }

  public saveInitialSurvey(
    payload: EnrollmentInitialSurveyPayload
  ): Observable<InitialSurveyStatus> {
    const body = {
      degreeProgramId: payload.degreeProgramId,
      admissionProcessId: payload.intakeId,
      highSchoolTrackId: payload.highSchoolOrientationId,
      highSchoolYear: payload.highSchoolYear,
      currentlyInSecondary: payload.currentlyStudiesHighSchool,
      highSchoolYearRepeatCount: payload.highSchoolYearRepeatCount,
      repeatsHighSchoolYear: payload.repeatsHighSchoolYear,
      fatherEducationLevelId: payload.fatherOrGuardianEducationLevelId,
      motherEducationLevelId: payload.motherOrGuardianEducationLevelId,
      careerDecisionYearId: payload.degreeProgramDecisionYearId,
      ortDecisionYearId: payload.ortDecisionYearId,
      researchedOtherUniversities: payload.researchedOtherUniversities,
      otherUniversitiesInfoLine1: payload.otherUniversitiesInfoLine1,
      otherUniversitiesInfoLine2: payload.otherUniversitiesInfoLine2,
      decisionSupportId: payload.decisionSupportId,
      secondaryInstitutionId: payload.highSchoolInstitutionId,
      secondaryInstitutionName: payload.highSchoolInstitutionName,
      lastSecondaryYearLocationId: payload.finalHighSchoolYearLocationId,
      previousHigherEducationId: payload.priorHigherEducationStatusId,
      decisionLevelId: payload.decisionLevelId,
      hadOrtAdvisory: payload.hadOrtAdvising,
      ortAdvisoryRatingId: payload.ortAdvisingRatingId,
      visitedOrtWebsite: payload.visitedOrtWebsite,
      ortWebsiteRatingId: payload.ortWebsiteRatingId,
      visitedOrtFacilities: payload.visitedOrtCampus,
      ortFacilitiesRatingId: payload.ortCampusRatingId,
      recallsOrtAdvertising: payload.recallsOrtAdvertising,
      motherIsOrtGraduate: payload.isMotherOrGuardianOrtGraduate,
      fatherIsOrtGraduate: payload.isFatherOrGuardianOrtGraduate,
      consideredUniversityIds: payload.consideredUniversityIds,
      consideredUniversityOthers: payload.otherConsideredUniversities,
      higherEducationUniversityIds: payload.higherEducationUniversityIds,
      higherEducationUniversityOthers: payload.otherHigherEducationUniversities,
      ortAdvertisingIds: payload.ortAdvertisingIds,
      ortChoiceReasonIds: payload.ortChoiceReasonIds,
    };

    return this.api
      .request(postEnrollmentsInitialSurveyEndpoint, { body })
      .pipe(map(response => (response?.status === 'definitivo' ? 'complete' : 'in-progress')));
  }

  public getStudentRegulationAcceptance(): Observable<EnrollmentStudentRegulationAcceptance> {
    return this.api.request(getEnrollmentsStudentRegulationsEndpoint).pipe(
      map(acceptance => ({
        acceptedStudentRegulation: acceptance.acceptedStudentRegulations === true,
        acceptanceDate: acceptance.acceptanceDate ?? null,
      }))
    );
  }

  public confirmPreEnrollment(
    payload: EnrollmentConfirmPreEnrollmentPayload
  ): Observable<EnrollmentPreEnrollmentResponse> {
    return this.api
      .request(postEnrollmentsConfirmPreEnrollmentEndpoint, {
        body: {
          acceptedRegulations: payload.acceptedRegulation,
          isCorporateEnrollment: payload.isCorporateEnrollment,
          selectedOfferingIds: payload.selectedOfferingIds,
        },
        showLoader: true,
      })
      .pipe(map(response => this.toPreEnrollmentResponse(response)));
  }

  // Actualización profesional (nivel 3/4) da de baja el paquete entero: se reactivan
  // todas sus anotaciones en una sola llamada, igual que el endpoint de pago las cobra en bloque.
  public reactivate(enrollmentIds: number[]): Observable<EnrollmentPreEnrollmentResponse> {
    return this.api
      .request(postEnrollmentsReactivateEndpoint, {
        body: { enrollmentIds: enrollmentIds },
        showLoader: true,
      })
      .pipe(map(response => this.toPreEnrollmentResponse(response)));
  }

  public pay(payload: EnrollmentPaymentPayload): Observable<EnrollmentPaymentResponse> {
    const body = buildPaymentPayload(payload);
    return this.api
      .request(postEnrollmentsStartPaymentEndpoint, {
        body,
      })
      .pipe(
        map(response => ({
          success: true,
          result: response.result ?? null,
          paymentUrl: response.paymentUrl ?? null,
          encryptedParameters: response.encryptedParameters ?? null,
          messages:
            response.messages?.map(message => ({
              key: message.key ?? null,
              value: message.value ?? null,
            })) ?? [],
          confirmed: this.toConfirmedDetail(response.confirmed),
          message: null,
          errorCode: null,
        }))
      );
  }

  public registerProductInterest(payload: EnrollmentProductInterestPayload): Observable<boolean> {
    return this.api
      .request(postEnrollmentsProductInterestEndpoint, {
        body: {
          offeringIds: payload.offeringIds,
          admissionProcessId: payload.selectedAdmissionProcessId,
          productId: payload.productId,
        },
        showLoader: true,
      })
      .pipe(map(() => true));
  }

  private toInitialSurvey(survey: InitialSurveyDetails): EnrollmentInitialSurvey {
    return {
      degreeProgramId: survey.degreeProgramId ?? null,
      intakeId: survey.admissionProcessId ?? null,
      shiftId: null,
      productLevelId: null,
      complete: survey.status === 'completa',
      activeSection: toSurveySection(survey.status),
      studiesHighSchool: survey.currentlyInSecondary ?? null,
      highSchoolOrientationId: survey.highSchoolTrackId ?? null,
      highSchoolYearId: survey.highSchoolYear ?? null,
      repeatsHighSchoolYear: survey.repeatsHighSchoolYear ?? null,
      highSchoolYearRepeatCount: survey.highSchoolYearRepeatCount ?? null,
      highSchoolInstitutionId: survey.secondaryInstitutionId ?? null,
      highSchoolLocationId: survey.lastSecondaryYearLocationId ?? null,
      highSchoolInstitutionName: survey.secondaryInstitutionName ?? null,
      priorHigherEducationStatusId: survey.previousHigherEducationId ?? null,
      motherEducationLevelId: survey.motherEducationLevelId ?? null,
      fatherEducationLevelId: survey.fatherEducationLevelId ?? null,
      isMotherOrtGraduate: survey.motherIsOrtGraduate ?? null,
      isFatherOrtGraduate: survey.fatherIsOrtGraduate ?? null,
      degreeProgramDecisionYearId: survey.careerDecisionYearId ?? null,
      ortDecisionYearId: survey.ortDecisionYearId ?? null,
      researchedOtherUniversities: survey.researchedOtherUniversities ?? null,
      decisionSupportId: survey.decisionSupportId ?? null,
      decisionLevelId: survey.decisionLevelId ?? null,
      hadOrtAdvising: survey.hadOrtAdvisory ?? null,
      ortAdvisingRating: survey.ortAdvisoryRatingId ?? null,
      visitedOrtWebsite: survey.visitedOrtWebsite ?? null,
      ortWebsiteRating: survey.ortWebsiteRatingId ?? null,
      visitedOrtCampus: survey.visitedOrtFacilities ?? null,
      ortCampusRating: survey.ortFacilitiesRatingId ?? null,
      recallsOrtAdvertising: survey.recallsOrtAdvertising ?? null,
    };
  }
  private toSummary(
    summary:
      | {
          offeringId?: number;
          productId?: number;
          degreeProgram?: string | null;
          intake?: string | null;
          shift?: string | null;
        }
      | null
      | undefined,
    offering?: { offeringId?: number; intake?: string | null; shift?: string | null } | null
  ): EnrollmentSummary | null {
    return summary
      ? {
          offeringId: offering?.offeringId ?? summary.offeringId ?? null,
          productId: summary.productId ?? null,
          degreeProgram: summary.degreeProgram ?? null,
          intake: offering?.intake ?? summary.intake ?? null,
          shift: offering?.shift ?? summary.shift ?? null,
        }
      : null;
  }

  // Una fila por oferta elegida (en Actualización profesional, un seminario por fila).
  // Sirve para los dos bloques: en `pendingPayment.enrollments` el enrollmentId es lo
  // que después se cobra en bloque mediante `enrollmentIds`, y en `summary.interests`
  // los offeringId son los que reconfirma la preinscripción al retomar.
  private toSeminars(
    offerings: Array<EnrollmentOffering> | null | undefined
  ): EnrollmentOfferingSummary[] {
    return (offerings ?? []).map(offering => ({
      enrollmentId: offering.enrollmentId ?? null,
      offeringId: offering.offeringId ?? null,
      name: offering.offeringDescription ?? null,
      intake: offering.intake ?? null,
      shift: offering.shift ?? null,
    }));
  }

  private toPreEnrollmentResponse(
    response: ConfirmPreEnrollmentResponse
  ): EnrollmentPreEnrollmentResponse {
    return {
      confirmed: response.confirmed === true,
      isWaiting: response.waiting === true,
      enrollmentId: response.enrollments?.[0]?.enrollmentId ?? null,
      paymentDueDate: response.summary?.paymentDueDate ?? null,
      enrollmentDeposit: response.depositAmount ?? null,
      accountBalance: response.currentAccount?.currentBalance ?? null,
      summary: response.summary
        ? {
            degreeProgram: response.summary.degreeProgram ?? null,
            intake: response.enrollments?.[0]?.intake ?? null,
            shift: response.enrollments?.[0]?.shift ?? null,
          }
        : null,
      seminars: this.toSeminars(response.enrollments),
    };
  }

  private toCoordinator(
    coordinator: { name?: string | null; email?: string | null } | null | undefined
  ): EnrollmentCoordinator | null {
    return coordinator
      ? { name: coordinator.name ?? null, email: coordinator.email ?? null }
      : null;
  }

  // El bloque `confirmed` ya no trae `summary`: producto y `degreeProgram` están en la
  // cabecera, y `intake`, `shift` y materias en cada inscripción confirmada.
  private toConfirmedDetail(
    confirmed: ConfirmedEnrollmentDetailsResponse | null | undefined
  ): EnrollmentConfirmedDetail | null {
    if (!confirmed) return null;

    const enrollments = confirmed.enrollments ?? [];
    return {
      studentNumber: confirmed.personId ?? null,
      summary: this.toSummary(confirmed, enrollments[0]),
      academicCoordinator: this.toCoordinator(confirmed.academicCoordinator),
      courseCoordinator: this.toCoordinator(confirmed.courseCoordinator),
      enrollments: enrollments.map(enrollment => ({
        enrollmentId: enrollment.enrollmentId ?? null,
        offeringId: enrollment.offeringId ?? null,
        intake: enrollment.intake ?? null,
        shift: enrollment.shift ?? null,
        firstSemesterSubjects: (enrollment.firstSemesterSubjects ?? []).map(subject => ({
          subjectId: subject.subjectId ?? null,
          name: subject.name ?? null,
        })),
      })),
    };
  }
}

function toSurveySection(value: string | null | undefined): SurveySectionId | null {
  switch (value) {
    case 'educacion':
      return 'education';
    case 'decision-academica':
      return 'academic-decision';
    case 'experiencia-ort':
      return 'ort-experience';
    case 'identidad':
      return 'identity';
    case 'reglamento':
      return 'regulation';
    default:
      return null;
  }
}
