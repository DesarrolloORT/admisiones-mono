import { inject, Injectable } from '@angular/core';
import { forkJoin, from, Observable, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

import { EnrollmentsEndpoint } from '../endpoints/enrollments.endpoint';
import type { EnrollmentDetail } from '../models/enrollment-detail';
import { toBlobFile, toIdentityFile, toIdentityUploadFile } from '../models/enrollment-files';
import type {
  EnrollmentConfirmPreEnrollmentPayload,
  EnrollmentInitialSurveyPayload,
  EnrollmentInitialSurveyResponse,
  EnrollmentPaymentPayload,
  EnrollmentPaymentResponse,
  EnrollmentPreEnrollmentResponse,
  EnrollmentProductInterestPayload,
  EnrollmentStudentRegulationAcceptance,
} from '../models/enrollment-flow';

export interface EnrollmentIdentityPreload {
  front: File | null;
  back: File | null;
  selfie: File | null;
  expirationDate: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class Enrollments {
  private readonly endpoint = inject(EnrollmentsEndpoint);

  public getDetail(
    productId: number,
    admissionProcessId: number,
    status?: string | null
  ): Observable<EnrollmentDetail> {
    return this.endpoint.getDetail(productId, admissionProcessId, status);
  }

  public getIdentityPreload(): Observable<EnrollmentIdentityPreload> {
    return forkJoin({
      document: this.endpoint.getIdentityDocument().pipe(catchError(() => of(null))),
      photo: this.endpoint.getIdentityPhoto().pipe(catchError(() => of(null))),
    }).pipe(
      map(({ document, photo }) => ({
        front: toIdentityFile(document?.front, 'identity-document-front'),
        back: toIdentityFile(document?.back, 'identity-document-back'),
        selfie: toBlobFile(photo, 'identity-photo'),
        expirationDate: document?.expirationDate ?? null,
      }))
    );
  }

  public uploadIdentityDocument(payload: {
    date: string;
    front: File;
    back: File;
  }): Observable<boolean> {
    return from(
      Promise.all([toIdentityUploadFile(payload.front), toIdentityUploadFile(payload.back)])
    ).pipe(
      switchMap(([front, back]) =>
        this.endpoint.uploadIdentityDocument({ date: payload.date, front, back })
      )
    );
  }

  public uploadIdentityPhoto(file: File): Observable<boolean> {
    return from(toIdentityUploadFile(file)).pipe(
      switchMap(attachedFile => this.endpoint.uploadIdentityPhoto({ attachedFile }))
    );
  }

  public getInitialSurvey(): Observable<EnrollmentInitialSurveyResponse> {
    return this.endpoint.getInitialSurvey();
  }

  public saveInitialSurvey(payload: EnrollmentInitialSurveyPayload): Observable<boolean> {
    return this.endpoint.saveInitialSurvey(payload);
  }

  public getStudentRegulationAcceptance(): Observable<EnrollmentStudentRegulationAcceptance> {
    return this.endpoint.getStudentRegulationAcceptance();
  }

  public confirmPreEnrollment(
    payload: EnrollmentConfirmPreEnrollmentPayload
  ): Observable<EnrollmentPreEnrollmentResponse> {
    return this.endpoint.confirmPreEnrollment(payload);
  }

  public reactivate(enrollmentIds: number[]): Observable<EnrollmentPreEnrollmentResponse> {
    return this.endpoint.reactivate(enrollmentIds);
  }

  public pay(payload: EnrollmentPaymentPayload): Observable<EnrollmentPaymentResponse> {
    return this.endpoint.pay(payload);
  }

  public registerProductInterest(payload: EnrollmentProductInterestPayload): Observable<boolean> {
    return this.endpoint.registerProductInterest(payload);
  }
}
