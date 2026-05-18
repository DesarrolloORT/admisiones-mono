import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client.service';
import {
  getCatalogosPaisesEstadosCiudadesEndpoint,
  getCatalogosTiposDocumentosEndpoint,
} from 'src/app/shared/api/endpoints/generated/catalogos.endpoints';

import { Country, DocumentType } from '../models/catalog.interface';
import { CatalogRequestError, CatalogType } from '../models/catalog-error';

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
          id: item.codigoPais,
          label: item.nombre,
        }))
      ),
      catchError(err => this.toRequestError('country', err))
    );
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
