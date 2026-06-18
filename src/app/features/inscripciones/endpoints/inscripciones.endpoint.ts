import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  postInscripcionesRegistrarInteresProductoEndpoint,
  type RegistrarInteresProductoPayload,
} from 'src/app/shared/api/generated/endpoints/inscripciones.endpoints';

@Injectable({
  providedIn: 'root',
})
export class InscripcionesEndpoint {
  private readonly api = inject(ApiHttpClient);

  public registerProductInterest(payload: RegistrarInteresProductoPayload): Observable<boolean> {
    return this.api.request(postInscripcionesRegistrarInteresProductoEndpoint, {
      body: payload,
      showLoader: true,
    });
  }
}
