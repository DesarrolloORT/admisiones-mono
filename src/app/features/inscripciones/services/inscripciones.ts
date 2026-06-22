import { inject, Injectable } from '@angular/core';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { InscripcionesEndpoint } from '../endpoints/inscripciones.endpoint';
import type {
  InscripcionConfirmPreEnrollmentPayload,
  InscripcionInitialSurveyPayload,
  InscripcionInitialSurveyResponse,
  InscripcionPreEnrollmentResponse,
  InscripcionProductInterestPayload,
} from '../models/inscripcion-flow';

interface IdentityDocumentFile {
  archivo?: string | null;
  nombreArchivo?: string | null;
}

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

  public getIdentityPreload(): Observable<InscripcionIdentityPreload> {
    return forkJoin({
      document: this.endpoint.getIdentityDocument().pipe(catchError(() => of(null))),
      photo: this.endpoint.getIdentityPhoto().pipe(catchError(() => of(null))),
    }).pipe(
      map(({ document, photo }) => ({
        frente: this.toFile(document?.frente, 'frente-documento.jpg'),
        dorso: this.toFile(document?.dorso, 'dorso-documento.jpg'),
        selfie: this.toBlobFile(photo, 'foto-persona.jpg'),
        fechaVencimiento: document?.fechaVencimiento ?? null,
      }))
    );
  }

  public getInitialSurvey(): Observable<InscripcionInitialSurveyResponse> {
    return this.endpoint.getInitialSurvey();
  }

  public saveInitialSurvey(payload: InscripcionInitialSurveyPayload): Observable<boolean> {
    return this.endpoint.saveInitialSurvey(payload);
  }

  public confirmPreEnrollment(
    payload: InscripcionConfirmPreEnrollmentPayload
  ): Observable<InscripcionPreEnrollmentResponse> {
    return this.endpoint.confirmPreEnrollment(payload);
  }

  public registerProductInterest(payload: InscripcionProductInterestPayload): Observable<boolean> {
    return this.endpoint.registerProductInterest(payload);
  }

  private toFile(file: IdentityDocumentFile | null | undefined, fallbackName: string): File | null {
    const rawContent = file?.archivo?.trim();
    if (!rawContent) return null;

    const name = this.safeFileName(file?.nombreArchivo, fallbackName);
    const parsed = this.parseBase64(rawContent, name);
    return parsed ? new File([parsed.bytes], name, { type: parsed.mimeType }) : null;
  }

  private toBlobFile(blob: Blob | null, fallbackName: string): File | null {
    if (!(blob instanceof Blob) || blob.size === 0) return null;

    return new File([blob], fallbackName, { type: blob.type || 'image/jpeg' });
  }

  private parseBase64(
    value: string,
    fileName: string
  ): { bytes: ArrayBuffer; mimeType: string } | null {
    const dataUrlMatch = /^data:([^;,]+);base64,(.*)$/i.exec(value);
    const mimeType = dataUrlMatch?.[1] ?? this.inferMimeType(fileName);
    const base64 = (dataUrlMatch?.[2] ?? value).replace(/\s/g, '');

    try {
      const binary = globalThis.atob(base64);
      const buffer = new ArrayBuffer(binary.length);
      const bytes = new Uint8Array(buffer);
      for (let index = 0; index < binary.length; index += 1) {
        bytes[index] = binary.charCodeAt(index);
      }

      return { bytes: buffer, mimeType };
    } catch {
      return null;
    }
  }

  private safeFileName(value: string | null | undefined, fallback: string): string {
    const name = value?.split(/[\\/]/).at(-1)?.trim();
    return name || fallback;
  }

  private inferMimeType(fileName: string): string {
    const extension = fileName.split('.').at(-1)?.toLowerCase();
    if (extension === 'png') return 'image/png';
    if (extension === 'jpg' || extension === 'jpeg') return 'image/jpeg';
    if (extension === 'webp') return 'image/webp';
    return 'image/jpeg';
  }
}
