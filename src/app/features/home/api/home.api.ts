import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { isProfessionalUpdateLevel } from 'src/app/features/catalogs/models/academic-proposal';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonEnrollmentsEndpoint,
  getPersonScholarshipsEndpoint,
} from 'src/app/shared/api/generated/endpoints/person.endpoints';
import type { MyEnrollmentsResponse } from 'src/app/shared/api/generated/models/myEnrollmentsResponse';

import { EnrollmentSeminarSummary, EnrollmentSummary } from '../models/enrollment-summary';
import { ScholarshipSummary } from '../models/scholarship-summary';

@Injectable({
  providedIn: 'root',
})
export class HomeApi {
  private readonly api = inject(ApiHttpClient);

  public getMyEnrollments(): Observable<EnrollmentSummary[]> {
    return this.api
      .list(getPersonEnrollmentsEndpoint, group => this.toEnrollmentSummariesFromGroup(group))
      .pipe(
        map(groups => groups.flat()),
        catchError((error: HttpErrorResponse) =>
          error.status === 404 ? of([]) : throwError(() => error)
        )
      );
  }

  /**
   * Postulaciones a becas de la persona. Lista vacia = no se postulo a ninguna.
   *
   * Las fechas viajan como el `string` que manda la API (ISO): armar el texto visible
   * —unir fecha y hora, formato corto— es trabajo de la UI, no del adapter.
   *
   * `examResult` y `benefit` quedan vacios: `MyScholarshipsResponse` todavia no los trae.
   */
  public getMyScholarships(): Observable<ScholarshipSummary[]> {
    return this.api.list(getPersonScholarshipsEndpoint, item => ({
      id: item.testEnrollmentId ?? 0,
      scholarshipName: item.scholarshipTypeName ?? '',
      degreeProgramName: item.productName ?? '',
      status: item.testEnrollmentStatus ?? '',
      applicationDeadline: item.applicationCloseDate ?? '',
      examDate: item.examDate ?? '',
      examResult: '',
      benefit: '',
      resultsDate: item.resultDate ?? '',
    }));
  }

  private toEnrollmentSummariesFromGroup(group: MyEnrollmentsResponse): EnrollmentSummary[] {
    const items = group.enrollments ?? [];

    if (items.length === 0) {
      return [];
    }

    const degreeProgramName = group.productFullName ?? '';
    const status = group.enrollmentStatus ?? '';
    const productLevelId = group.productLevelId ?? null;

    if (isProfessionalUpdateLevel(group.productLevelId)) {
      const first = items[0];
      return [
        {
          enrollmentId: first?.enrollmentId ?? 0,
          offeringIds: [...new Set(items.map(item => item.offeringId).filter(isPositiveInteger))],
          productId: group.productId ?? 0,
          admissionProcessId: group.admissionProcessId ?? 0,
          productLevelId,
          intakeId: first?.intakeId ?? 0,
          shiftId: first?.shiftId ?? 0,
          degreeProgramName,
          intakeName: first?.intakeName ?? '',
          shiftName: first?.shiftName ?? '',
          status,
          paymentDueDate: readPaymentDueDate(group),
          seminars: items.map((item): EnrollmentSeminarSummary => ({
            enrollmentId: item.enrollmentId ?? 0,
            offeringId: item.offeringId ?? 0,
            offeringDescription: item.offeringDescription ?? '',
            intakeId: item.intakeId ?? 0,
            shiftId: item.shiftId ?? 0,
            intakeName: item.intakeName ?? '',
            shiftName: item.shiftName ?? '',
          })),
        },
      ];
    }

    return items.map(item => ({
      enrollmentId: item.enrollmentId ?? 0,
      offeringIds: isPositiveInteger(item.offeringId) ? [item.offeringId] : [],
      productId: group.productId ?? 0,
      admissionProcessId: group.admissionProcessId ?? 0,
      productLevelId,
      intakeId: item.intakeId ?? 0,
      shiftId: item.shiftId ?? 0,
      degreeProgramName,
      intakeName: item.intakeName ?? '',
      shiftName: item.shiftName ?? '',
      status,
      paymentDueDate: readPaymentDueDate(group),
      seminars: [],
    }));
  }
}

function isPositiveInteger(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function readPaymentDueDate(group: MyEnrollmentsResponse): string | null {
  return group.paymentDueDate ?? null;
}
