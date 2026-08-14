import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HomeEndpoint } from '../endpoints/home.endpoint';
import { EnrollmentSummary } from '../models/enrollment-summary';

@Injectable({
  providedIn: 'root',
})
export class HomeService {
  private readonly endpoint = inject(HomeEndpoint);

  public getMyEnrollments(): Observable<EnrollmentSummary[]> {
    return this.endpoint.getMyEnrollments();
  }
}
