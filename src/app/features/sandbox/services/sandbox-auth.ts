import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface SandboxLoginRequest {
  codigoPersona: number;
  password: string;
}

export interface SandboxLoginResponse {
  message?: string;
}

export interface SandboxDocumentRecognitionRequest {
  tipoMime: string;
  archivoAdjunto: {
    nombreArchivo: string;
    archivo: string;
  };
}

export interface SandboxDocumentRecognitionFields {
  [key: string]: unknown;
}

export interface SandboxDocumentRecognitionLine {
  pagina: number;
  texto: string;
}

export interface SandboxDocumentRecognitionData {
  modeloUtilizado?: string;
  versionApi?: string;
  textoCompleto?: string;
  cantidadPaginas?: number;
  anguloPaginaPrincipal?: number;
  requiereRevision?: boolean;
  campos?: SandboxDocumentRecognitionFields;
  lineas?: SandboxDocumentRecognitionLine[];
  advertencias?: string[];
}

export interface SandboxDocumentRecognitionResponse {
  success?: boolean;
  httpCode?: number;
  errorCode?: string;
  method?: string;
  message?: string;
  data?: SandboxDocumentRecognitionData;
  [key: string]: unknown;
}

@Injectable({
  providedIn: 'root',
})
export class SandboxAuth {
  private readonly http = inject(HttpClient);

  public readonly loginUrl = environment.API_URL;
  public readonly documentRecognitionUrl = this.resolveDocumentRecognitionUrl(environment.API_URL);

  public login(payload: SandboxLoginRequest): Observable<SandboxLoginResponse> {
    return this.http.post(this.loginUrl, payload, { withCredentials: true });
  }

  public recognizeDocument(
    payload: SandboxDocumentRecognitionRequest
  ): Observable<SandboxDocumentRecognitionResponse> {
    return this.http.post<SandboxDocumentRecognitionResponse>(
      this.documentRecognitionUrl,
      payload,
      {
        withCredentials: true,
      }
    );
  }

  private resolveDocumentRecognitionUrl(loginUrl: string): string {
    try {
      return new URL('/ReconocimientoDocumento/Reconocer', loginUrl).toString();
    } catch {
      return '/ReconocimientoDocumento/Reconocer';
    }
  }
}

