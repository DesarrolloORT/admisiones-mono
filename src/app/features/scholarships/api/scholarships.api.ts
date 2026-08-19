import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { getScholarshipsEnrollmentsEndpoint } from 'src/app/shared/api/generated/endpoints/scholarships.endpoints';

import type { ConfirmedEnrollment } from '../models/confirmed-enrollment.interface';

@Injectable({
  providedIn: 'root',
})
export class ScholarshipsApi {
  private readonly api = inject(ApiHttpClient);

  /** Inscripciones confirmadas que habilitan la postulación a becas. */
  public getConfirmedEnrollments(): Observable<ConfirmedEnrollment[]> {
    return this.api.list(getScholarshipsEnrollmentsEndpoint, item => ({
      enrollmentId: item.enrollmentId ?? 0,
      enrollmentDate: item.enrollmentDate ?? null,
      status: item.enrollmentStatus ?? '',
      productId: item.productId ?? 0,
      degreeProgramName: item.productFullName ?? '',
      productLevelId: item.productLevelId ?? null,
      admissionProcessId: item.admissionProcessId ?? 0,
      intakeId: item.intakeId ?? 0,
      intakeName: item.intakeName ?? '',
      intakeStartDate: item.intakeStartDate ?? null,
      shiftId: item.shiftId ?? 0,
      shiftName: item.shiftName ?? '',
      offeringId: item.offeringId ?? 0,
      origin: item.origin ?? '',
    }));
  }
}
