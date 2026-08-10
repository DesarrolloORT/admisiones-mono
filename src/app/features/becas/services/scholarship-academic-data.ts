import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export interface ScholarshipAcademicStepData {
  carrera: string;
  comienzo: string;
  turno: string;
}

@Injectable({ providedIn: 'root' })
export class ScholarshipAcademicData {
  // TODO: restore ScholarshipEndpoint call once scholarship.endpoint is reinstated.
  public getAcademicStepData(): Observable<ScholarshipAcademicStepData[]> {
    return of([]);
  }
}
