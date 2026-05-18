// -----------------------------------------------------------------------------
// AUTO-GENERATED FILE.
// Do not edit manually.
// Run: npm run update-endpoints
// -----------------------------------------------------------------------------

import { defineEndpoint } from '../../core/api-endpoint';
import type { OfertasInscripcionResponseOperationResult } from '../../../api-models/model/ofertasInscripcionResponseOperationResult';

/**
 * Response 200: OK
 * Response 400: Bad Request
 * Response 401: Unauthorized
 * Response 422: Unprocessable Content
 *
 * Backend: GET /EjemploOfertas/Ofertas
 * OperationId: GET /EjemploOfertas/Ofertas
 */
export const getEjemploOfertasOfertasEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: {
    idProducto?: number;
    idComienzo?: number;
    idTurno?: number;
  };
  request: never;
  response: OfertasInscripcionResponseOperationResult;
}>({
  operationId: 'GET /EjemploOfertas/Ofertas',
  method: 'GET',
  path: '/EjemploOfertas/Ofertas',
});
