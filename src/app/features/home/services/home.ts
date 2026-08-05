import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HomeEndpoint } from '../endpoints/home.endpoint';
import { MiBeca } from '../models/mi-beca';
import { MiInscripcion } from '../models/mi-inscripcion';

@Injectable({
  providedIn: 'root',
})
export class HomeService {
  private readonly endpoint = inject(HomeEndpoint);

  public getMisInscripciones(): Observable<MiInscripcion[]> {
    return this.endpoint.getMisInscripciones();
  }

  public getMisBecas(): Observable<MiBeca[]> {
    return this.endpoint.getMisBecas();
  }
}
