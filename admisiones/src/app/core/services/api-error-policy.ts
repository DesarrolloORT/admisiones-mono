import { ApiErrorPolicy, DEFAULT_API_ERROR_POLICY } from '@desarrolloort/ngx-utils';

const STATUSES_SHOWING_BACKEND_MESSAGE = [400, 401, 403, 404, 409, 422, 429] as const;

/**
 * Criterio unico de la app: el mensaje que ve el usuario es el que manda el
 * backend. La politica default de `ngx-utils` lo descarta en 401/403/404, asi
 * que se reabilita aca; el `defaultMessage` de la libreria queda solo como
 * fallback para respuestas sin `message`.
 *
 * Los 5xx quedan fuera a proposito: el normalizador de la libreria fuerza un
 * texto generico para cualquier status >= 500 y el backend oculta el detalle
 * real en produccion, para no filtrar mensajes crudos de LDAP, Azure o la API
 * de Inscripciones y Pagos.
 */
export const APP_API_ERROR_POLICY: ApiErrorPolicy = Object.fromEntries(
  STATUSES_SHOWING_BACKEND_MESSAGE.map(status => [
    status,
    { ...DEFAULT_API_ERROR_POLICY[status], useBackendMessage: true },
  ])
);
