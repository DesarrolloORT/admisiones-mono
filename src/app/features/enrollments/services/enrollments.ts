import { inject, Injectable } from '@angular/core';
import { forkJoin, from, Observable, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

import { EnrollmentsApi } from '../api/enrollments.api';
import { toBlobFile, toIdentityFile, toIdentityUploadFile } from '../models/enrollment-files';

export interface EnrollmentIdentityPreload {
  front: File | null;
  back: File | null;
  selfie: File | null;
  expirationDate: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class Enrollments {
  private readonly endpoint = inject(EnrollmentsApi);

  public getIdentityPreload(): Observable<EnrollmentIdentityPreload> {
    return forkJoin({
      document: this.endpoint.getIdentityDocument().pipe(catchError(() => of(null))),
      photo: this.endpoint.getIdentityPhoto().pipe(catchError(() => of(null))),
    }).pipe(
      map(({ document, photo }) => ({
        front: toIdentityFile(document?.front, 'identity-document-front'),
        back: toIdentityFile(document?.back, 'identity-document-back'),
        selfie: toBlobFile(photo, 'identity-photo'),
        expirationDate: document?.expirationDate ?? null,
      }))
    );
  }

  public uploadIdentityDocument(payload: {
    date: string;
    front: File;
    back: File;
  }): Observable<boolean> {
    return from(
      Promise.all([toIdentityUploadFile(payload.front), toIdentityUploadFile(payload.back)])
    ).pipe(
      switchMap(([front, back]) =>
        this.endpoint.uploadIdentityDocument({ date: payload.date, front, back })
      )
    );
  }

  public uploadIdentityPhoto(file: File): Observable<boolean> {
    return from(toIdentityUploadFile(file)).pipe(
      switchMap(attachedFile => this.endpoint.uploadIdentityPhoto({ attachedFile }))
    );
  }
}
