using System.Globalization;
using AppLogic.Requests;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Helpers
{
    public static class PersonaIdentityHelper
    {
        public static bool TieneIdentidadRestringida(Persona persona, IUnitOfWork uow)
        {
            var funcionarioActivo = EsSi(persona.FuncionarioActivoPersona);
            var usoExclusivoDba = EsSi(persona.UsoexclusivodbaPersona);
            if (funcionarioActivo || usoExclusivoDba)
            {
                return true;
            }

            return EsSi(persona.AlumnoExtranjeroPersona)
                || uow.Inscriptos.TieneInscripcionActiva(persona.CodigoPersona);
        }

        public static OperationResult<bool> ValidarCambiosIdentidad(
            Persona persona,
            ActualizarDatosPersonaRequest request,
            bool identidadRestringida,
            string callingMethod)
        {
            if (!identidadRestringida)
            {
                return OperationResult<bool>.Ok(true, callingMethod);
            }

            if (CambioTexto(request.TipoDocumento, persona.TipoDocumento)
                || CambioTexto(request.Documento, persona.Documento)
                || CambioTexto(request.PrimerNombre, persona.PrimerNombre)
                || CambioTexto(request.SegundoNombre, persona.SegundoNombre)
                || CambioTexto(request.PrimerApellido, persona.PrimerApellido)
                || CambioTexto(request.SegundoApellido, persona.SegundoApellido)
                || CambioFecha(request.FechaNacimiento, persona.FechaNacimiento)
                || CambioTexto(request.Sexo, persona.Sexo))
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_06",
                    callingMethod,
                    "No se pueden modificar datos de identidad para esta persona.",
                    400);
            }

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        public static void AplicarCambiosIdentidad(
            Persona persona,
            ActualizarDatosPersonaRequest request,
            bool identidadRestringida)
        {
            if (identidadRestringida)
            {
                return;
            }

            if (request.TipoDocumento is not null)
            {
                persona.TipoDocumento = request.TipoDocumento.Trim().ToUpperInvariant();
            }

            if (request.Documento is not null)
            {
                persona.Documento = request.Documento.Trim();
            }

            if (request.PrimerNombre is not null)
            {
                persona.PrimerNombre = FormatearTextoCapitalizado(request.PrimerNombre);
                persona.PrimerNombreMay = persona.PrimerNombre.ToUpperInvariant();
            }

            if (request.SegundoNombre is not null)
            {
                persona.SegundoNombre = FormatearTextoCapitalizadoNullable(request.SegundoNombre);
                persona.SegundoNombreMay = persona.SegundoNombre?.ToUpperInvariant();
            }

            if (request.PrimerApellido is not null)
            {
                persona.PrimerApellido = FormatearTextoCapitalizado(request.PrimerApellido);
                persona.PrimerApellidoMay = persona.PrimerApellido.ToUpperInvariant();
            }

            if (request.SegundoApellido is not null)
            {
                persona.SegundoApellido = FormatearTextoCapitalizadoNullable(request.SegundoApellido);
                persona.SegundoApellidoMay = persona.SegundoApellido?.ToUpperInvariant();
            }

            if (request.FechaNacimiento.HasValue)
            {
                persona.FechaNacimiento = request.FechaNacimiento.Value.Date;
            }

            if (request.Sexo is not null)
            {
                persona.Sexo = string.IsNullOrWhiteSpace(request.Sexo)
                    ? null
                    : request.Sexo.Trim().ToUpperInvariant();
            }
        }

        private static bool EsSi(string? valor)
        {
            return string.Equals(valor?.Trim(), "SI", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CambioTexto(string? valorNuevo, string? valorActual)
        {
            return valorNuevo is not null
                && !string.Equals(valorNuevo.Trim(), valorActual?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static bool CambioFecha(DateTime? valorNuevo, DateTime? valorActual)
        {
            return valorNuevo.HasValue
                && (!valorActual.HasValue || valorNuevo.Value.Date != valorActual.Value.Date);
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

        private static string? FormatearTextoCapitalizadoNullable(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : FormatearTextoCapitalizado(valor);
        }
    }
}
