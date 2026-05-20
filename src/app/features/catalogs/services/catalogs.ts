import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CatalogsEndpoint } from '../endpoints/catalogs.endpoint';
import {
  Career,
  Comienzo,
  Country,
  DocumentType,
  LocationCountry,
} from '../models/catalog.interface';

@Injectable({
  providedIn: 'root',
})
export class Catalogs {
  private readonly endpoint = inject(CatalogsEndpoint);

  public getDocumentTypes(): Observable<DocumentType[]> {
    return this.endpoint.getDocumentTypes();
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

