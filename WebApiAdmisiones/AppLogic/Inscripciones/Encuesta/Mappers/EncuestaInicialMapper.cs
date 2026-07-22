using AppLogic.Personas.Constants;
using AppLogic.Inscripciones.Encuesta.Dtos;
using AppLogic.Inscripciones.Encuesta.Rules;
using AppLogic.Inscripciones.Encuesta.Validators;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using System.Security.Cryptography;
using System.Text;
using AppLogic.Common.Constants;

namespace AppLogic.Inscripciones.Encuesta.Mappers;

internal static class EncuestaInicialMapper
{
    private const long CodigoInstitucionExteriorLegacy = 2898;
    private const long CodigoTituloExteriorSextoLegacy = 5;

    internal static void AplicarDatosTecnicos(
        EncuestaIniAdmision encuesta,
        Persona persona,
        long codigoPersona,
        long? idProducto,
        long? idProceso,
        long? idComienzo,
        long? idTurno)
    {
        encuesta.TipoDocumento = persona.TipoDocumento;
        encuesta.Documento = persona.Documento;
        encuesta.CodigoPersona = codigoPersona;
        encuesta.TipoInscripcion = EncuestaInicialState.TipoInscripcionSoloEncuesta;
        encuesta.EstadoEncuestaIniAdmision ??= EncuestaInicialState.EstadoTemporal;
        encuesta.NuevaversionEncuestaIni = CommonConstants.Booleanos.Si;

        if (idProducto.HasValue)
        {
            encuesta.IdProducto = idProducto.Value;
            encuesta.ClaveEncuestaIni = GenerarClaveEncuesta(idProducto.Value, persona.Documento);
        }

        if (idProceso.HasValue)
            encuesta.IdProceso = idProceso.Value;
        if (idComienzo.HasValue)
            encuesta.IdComienzo = idComienzo.Value;
        if (idTurno.HasValue)
            encuesta.IdTurno = idTurno.Value;
    }

    internal static void AplicarEducacion(IUnitOfWork uow, EncuestaIniAdmision encuesta, DtoGuardarEncuestaInicialRequest request)
    {
        if (request.UbicacionUltimoAnioSecundariaId.HasValue)
        {
            encuesta.UltimoanioSecundariaEncuestaIni = (short)request.UbicacionUltimoAnioSecundariaId.Value;
            if (request.UbicacionUltimoAnioSecundariaId.Value == PersonaConstants.Parametros.UruguayCodigoPais)
                encuesta.NombreInstSecEncuestaIni = null;
            else
                encuesta.CodigoInstitucionBac = CodigoInstitucionExteriorLegacy;
        }

        if (request.InstitucionSecundariaId.HasValue)
            encuesta.CodigoInstitucionBac = request.InstitucionSecundariaId.Value;

        if (request.NombreInstitucionSecundaria != null)
            encuesta.NombreInstSecEncuestaIni = EncuestaInicialState.NormalizarTexto(request.NombreInstitucionSecundaria);

        if (request.CursaSecundariaActualmente.HasValue)
            encuesta.CursaSecundariaActualmenteEncuestaIni = EncuestaInicialState.BoolToSN(request.CursaSecundariaActualmente.Value);

        if (request.CursaSecundariaActualmente == false)
        {
            encuesta.AniosInstruccionEncuestaIni = null;
            encuesta.UltimoAnioSextoEncuestaIni = null;
            encuesta.CodigoTitulo = null;
        }
        else
        {
            if (request.AnioBachillerato.HasValue)
            {
                var value = ResolverCantAniosAnioBachiller(uow, request.AnioBachillerato.Value).ToString();
                encuesta.UltimoAnioSextoEncuestaIni = value;
            }

            if (request.OrientacionBachilleratoId.HasValue)
                encuesta.CodigoTitulo = request.OrientacionBachilleratoId.Value;
            else if (encuesta.UltimoanioSecundariaEncuestaIni != PersonaConstants.Parametros.UruguayCodigoPais
                && request.AnioBachillerato.HasValue
                && ResolverCantAniosAnioBachiller(uow, request.AnioBachillerato.Value) == 12)
                encuesta.CodigoTitulo = CodigoTituloExteriorSextoLegacy;
        }

        if (request.RecursaAnioBachillerato.HasValue)
        {
            encuesta.VecesSextoEncuestaIni = request.RecursaAnioBachillerato.Value
                ? request.VecesRecursaAnioBachillerato?.ToString()
                : null;
        }

        if (request.EstadoEducacionSuperiorPreviaId.HasValue)
        {
            encuesta.TieneEducacionSuperiorEncuestaIni = request.EstadoEducacionSuperiorPreviaId.Value switch
            {
                EncuestaInicialState.EstadoEducacionSuperiorPrevia.Uruguay
                    or EncuestaInicialState.EstadoEducacionSuperiorPrevia.Exterior => CommonConstants.Booleanos.Si,
                EncuestaInicialState.EstadoEducacionSuperiorPrevia.No => CommonConstants.Booleanos.No,
                _ => encuesta.TieneEducacionSuperiorEncuestaIni
            };
        }

        if (request.NivelFormacionPadreTutorId.HasValue)
        {
            encuesta.InstruccionPadreEncuestaIni = request.NivelFormacionPadreTutorId.Value.ToString();
            encuesta.InstruccionPadreOrtEncuestaIni = EncuestaInicialState.EsInstruccionAlta(request.NivelFormacionPadreTutorId)
                ? request.PadreTutorEgresadoOrt.HasValue ? EncuestaInicialState.BoolToSN(request.PadreTutorEgresadoOrt.Value) : encuesta.InstruccionPadreOrtEncuestaIni
                : null;
        }

        if (request.NivelFormacionMadreTutorId.HasValue)
        {
            encuesta.InstruccionMadreEncuestaIni = request.NivelFormacionMadreTutorId.Value.ToString();
            encuesta.InstruccionMadreOrtEncuestaIni = EncuestaInicialState.EsInstruccionAlta(request.NivelFormacionMadreTutorId)
                ? request.MadreTutorEgresadoOrt.HasValue ? EncuestaInicialState.BoolToSN(request.MadreTutorEgresadoOrt.Value) : encuesta.InstruccionMadreOrtEncuestaIni
                : null;
        }
    }

