import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getInscripcionesMisInscripcionesEndpoint,
  MisInscripcionesItem,
} from 'src/app/shared/api/generated/endpoints/inscripciones.endpoints';

import { MiInscripcion } from '../models/mi-inscripcion';

@Injectable({
  providedIn: 'root',
})
export class HomeEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getMisInscripciones(): Observable<MiInscripcion[]> {
    return this.api
      .request(getInscripcionesMisInscripcionesEndpoint)
      .pipe(map(data => this.toMisInscripciones(data)));
  }

  private toMisInscripciones(
    data: { data: MisInscripcionesItem[] | null } | MisInscripcionesItem[] | null | undefined
  ): MiInscripcion[] {
    const items = Array.isArray(data) ? data : (data?.data ?? []);

    return items.map(item => ({
      idProducto: item.idProducto ?? 0,
      idComienzo: item.idComienzo ?? 0,
      idTurno: item.idTurno ?? 0,
      nombreProducto: item.nombreExtensoProducto ?? '',
      nombreComienzo: item.nombreComienzo ?? '',
      nombreTurno: item.nombreTurno ?? '',
      estado: item.estadoInscripcion ?? '',
    }));
  }
}

