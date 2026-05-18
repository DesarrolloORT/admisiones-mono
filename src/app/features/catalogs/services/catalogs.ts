import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client.service';
import {
  getCatalogosPaisesEstadosCiudadesEndpoint,
  getCatalogosTiposDocumentosEndpoint,
} from 'src/app/shared/api/endpoints/generated/catalogos.endpoints';

import { Country, DocumentType, LocationCountry } from '../models/catalog.interface';

@Injectable({
  providedIn: 'root',
})
export class Catalogs {
  private readonly api = inject(ApiHttpClient);

  public getDocumentTypes(): Observable<DocumentType[]> {
    return this.api.list(getCatalogosTiposDocumentosEndpoint, item => ({
      id: item.codTipoDocumento,
      label: item.descripcion ?? '',
      code: item.descrTd ?? '',
    }));
  }

  public getCountries(): Observable<Country[]> {
    return this.api.list(getCatalogosPaisesEstadosCiudadesEndpoint, item => ({
      id: item.codigoPais,
      label: item.nombre,
    }));
  }

  public getCountryLocations(): Observable<LocationCountry[]> {
    return this.api.list(getCatalogosPaisesEstadosCiudadesEndpoint);
  }

  public clearCache(): void {
    this.api.clearCache();
  }
}

