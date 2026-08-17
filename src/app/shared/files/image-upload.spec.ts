import { vi } from 'vitest';

import {
  compressImageIfNeeded,
  IMAGE_COMPRESSION_THRESHOLD_BYTES,
  imageExtensionForMime,
  resolveImageMimeType,
} from './image-upload';

describe('image-upload', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  describe('resolveImageMimeType', () => {
    it('accepts jpeg and png by declared type', () => {
      expect(resolveImageMimeType(new File([''], 'a.jpg', { type: 'image/jpeg' }))).toBe(
        'image/jpeg'
      );
      expect(resolveImageMimeType(new File([''], 'a.png', { type: 'image/png' }))).toBe(
        'image/png'
      );
    });

    it('infers the type from the extension when the file has no type', () => {
      expect(resolveImageMimeType(new File([''], 'a.jpeg'))).toBe('image/jpeg');
      expect(resolveImageMimeType(new File([''], 'a.PNG'))).toBe('image/png');
    });

    it('rejects any other type, pdf, svg or missing extension', () => {
      expect(resolveImageMimeType(new File([''], 'a.pdf', { type: 'application/pdf' }))).toBeNull();
      expect(resolveImageMimeType(new File([''], 'a.svg', { type: 'image/svg+xml' }))).toBeNull();
      expect(resolveImageMimeType(new File([''], 'a.webp'))).toBeNull();
      expect(resolveImageMimeType(new File([''], 'document'))).toBeNull();
    });
  });

  describe('imageExtensionForMime', () => {
    it('maps mime to extension defaulting to jpg', () => {
      expect(imageExtensionForMime('image/png')).toBe('png');
      expect(imageExtensionForMime('image/jpeg')).toBe('jpg');
      expect(imageExtensionForMime('anything-else')).toBe('jpg');
    });
  });

  describe('compressImageIfNeeded', () => {
    it('returns the same file when it is below the compression threshold', async () => {
      const file = new File(['small'], 'small.png', { type: 'image/png' });

      await expect(compressImageIfNeeded(file, 'image/png')).resolves.toBe(file);
    });

    it('compresses large files preserving the original format', async () => {
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
      vi.stubGlobal(
        'createImageBitmap',
        vi.fn().mockResolvedValue({ width: 4000, height: 2000, close: vi.fn() } as ImageBitmap)
      );

      const file = fileWithSize(
        new File(['original'], 'big.png', { type: 'image/png' }),
        IMAGE_COMPRESSION_THRESHOLD_BYTES + 1
      );

      const result = await compressImageIfNeeded(file, 'image/png');

      expect(result).not.toBe(file);
      expect(result.type).toBe('image/png');
      expect(result.name).toBe('big.png');
    });
  });
});

function fileWithSize(file: File, size: number): File {
  Object.defineProperty(file, 'size', { configurable: true, value: size });
  return file;
}
