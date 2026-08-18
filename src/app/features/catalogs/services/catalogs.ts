import { inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

import { CatalogsEndpoint } from '../endpoints/catalogs.endpoint';
import type { AcademicProposalTypeId } from '../models/academic-proposal';
import {
  Bank,
  Country,
  DegreeProgram,
  DocumentType,
  EducationalInstitution,
  InitialSurveyCatalogs,
  Intake,
  LocationCountry,
  Seminar,
  Shift,
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

  public getDegreePrograms(academicProposal: AcademicProposalTypeId): Observable<DegreeProgram[]> {
    return this.endpoint.getDegreePrograms(academicProposal);
  }

  public getIntakes(degreeProgramId: number): Observable<Intake[]> {
    return this.endpoint.getIntakes(degreeProgramId);
  }

  public getInitialSurveyCatalogs(): Observable<InitialSurveyCatalogs> {
    return this.endpoint.getInitialSurveyCatalogs();
  }

  public getBanks(): Observable<Bank[]> {
    return this.endpoint.getBanks();
  }

  public getInstitutions(
    countryCode: number,
    stateCode: number
  ): Observable<EducationalInstitution[]> {
    return this.endpoint.getInstitutions(countryCode, stateCode);
  }

  public getShifts(degreeProgramId: number, admissionProcessId: number): Observable<Shift[]> {
    return this.endpoint.getShifts(degreeProgramId, admissionProcessId);
  }

  public getSeminars(degreeProgramId: number, admissionProcessId: number): Observable<Seminar[]> {
    return this.endpoint.getShifts(degreeProgramId, admissionProcessId).pipe(
      map(shifts =>
        shifts.map(shift => ({
          offeringId: shift.offeringId,
          admissionProcessId: admissionProcessId,
          name: shift.offeringDescription,
          startDate: shift.referenceDate,
        }))
      )
    );
  }
}
