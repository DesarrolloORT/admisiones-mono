import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { isProfessionalUpdateLevel } from 'src/app/features/catalogs/models/academic-proposal';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonEnrollmentsEndpoint,
  getPersonScholarshipsEndpoint,
} from 'src/app/shared/api/generated/endpoints/person.endpoints';
import type { MyEnrollmentsResponse } from 'src/app/shared/api/generated/models/myEnrollmentsResponse';
import type { ScholarshipSummary } from 'src/app/shared/api/generated/models/scholarshipSummary';

import { MiBeca } from '../models/mi-beca';
import { MiInscripcion, MiInscripcionSeminario } from '../models/mi-inscripcion';

@Injectable({
  providedIn: 'root',
})
export class HomeEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getMisInscripciones(): Observable<MiInscripcion[]> {
    return this.api
      .request(getPersonEnrollmentsEndpoint)
      .pipe(map(data => this.toMisInscripciones(data)));
  }

  public getMisBecas(): Observable<MiBeca[]> {
    return this.api.request(getPersonScholarshipsEndpoint).pipe(map(data => this.toMisBecas(data)));
  }

  private toMisInscripciones(
    data: { data: MyEnrollmentsResponse[] | null } | MyEnrollmentsResponse[] | null | undefined
  ): MiInscripcion[] {
    const groups = Array.isArray(data) ? data : (data?.data ?? []);

    return groups.flatMap(group => this.toMisInscripcionesFromGroup(group));
  }

  private toMisInscripcionesFromGroup(group: MyEnrollmentsResponse): MiInscripcion[] {
    const items = group.enrollments ?? [];
    const nombreProducto = group.productFullName ?? '';
    const estado = group.enrollmentStatus ?? '';

    if (isProfessionalUpdateLevel(group.productLevelId)) {
      const primero = items[0];
      return [
        {
          idInscripto: primero?.enrollmentId ?? 0,
          idOfertas: [...new Set(items.map(item => item.offeringId).filter(isPositiveInteger))],
          idProducto: group.productId ?? 0,
          idProceso: group.admissionProcessId ?? 0,
          idComienzo: primero?.intakeId ?? 0,
          idTurno: primero?.shiftId ?? 0,
          nombreProducto,
          nombreComienzo: primero?.intakeName ?? '',
          nombreTurno: primero?.shiftName ?? '',
          estado,
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

    if (items.length === 0) {
      return [
        {
          idInscripto: 0,
          idOfertas: [],
          idProducto: group.productId ?? 0,
          idProceso: group.admissionProcessId ?? 0,
          idComienzo: 0,
          idTurno: 0,
          nombreProducto,
          nombreComienzo: '',
          nombreTurno: '',
          estado,
          seminarios: [],
        },
      ];
    }

    return items.map(item => ({
      idInscripto: item.enrollmentId ?? 0,
      idOfertas: isPositiveInteger(item.offeringId) ? [item.offeringId] : [],
      idProducto: group.productId ?? 0,
      idProceso: group.admissionProcessId ?? 0,
      idComienzo: item.intakeId ?? 0,
      idTurno: item.shiftId ?? 0,
      nombreProducto,
      nombreComienzo: item.intakeName ?? '',
      nombreTurno: item.shiftName ?? '',
      estado,
      seminarios: [],
    }));
  }

  private toMisBecas(
    data: { data: ScholarshipSummary[] | null } | ScholarshipSummary[] | null | undefined
  ): MiBeca[] {
    const items = Array.isArray(data) ? data : (data?.data ?? []);

    return items.map(item => ({
      id: item.applicationId ?? item.scholarshipId ?? 0,
      nombreBeca: item.name ?? '',
      nombreCarrera: item.degreeProgram ?? '',
      estado: item.status ?? '',
      cierrePostulacion: this.formatDate(item.applicationCloseDate),
      fechaPrueba: this.formatDate(item.testDate),
      resultadoPrueba: '',
      beneficio: '',
      fechaResultados: this.formatDate(item.resultsDate),
    }));
  }

  private formatDate(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const formatted = new Intl.DateTimeFormat('es-UY', {
      weekday: 'long',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      timeZone: 'UTC',
    })
      .format(date)
      .replace(',', '');

    return `${formatted.charAt(0).toUpperCase()}${formatted.slice(1)}`;
  }
}

function isPositiveInteger(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}
