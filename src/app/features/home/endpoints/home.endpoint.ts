import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { isProfessionalUpdateLevel } from 'src/app/features/catalogs/models/academic-proposal';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { getPersonEnrollmentsEndpoint } from 'src/app/shared/api/generated/endpoints/person.endpoints';
import type { MyEnrollmentsResponse } from 'src/app/shared/api/generated/models/myEnrollmentsResponse';

import { MiInscripcion, MiInscripcionSeminario } from '../models/mi-inscripcion';

@Injectable({
  providedIn: 'root',
})
export class HomeEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getMisInscripciones(): Observable<MiInscripcion[]> {
    return this.api.request(getPersonEnrollmentsEndpoint).pipe(
      catchError((error: HttpErrorResponse) =>
        error.status === 404 ? of([]) : throwError(() => error)
      ),
      map(data => this.toMisInscripciones(data))
    );
  }

  private toMisInscripciones(
    data: { data: MyEnrollmentsResponse[] | null } | MyEnrollmentsResponse[] | null | undefined
  ): MiInscripcion[] {
    const groups = Array.isArray(data) ? data : (data?.data ?? []);

    return groups.flatMap(group => this.toMisInscripcionesFromGroup(group));
  }

  private toMisInscripcionesFromGroup(group: MyEnrollmentsResponse): MiInscripcion[] {
    const items = group.enrollments ?? [];

    if (items.length === 0) {
      return [];
    }

    const nombreProducto = group.productFullName ?? '';
    const estado = group.enrollmentStatus ?? '';
    const idNivelProducto = group.productLevelId ?? null;

    if (isProfessionalUpdateLevel(group.productLevelId)) {
      const primero = items[0];
      return [
        {
          idInscripto: primero?.enrollmentId ?? 0,
          idOfertas: [...new Set(items.map(item => item.offeringId).filter(isPositiveInteger))],
          idProducto: group.productId ?? 0,
          idProceso: group.admissionProcessId ?? 0,
          idNivelProducto,
          idComienzo: primero?.intakeId ?? 0,
          idTurno: primero?.shiftId ?? 0,
          nombreProducto,
          nombreComienzo: primero?.intakeName ?? '',
          nombreTurno: primero?.shiftName ?? '',
          estado,
          fechaVencimientoPago: readFechaVencimientoPago(group),
          seminarios: items.map((item): MiInscripcionSeminario => ({
            idInscripto: item.enrollmentId ?? 0,
            idOferta: item.offeringId ?? 0,
            descripcionOferta: item.offeringDescription ?? '',
            idComienzo: item.intakeId ?? 0,
            idTurno: item.shiftId ?? 0,
            nombreComienzo: item.intakeName ?? '',
            nombreTurno: item.shiftName ?? '',
          })),
        },
      ];
    }

    return items.map(item => ({
      idInscripto: item.enrollmentId ?? 0,
      idOfertas: isPositiveInteger(item.offeringId) ? [item.offeringId] : [],
      idProducto: group.productId ?? 0,
      idProceso: group.admissionProcessId ?? 0,
      idNivelProducto,
      idComienzo: item.intakeId ?? 0,
      idTurno: item.shiftId ?? 0,
      nombreProducto,
      nombreComienzo: item.intakeName ?? '',
      nombreTurno: item.shiftName ?? '',
      estado,
      fechaVencimientoPago: readFechaVencimientoPago(group),
      seminarios: [],
    }));
  }
}

function isPositiveInteger(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function readFechaVencimientoPago(group: MyEnrollmentsResponse): string | null {
  return group.paymentDueDate ?? null;
}