    internal static void AplicarDecisionAcademica(EncuestaIniAdmision encuesta, DtoGuardarEncuestaInicialRequest request)
    {
        if (request.AnioDecisionCarreraId.HasValue)
            encuesta.DecisionCarreraEncuestaIni = request.AnioDecisionCarreraId.Value.ToString();
        if (request.AnioDecisionOrtId.HasValue)
            encuesta.DecisionUniverEncuestaIni = request.AnioDecisionOrtId.Value.ToString();
        if (request.NivelDecisionId.HasValue)
            encuesta.NivelDecisionEncuestaIni = (short)request.NivelDecisionId.Value;
        if (request.SeInformoEnOtrasUniversidades.HasValue)
            encuesta.InforOtrasAntesEncuestaIni = EncuestaInicialState.BoolToSN(request.SeInformoEnOtrasUniversidades.Value);
        if (request.InformacionOtrasUniversidadesLinea1 != null)
            encuesta.InforOtrasLinea1Ini = EncuestaInicialState.NormalizarTexto(request.InformacionOtrasUniversidadesLinea1);
        if (request.InformacionOtrasUniversidadesLinea2 != null)
            encuesta.InforOtrasLinea2Ini = EncuestaInicialState.NormalizarTexto(request.InformacionOtrasUniversidadesLinea2);
        if (request.ApoyoDecisionId.HasValue)
            EncuestaInicialState.AplicarApoyoDecision(encuesta, request.ApoyoDecisionId.Value);
    }

