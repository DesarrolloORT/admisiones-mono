// -----------------------------------------------------------------------------
// AUTO-GENERATED FILE.
// Do not edit manually.
// Run: npm run update-endpoints
// -----------------------------------------------------------------------------

import { defineEndpoint } from '../../core/api-endpoint';
import type { DtoAcaTipoDocumentoDevartIEnumerableOperationResult } from '../../../api-models/model/dtoAcaTipoDocumentoDevartIEnumerableOperationResult';
import type { DtoCarreraResponseIEnumerableOperationResult } from '../../../api-models/model/dtoCarreraResponseIEnumerableOperationResult';
import type { DtoComienzoResponseIEnumerableOperationResult } from '../../../api-models/model/dtoComienzoResponseIEnumerableOperationResult';
import type { DtoPaisDevartIEnumerableOperationResult } from '../../../api-models/model/dtoPaisDevartIEnumerableOperationResult';

/**
 * Response 200: OK
 * Response 400: Bad Request
 *
 * Backend: GET /Catalogos/Carreras
 * OperationId: GET /Catalogos/Carreras
 */
export const getCatalogosCarrerasEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: never;
  response: DtoCarreraResponseIEnumerableOperationResult;
}>({
  operationId: 'GET /Catalogos/Carreras',
  method: 'GET',
  path: '/Catalogos/Carreras',
});

/**
 * Response 200: OK
 * Response 400: Bad Request
 *
 * Backend: GET /Catalogos/Comienzos
 * OperationId: GET /Catalogos/Comienzos
 */
export const getCatalogosComienzosEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: {
    idCarrera?: number;
  };
  request: never;
  response: DtoComienzoResponseIEnumerableOperationResult;
}>({
  operationId: 'GET /Catalogos/Comienzos',
  method: 'GET',
  path: '/Catalogos/Comienzos',
});

/**
 * Obtiene el país y sus ciudades asociadas.
 *
 * Response 200: Datos obtenidos correctamente.
 * Response 400: Solicitud inválida.
 * Response 404: País no encontrado.
 *
 * Backend: GET /Catalogos/PaisesEstadosCiudades
 * OperationId: GET /Catalogos/PaisesEstadosCiudades
 */
export const getCatalogosPaisesEstadosCiudadesEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: never;
  response: DtoPaisDevartIEnumerableOperationResult;
}>({
  operationId: 'GET /Catalogos/PaisesEstadosCiudades',
  method: 'GET',
  path: '/Catalogos/PaisesEstadosCiudades',
});

/**
 * Response 200: OK
 * Response 400: Bad Request
 *
 * Backend: GET /Catalogos/TiposDocumentos
 * OperationId: GET /Catalogos/TiposDocumentos
 */
export const getCatalogosTiposDocumentosEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: never;
  response: DtoAcaTipoDocumentoDevartIEnumerableOperationResult;
}>({
  operationId: 'GET /Catalogos/TiposDocumentos',
  method: 'GET',
  path: '/Catalogos/TiposDocumentos',
});
