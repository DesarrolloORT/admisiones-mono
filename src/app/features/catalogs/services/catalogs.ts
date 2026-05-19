import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CatalogsEndpoint } from '../endpoints/catalogs.endpoint';
import { Country, DocumentType, LocationCountry } from '../models/catalog.interface';

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

  public clearCache(): void {
    this.endpoint.clearCache();
  }
}

