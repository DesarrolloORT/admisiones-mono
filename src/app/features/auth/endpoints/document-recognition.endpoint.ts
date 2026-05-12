import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

import {
  DocumentRecognitionRequest,
  DocumentRecognitionResponse,
} from '../models/document-recognition.interface';
import { DocumentRecognitionRequestError } from '../models/document-recognition-error';

@Injectable({
  providedIn: 'root',
})
export class DocumentRecognitionEndpoint {
  private readonly http = inject(HttpClient);

  public readonly documentRecognitionUrl = this.resolveDocumentRecognitionUrl(environment.API_URL);

  public recognizeDocument(
    payload: DocumentRecognitionRequest
  ): Observable<DocumentRecognitionResponse> {
    return this.http
      .post<DocumentRecognitionResponse>(this.documentRecognitionUrl, payload, {
        withCredentials: true,
      })
      .pipe(catchError(error => this.toRequestError(error)));
  }

  private toRequestError(error: unknown): Observable<never> {
    const status = error instanceof HttpErrorResponse ? error.status : null;
    return throwError(() => new DocumentRecognitionRequestError(status));
  }

  private resolveDocumentRecognitionUrl(loginUrl: string): string {
    try {
      return new URL('/ReconocimientoDocumento/Reconocer', loginUrl).toString();
    } catch {
      return '/ReconocimientoDocumento/Reconocer';
    }
  }
}
