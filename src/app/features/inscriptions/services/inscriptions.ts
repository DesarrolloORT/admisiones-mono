import { inject, Injectable } from '@angular/core';
import { forkJoin, from, Observable, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

import { InscripcionesEndpoint } from '../endpoints/inscriptions.endpoint';
import type { InscripcionDetail } from '../models/inscription-detail';
import type {
  InscripcionConfirmPreEnrollmentPayload,
  InscripcionIdentityDocumentFile,
  InscripcionIdentityUploadFile,
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
        frente: this.toFile(document?.frente, 'frente-documento.jpg'),
        dorso: this.toFile(document?.dorso, 'dorso-documento.jpg'),
        selfie: this.toBlobFile(photo, 'foto-persona.jpg'),
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
      Promise.all([this.toUploadFile(payload.frente), this.toUploadFile(payload.dorso)])
    ).pipe(
      switchMap(([frente, dorso]) =>
        this.endpoint.uploadIdentityDocument({ fecha: payload.fecha, frente, dorso })
      )
    );
  }

  public uploadIdentityPhoto(file: File): Observable<boolean> {
    return from(this.toUploadFile(file)).pipe(
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

  public pay(payload: InscripcionPaymentPayload): Observable<InscripcionPaymentResponse> {
    return this.endpoint.pay(payload);
  }

  public registerProductInterest(payload: InscripcionProductInterestPayload): Observable<boolean> {
    return this.endpoint.registerProductInterest(payload);
  }

  private async toUploadFile(file: File): Promise<InscripcionIdentityUploadFile> {
    const mimeType = file.type || this.inferUploadMimeType(file.name);
    if (mimeType !== 'image/jpeg' && mimeType !== 'image/png') {
      throw new Error('Invalid identity image type.');
    }

    return {
      nombreArchivo: this.safeFileName(file.name, 'identidad.jpg'),
      archivo: await this.readFileAsBase64(file),
    };
  }

  private inferUploadMimeType(fileName: string): string | null {
    const extension = fileName.split('.').at(-1)?.toLowerCase();
    if (extension === 'png') return 'image/png';
    if (extension === 'jpg' || extension === 'jpeg') return 'image/jpeg';
    return null;
  }

  private readFileAsBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        const result = reader.result;
        if (typeof result !== 'string') {
          reject(new Error('Could not read identity file.'));
          return;
        }

        const [, base64] = result.split(',', 2);
        if (!base64) {
          reject(new Error('Could not read identity file.'));
          return;
        }

        resolve(base64);
      };
      reader.onerror = () => reject(new Error('Could not read identity file.'));
      reader.readAsDataURL(file);
    });
  }
  private toFile(
    file: InscripcionIdentityDocumentFile | null | undefined,
    fallbackName: string
  ): File | null {
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
