// -----------------------------------------------------------------------------
// AUTO-GENERATED FILE.
// Do not edit manually.
// Run: npm run update-endpoints
// -----------------------------------------------------------------------------

import { defineEndpoint } from '../../core/api-endpoint';
import type { AuthRequest } from '../../../api-models/model/authRequest';
import type { DtoAuthenticationResponseOperationResult } from '../../../api-models/model/dtoAuthenticationResponseOperationResult';
import type { DtoCambiarPasswordRequest } from '../../../api-models/model/dtoCambiarPasswordRequest';
import type { DtoRecuperarPasswordRequest } from '../../../api-models/model/dtoRecuperarPasswordRequest';
import type { ObjectOperationResult } from '../../../api-models/model/objectOperationResult';
import type { StringOperationResult } from '../../../api-models/model/stringOperationResult';

/**
 * Cambia la contraseña del usuario autenticado.
 *
 * Request: Password actual y nueva password.
 *
 * Response 200: Contraseña actualizada correctamente.
 * Response 400: Error de validacion o de negocio.
 * Response 401: Usuario no autenticado.
 * Response 500: Error interno no controlado.
 *
 * Backend: POST /Auth/CambiarContraseña
 * OperationId: POST /Auth/CambiarContraseña
 */
export const postAuthCambiarContrasenaEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: DtoCambiarPasswordRequest;
  response: ObjectOperationResult;
}>({
  operationId: 'POST /Auth/CambiarContraseña',
  method: 'POST',
  path: '/Auth/CambiarContraseña',
});

/**
 * Autentica un usuario mediante LDAP.
Los tokens se devuelven como cookies HttpOnly seguras, no en el body de la respuesta.
 *
 * Request: Datos de autenticación del usuario.
 *
 * Response 200: Autenticación exitosa. Las cookies X-Access-Token y X-Refresh-Token han sido establecidas.
 * Response 400: Error en los datos de entrada.
 * Response 401: Credenciales inválidas.
 * Response 404: Not Found
 *
 * Backend: POST /Auth/Login
 * OperationId: POST /Auth/Login
 */
export const postAuthLoginEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: AuthRequest;
  response: DtoAuthenticationResponseOperationResult;
}>({
  operationId: 'POST /Auth/Login',
  method: 'POST',
  path: '/Auth/Login',
});

/**
 * Cierra la sesión del usuario eliminando las cookies de autenticación.
 *
 * Response 200: Logout exitoso.
 *
 * Backend: POST /Auth/Logout
 * OperationId: POST /Auth/Logout
 */
export const postAuthLogoutEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: never;
  response: StringOperationResult;
}>({
  operationId: 'POST /Auth/Logout',
  method: 'POST',
  path: '/Auth/Logout',
});

/**
 * Recuperar contraseña
 *
 * Request: Datos de la persona a recuperar.
 *
 * Response 200: Recuperacion exitosa.
 * Response 400: Error de validacion o de negocio.
 * Response 500: Error interno no controlado.
 *
 * Backend: POST /Auth/RecuperarContraseña
 * OperationId: POST /Auth/RecuperarContraseña
 */
export const postAuthRecuperarContrasenaEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: DtoRecuperarPasswordRequest;
  response: ObjectOperationResult;
}>({
  operationId: 'POST /Auth/RecuperarContraseña',
  method: 'POST',
  path: '/Auth/RecuperarContraseña',
});

/**
 * Renueva el access token usando el refresh token almacenado en cookies.
Este endpoint valida el refresh token contra la base de datos y genera nuevos tokens.
 *
 * Response 200: Tokens renovados correctamente. Las cookies se actualizan automáticamente.
 * Response 401: Refresh token inválido, expirado o no encontrado.
 * Response 404: Usuario no encontrado en la base de datos.
 *
 * Backend: POST /Auth/RefreshToken
 * OperationId: POST /Auth/RefreshToken
 */
export const postAuthRefreshTokenEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: never;
  response: DtoAuthenticationResponseOperationResult;
}>({
  operationId: 'POST /Auth/RefreshToken',
  method: 'POST',
  path: '/Auth/RefreshToken',
});
