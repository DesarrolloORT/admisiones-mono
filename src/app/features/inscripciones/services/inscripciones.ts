import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { InscripcionesEndpoint } from '../endpoints/inscripciones.endpoint';

@Injectable({
  providedIn: 'root',
})
export class Inscripciones {
  private readonly endpoint = inject(InscripcionesEndpoint);

  public registerProductInterest(payload: {
    idOferta: number;
    idProcesoSeleccionado: number;
    idProducto: number;
  }): Observable<boolean> {
    return this.endpoint.registerProductInterest(payload);
  }
}
