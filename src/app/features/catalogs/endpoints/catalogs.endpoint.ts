import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getCatalogosCarrerasEndpoint,
  getCatalogosComienzosEndpoint,
  getCatalogosPaisesEstadosCiudadesEndpoint,
  PaisesEstadosCiudadesItem,
  PaisesEstadosCiudadesItemEstado,
  PaisesEstadosCiudadesItemEstadoCiudad,
} from 'src/app/shared/api/generated/endpoints/catalogos.endpoints';

import {
  Career,
  Comienzo,
  Country,
  LocationCity,
  LocationCountry,
  LocationState,
} from '../models/catalog.interface';

@Injectable({
  providedIn: 'root',
})
export class CatalogsEndpoint {
  private readonly api = inject(ApiHttpClient);

  // TODO: getCatalogosTiposDocumentosEndpoint fue removido del API.
  // Reimplementar getDocumentTypes() cuando haya un endpoint de reemplazo.

  public getCountries(): Observable<Country[]> {
    return this.api.request(getCatalogosPaisesEstadosCiudadesEndpoint).pipe(
      map(data =>
        this.fromData(data, item => ({
          id: item.codigoPais ?? 0,
          label: item.nombre ?? '',
        }))
      )
    );
  }

  public getCountryLocations(): Observable<LocationCountry[]> {
    return this.api
      .request(getCatalogosPaisesEstadosCiudadesEndpoint)
      .pipe(map(data => this.fromData(data, item => this.toLocationCountry(item))));
  }

  public getCareers(): Observable<Career[]> {
    return this.api.request(getCatalogosCarrerasEndpoint).pipe(
      map(data =>
        this.fromData(data, item => ({
          idProducto: item.idProducto ?? 0,
          idNivelProducto: item.idNivelProducto ?? 0,
          nombreProducto: item.nombreProducto ?? '',
          nombreNivelProducto: item.nombreNivelProducto ?? '',
        }))
      )
    );
  }

  public getComienzos(idCarrera: number): Observable<Comienzo[]> {
    return this.api.request(getCatalogosComienzosEndpoint, { queryParams: { idCarrera } }).pipe(
      map(data =>
        this.fromData(data, item => ({
          idProceso: item.idProceso ?? 0,
          nombreProceso: item.nombreProceso ?? '',
        }))
      )
    );
  }

  public clearCache(): void {
    this.api.clearCache();
  }

  private toLocationCountry(item: PaisesEstadosCiudadesItem): LocationCountry {
    return {
      codigoPais: item.codigoPais ?? 0,
      nombre: item.nombre ?? '',
      estado: item.estado?.map(s => this.toLocationState(s)) ?? null,
    };
  }

  private toLocationState(s: PaisesEstadosCiudadesItemEstado): LocationState {
    return {
      codigoPais: s.codigoPais ?? 0,
      codigoEstado: s.codigoEstado ?? 0,
      nombre: s.nombre ?? '',
      ciudad: s.ciudad?.map(c => this.toLocationCity(c)) ?? null,
    };
  }

  private toLocationCity(c: PaisesEstadosCiudadesItemEstadoCiudad): LocationCity {
    return {
      codigoPais: c.codigoPais ?? 0,
      codigoEstado: c.codigoEstado ?? 0,
      codigoCiudad: c.codigoCiudad ?? 0,
      nombre: c.nombre ?? '',
    };
  }

  private fromData<TItem, TResult>(
    data: TItem | TItem[] | null | undefined,
    mapper: (item: TItem) => TResult
  ): TResult[] {
    if (!data) {
      return [];
    }

    return (Array.isArray(data) ? data : [data]).map(mapper);
  }
}
