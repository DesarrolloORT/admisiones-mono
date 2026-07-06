import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ScholarshipAcademicStepData,
  ScholarshipEndpoint,
} from '../endpoints/scholarship.endpoint';

@Injectable({ providedIn: 'root' })
export class ScholarshipAcademicData {
  private readonly endpoint = inject(ScholarshipEndpoint);

  public getAcademicStepData(): Observable<ScholarshipAcademicStepData[]> {
    return this.endpoint.getAcademicStepData();
  }
}
