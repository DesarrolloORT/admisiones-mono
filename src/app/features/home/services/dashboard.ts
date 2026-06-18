import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HomeEndpoint } from '../endpoints/home.endpoint';
import { MiInscripcion } from '../models/mi-inscripcion';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private readonly endpoint = inject(HomeEndpoint);

  getMisInscripciones(): Observable<MiInscripcion[]> {
    return this.endpoint.getMisInscripciones();
  }
}
