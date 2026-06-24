import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonaBecasEndpoint,
  getPersonaInscripcionesEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';
import type { DtoBecaPersona } from 'src/app/shared/api/generated/models/dtoBecaPersona';
import type { DtoVdInscripcionesFresco1y2Devart } from 'src/app/shared/api/generated/models/dtoVdInscripcionesFresco1y2Devart';

import { MiBeca } from '../models/mi-beca';
import { MiInscripcion } from '../models/mi-inscripcion';

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
      | { data: DtoVdInscripcionesFresco1y2Devart[] | null }
      | DtoVdInscripcionesFresco1y2Devart[]
      | null
      | undefined
  ): MiInscripcion[] {
    const items = Array.isArray(data) ? data : (data?.data ?? []);

    return items.map(item => ({
      idProducto: item.idProducto ?? 0,
      idProceso: item.idProceso ?? 0,
      idComienzo: item.idComienzo ?? 0,
      idTurno: item.idTurno ?? 0,
      nombreProducto: item.nombreExtensoProducto ?? '',
      nombreComienzo: item.nombreComienzo ?? '',
      nombreTurno: item.nombreTurno ?? '',
      estado: item.estadoInscripcion ?? '',
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
