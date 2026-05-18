// -----------------------------------------------------------------------------
// AUTO-GENERATED FILE.
// Do not edit manually.
// Run: npm run update-endpoints
// -----------------------------------------------------------------------------

import { defineEndpoint } from '../../core/api-endpoint';
import type { ObjectOperationResult } from '../../../api-models/model/objectOperationResult';
import type { ReconocimientoDocumentoApiRequest } from '../../../api-models/model/reconocimientoDocumentoApiRequest';
import type { ReconocimientoDocumentoResponseOperationResult } from '../../../api-models/model/reconocimientoDocumentoResponseOperationResult';
import type { RegistroConfirmarNuevaPersonaRequest } from '../../../api-models/model/registroConfirmarNuevaPersonaRequest';
import type { RegistroConfirmarPersonaExistenteRequest } from '../../../api-models/model/registroConfirmarPersonaExistenteRequest';
import type { RegistroConfirmarSolicitudAltaRequest } from '../../../api-models/model/registroConfirmarSolicitudAltaRequest';
import type { RegistroEvaluacionResponseOperationResult } from '../../../api-models/model/registroEvaluacionResponseOperationResult';
import type { RegistroEvaluarDocumentoRequest } from '../../../api-models/model/registroEvaluarDocumentoRequest';
import type { RegistroVerificarIdentidadRequest } from '../../../api-models/model/registroVerificarIdentidadRequest';

/**
 * Analiza un documento de identidad adjunto y devuelve los datos extraídos.
 * Utiliza Azure Document Intelligence para detectar si el documento es una cédula uruguaya,
 * pasaporte o documento extranjero admitido. Acepta imagen (JPG, PNG) o PDF.
 *
 * Request: Archivo adjunto (imagen o PDF) junto con su tipo MIME.
 *
 * Response 200: Documento analizado y datos extraídos correctamente.
 * Response 400: No se recibió el archivo adjunto o los datos son inválidos.
 * Response 422: El documento no pudo ser procesado o no es de un tipo admitido.
 * Response 500: Error interno al procesar el documento.
 * Response 502: Error en la comunicación con Azure Document Intelligence.
 * Response 504: Tiempo de espera agotado al consultar Azure Document Intelligence.
 *
 * Backend: POST /Registro/AnalizarAdjunto
 * OperationId: POST /Registro/AnalizarAdjunto
 */
export const postRegistroAnalizarAdjuntoEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: ReconocimientoDocumentoApiRequest;
  response: ReconocimientoDocumentoResponseOperationResult;
}>({
  operationId: 'POST /Registro/AnalizarAdjunto',
  method: 'POST',
  path: '/Registro/AnalizarAdjunto',
});

/**
 * Confirma el registro de una nueva persona en el sistema.
 * Crea el usuario utilizando los datos extraídos y verificados del documento de identidad.
 *
 * Request: Datos de la nueva persona a registrar.
 *
 * Response 200: Nueva persona registrada correctamente.
 * Response 400: Error en los datos de entrada o conflicto con datos existentes.
 *
 * Backend: POST /Registro/ConfirmarNuevaPersona
 * OperationId: POST /Registro/ConfirmarNuevaPersona
 */
export const postRegistroConfirmarNuevaPersonaEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: RegistroConfirmarNuevaPersonaRequest;
  response: ObjectOperationResult;
}>({
  operationId: 'POST /Registro/ConfirmarNuevaPersona',
  method: 'POST',
  path: '/Registro/ConfirmarNuevaPersona',
});

/**
 * Confirma el registro de una persona ya existente en el sistema.
 * Asocia las credenciales de acceso a la persona identificada previamente en el flujo de registro.
 *
 * Request: Datos de confirmación de la persona existente.
 *
 * Response 200: Persona existente confirmada correctamente.
 * Response 400: Error en los datos de entrada o persona no encontrada.
 *
 * Backend: POST /Registro/ConfirmarPersonaExistente
 * OperationId: POST /Registro/ConfirmarPersonaExistente
 */
export const postRegistroConfirmarPersonaExistenteEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: RegistroConfirmarPersonaExistenteRequest;
  response: ObjectOperationResult;
}>({
  operationId: 'POST /Registro/ConfirmarPersonaExistente',
  method: 'POST',
  path: '/Registro/ConfirmarPersonaExistente',
});

/**
 * Registra una solicitud de alta manual para una persona.
 * Utilizada cuando la persona no puede completar el proceso de registro automático.
 * La solicitud queda pendiente de revisión por parte del equipo de admisiones.
 *
 * Request: Datos de la solicitud de alta a registrar.
 *
 * Response 200: Solicitud de alta registrada correctamente.
 * Response 400: Error en los datos de entrada.
 *
 * Backend: POST /Registro/ConfirmarSolicitudAlta
 * OperationId: POST /Registro/ConfirmarSolicitudAlta
 */
export const postRegistroConfirmarSolicitudAltaEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: RegistroConfirmarSolicitudAltaRequest;
  response: ObjectOperationResult;
}>({
  operationId: 'POST /Registro/ConfirmarSolicitudAlta',
  method: 'POST',
  path: '/Registro/ConfirmarSolicitudAlta',
});

/**
 * Evalúa los datos de un documento de identidad.
 * Procesa el documento recibido y devuelve el resultado del análisis con los datos detectados.
 *
 * Request: Datos del documento a evaluar.
 *
 * Response 200: Documento evaluado correctamente.
 * Response 400: Error en los datos de entrada o documento no válido.
 *
 * Backend: POST /Registro/EvaluarDocumento
 * OperationId: POST /Registro/EvaluarDocumento
 */
export const postRegistroEvaluarDocumentoEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: RegistroEvaluarDocumentoRequest;
  response: RegistroEvaluacionResponseOperationResult;
}>({
  operationId: 'POST /Registro/EvaluarDocumento',
  method: 'POST',
  path: '/Registro/EvaluarDocumento',
});

/**
 * Verifica la identidad de una persona.
 * Compara los datos del documento presentado con la información registrada en el sistema.
 *
 * Request: Datos de identidad a verificar.
 *
 * Response 200: Identidad verificada correctamente.
 * Response 400: Error en los datos de entrada o verificación fallida.
 *
 * Backend: POST /Registro/VerificarIdentidad
 * OperationId: POST /Registro/VerificarIdentidad
 */
export const postRegistroVerificarIdentidadEndpoint = defineEndpoint<{
  pathParams: never;
  queryParams: never;
  request: RegistroVerificarIdentidadRequest;
  response: ObjectOperationResult;
}>({
  operationId: 'POST /Registro/VerificarIdentidad',
  method: 'POST',
  path: '/Registro/VerificarIdentidad',
});
