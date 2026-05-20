import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client.service';
import {
  getCatalogosCarrerasEndpoint,
  getCatalogosComienzosEndpoint,
  getCatalogosPaisesEstadosCiudadesEndpoint,
  getCatalogosTiposDocumentosEndpoint,
  PaisesEstadosCiudadesItem,
  PaisesEstadosCiudadesItemEstado,
  PaisesEstadosCiudadesItemEstadoCiudad,
} from 'src/app/shared/api/generated/endpoints/catalogos.endpoints';

import { CatalogRequestError, CatalogType } from '../models/catalog-error';
import {
  Career,
  Comienzo,
  Country,
  DocumentType,
  LocationCity,
  LocationCountry,
  LocationState,
} from '../models/catalog.interface';

@Injectable({
  providedIn: 'root',
})
export class CatalogsEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getDocumentTypes(): Observable<DocumentType[]> {
    return this.api.request(getCatalogosTiposDocumentosEndpoint).pipe(
      map(result =>
        this.fromData(result, item => ({
          id: item.codTipoDocumento,
          label: item.descripcion ?? '',
          code: item.descrTd ?? '',
        }))
      ),
      catchError(err => this.toRequestError('documentType', err))
    );
  }

  public getCountries(): Observable<Country[]> {
    return this.api.request(getCatalogosPaisesEstadosCiudadesEndpoint).pipe(
      map(result =>
        this.fromData(result, item => ({
          id: item.codigoPais ?? 0,
          label: item.nombre ?? '',
        }))
      ),
      catchError(err => this.toRequestError('country', err))
    );
  }

  public getCountryLocations(): Observable<LocationCountry[]> {
    return this.api.request(getCatalogosPaisesEstadosCiudadesEndpoint).pipe(
      map(result => this.fromData(result, item => this.toLocationCountry(item))),
      catchError(err => this.toRequestError('country', err))
    );
  }

  public getCareers(): Observable<Career[]> {
    return this.api.request(getCatalogosCarrerasEndpoint).pipe(
      map(result =>
        this.fromData(result, item => ({
          idProducto: item.idProducto ?? 0,
          idNivelProducto: item.idNivelProducto ?? 0,
          nombreProducto: item.nombreProducto ?? '',
          nombreNivelProducto: item.nombreNivelProducto ?? '',
        }))
      ),
      catchError(err => this.toRequestError('career', err))
    );
  }

  public getComienzos(idCarrera: number): Observable<Comienzo[]> {
    return this.api.request(getCatalogosComienzosEndpoint, { queryParams: { idCarrera } }).pipe(
      map(result =>
        this.fromData(result, item => ({
          idProceso: item.idProceso ?? 0,
          nombreProceso: item.nombreProceso ?? '',
        }))
      ),
      catchError(err => this.toRequestError('comienzo', err))
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
    result: { data?: TItem | TItem[] | null },
    mapper: (item: TItem) => TResult
  ): TResult[] {
    const data = result.data;

    if (!data) {
      return [];
    }

    return (Array.isArray(data) ? data : [data]).map(mapper);
  }

  private toRequestError(catalog: CatalogType, error: unknown): Observable<never> {
    const status = error instanceof HttpErrorResponse ? error.status : null;
    return throwError(() => new CatalogRequestError(catalog, status, this.getErrorMessage(error)));
  }

  private getErrorMessage(error: unknown): string | null {
    if (error instanceof HttpErrorResponse) {
      const payload = error.error;

      if (payload && typeof payload === 'object' && 'message' in payload) {
        const message = payload.message;
        return typeof message === 'string' ? message : null;
      }

      return typeof payload === 'string' ? payload : error.message;
    }

    return error instanceof Error ? error.message : null;
  }
}

