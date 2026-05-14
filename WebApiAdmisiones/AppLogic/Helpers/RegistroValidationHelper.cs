using System;
using System.Linq;
using AppLogic.DTOs;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Helpers
{
    public static class RegistroValidationHelper
    {
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
            var apellidoEntrada = DocumentUtils.NormalizarMayusculas(request.PrimerApellido);
            var apellidoPersona = !string.IsNullOrWhiteSpace(persona.PrimerApellidoMay)
                ? DocumentUtils.Normalizar(persona.PrimerApellidoMay)
                : DocumentUtils.NormalizarMayusculas(persona.PrimerApellido);

            if (DocumentUtils.Normalizar(persona.TipoDocumento) != DocumentUtils.Normalizar(request.TipoDocumento)
                || DocumentUtils.Normalizar(persona.Documento) != DocumentUtils.Normalizar(request.Documento)
                || apellidoPersona != apellidoEntrada
                || !string.Equals(
                    DocumentUtils.Normalizar(persona.Email),
                    DocumentUtils.Normalizar(request.Mail),
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
