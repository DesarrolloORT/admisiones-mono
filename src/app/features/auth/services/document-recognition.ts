import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DocumentRecognitionEndpoint } from '../endpoints/document-recognition.endpoint';
import {
  DocumentRecognitionRequest,
  DocumentRecognitionResponse,
} from '../models/document-recognition.interface';
import { DocumentRecognitionFileError } from '../models/document-recognition-error';

@Injectable({
  providedIn: 'root',
})
export class DocumentRecognition {
  public static readonly MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

  private static readonly MIME_PATTERN = /^[\w.+-]+\/[\w.+-]+$/;
  private static readonly MIME_BY_EXTENSION: Record<string, string> = {
    pdf: 'application/pdf',
    jpg: 'image/jpeg',
    jpeg: 'image/jpeg',
    png: 'image/png',
    tif: 'image/tiff',
    tiff: 'image/tiff',
    bmp: 'image/bmp',
    webp: 'image/webp',
    heic: 'image/heic',
  };

  private readonly endpoint = inject(DocumentRecognitionEndpoint);

  public readonly maxFileSizeBytes = DocumentRecognition.MAX_FILE_SIZE_BYTES;

  public recognizeDocument(
    payload: DocumentRecognitionRequest
  ): Observable<DocumentRecognitionResponse> {
    return this.endpoint.recognizeDocument(payload);
  }

  public async createRequestFromFile(file: File): Promise<DocumentRecognitionRequest> {
    if (file.size > DocumentRecognition.MAX_FILE_SIZE_BYTES) {
      throw new DocumentRecognitionFileError('maxFileSize');
    }

    const tipoMime = this.inferMimeType(file);
    if (!tipoMime) {
      throw new DocumentRecognitionFileError('invalidMimeType');
    }

    const archivo = await this.readFileAsBase64(file);

    return {
      tipoMime,
      archivoAdjunto: {
        nombreArchivo: file.name,
        archivo,
      },
    };
  }

  private inferMimeType(file: File): string | null {
    if (file.type && DocumentRecognition.MIME_PATTERN.test(file.type)) {
      return file.type;
    }

    const extension = this.getFileExtension(file.name);
    return extension ? (DocumentRecognition.MIME_BY_EXTENSION[extension] ?? null) : null;
  }

  private readFileAsBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = () => {
        const result = reader.result;
        if (typeof result !== 'string') {
          reject(new DocumentRecognitionFileError('readFailed'));
          return;
        }

        const [, base64] = result.split(',', 2);
        if (!base64) {
          reject(new DocumentRecognitionFileError('readFailed'));
          return;
        }

        resolve(base64);
      };

      reader.onerror = () => {
        reject(new DocumentRecognitionFileError('readFailed'));
      };

      reader.readAsDataURL(file);
    });
  }

  private getFileExtension(fileName: string): string | null {
    const dotIndex = fileName.lastIndexOf('.');
    if (dotIndex === -1 || dotIndex === fileName.length - 1) {
      return null;
    }

    return fileName.slice(dotIndex + 1).toLowerCase();
  }
}
