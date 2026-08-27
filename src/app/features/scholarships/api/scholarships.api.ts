import { inject, Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { getScholarshipsEnrollmentsEndpoint } from 'src/app/shared/api/generated/endpoints/scholarships.endpoints';

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
   * ponytail: stub temporal. El backend removió `GET /scholarships/available` (hoy
   * `ScholarshipsController` solo expone `enrollments`), así que la feature quedó sin
   * endpoint. Devuelve la lista vacía para no romper la compilación ni la pantalla
   * mientras se define si el endpoint vuelve o la feature se rediseña.
   *
   * Techo conocido: la pantalla de becas no muestra ninguna beca disponible.
   */
  public getAvailableScholarships(): Observable<AvailableScholarships> {
    return of({ requiresPriorEnrollment: true, scholarships: [] });
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
   * ponytail: stub temporal. El backend removió `POST /scholarships/applications`. Falla
   * en vez de simular un alta: hoy no tiene consumidor, y devolver un id inventado haría
   * pasar por exitosa una postulación que nunca existió.
   */
  public createApplication(
    payload: ScholarshipApplicationPayload
  ): Observable<ScholarshipApplication> {
    void payload;
    return throwError(() => new Error('POST /scholarships/applications no está disponible.'));
  }
}
