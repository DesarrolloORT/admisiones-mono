import {
  compressImageIfNeeded,
  imageExtensionForMime,
  resolveImageMimeType,
} from 'src/app/shared/files/image-upload';

import type {
  InscripcionIdentityDocumentFile,
  InscripcionIdentityUploadFile,
} from './inscription-flow';

/**
 * Conversión de archivos de identidad entre el formato del backend
 * (base64/data-url) y `File` del navegador. Funciones puras sin Angular.
 */

export async function toIdentityUploadFile(file: File): Promise<InscripcionIdentityUploadFile> {
  const mimeType = resolveImageMimeType(file);
  if (!mimeType) {
    throw new Error('Invalid identity image type.');
  }

  const prepared = await compressImageIfNeeded(file, mimeType);
  return {
    nombreArchivo: uploadFileName(prepared, mimeType),
    archivo: await readFileAsBase64(prepared),
  };
}

export function toIdentityFile(
  file: InscripcionIdentityDocumentFile | null | undefined,
  baseName: string
): File | null {
  const rawContent = file?.archivo?.trim();
  if (!rawContent) return null;

  const providedName = safeFileName(file?.nombreArchivo, '');
  const parsed = parseBase64(rawContent, providedName || baseName);
  if (!parsed) return null;

  const name = providedName || `${baseName}.${imageExtensionForMime(parsed.mimeType)}`;
  return new File([parsed.bytes], name, { type: parsed.mimeType });
}

export function toBlobFile(blob: Blob | null, baseName: string): File | null {
  if (!(blob instanceof Blob) || blob.size === 0) return null;

  const mimeType = blob.type || 'image/jpeg';
  return new File([blob], `${baseName}.${imageExtensionForMime(mimeType)}`, { type: mimeType });
}

/** Nombre para subir: conserva el nombre base y ajusta la extensión al tipo real. */
function uploadFileName(file: File, mimeType: string): string {
  const extension = imageExtensionForMime(file.type || mimeType);
  const name = safeFileName(file.name, '');
  const dotIndex = name.lastIndexOf('.');
  const stem = dotIndex > 0 ? name.slice(0, dotIndex) : name;
  return `${stem || 'imagen'}.${extension}`;
}

function readFileAsBase64(file: File): Promise<string> {
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

function parseBase64(
  value: string,
  fileName: string
): { bytes: ArrayBuffer; mimeType: string } | null {
  const dataUrlMatch = /^data:([^;,]+);base64,(.*)$/i.exec(value);
  const mimeType = dataUrlMatch?.[1] ?? inferMimeType(fileName);
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

function safeFileName(value: string | null | undefined, fallback: string): string {
  const name = value?.split(/[\\/]/).at(-1)?.trim();
  return name || fallback;
}

function inferMimeType(fileName: string): string {
  const extension = fileName.split('.').at(-1)?.toLowerCase();
  if (extension === 'png') return 'image/png';
  if (extension === 'jpg' || extension === 'jpeg') return 'image/jpeg';
  if (extension === 'webp') return 'image/webp';
  return 'image/jpeg';
}
