import { ImageCompressionUtils } from '@desarrolloort/ngx-utils';

/**
 * Utilidades compartidas para subir imágenes: tipos permitidos y compresión. Única
 * fuente de verdad para que todas las subidas (identidad, OCR de registro, etc.)
 * acepten exactamente jpg/png/jpeg y compriman con la misma configuración.
 */

export const ACCEPTED_IMAGE_MIME_TYPES = ['image/jpeg', 'image/png'] as const;

export type AcceptedImageMimeType = (typeof ACCEPTED_IMAGE_MIME_TYPES)[number];

export const MAX_IMAGE_SIZE_BYTES = 10 * 1024 * 1024;
export const IMAGE_COMPRESSION_THRESHOLD_BYTES = 5 * 1024 * 1024;

const IMAGE_MAX_SIDE_PX = 2000;
const IMAGE_QUALITY = 0.82;

const MIME_BY_EXTENSION: Record<string, AcceptedImageMimeType> = {
  jpg: 'image/jpeg',
  jpeg: 'image/jpeg',
  png: 'image/png',
};

/**
 * Tipo permitido del archivo: usa `file.type` si es jpg/png; si no viene tipo, lo
 * infiere por extensión. Devuelve `null` para cualquier otra cosa (pdf, svg, etc.).
 */
export function resolveImageMimeType(file: File): AcceptedImageMimeType | null {
  if (isAcceptedImageMimeType(file.type)) return file.type;
  if (file.type) return null;

  const extension = getFileExtension(file.name);
  return extension ? (MIME_BY_EXTENSION[extension] ?? null) : null;
}

export function imageExtensionForMime(mime: string): 'jpg' | 'png' {
  return mime === 'image/png' ? 'png' : 'jpg';
}

/**
 * Comprime solo si el archivo supera el umbral, preservando el formato original
 * (`outputType: mime`). Bajo el umbral devuelve el mismo archivo sin re-encodear.
 */
export function compressImageIfNeeded(file: File, mime: AcceptedImageMimeType): Promise<File> {
  if (file.size <= IMAGE_COMPRESSION_THRESHOLD_BYTES) return Promise.resolve(file);

  return ImageCompressionUtils.compressFile(file, {
    inputType: mime,
    outputType: mime,
    maxWidth: IMAGE_MAX_SIDE_PX,
    maxHeight: IMAGE_MAX_SIDE_PX,
    quality: IMAGE_QUALITY,
    preserveOriginalWhenSmaller: true,
  });
}

function isAcceptedImageMimeType(value: string): value is AcceptedImageMimeType {
  return (ACCEPTED_IMAGE_MIME_TYPES as readonly string[]).includes(value);
}

function getFileExtension(fileName: string): string | null {
  const dotIndex = fileName.lastIndexOf('.');
  if (dotIndex === -1 || dotIndex === fileName.length - 1) return null;
  return fileName.slice(dotIndex + 1).toLowerCase();
}