    internal static void AplicarExperienciaOrt(EncuestaIniAdmision encuesta, DtoGuardarEncuestaInicialRequest request)
    {
        if (request.TuvoAsesoramientoOrt.HasValue)
        {
            encuesta.AsesoramientoOrtEncuestaIni = EncuestaInicialState.BoolToSN(request.TuvoAsesoramientoOrt.Value);
            encuesta.ValoracionAsesoramientoOrtEncuestaIni = request.TuvoAsesoramientoOrt.Value
                ? request.ValoracionAsesoramientoOrtId.HasValue ? (short)request.ValoracionAsesoramientoOrtId.Value : encuesta.ValoracionAsesoramientoOrtEncuestaIni
                : null;
        }

        if (request.VisitoSitioWebOrt.HasValue)
        {
            encuesta.VistaSitioWebOrtEncuestaIni = EncuestaInicialState.BoolToSN(request.VisitoSitioWebOrt.Value);
            encuesta.ValoracionSitioWebOrtEncuestaIni = request.VisitoSitioWebOrt.Value
                ? request.ValoracionSitioWebOrtId.HasValue ? (short)request.ValoracionSitioWebOrtId.Value : encuesta.ValoracionSitioWebOrtEncuestaIni
                : null;
        }

        if (request.VisitoInstalacionesOrt.HasValue)
        {
            encuesta.VistaInstalacionesOrtEncuestaIni = EncuestaInicialState.BoolToSN(request.VisitoInstalacionesOrt.Value);
            encuesta.ValoracionInstalacionesOrtEncuestaIni = request.VisitoInstalacionesOrt.Value
                ? request.ValoracionInstalacionesOrtId.HasValue ? (short)request.ValoracionInstalacionesOrtId.Value : encuesta.ValoracionInstalacionesOrtEncuestaIni
                : null;
        }

        if (request.RecuerdaPublicidadOrt.HasValue)
            encuesta.PublicidadOrtEncuestaIni = EncuestaInicialState.BoolToSN(request.RecuerdaPublicidadOrt.Value);
    }

    internal static bool AplicarSituacionLaboral(Persona persona, DtoGuardarEncuestaInicialRequest request)
    {
        if (!request.TrabajaActualmente.HasValue)
            return false;

        persona.TrabajaActualmente = EncuestaInicialState.BoolToSNCorto(request.TrabajaActualmente.Value);
        persona.TipoJornada = request.TrabajaActualmente.Value
            ? request.TipoJornadaId.HasValue ? (byte)request.TipoJornadaId.Value : persona.TipoJornada
            : null;

        return true;
    }

