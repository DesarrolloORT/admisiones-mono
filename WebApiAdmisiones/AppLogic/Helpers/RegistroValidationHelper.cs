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

            if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(documento))
            {
                return OperationResult<bool>.IsFailed("REG_DOC_01", method, "Faltan parámetros obligatorios.", 400, false);
            }

            if (!TiposDocumentoPermitidos.Contains(tipoDocumento))
            {
                return OperationResult<bool>.IsFailed("REG_DOC_02", method, "Tipo de documento inválido.", 400, false);
            }

            if (tipoDocumento == "CI")
            {
                var mensaje = Util.ValidoCI(documento);
                if (!string.IsNullOrWhiteSpace(mensaje))
                {
                    return OperationResult<bool>.IsFailed("REG_DOC_03", method, mensaje, 400, false);
                }
            }

            return OperationResult<bool>.Ok(true, method);
        }

        public static OperationResult<object?> ValidarProductoYProceso(
            IUnitOfWork uow,
            RegistroConfirmarRequest request,
            string method)
        {
            if (request.IdProducto <= 0 || request.IdProceso <= 0)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PRODUCTO_PROCESO_01",
                    method,
                    "Producto y proceso son obligatorios.",
                    400);
            }

            if (!uow.Productos.EsProductoValidoParaInteres(request.IdProducto))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PRODUCTO_01",
                    method,
                    "El producto indicado es inválido.",
                    400);
            }

            if (!uow.Procesos.TieneProcesoHabilitadoPorProducto(request.IdProducto, request.IdProceso))
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
            RegistroConfirmarRequest request,
            string method)
        {
            return ValidarVerificacionPersonaExistente(
                persona,
                request.TipoDocumento,
                request.Documento,
                request.PrimerApellido,
                request.Mail,
                request.VerificacionMail,
                method);
        }

        public static OperationResult<object?> ValidarVerificacionPersonaExistente(
            Persona persona,
            string tipoDocumento,
            string documento,
            string primerApellido,
            string mail,
            string verificacionMail,
            string method)
        {
            var mailValidation = ValidarMail(mail, verificacionMail, method);
            if (!mailValidation.Success)
            {
                return mailValidation;
            }

            if (string.IsNullOrWhiteSpace(primerApellido))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_VERIF_01",
                    method,
                    "El primer apellido es obligatorio.",
                    400);
            }

            var apellidoEntrada = RegistroNormalizationHelper.NormalizarMayusculas(primerApellido);
            var apellidoPersona = !string.IsNullOrWhiteSpace(persona.PrimerApellidoMay)
                ? RegistroNormalizationHelper.Normalizar(persona.PrimerApellidoMay)
                : RegistroNormalizationHelper.NormalizarMayusculas(persona.PrimerApellido);

            if (RegistroNormalizationHelper.Normalizar(persona.TipoDocumento) != RegistroNormalizationHelper.Normalizar(tipoDocumento)
                || RegistroNormalizationHelper.Normalizar(persona.Documento) != RegistroNormalizationHelper.Normalizar(documento)
                || apellidoPersona != apellidoEntrada
                || !string.Equals(
                    RegistroNormalizationHelper.Normalizar(persona.Email),
                    RegistroNormalizationHelper.Normalizar(mail),
                    StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_VERIF_02",
                    method,
                    "No podemos completar el registro ya que hemos encontrado inconsistencias en los datos ingresados.",
                    400);
            }

            return OperationResult<object?>.Ok(default, method);
        }

        public static OperationResult<object?> ValidarDatosPersonaCompleta(
            RegistroConfirmarRequest request,
            bool requiereDireccionYCiudad,
            string method)
        {
            var mailValidation = ValidarMail(request, method);
            if (!mailValidation.Success)
            {
                return mailValidation;
            }

            if (string.IsNullOrWhiteSpace(request.PrimerNombre)
                || string.IsNullOrWhiteSpace(request.PrimerApellido)
                || string.IsNullOrWhiteSpace(request.Sexo)
                || string.IsNullOrWhiteSpace(request.Telefono1))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_01",
                    method,
                    "Faltan parámetros obligatorios.",
                    400);
            }

            if (request.PrimerNombre.Trim().Length <= 1)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_02",
                    method,
                    "Primer nombre inválido.",
                    400);
            }

            if (request.PrimerApellido.Trim().Length <= 1)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_03",
                    method,
                    "Primer apellido inválido.",
                    400);
            }

            if (request.FechaNacimiento.Date <= new DateTime(1900, 1, 1))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_04",
                    method,
                    "Fecha de nacimiento inválida.",
                    400);
            }

            var sexo = RegistroNormalizationHelper.Normalizar(request.Sexo);
            if (sexo != "M" && sexo != "F")
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_05",
                    method,
                    "Sexo inválido.",
                    400);
            }

            if (requiereDireccionYCiudad
                && (string.IsNullOrWhiteSpace(request.Direccion)
                    || request.CodigoPais <= 0
                    || request.CodigoEstado <= 0
                    || request.CodigoCiudad <= 0))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_06",
                    method,
                    "Dirección, país, departamento y ciudad son obligatorios.",
                    400);
            }

            return OperationResult<object?>.Ok(default, method);
        }

        public static OperationResult<object?> ValidarMail(
            RegistroConfirmarRequest request,
            string method)
        {
            return ValidarMail(request.Mail, request.VerificacionMail, method);
        }

        public static OperationResult<object?> ValidarMail(
            string mail,
            string verificacionMail,
            string method)
        {
            if (string.IsNullOrWhiteSpace(mail) || string.IsNullOrWhiteSpace(verificacionMail))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_MAIL_01",
                    method,
                    "El mail y su verificación son obligatorios.",
                    400);
            }

            if (!string.Equals(
                RegistroNormalizationHelper.Normalizar(mail),
                RegistroNormalizationHelper.Normalizar(verificacionMail),
                StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_MAIL_02",
                    method,
                    "El mail y su verificación no coinciden.",
                    400);
            }

            if (!Util.EsCorreoValido(mail))
            {
                return OperationResult<object?>.IsFailed(
                    "REG_MAIL_03",
                    method,
                    "El formato del mail es inválido.",
                    400);
            }

            return OperationResult<object?>.Ok(default, method);
        }

    }
}
