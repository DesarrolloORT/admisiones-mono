using AppLogic.Constants;
using AppLogic.Dtos.EncuestaInicial;
using BusinessLogic.Entities;
using System.Security.Cryptography;
using System.Text;

namespace AppLogic.Helpers
{
    internal static class EncuestaInicialMapper
    {
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

        internal static void AplicarEducacion(EncuestaIniAdmision encuesta, DtoGuardarEncuestaInicialRequest request)
        {
            if (request.UbicacionUltimoAnioSecundariaId.HasValue)
            {
                encuesta.UltimoanioSecundariaEncuestaIni = (short)request.UbicacionUltimoAnioSecundariaId.Value;
                if (request.UbicacionUltimoAnioSecundariaId.Value == PersonaConstants.Parametros.UruguayCodigoPais)
                    encuesta.NombreInstSecEncuestaIni = null;
                else
                    encuesta.CodigoInstitucionBac = null;
            }

            if (request.InstitucionSecundariaId.HasValue)
                encuesta.CodigoInstitucionBac = request.InstitucionSecundariaId.Value;

            if (request.NombreInstitucionSecundaria != null)
                encuesta.NombreInstSecEncuestaIni = EncuestaInicialState.NormalizarTexto(request.NombreInstitucionSecundaria);

            if (request.AnioBachillerato.HasValue)
            {
                var value = request.AnioBachillerato.Value.ToString();
                encuesta.AniosInstruccionEncuestaIni = value;
                encuesta.UltimoAnioSextoEncuestaIni = value;
            }

            if (request.OrientacionBachilleratoId.HasValue)
                encuesta.CodigoTitulo = request.OrientacionBachilleratoId.Value;

            if (request.RecursaAnioBachillerato.HasValue)
            {
                encuesta.VecesSextoEncuestaIni = request.RecursaAnioBachillerato.Value
                    ? request.VecesRecursaAnioBachillerato?.ToString()
                    : null;
            }

            if (request.EstadoEducacionSuperiorPreviaId.HasValue)
            {
                encuesta.TieneEducacionSuperiorEncuestaIni = request.EstadoEducacionSuperiorPreviaId.Value is 1 or 2
                    ? CommonConstants.Booleanos.Si
                    : CommonConstants.Booleanos.No;
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

            persona.TrabajaActualmente = EncuestaInicialState.BoolToSN(request.TrabajaActualmente.Value);
            persona.TipoJornada = request.TrabajaActualmente.Value
                ? request.TipoJornadaId.HasValue ? (byte)request.TipoJornadaId.Value : persona.TipoJornada
                : null;

            return true;
        }

        private static string GenerarClaveEncuesta(long idProducto, string? documento)
        {
            var input = $"{idProducto}/{(documento ?? string.Empty).Trim().ToUpperInvariant()}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash)[..30];
        }
    }
}