    internal static DtoEncuestaInicialLectura MapearLectura(
        IUnitOfWork uow,
        EncuestaIniAdmision encuesta,
        Persona persona,
        long codigoPersona,
        DtoGuardarEncuestaInicialResponse pendientes)
    {
        var vecesRecursa = EncuestaInicialState.LeerInt(encuesta.VecesSextoEncuestaIni);
        var educacionSuperior = uow.EducacionSuperiorAdmisions?.GetByPersona(codigoPersona);
        var estadoEducacionSuperior = LeerEstadoEducacionSuperior(encuesta.TieneEducacionSuperiorEncuestaIni, educacionSuperior?.Count > 0);

        return new DtoEncuestaInicialLectura
        {
            IdEncuestaIni = encuesta.IdEncuestaIni,
            Estado = encuesta.EstadoEncuestaIniAdmision ?? EncuestaInicialState.EstadoTemporal,
            CarreraId = encuesta.IdProducto,
            ProcesoId = encuesta.IdProceso,
            CursaSecundariaActualmente = EncuestaInicialState.SNToBool(encuesta.CursaSecundariaActualmenteEncuestaIni),
            OrientacionBachilleratoId = encuesta.CodigoTitulo,
            AnioBachillerato = EncuestaInicialState.LeerLong(encuesta.UltimoAnioSextoEncuestaIni)
                ?? EncuestaInicialState.LeerLong(encuesta.AniosInstruccionEncuestaIni),
            VecesRecursaAnioBachillerato = vecesRecursa,
            RecursaAnioBachillerato = vecesRecursa.HasValue ? true : null,
            NivelFormacionPadreTutorId = EncuestaInicialState.LeerInt(encuesta.InstruccionPadreEncuestaIni),
            NivelFormacionMadreTutorId = EncuestaInicialState.LeerInt(encuesta.InstruccionMadreEncuestaIni),
            AnioDecisionCarreraId = EncuestaInicialState.LeerInt(encuesta.DecisionCarreraEncuestaIni),
            AnioDecisionOrtId = EncuestaInicialState.LeerInt(encuesta.DecisionUniverEncuestaIni),
            SeInformoEnOtrasUniversidades = EncuestaInicialState.SNToBool(encuesta.InforOtrasAntesEncuestaIni),
            InformacionOtrasUniversidadesLinea1 = encuesta.InforOtrasLinea1Ini,
            InformacionOtrasUniversidadesLinea2 = encuesta.InforOtrasLinea2Ini,
            ApoyoDecisionId = EncuestaInicialState.LeerApoyoDecision(encuesta),
            InstitucionSecundariaId = encuesta.CodigoInstitucionBac,
            NombreInstitucionSecundaria = encuesta.NombreInstSecEncuestaIni,
            UbicacionUltimoAnioSecundariaId = encuesta.UltimoanioSecundariaEncuestaIni,
            EstadoEducacionSuperiorPreviaId = estadoEducacionSuperior,
            NivelDecisionId = encuesta.NivelDecisionEncuestaIni,
            TuvoAsesoramientoOrt = EncuestaInicialState.SNToBool(encuesta.AsesoramientoOrtEncuestaIni),
            ValoracionAsesoramientoOrtId = encuesta.ValoracionAsesoramientoOrtEncuestaIni,
            VisitoSitioWebOrt = EncuestaInicialState.SNToBool(encuesta.VistaSitioWebOrtEncuestaIni),
            ValoracionSitioWebOrtId = encuesta.ValoracionSitioWebOrtEncuestaIni,
            VisitoInstalacionesOrt = EncuestaInicialState.SNToBool(encuesta.VistaInstalacionesOrtEncuestaIni),
            ValoracionInstalacionesOrtId = encuesta.ValoracionInstalacionesOrtEncuestaIni,
            RecuerdaPublicidadOrt = EncuestaInicialState.SNToBool(encuesta.PublicidadOrtEncuestaIni),
            MadreTutorEgresadoOrt = EncuestaInicialState.SNToBool(encuesta.InstruccionMadreOrtEncuestaIni),
            PadreTutorEgresadoOrt = EncuestaInicialState.SNToBool(encuesta.InstruccionPadreOrtEncuestaIni),
            TrabajaActualmente = EncuestaInicialState.SNToBool(persona.TrabajaActualmente),
            TipoJornadaId = persona.TipoJornada,
            UniversidadConsideradaIds = uow.EmpresaConsideradaAdmisions?.GetByPersona(codigoPersona)?.Where(e => e.CodigoEmpresa.HasValue).Select(e => e.CodigoEmpresa!.Value).ToList(),
            UniversidadConsideradaOtros = uow.EmpresaConsideradaAdmisions?.GetByPersona(codigoPersona)?.Where(e => !string.IsNullOrWhiteSpace(e.NombreOtraEmpresa)).Select(e => e.NombreOtraEmpresa!.Trim()).ToList(),
            UniversidadEducacionSuperiorIds = estadoEducacionSuperior == EncuestaInicialState.EstadoEducacionSuperiorPrevia.Uruguay ? educacionSuperior?.Where(e => e.CodigoEmpresa.HasValue).Select(e => e.CodigoEmpresa!.Value).ToList() : null,
            UniversidadEducacionSuperiorOtros = estadoEducacionSuperior == EncuestaInicialState.EstadoEducacionSuperiorPrevia.Uruguay ? educacionSuperior?.Where(e => !string.IsNullOrWhiteSpace(e.NombreOtraEmpresa)).Select(e => e.NombreOtraEmpresa!.Trim()).ToList() : null,
            PublicidadOrtIds = uow.PublicidadEleccionAdmisions?.GetByPersona(codigoPersona)?.Select(p => p.IdPublicidad).ToList(),
            MotivoEleccionOrtIds = uow.MotivoEleccionAdmisions?.GetByPersona(codigoPersona)?.Select(m => m.IdMotivo).ToList()
        };
    }

    private static long? LeerEstadoEducacionSuperior(string? value, bool tieneUniversidades)
    {
        // "SI" cubre Uruguay y Exterior; se distingue por la presencia de universidades
        // (invariante garantizado por INS_EI_63 / INS_EI_25 en el guardado).
        return EncuestaInicialState.SNToBool(value) switch
        {
            true => tieneUniversidades
                ? EncuestaInicialState.EstadoEducacionSuperiorPrevia.Uruguay
                : EncuestaInicialState.EstadoEducacionSuperiorPrevia.Exterior,
            false => EncuestaInicialState.EstadoEducacionSuperiorPrevia.No,
            _ => null
        };
    }

    private static decimal ResolverCantAniosAnioBachiller(IUnitOfWork uow, long value)
        => EncuestaInicialCatalogValidator.ResolverAnioBachiller(uow, value)?.CantAniosAnioBachiller ?? value;

    private static string GenerarClaveEncuesta(long idProducto, string? documento)
    {
        var input = $"{idProducto}/{(documento ?? string.Empty).Trim().ToUpperInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..30];
    }
}
