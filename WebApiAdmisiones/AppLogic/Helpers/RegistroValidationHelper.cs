using System;
using System.Linq;
using AppLogic.DTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Helpers
{
    public static class RegistroValidationHelper
    {
        private static readonly string[] TiposDocumentoPermitidos = ["CI", "DE", "PS", "CC"];

        public static OperationResult<bool> ValidarDocumentoBase(string tipoDocumentoRaw, string documentoRaw, string method)
        {
            var tipoDocumento = RegistroNormalizationHelper.Normalizar(tipoDocumentoRaw);
            var documento = RegistroNormalizationHelper.Normalizar(documentoRaw);

            if (!TiposDocumentoPermitidos.Contains(tipoDocumento))
            {
                return OperationResult<bool>.IsFailed("REG_DOC_01", method, "Tipo de documento inválido.", 400, false);
            }

            if (tipoDocumento == "CI")
            {
                var mensaje = Util.ValidoCI(documento);
                if (!string.IsNullOrWhiteSpace(mensaje))
                {
                    return OperationResult<bool>.IsFailed("REG_DOC_02", method, mensaje, 400, false);
                }
            }

            return OperationResult<bool>.Ok(true, method);
        }

        public static OperationResult<object?> ValidarProductoYProceso(
            IUnitOfWork uow,
            long idProducto,
            long idProceso,
            string method)
        {
            if (!uow.Productos.EsProductoValidoParaInteres(idProducto))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PRODUCTO_01",
                    method,
                    "El producto indicado es inválido.",
                    400);
            }

            if (!uow.Procesos.TieneProcesoHabilitadoPorProducto(idProducto, idProceso))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PROCESO_01",
                    method,
                    "Proceso no habilitado para el producto seleccionado.",
                    400);
            }

            return OperationResult<object?>.Ok(default, method);
        }

        public static OperationResult<object?> ValidarVerificacionPersonaExistente(
            Persona persona,
            RegistroVerificarIdentidadRequest request,
            string method)
        {
            var apellidoEntrada = RegistroNormalizationHelper.NormalizarMayusculas(request.PrimerApellido);
            var apellidoPersona = !string.IsNullOrWhiteSpace(persona.PrimerApellidoMay)
                ? RegistroNormalizationHelper.Normalizar(persona.PrimerApellidoMay)
                : RegistroNormalizationHelper.NormalizarMayusculas(persona.PrimerApellido);

            if (RegistroNormalizationHelper.Normalizar(persona.TipoDocumento) != RegistroNormalizationHelper.Normalizar(request.TipoDocumento)
                || RegistroNormalizationHelper.Normalizar(persona.Documento) != RegistroNormalizationHelper.Normalizar(request.Documento)
                || apellidoPersona != apellidoEntrada
                || !string.Equals(
                    RegistroNormalizationHelper.Normalizar(persona.Email),
                    RegistroNormalizationHelper.Normalizar(request.Mail),
                    StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_VERIF_01",
                    method,
                    "No podemos completar el registro ya que hemos encontrado inconsistencias en los datos ingresados.",
                    400);
            }

            return OperationResult<object?>.Ok(default, method);
        }

    }
}
