import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import {
  IMAGE_COMPRESSION_THRESHOLD_BYTES,
  MAX_IMAGE_SIZE_BYTES,
} from 'src/app/shared/files/image-upload';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { DocumentRecognitionFileError } from '../models/document-recognition-error';
import { DocumentRecognition } from './document-recognition';

describe('DocumentRecognition', () => {
  let service: DocumentRecognition;
  let endpointMock: {
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      recognizeDocument: vi.fn().mockReturnValue(of({ fields: { firstName: 'Ana' } })),
    };

    TestBed.configureTestingModule({
      providers: [DocumentRecognition, { provide: AuthEndpoint, useValue: endpointMock }],
    });

    service = TestBed.inject(DocumentRecognition);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('should delegate document recognition payload to the endpoint', () => {
    const payload = {
      mimeType: 'image/jpeg',
      attachment: {
        fileName: 'identity-document.jpg',
        content: 'base64-content',
      },
    };

    service.recognizeDocument(payload).subscribe(response => {
      expect(response.fields?.firstName).toBe('Ana');
    });

    expect(endpointMock.recognizeDocument).toHaveBeenCalledWith(payload);
  });

  it('should propagate errors from the endpoint adapter', () => {
    const requestError = new Error('request failed');
    endpointMock.recognizeDocument.mockReturnValueOnce(throwError(() => requestError));

    service
      .recognizeDocument({
        mimeType: 'image/jpeg',
        attachment: {
          fileName: 'identity-document.jpg',
          content: 'base64-content',
        },
      })
      .subscribe({
        error: error => {
          expect(error).toBe(requestError);
        },
      });
  });

  it('should create a base64 request from a valid image file', async () => {
    const payload = await service.createRequestFromFile(
      new File(['content'], 'identity-document.jpg', { type: 'image/jpeg' })
    );

    expect(payload).toEqual({
      mimeType: 'image/jpeg',
      attachment: {
        fileName: 'identity-document.jpg',
        content: 'Y29udGVudA==',
      },
    });
  });

  it('should compress large image files before creating the request', async () => {
    const close = vi.fn();
    const drawImage = vi.fn();
    const canvas = {
      width: 0,
      height: 0,
      getContext: vi.fn(() => ({ drawImage })),
      toBlob: vi.fn((callback: BlobCallback, type?: string, quality?: number) => {
        expect(type).toBe('image/png');
        expect(quality).toBe(0.82);
        callback(new Blob(['compressed'], { type }));
      }),
    } as unknown as HTMLCanvasElement;
    const createElement = document.createElement.bind(document);

    vi.spyOn(document, 'createElement').mockImplementation(((
      tagName: string,
      options?: ElementCreationOptions
    ) =>
      tagName === 'canvas'
        ? canvas
        : createElement(tagName, options)) as typeof document.createElement);
    const bitmap = { width: 4000, height: 2000, close } as ImageBitmap;
    const createImageBitmapMock = vi.fn().mockResolvedValue(bitmap);
    vi.stubGlobal('createImageBitmap', createImageBitmapMock);

    const file = fileWithSize(
      new File(['original'], 'identity-document.png', { type: 'image/png' }),
      IMAGE_COMPRESSION_THRESHOLD_BYTES + 1
    );

    const payload = await service.createRequestFromFile(file);

    expect(createImageBitmapMock).toHaveBeenCalledWith(file);
    expect(canvas.width).toBe(2000);
    expect(canvas.height).toBe(1000);
    expect(drawImage).toHaveBeenCalledWith(bitmap, 0, 0, 2000, 1000);
    expect(close).toHaveBeenCalledOnce();
    expect(payload).toEqual({
      mimeType: 'image/png',
      attachment: {
        fileName: 'identity-document.png',
        content: 'Y29tcHJlc3NlZA==',
      },
    });
  });

  it('should try to compress files above the max size before rejecting them', async () => {
    const close = vi.fn();
    const drawImage = vi.fn();
    const canvas = {
      width: 0,
      height: 0,
      getContext: vi.fn(() => ({ drawImage })),
      toBlob: vi.fn((callback: BlobCallback, type?: string) => {
        callback(new Blob([new Uint8Array(MAX_IMAGE_SIZE_BYTES + 500)], { type }));
      }),
    } as unknown as HTMLCanvasElement;
    const createElement = document.createElement.bind(document);

    vi.spyOn(document, 'createElement').mockImplementation(((
      tagName: string,
      options?: ElementCreationOptions
    ) =>
      tagName === 'canvas'
        ? canvas
        : createElement(tagName, options)) as typeof document.createElement);
    const bitmap = { width: 4000, height: 2000, close } as ImageBitmap;
    vi.stubGlobal('createImageBitmap', vi.fn().mockResolvedValue(bitmap));

    const file = new File([new Uint8Array(MAX_IMAGE_SIZE_BYTES + 1000)], 'identity-document.jpg', {
      type: 'image/jpeg',
    });

    await expect(service.createRequestFromFile(file)).rejects.toEqual(
      new DocumentRecognitionFileError('maxFileSize')
    );
    expect(createImageBitmap).toHaveBeenCalledWith(file);
  });

  it('should reject PDF files', async () => {
    await expect(
      service.createRequestFromFile(
        new File(['content'], 'identity-document.pdf', { type: 'application/pdf' })
      )
    ).rejects.toEqual(new DocumentRecognitionFileError('invalidMimeType'));
  });

  it('should reject files without a valid MIME type', async () => {
    await expect(service.createRequestFromFile(new File(['content'], 'document'))).rejects.toEqual(
      new DocumentRecognitionFileError('invalidMimeType')
    );
  });

  it('should reject active or unsupported image formats', async () => {
    await expect(
      service.createRequestFromFile(
        new File(['<svg/>'], 'identity-document.svg', { type: 'image/svg+xml' })
      )
    ).rejects.toEqual(new DocumentRecognitionFileError('invalidMimeType'));
  });
});

function fileWithSize(file: File, size: number): File {
  Object.defineProperty(file, 'size', { configurable: true, value: size });
  return file;
}
