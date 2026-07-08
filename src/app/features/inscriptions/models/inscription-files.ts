import type {
  InscripcionIdentityDocumentFile,
  InscripcionIdentityUploadFile,
} from './inscription-flow';

/**
 * Conversión de archivos de identidad entre el formato del backend
 * (base64/data-url) y `File` del navegador. Funciones puras sin Angular.
 */

export async function toIdentityUploadFile(file: File): Promise<InscripcionIdentityUploadFile> {
  const mimeType = file.type || inferUploadMimeType(file.name);
  if (mimeType !== 'image/jpeg' && mimeType !== 'image/png') {
    throw new Error('Invalid identity image type.');
  }

  return {
    nombreArchivo: safeFileName(file.name, 'identidad.jpg'),
    archivo: await readFileAsBase64(file),
  };
}

export function toIdentityFile(
  file: InscripcionIdentityDocumentFile | null | undefined,
  fallbackName: string
): File | null {
  const rawContent = file?.archivo?.trim();
  if (!rawContent) return null;

  const name = safeFileName(file?.nombreArchivo, fallbackName);
  const parsed = parseBase64(rawContent, name);
  return parsed ? new File([parsed.bytes], name, { type: parsed.mimeType }) : null;
}

export function toBlobFile(blob: Blob | null, fallbackName: string): File | null {
  if (!(blob instanceof Blob) || blob.size === 0) return null;

  return new File([blob], fallbackName, { type: blob.type || 'image/jpeg' });
}

function inferUploadMimeType(fileName: string): string | null {
  const extension = fileName.split('.').at(-1)?.toLowerCase();
  if (extension === 'png') return 'image/png';
  if (extension === 'jpg' || extension === 'jpeg') return 'image/jpeg';
  return null;
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
