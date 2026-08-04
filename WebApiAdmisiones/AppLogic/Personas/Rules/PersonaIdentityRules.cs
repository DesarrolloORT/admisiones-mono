using AppLogic.Personas.Dtos;
using AppLogic.Common.Validation;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Personas.Rules;

public static class PersonaIdentityRules
{
    public static bool TieneIdentidadRestringida(Persona persona, bool tieneInscripcionActiva)
    {
        var funcionarioActivo = DocumentUtils.EsSi(persona.FuncionarioActivoPersona);
        var usoExclusivoDba = DocumentUtils.EsSi(persona.UsoexclusivodbaPersona);
        if (funcionarioActivo || usoExclusivoDba)
        {
            return true;
        }

        return DocumentUtils.EsSi(persona.AlumnoExtranjeroPersona)
            || tieneInscripcionActiva;
    }

    public static OperationResult<bool> ValidarCambiosIdentidad(
        Persona persona,
        DtoActualizarDatosPersonaRequest request,
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
        DtoActualizarDatosPersonaRequest request,
        bool identidadRestringida)
    {
        if (identidadRestringida)
        {
            return;
        }

        if (request.TipoDocumento is not null)
        {
            persona.TipoDocumento = DocumentUtils.NormalizarMayusculas(request.TipoDocumento);
        }

        if (request.Documento is not null)
        {
            persona.Documento = DocumentUtils.Normalizar(request.Documento);
        }

        if (request.PrimerNombre is not null)
        {
            persona.PrimerNombre = DocumentUtils.FormatearTextoCapitalizado(request.PrimerNombre);
            persona.PrimerNombreMay = persona.PrimerNombre.ToUpperInvariant();
        }

        if (request.SegundoNombre is not null)
        {
            persona.SegundoNombre = DocumentUtils.FormatearTextoCapitalizadoOpcional(request.SegundoNombre);
            persona.SegundoNombreMay = persona.SegundoNombre?.ToUpperInvariant();
        }

        if (request.PrimerApellido is not null)
        {
            persona.PrimerApellido = DocumentUtils.FormatearTextoCapitalizado(request.PrimerApellido);
            persona.PrimerApellidoMay = persona.PrimerApellido.ToUpperInvariant();
        }

        if (request.SegundoApellido is not null)
        {
            persona.SegundoApellido = DocumentUtils.FormatearTextoCapitalizadoOpcional(request.SegundoApellido);
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
                : DocumentUtils.NormalizarMayusculas(request.Sexo);
        }
    }

    private static bool CambioTexto(string? valorNuevo, string? valorActual)
    {
        return valorNuevo is not null
            && !string.Equals(DocumentUtils.Normalizar(valorNuevo), DocumentUtils.Normalizar(valorActual), StringComparison.OrdinalIgnoreCase);
    }

    private static bool CambioFecha(DateTime? valorNuevo, DateTime? valorActual)
    {
        return valorNuevo.HasValue
            && (!valorActual.HasValue || valorNuevo.Value.Date != valorActual.Value.Date);
    }

}
