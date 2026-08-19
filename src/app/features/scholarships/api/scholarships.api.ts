import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getScholarshipsAvailableEndpoint,
  getScholarshipsEnrollmentsEndpoint,
  postScholarshipsApplicationsEndpoint,
} from 'src/app/shared/api/generated/endpoints/scholarships.endpoints';

import type {
  AvailableScholarships,
  ScholarshipApplication,
  ScholarshipApplicationPayload,
} from '../models/available-scholarship.interface';
import type { ConfirmedEnrollment } from '../models/confirmed-enrollment.interface';

/**
 * Adapter de becas: la **única** capa de la feature que puede importar
 * `shared/api/generated/**` y `ApiHttpClient` (lo enforzan `eslint.config.js` y
 * `npm run check-api-contracts`). Todo método público recibe y devuelve tipos de
 * la feature, nunca DTO generados.
 *
 * Los endpoints no se escriben a mano: salen de `.api-spec/swagger.json` vía
 * `npm run update-api` hacia `shared/api/generated/endpoints/*.endpoints.ts`.
 *
 * Contrato de negocio de estas pantallas: `.api-spec/contracts/becas.contract.json`.
 */
@Injectable({
  providedIn: 'root',
})
export class ScholarshipsApi {
  private readonly api = inject(ApiHttpClient);

  /**
   * Becas que la persona autenticada puede ver. `api.data` porque `/available`
   * devuelve un **objeto** (`{ requiresPriorEnrollment, scholarships }`); si
   * devolviera un array plano correspondería `api.list`.
   */
  public getAvailableScholarships(): Observable<AvailableScholarships> {
    return this.api.data(getScholarshipsAvailableEndpoint).pipe(
      map(response => ({
        requiresPriorEnrollment: response?.requiresPriorEnrollment ?? true,
        scholarships: (response?.scholarships ?? []).map(item => ({
          scholarshipTypeIds: item.scholarshipTypeIds ?? [],
          name: item.name ?? '',
          description: item.description ?? '',
          requiresTest: item.requiresTest ?? false,
        })),
      }))
    );
  }

  /**
   * Inscripciones confirmadas que habilitan la postulación a becas. `api.list`
   * normaliza `null`, objeto suelto o array a un array y aplica el mapper por
   * item: ese chequeo no se escribe a mano.
   */
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

  /**
   * Alta de la postulación. Un POST se hace con `api.request` + `body`; el
   * `showLoader` prende el loader global mientras viaja.
   *
   * Todavía no tiene consumidor: el paso pendiente es dispararlo desde
   * `facades/scholarship-proposal.ts` cuando se confirme la postulación. Ver el
   * README de la feature.
   */
  public createApplication(
    payload: ScholarshipApplicationPayload
  ): Observable<ScholarshipApplication> {
    return this.api
      .request(postScholarshipsApplicationsEndpoint, {
        body: {
          enrollmentId: payload.enrollmentId,
          testId: payload.testId,
        },
        showLoader: true,
      })
      .pipe(
        map(response => ({
          applicationId: response?.applicationId ?? 0,
          affidavitId: response?.affidavitId ?? null,
          affidavitStatus: response?.affidavitStatus ?? '',
        }))
      );
  }
}
