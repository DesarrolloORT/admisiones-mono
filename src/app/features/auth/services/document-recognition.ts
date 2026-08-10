import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  compressImageIfNeeded,
  MAX_IMAGE_SIZE_BYTES,
  resolveImageMimeType,
} from 'src/app/shared/files/image-upload';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import type {
  DocumentRecognitionData,
  DocumentRecognitionRequest,
} from '../models/document-recognition.interface';
import { DocumentRecognitionFileError } from '../models/document-recognition-error';

@Injectable({
  providedIn: 'root',
})
export class DocumentRecognition {
  public readonly maxFileSizeBytes = MAX_IMAGE_SIZE_BYTES;

  private readonly endpoint = inject(AuthEndpoint);

  public recognizeDocument(
    payload: DocumentRecognitionRequest
  ): Observable<DocumentRecognitionData> {
    return this.endpoint.recognizeDocument(payload);
  }

  public async createRequestFromFile(file: File): Promise<DocumentRecognitionRequest> {
    const tipoMime = resolveImageMimeType(file);
    if (!tipoMime) {
      throw new DocumentRecognitionFileError('invalidMimeType');
    }

    const preparedFile = await compressImageIfNeeded(file, tipoMime);
    if (preparedFile.size > MAX_IMAGE_SIZE_BYTES) {
      throw new DocumentRecognitionFileError('maxFileSize');
    }

    const archivo = await this.readFileAsBase64(preparedFile);

    return {
      tipoMime: preparedFile.type || tipoMime,
      archivoAdjunto: {
        nombreArchivo: preparedFile.name,
        archivo,
      },
    };
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
}
