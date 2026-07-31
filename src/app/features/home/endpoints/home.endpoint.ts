import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { isProfessionalUpdateLevel } from 'src/app/features/catalogs/models/academic-proposal';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { postInscripcionesReactivarEndpoint } from 'src/app/shared/api/generated/endpoints/inscripciones.endpoints';
import {
  getPersonaBecasEndpoint,
  getPersonaInscripcionesEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';
import type { DtoBecaPersona } from 'src/app/shared/api/generated/models/dtoBecaPersona';
import type { DtoInscripcionesPorProductoProcesoResponse } from 'src/app/shared/api/generated/models/dtoInscripcionesPorProductoProcesoResponse';

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

  public reactivarInscripcion(idInscripcion: number): Observable<boolean> {
    return this.api
      .request(postInscripcionesReactivarEndpoint, { body: { idInscripcion }, showLoader: true })
      .pipe(
        tap(() => this.api.clearCache()),
        map(() => true)
      );
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
          idProducto: group.idProducto ?? 0,
          idProceso: group.idProceso ?? 0,
          idComienzo: primero?.idComienzo ?? 0,
          idTurno: primero?.idTurno ?? 0,
          nombreProducto,
          nombreComienzo: primero?.nombreComienzo ?? '',
          nombreTurno: primero?.nombreTurno ?? '',
          estado,
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
          idProducto: group.idProducto ?? 0,
          idProceso: group.idProceso ?? 0,
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
      idInscripto: item.idInscripto ?? 0,
      idProducto: group.idProducto ?? 0,
      idProceso: group.idProceso ?? 0,
      idComienzo: item.idComienzo ?? 0,
      idTurno: item.idTurno ?? 0,
      nombreProducto,
      nombreComienzo: item.nombreComienzo ?? '',
      nombreTurno: item.nombreTurno ?? '',
      estado,
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
