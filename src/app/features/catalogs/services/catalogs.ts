import { inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

import { CatalogsEndpoint } from '../endpoints/catalogs.endpoint';
import {
  Career,
  Comienzo,
  Country,
  DocumentType,
  LocationCountry,
} from '../models/catalog.interface';

// TODO: reemplazar cuando el endpoint getCatalogosTiposDocumentos vuelva al API.
const STATIC_DOCUMENT_TYPES: DocumentType[] = [
  { id: 1, label: 'Cédula', code: 'CI' },
  { id: 2, label: 'Pasaporte', code: 'PS' },
  { id: 3, label: 'Documento extranjero', code: 'DE' },
];

@Injectable({
  providedIn: 'root',
})
export class Catalogs {
  private readonly endpoint = inject(CatalogsEndpoint);

  public getDocumentTypes(): Observable<DocumentType[]> {
    return of(STATIC_DOCUMENT_TYPES);
  }

  public getCountries(): Observable<Country[]> {
    return this.endpoint.getCountries();
  }

  public getCountryLocations(): Observable<LocationCountry[]> {
    return this.endpoint.getCountryLocations();
  }

  public getCareers(): Observable<Career[]> {
    return this.endpoint.getCareers();
  }

  public getComienzos(idCarrera: number): Observable<Comienzo[]> {
    return this.endpoint.getComienzos(idCarrera);
  }

  public clearCache(): void {
    this.endpoint.clearCache();
  }
}

