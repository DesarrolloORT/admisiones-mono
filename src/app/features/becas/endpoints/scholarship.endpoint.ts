// import { inject, Injectable } from '@angular/core';
// import { Observable } from 'rxjs';
// import { map } from 'rxjs/operators';

// import { ApiHttpClient } from '../../../shared/api/core/api-http-client';
// import { getBecasInscripcionesEndpoint } from '../../../shared/api/generated/endpoints/becas.endpoints';
// import type { DtoVdInscripcionesFresco1y2Devart } from '../../../shared/api/generated/models/dtoVdInscripcionesFresco1y2Devart';

// export interface ScholarshipAcademicStepData {
//   carrera: string;
//   comienzo: string;
//   turno: string;
// }

// @Injectable({ providedIn: 'root' })
// export class ScholarshipEndpoint {
//   private readonly api = inject(ApiHttpClient);

//   public getAcademicStepData(): Observable<ScholarshipAcademicStepData[]> {
//     return this.api
//       .request(getBecasInscripcionesEndpoint, { cache: false })
//       .pipe(map(response => this.toAcademicStepData(response)));
//   }

//   private toAcademicStepData(
//     data:
//       | { data: DtoVdInscripcionesFresco1y2Devart[] | null }
//       | DtoVdInscripcionesFresco1y2Devart[]
//       | null
//       | undefined
//   ): ScholarshipAcademicStepData[] {
//     const items = Array.isArray(data) ? data : (data?.data ?? []);

//     return items.map(item => ({
//       carrera: item?.nombreExtensoProducto ?? '',
//       comienzo: item?.nombreComienzo ?? '',
//       turno: item?.nombreTurno ?? '',
//     }));
//   }
// }
