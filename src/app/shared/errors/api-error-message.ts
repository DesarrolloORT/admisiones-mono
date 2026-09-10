import { isNormalizedApiError } from '@desarrolloort/ngx-utils';

/**
 * Mensaje para mostrar al usuario ante un error de API: el mensaje normalizado
 * del backend cuando existe, o el fallback provisto por el punto de uso.
 */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  return isNormalizedApiError(error) ? error.message : fallback;
}
