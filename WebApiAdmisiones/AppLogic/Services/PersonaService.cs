using System.Globalization;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.IServices;
using AppLogic.Requests;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using LdapService.Interfaces;
using Utilities;

namespace AppLogic.Services
{
    public class PersonaService(
        IUnitOfWorkFactory uowFactory,
        ILdap ldap)
        : IPersonaService
    {
        public OperationResult<DtoDatosPersona> ObtenerDatosPersona(long codigoPersona)
        {
            using var uow = uowFactory.Create();
            var persona = uow.Personas.GetPersonaWithRelated(codigoPersona);
            if (persona is null)
            {
                return OperationResult<DtoDatosPersona>.IsFailed(
                    "PER_DAT_01",
                    nameof(ObtenerDatosPersona),
                    "No se encontró la persona autenticada.",
                    404);
            }

            return OperationResult<DtoDatosPersona>.Ok(MapearDatosPersona(persona), nameof(ObtenerDatosPersona));
        }

        public OperationResult<bool> ActualizarDatosPersona(long codigoPersona, ActualizarDatosPersonaRequest request)
        {
            using var uow = uowFactory.Create();
            var persona = uow.Personas.GetPersonaWithRelated(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_01",
                    nameof(ActualizarDatosPersona),
                    "No se encontró la persona autenticada.",
                    404);
            }

            var validacion = ValidarActualizarDatosPersona(request);
            if (!validacion.Success)
            {
                return validacion;
            }

            var ciudad = uow.Ciudads.GetByKey(request.CodigoPais, request.CodigoEstado, request.CodigoCiudad);
            if (ciudad is null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_05",
                    nameof(ActualizarDatosPersona),
                    "No existe la ciudad indicada para el país y estado enviados.",
                    400);
            }

            persona.CodigoPais = request.CodigoPais;
            persona.CodigoEstado = request.CodigoEstado;
            persona.CodigoCiudad = request.CodigoCiudad;
            persona.Direccion = FormatearTextoCapitalizado(request.Direccion);
            persona.Telefono1 = request.Telefono1?.Trim();
            persona.Email = request.Mail?.Trim();

            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Personas.Update(persona);
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(ActualizarDatosPersona));
        }

        public async Task<OperationResult<object>> CambiarPasswordAsync(long codigoPersona, DtoCambiarPasswordRequest request)
        {
            try
            {
                if (request == null)
                {
                    return OperationResult<object>.IsFailed(
                        "CAM_PAS_01",
                        nameof(CambiarPasswordAsync),
                        "La solicitud es obligatoria.",
                        400);
                }

                var validacionPassword = Util.ValidarPassword(request.PasswordActual, request.PasswordNueva);
                if (!string.IsNullOrWhiteSpace(validacionPassword))
                {
                    return OperationResult<object>.IsFailed(
                        "CAM_PAS_02",
                        nameof(CambiarPasswordAsync),
                        validacionPassword,
                        400);
                }

                var cambioPassword = await ldap.CambiarPasswordAsync(
                    codigoPersona.ToString(CultureInfo.InvariantCulture),
                    request.PasswordActual,
                    request.PasswordNueva);

                if (!cambioPassword.Success)
                {
                    return OperationResult<object>.IsFailed(
                        cambioPassword.ErrorCode,
                        nameof(CambiarPasswordAsync),
                        cambioPassword.Message,
                        cambioPassword.HttpCode);
                }

                return OperationResult<object>.Ok(
                    "Se actualizó tu contraseña",
                    nameof(CambiarPasswordAsync));
            }
            catch (Exception ex)
            {
                return OperationResult<object>.IsFailed(
                    "CAM_PAS_99",
                    nameof(CambiarPasswordAsync),
                    $"Error al cambiar contraseña: {ex.Message}",
                    500,
                    default!);
            }
        }

        private static OperationResult<bool> ValidarActualizarDatosPersona(ActualizarDatosPersonaRequest request)
        {
            if (request is null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_02",
                    nameof(ActualizarDatosPersona),
                    "La solicitud es obligatoria.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(request.Direccion)
                || string.IsNullOrWhiteSpace(request.Mail)
                || string.IsNullOrWhiteSpace(request.VerificacionMail))
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_03",
                    nameof(ActualizarDatosPersona),
                    "Faltan parámetros obligatorios.",
                    400);
            }

            if (!string.Equals(request.Mail, request.VerificacionMail, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_04",
                    nameof(ActualizarDatosPersona),
                    "El mail y su verificación no coinciden.",
                    400);
            }

            return OperationResult<bool>.Ok(true, nameof(ActualizarDatosPersona));
        }

        private static DtoDatosPersona MapearDatosPersona(Persona persona)
        {
            var mail = persona.Email ?? string.Empty;
            return new DtoDatosPersona
            {
                TipoDocumento = persona.TipoDocumento?.Trim() ?? string.Empty,
                Documento = persona.Documento?.Trim() ?? string.Empty,
                PrimerNombre = persona.PrimerNombre?.Trim() ?? string.Empty,
                SegundoNombre = persona.SegundoNombre?.Trim() ?? string.Empty,
                PrimerApellido = persona.PrimerApellido?.Trim() ?? string.Empty,
                SegundoApellido = persona.SegundoApellido?.Trim() ?? string.Empty,
                FechaNacimiento = persona.FechaNacimiento ?? default,
                Sexo = persona.Sexo?.Trim() ?? string.Empty,
                CodigoPais = persona.CodigoPais ?? 0,
                CodigoEstado = persona.CodigoEstado ?? 0,
                CodigoCiudad = persona.CodigoCiudad ?? 0,
                Direccion = persona.Direccion?.Trim() ?? string.Empty,
                Telefono1 = persona.Telefono1?.Trim() ?? string.Empty,
                Mail = mail,
                VerificacionMail = mail
            };
        }

        private static string FormatearTextoCapitalizado(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            var texto = string.Join(" ", valor.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(texto.ToLower(CultureInfo.CurrentCulture));
        }
    }
}
