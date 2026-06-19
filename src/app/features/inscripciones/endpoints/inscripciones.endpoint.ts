import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  type InteresProductoPayload,
  postInscripcionesInteresProductoEndpoint,
} from 'src/app/shared/api/generated/endpoints/inscripciones.endpoints';

@Injectable({
  providedIn: 'root',
})
export class InscripcionesEndpoint {
  private readonly api = inject(ApiHttpClient);

  public registerProductInterest(payload: InteresProductoPayload): Observable<boolean> {
    return this.api.request(postInscripcionesInteresProductoEndpoint, {
      body: payload,
      showLoader: true,
    });
  }
}
