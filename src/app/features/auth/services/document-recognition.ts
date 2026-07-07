import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

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
  public static readonly MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
  public static readonly IMAGE_COMPRESSION_THRESHOLD_BYTES = 5 * 1024 * 1024;

  private static readonly COMPRESSED_IMAGE_MAX_SIDE_PX = 2000;
  private static readonly COMPRESSED_IMAGE_QUALITY = 0.82;
  private static readonly COMPRESSED_IMAGE_TYPE = 'image/jpeg';
  private static readonly MIME_BY_EXTENSION: Record<string, string> = {
    pdf: 'application/pdf',
    jpg: 'image/jpeg',
    jpeg: 'image/jpeg',
    png: 'image/png',
  };
  private static readonly ALLOWED_MIME_TYPES = new Set(
    Object.values(DocumentRecognition.MIME_BY_EXTENSION)
  );

  private readonly endpoint = inject(AuthEndpoint);

  public readonly maxFileSizeBytes = DocumentRecognition.MAX_FILE_SIZE_BYTES;
  public readonly imageCompressionThresholdBytes =
    DocumentRecognition.IMAGE_COMPRESSION_THRESHOLD_BYTES;

  public recognizeDocument(
    payload: DocumentRecognitionRequest
  ): Observable<DocumentRecognitionData> {
    return this.endpoint.recognizeDocument(payload);
  }

  public async createRequestFromFile(file: File): Promise<DocumentRecognitionRequest> {
    const tipoMime = this.inferMimeType(file);
    if (!tipoMime) {
      throw new DocumentRecognitionFileError('invalidMimeType');
    }

    if (file.size > DocumentRecognition.MAX_FILE_SIZE_BYTES) {
      throw new DocumentRecognitionFileError('maxFileSize');
    }

    const preparedFile = await this.compressImageIfNeeded(file, tipoMime);
    if (preparedFile.size > DocumentRecognition.MAX_FILE_SIZE_BYTES) {
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

  private async compressImageIfNeeded(file: File, tipoMime: string): Promise<File> {
    if (!this.shouldCompressImage(file, tipoMime)) {
      return file;
    }

    const compressedFile = await this.compressImage(file);
    return compressedFile && compressedFile.size > 0 && compressedFile.size < file.size
      ? compressedFile
      : file;
  }

  private shouldCompressImage(file: File, tipoMime: string): boolean {
    return (
      file.size > DocumentRecognition.IMAGE_COMPRESSION_THRESHOLD_BYTES &&
      file.size <= DocumentRecognition.MAX_FILE_SIZE_BYTES &&
      tipoMime.startsWith('image/')
    );
  }

  private async compressImage(file: File): Promise<File | null> {
    if (typeof globalThis.createImageBitmap !== 'function' || !globalThis.document) {
      return null;
    }

    try {
      const bitmap = await globalThis.createImageBitmap(file);

      try {
        if (bitmap.width < 1 || bitmap.height < 1) {
          return null;
        }

        const canvas = globalThis.document.createElement('canvas');
        const size = this.getCompressedImageSize(bitmap.width, bitmap.height);
        canvas.width = size.width;
        canvas.height = size.height;

        const context = canvas.getContext('2d');
        if (!context) {
          return null;
        }

        context.drawImage(bitmap, 0, 0, size.width, size.height);

        const blob = await this.canvasToBlob(
          canvas,
          DocumentRecognition.COMPRESSED_IMAGE_TYPE,
          DocumentRecognition.COMPRESSED_IMAGE_QUALITY
        );
        if (!blob) {
          return null;
        }

        return new File([blob], this.toJpegFileName(file.name), {
          lastModified: file.lastModified,
          type: DocumentRecognition.COMPRESSED_IMAGE_TYPE,
        });
      } finally {
        bitmap.close();
      }
    } catch {
      return null;
    }
  }

  private getCompressedImageSize(width: number, height: number): { width: number; height: number } {
    const maxSide = Math.max(width, height);
    if (maxSide <= DocumentRecognition.COMPRESSED_IMAGE_MAX_SIDE_PX) {
      return { width, height };
    }

    const scale = DocumentRecognition.COMPRESSED_IMAGE_MAX_SIDE_PX / maxSide;
    return {
      width: Math.max(1, Math.round(width * scale)),
      height: Math.max(1, Math.round(height * scale)),
    };
  }

  private canvasToBlob(
    canvas: HTMLCanvasElement,
    type: string,
    quality: number
  ): Promise<Blob | null> {
    if (typeof canvas.toBlob !== 'function') {
      return Promise.resolve(null);
    }

    return new Promise(resolve => {
      canvas.toBlob(resolve, type, quality);
    });
  }

  private inferMimeType(file: File): string | null {
    if (DocumentRecognition.ALLOWED_MIME_TYPES.has(file.type)) {
      return file.type;
    }

    if (file.type) {
      return null;
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

  private toJpegFileName(fileName: string): string {
    const dotIndex = fileName.lastIndexOf('.');
    const baseName = dotIndex > 0 ? fileName.slice(0, dotIndex) : fileName || 'documento';

    return baseName + '.jpg';
  }
}
