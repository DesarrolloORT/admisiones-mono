using AppLogic.Dtos.Registro;
using System;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Helpers.ValidationHelpers
{
    public static class RegistroValidationHelper
    {
        public static OperationResult<object?> ValidarVerificacionPersonaExistente(
            Persona persona,
            DtoRegistroVerificarIdentidadRequest request,
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
