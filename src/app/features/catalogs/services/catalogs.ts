import { inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

import { CatalogsEndpoint } from '../endpoints/catalogs.endpoint';
import {
  Bank,
  Career,
  Comienzo,
  Country,
  DocumentType,
  EducationalInstitution,
  InitialSurveyCatalogs,
  LocationCountry,
  Seminario,
  Turno,
} from '../models/catalog.interface';

const DOCUMENT_TYPES: DocumentType[] = [
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
    return of(DOCUMENT_TYPES);
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

  public getInitialSurveyCatalogs(): Observable<InitialSurveyCatalogs> {
    return this.endpoint.getInitialSurveyCatalogs();
  }

  public getBancos(): Observable<Bank[]> {
    return this.endpoint.getBancos();
  }

  public getInstituciones(
    codigoPais: number,
    codigoEstado: number
  ): Observable<EducationalInstitution[]> {
    return this.endpoint.getInstituciones(codigoPais, codigoEstado);
  }

  public getTurnos(idCarrera: number, idProceso: number): Observable<Turno[]> {
    return this.endpoint.getTurnos(idCarrera, idProceso);
  }

  public getSeminarios(idPrograma: number, idProceso: number): Observable<Seminario[]> {
    return this.endpoint.getTurnos(idPrograma, idProceso).pipe(
      map(turnos =>
        turnos.map(turno => ({
          idOferta: turno.idOferta,
          idProceso,
          nombre: turno.horarioReferencia
            ? `${turno.nombreTurno} (${turno.horarioReferencia})`
            : turno.nombreTurno,
          fechaComienzo: null,
        }))
      )
    );
  }

  public clearCache(): void {
    this.endpoint.clearCache();
  }
}
