import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HomeEndpoint } from '../endpoints/home.endpoint';
import { MiInscripcion } from '../models/mi-inscripcion';

@Injectable({
  providedIn: 'root',
})
export class HomeService {
  private readonly endpoint = inject(HomeEndpoint);

  public getMisEnrollments(): Observable<MiInscripcion[]> {
    return this.endpoint.getMisEnrollments();
  }
}
