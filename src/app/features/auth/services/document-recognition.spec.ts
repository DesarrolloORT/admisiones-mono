import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
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
      recognizeDocument: vi.fn().mockReturnValue(of({ requiereRevision: false })),
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
      tipoMime: 'application/pdf',
      archivoAdjunto: {
        nombreArchivo: 'documento.pdf',
        archivo: 'base64-content',
      },
    };

    service.recognizeDocument(payload).subscribe(response => {
      expect(response.requiereRevision).toBe(false);
    });

    expect(endpointMock.recognizeDocument).toHaveBeenCalledWith(payload);
  });

  it('should propagate errors from the endpoint adapter', () => {
    const requestError = new Error('request failed');
    endpointMock.recognizeDocument.mockReturnValueOnce(throwError(() => requestError));

    service
      .recognizeDocument({
        tipoMime: 'application/pdf',
        archivoAdjunto: {
          nombreArchivo: 'documento.pdf',
          archivo: 'base64-content',
        },
      })
      .subscribe({
        error: error => {
          expect(error).toBe(requestError);
        },
      });
  });

  it('should create a base64 request from a valid file', async () => {
    const payload = await service.createRequestFromFile(
      new File(['content'], 'documento.pdf', { type: 'application/pdf' })
    );

    expect(payload).toEqual({
      tipoMime: 'application/pdf',
      archivoAdjunto: {
        nombreArchivo: 'documento.pdf',
        archivo: 'Y29udGVudA==',
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
        expect(type).toBe('image/jpeg');
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
      new File(['original'], 'cedula.png', { type: 'image/png' }),
      DocumentRecognition.IMAGE_COMPRESSION_THRESHOLD_BYTES + 1
    );

    const payload = await service.createRequestFromFile(file);

    expect(createImageBitmapMock).toHaveBeenCalledWith(file);
    expect(canvas.width).toBe(2000);
    expect(canvas.height).toBe(1000);
    expect(drawImage).toHaveBeenCalledWith(bitmap, 0, 0, 2000, 1000);
    expect(close).toHaveBeenCalledOnce();
    expect(payload).toEqual({
      tipoMime: 'image/jpeg',
      archivoAdjunto: {
        nombreArchivo: 'cedula.jpg',
        archivo: 'Y29tcHJlc3NlZA==',
      },
    });
  });

  it('should reject files bigger than the final max size', async () => {
    const file = fileWithSize(
      new File(['content'], 'documento.pdf', { type: 'application/pdf' }),
      DocumentRecognition.MAX_FILE_SIZE_BYTES + 1
    );

    await expect(service.createRequestFromFile(file)).rejects.toEqual(
      new DocumentRecognitionFileError('maxFileSize')
    );
  });

  it('should reject files without a valid MIME type', async () => {
    await expect(service.createRequestFromFile(new File(['content'], 'documento'))).rejects.toEqual(
      new DocumentRecognitionFileError('invalidMimeType')
    );
  });

  it('should reject active or unsupported image formats', async () => {
    await expect(
      service.createRequestFromFile(
        new File(['<svg/>'], 'documento.svg', { type: 'image/svg+xml' })
      )
    ).rejects.toEqual(new DocumentRecognitionFileError('invalidMimeType'));
  });
});

function fileWithSize(file: File, size: number): File {
  Object.defineProperty(file, 'size', { configurable: true, value: size });
  return file;
}
