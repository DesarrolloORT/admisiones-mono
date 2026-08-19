import { Injectable } from '@angular/core';
import {
  compressImageIfNeeded,
  MAX_IMAGE_SIZE_BYTES,
  resolveImageMimeType,
} from 'src/app/shared/files/image-upload';

import type { DocumentRecognitionRequest } from '../models/document-recognition.interface';
import { DocumentRecognitionFileError } from '../models/document-recognition-error';

@Injectable({
  providedIn: 'root',
})
export class DocumentRecognition {
  public readonly maxFileSizeBytes = MAX_IMAGE_SIZE_BYTES;

  public async createRequestFromFile(file: File): Promise<DocumentRecognitionRequest> {
    const mimeType = resolveImageMimeType(file);
    if (!mimeType) {
      throw new DocumentRecognitionFileError('invalidMimeType');
    }

    const preparedFile = await compressImageIfNeeded(file, mimeType);
    if (preparedFile.size > MAX_IMAGE_SIZE_BYTES) {
      throw new DocumentRecognitionFileError('maxFileSize');
    }

    const content = await this.readFileAsBase64(preparedFile);

    return {
      mimeType: preparedFile.type || mimeType,
      attachment: {
        fileName: preparedFile.name,
        content,
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
