import { inject, Injectable } from '@angular/core';
import { forkJoin, from, Observable, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

import { InscripcionesEndpoint } from '../endpoints/inscriptions.endpoint';
import type { InscripcionDetail } from '../models/inscription-detail';
import { toBlobFile, toIdentityFile, toIdentityUploadFile } from '../models/inscription-files';
import type {
  InscripcionConfirmPreEnrollmentPayload,
  InscripcionInitialSurveyPayload,
  InscripcionInitialSurveyResponse,
  InscripcionPaymentPayload,
  InscripcionPaymentResponse,
  InscripcionPreEnrollmentResponse,
  InscripcionProductInterestPayload,
  InscripcionStudentRegulationAcceptance,
} from '../models/inscription-flow';

export interface InscripcionIdentityPreload {
  frente: File | null;
  dorso: File | null;
  selfie: File | null;
  fechaVencimiento: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class Inscripciones {
  private readonly endpoint = inject(InscripcionesEndpoint);

  public getDetail(idProducto: number, idProceso: number): Observable<InscripcionDetail> {
    return this.endpoint.getDetail(idProducto, idProceso);
  }

  public getIdentityPreload(): Observable<InscripcionIdentityPreload> {
    return forkJoin({
      document: this.endpoint.getIdentityDocument().pipe(catchError(() => of(null))),
      photo: this.endpoint.getIdentityPhoto().pipe(catchError(() => of(null))),
    }).pipe(
      map(({ document, photo }) => ({
        frente: toIdentityFile(document?.frente, 'frente-documento'),
        dorso: toIdentityFile(document?.dorso, 'dorso-documento'),
        selfie: toBlobFile(photo, 'foto-persona'),
        fechaVencimiento: document?.fechaVencimiento ?? null,
      }))
    );
  }

  public uploadIdentityDocument(payload: {
    fecha: string;
    frente: File;
    dorso: File;
  }): Observable<boolean> {
    return from(
      Promise.all([toIdentityUploadFile(payload.frente), toIdentityUploadFile(payload.dorso)])
    ).pipe(
      switchMap(([frente, dorso]) =>
        this.endpoint.uploadIdentityDocument({ fecha: payload.fecha, frente, dorso })
      )
    );
  }

  public uploadIdentityPhoto(file: File): Observable<boolean> {
    return from(toIdentityUploadFile(file)).pipe(
      switchMap(archivoAdjunto => this.endpoint.uploadIdentityPhoto({ archivoAdjunto }))
    );
  }

  public getInitialSurvey(): Observable<InscripcionInitialSurveyResponse> {
    return this.endpoint.getInitialSurvey();
  }

  public saveInitialSurvey(payload: InscripcionInitialSurveyPayload): Observable<boolean> {
    return this.endpoint.saveInitialSurvey(payload);
  }

  public getStudentRegulationAcceptance(): Observable<InscripcionStudentRegulationAcceptance> {
    return this.endpoint.getStudentRegulationAcceptance();
  }

  public confirmPreEnrollment(
    payload: InscripcionConfirmPreEnrollmentPayload
  ): Observable<InscripcionPreEnrollmentResponse> {
    return this.endpoint.confirmPreEnrollment(payload);
  }

  public reactivate(idInscripcion: number): Observable<InscripcionPreEnrollmentResponse> {
    return this.endpoint.reactivate(idInscripcion);
  }

  public pay(payload: InscripcionPaymentPayload): Observable<InscripcionPaymentResponse> {
    return this.endpoint.pay(payload);
  }

  public registerProductInterest(payload: InscripcionProductInterestPayload): Observable<boolean> {
    return this.endpoint.registerProductInterest(payload);
  }
}
