import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { isProfessionalUpdateLevel } from 'src/app/features/catalogs/models/academic-proposal';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonaBecasEndpoint,
  getPersonaInscripcionesEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';
import type { DtoBecaPersona } from 'src/app/shared/api/generated/models/dtoBecaPersona';
import type { DtoInscripcionesPorProductoProcesoResponse } from 'src/app/shared/api/generated/models/dtoInscripcionesPorProductoProcesoResponse';
import type { DtoInscripcionItemResponse } from 'src/app/shared/api/generated/models/dtoInscripcionItemResponse';

import { MiBeca } from '../models/mi-beca';
import { MiInscripcion, MiInscripcionSeminario } from '../models/mi-inscripcion';

@Injectable({
  providedIn: 'root',
})
export class HomeEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getMisInscripciones(): Observable<MiInscripcion[]> {
    return this.api
      .request(getPersonaInscripcionesEndpoint)
      .pipe(map(data => this.toMisInscripciones(data)));
  }

  public getMisBecas(): Observable<MiBeca[]> {
    return this.api.request(getPersonaBecasEndpoint).pipe(map(data => this.toMisBecas(data)));
  }

  private toMisInscripciones(
    data:
      | { data: DtoInscripcionesPorProductoProcesoResponse[] | null }
      | DtoInscripcionesPorProductoProcesoResponse[]
      | null
      | undefined
  ): MiInscripcion[] {
    const groups = Array.isArray(data) ? data : (data?.data ?? []);

    return groups.flatMap(group => this.toMisInscripcionesFromGroup(group));
  }

  private toMisInscripcionesFromGroup(
    group: DtoInscripcionesPorProductoProcesoResponse
  ): MiInscripcion[] {
    const items = group.inscripciones ?? [];
    const nombreProducto = group.nombreExtensoProducto ?? '';
    const estado = group.estadoInscripcion ?? '';

    if (isProfessionalUpdateLevel(group.idNivelProducto)) {
      const primero = items[0];
      return [
        {
          idInscripto: primero?.idInscripto ?? 0,
          idOfertas: [...new Set(items.map(item => item.idOferta).filter(isPositiveInteger))],
          idProducto: group.idProducto ?? 0,
          idProceso: group.idProceso ?? 0,
          idComienzo: primero?.idComienzo ?? 0,
          idTurno: primero?.idTurno ?? 0,
          nombreProducto,
          nombreComienzo: primero?.nombreComienzo ?? '',
          nombreTurno: primero?.nombreTurno ?? '',
          estado,
          fechaVencimientoPago: readFechaVencimientoPago(group, primero),
          seminarios: items.map((item): MiInscripcionSeminario => ({
            idInscripto: item.idInscripto ?? 0,
            idOferta: item.idOferta ?? 0,
            descripcionOferta: item.descripcionOferta ?? '',
            idComienzo: item.idComienzo ?? 0,
            idTurno: item.idTurno ?? 0,
            nombreComienzo: item.nombreComienzo ?? '',
            nombreTurno: item.nombreTurno ?? '',
          })),
        },
      ];
    }

    if (items.length === 0) {
      return [
        {
          idInscripto: 0,
          idOfertas: [],
          idProducto: group.idProducto ?? 0,
          idProceso: group.idProceso ?? 0,
          idComienzo: 0,
          idTurno: 0,
          nombreProducto,
          nombreComienzo: '',
          nombreTurno: '',
          estado,
          fechaVencimientoPago: readFechaVencimientoPago(group),
          seminarios: [],
        },
      ];
    }

    return items.map(item => ({
      idInscripto: item.idInscripto ?? 0,
      idOfertas: isPositiveInteger(item.idOferta) ? [item.idOferta] : [],
      idProducto: group.idProducto ?? 0,
      idProceso: group.idProceso ?? 0,
      idComienzo: item.idComienzo ?? 0,
      idTurno: item.idTurno ?? 0,
      nombreProducto,
      nombreComienzo: item.nombreComienzo ?? '',
      nombreTurno: item.nombreTurno ?? '',
      estado,
      fechaVencimientoPago: readFechaVencimientoPago(group, item),
      seminarios: [],
    }));
  }

  private toMisBecas(
    data: { data: DtoBecaPersona[] | null } | DtoBecaPersona[] | null | undefined
  ): MiBeca[] {
    const items = Array.isArray(data) ? data : (data?.data ?? []);

    return items.map(item => ({
      id: item.idPostulacion ?? item.idBeca ?? 0,
      nombreBeca: item.nombre ?? '',
      nombreCarrera: item.carrera ?? '',
      estado: item.estado ?? '',
      cierrePostulacion: this.formatDate(item.fechaCierrePostulacion),
      fechaPrueba: this.formatDate(item.fechaPrueba),
      resultadoPrueba: '',
      beneficio: '',
      fechaResultados: this.formatDate(item.fechaResultados),
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

// TODO(api): GET /Persona/Inscripciones todavia no expone la fecha de vencimiento del pago (solo
// existe en el detalle, DtoCabeceraInscripcion). Se lee de forma tolerante a nivel grupo y, si no
// esta, del item; hasta que el backend la agregue resuelve a null y la UI cae al texto generico.
function readFechaVencimientoPago(
  group: DtoInscripcionesPorProductoProcesoResponse,
  item?: DtoInscripcionItemResponse
): string | null {
  return readOptionalDate(group) ?? readOptionalDate(item);
}

function readOptionalDate(source: object | undefined): string | null {
  const value = (source as { fechaVencimientoPago?: unknown } | undefined)?.fechaVencimientoPago;

  return typeof value === 'string' && value.trim().length > 0 ? value : null;
}
