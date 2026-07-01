using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Helpers;
using AppLogic.Helpers.ValidationHelpers;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.Services.Inscripciones.Encuesta
{
    internal static class EncuestaInicialValidator
    {
        internal static DtoGuardarEncuestaInicialResponse ValidarCompletitudDesdeBase(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            Persona persona,
            long codigoPersona)
        {
            var pendientes = new PendingBuilder(encuesta.IdEncuestaIni);

            ValidarDatosTecnicos(encuesta, pendientes);
            ValidarEducacion(uow, encuesta, codigoPersona, pendientes);
            ValidarDecisionAcademica(uow, encuesta, codigoPersona, pendientes);
            ValidarExperienciaOrt(uow, encuesta, codigoPersona, pendientes);
            ValidarSituacionLaboral(persona, pendientes);

            return pendientes.ToResponse();
        }

        private static void ValidarDatosTecnicos(EncuestaIniAdmision encuesta, PendingBuilder pendientes)
        {
            pendientes.AddCampoSi(!encuesta.IdProducto.HasValue, "carreraId");
            pendientes.AddCampoSi(!encuesta.IdProceso.HasValue, "procesoId");
            pendientes.AddCampoSi(!encuesta.IdComienzo.HasValue, "idComienzo");
            pendientes.AddCampoSi(!encuesta.IdTurno.HasValue, "idTurno");
        }

        private static void ValidarEducacion(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            long codigoPersona,
            PendingBuilder pendientes)
        {
            var section = EncuestaInicialState.Educacion;
            pendientes.AddSi(!encuesta.UltimoanioSecundariaEncuestaIni.HasValue, section, "ubicacionUltimoAnioSecundariaId");

            if (encuesta.UltimoanioSecundariaEncuestaIni == 1)
                pendientes.AddSi(!encuesta.CodigoInstitucionBac.HasValue || encuesta.CodigoInstitucionBac <= 0, section, "institucionSecundariaId");
            if (encuesta.UltimoanioSecundariaEncuestaIni == 2)
                pendientes.AddSi(string.IsNullOrWhiteSpace(encuesta.NombreInstSecEncuestaIni), section, "nombreInstitucionSecundaria");

            var anio = EncuestaInicialState.LeerLong(encuesta.UltimoAnioSextoEncuestaIni)
                ?? EncuestaInicialState.LeerLong(encuesta.AniosInstruccionEncuestaIni);
            pendientes.AddSi(!anio.HasValue, section, "anioBachillerato");
            if (anio.HasValue && EncuestaInicialCatalogValidator.AnioBachillerTieneOrientaciones(uow, anio.Value))
                pendientes.AddSi(!encuesta.CodigoTitulo.HasValue || encuesta.CodigoTitulo <= 0, section, "orientacionBachilleratoId");

            pendientes.AddSi(!TieneEducacionSuperiorRespondido(encuesta.TieneEducacionSuperiorEncuestaIni), section, "estadoEducacionSuperiorPreviaId");
            if (string.Equals(encuesta.TieneEducacionSuperiorEncuestaIni, "SI", StringComparison.OrdinalIgnoreCase))
                pendientes.AddSi((uow.EducacionSuperiorAdmisions?.GetByPersona(codigoPersona)?.Count ?? 0) == 0, section, "universidadEducacionSuperiorIds");

            var padre = EncuestaInicialState.LeerInt(encuesta.InstruccionPadreEncuestaIni);
            var madre = EncuestaInicialState.LeerInt(encuesta.InstruccionMadreEncuestaIni);
            pendientes.AddSi(!padre.HasValue, section, "nivelFormacionPadreTutorId");
            pendientes.AddSi(!madre.HasValue, section, "nivelFormacionMadreTutorId");
            if (EncuestaInicialState.EsInstruccionAlta(padre))
                pendientes.AddSi(!EncuestaInicialState.IsAnsweredSN(encuesta.InstruccionPadreOrtEncuestaIni), section, "padreTutorEgresadoOrt");
            if (EncuestaInicialState.EsInstruccionAlta(madre))
                pendientes.AddSi(!EncuestaInicialState.IsAnsweredSN(encuesta.InstruccionMadreOrtEncuestaIni), section, "madreTutorEgresadoOrt");
        }

        private static void ValidarDecisionAcademica(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            long codigoPersona,
            PendingBuilder pendientes)
        {
            var section = EncuestaInicialState.DecisionAcademica;
            pendientes.AddSi(string.IsNullOrWhiteSpace(encuesta.DecisionCarreraEncuestaIni), section, "anioDecisionCarreraId");
            pendientes.AddSi(string.IsNullOrWhiteSpace(encuesta.DecisionUniverEncuestaIni), section, "anioDecisionOrtId");
            pendientes.AddSi(!encuesta.NivelDecisionEncuestaIni.HasValue, section, "nivelDecisionId");
            pendientes.AddSi(!EncuestaInicialState.IsAnsweredSN(encuesta.InforOtrasAntesEncuestaIni), section, "seInformoEnOtrasUniversidades");

            if (EncuestaInicialState.SNToBool(encuesta.InforOtrasAntesEncuestaIni) == true)
                pendientes.AddSi((uow.EmpresaConsideradaAdmisions?.GetByPersona(codigoPersona)?.Count ?? 0) == 0, section, "universidadConsideradaIds");

            pendientes.AddSi(!EncuestaInicialState.TieneApoyoDecision(encuesta), section, "apoyoDecisionId");
            pendientes.AddSi((uow.MotivoEleccionAdmisions?.GetByPersona(codigoPersona)?.Count ?? 0) == 0, section, "motivoEleccionOrtIds");
        }

        private static void ValidarExperienciaOrt(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            long codigoPersona,
            PendingBuilder pendientes)
        {
            var section = EncuestaInicialState.ExperienciaOrt;
            pendientes.AddSi(!EncuestaInicialState.IsAnsweredSN(encuesta.AsesoramientoOrtEncuestaIni), section, "tuvoAsesoramientoOrt");
            if (EncuestaInicialState.SNToBool(encuesta.AsesoramientoOrtEncuestaIni) == true)
                pendientes.AddSi(!encuesta.ValoracionAsesoramientoOrtEncuestaIni.HasValue, section, "valoracionAsesoramientoOrtId");

            pendientes.AddSi(!EncuestaInicialState.IsAnsweredSN(encuesta.VistaSitioWebOrtEncuestaIni), section, "visitoSitioWebOrt");
            if (EncuestaInicialState.SNToBool(encuesta.VistaSitioWebOrtEncuestaIni) == true)
                pendientes.AddSi(!encuesta.ValoracionSitioWebOrtEncuestaIni.HasValue, section, "valoracionSitioWebOrtId");

            pendientes.AddSi(!EncuestaInicialState.IsAnsweredSN(encuesta.VistaInstalacionesOrtEncuestaIni), section, "visitoInstalacionesOrt");
            if (EncuestaInicialState.SNToBool(encuesta.VistaInstalacionesOrtEncuestaIni) == true)
                pendientes.AddSi(!encuesta.ValoracionInstalacionesOrtEncuestaIni.HasValue, section, "valoracionInstalacionesOrtId");

            pendientes.AddSi(!EncuestaInicialState.IsAnsweredSN(encuesta.PublicidadOrtEncuestaIni), section, "recuerdaPublicidadOrt");
            if (EncuestaInicialState.SNToBool(encuesta.PublicidadOrtEncuestaIni) == true)
                pendientes.AddSi((uow.PublicidadEleccionAdmisions?.GetByPersona(codigoPersona)?.Count ?? 0) == 0, section, "publicidadOrtIds");
        }

        private static void ValidarSituacionLaboral(Persona persona, PendingBuilder pendientes)
        {
            var section = EncuestaInicialState.SituacionLaboral;
            pendientes.AddSi(!EncuestaInicialState.IsAnsweredSN(persona.TrabajaActualmente), section, "trabajaActualmente");
            if (EncuestaInicialState.SNToBool(persona.TrabajaActualmente) == true)
                pendientes.AddSi(!persona.TipoJornada.HasValue, section, "tipoJornadaId");
        }

        private static bool TieneEducacionSuperiorRespondido(string? value)
        {
            return string.Equals(value, "SI", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "SE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "NO", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class PendingBuilder(long idEncuestaIni)
        {
            private readonly HashSet<string> _sections = [];
            private readonly HashSet<string> _fields = [];

            internal void AddSi(bool condition, string section, string field)
            {
                if (!condition)
                    return;

                _sections.Add(section);
                _fields.Add(field);
            }

            internal void AddCampoSi(bool condition, string field)
            {
                if (condition)
                    _fields.Add(field);
            }

            internal DtoGuardarEncuestaInicialResponse ToResponse()
            {
                var hasPending = _fields.Count > 0;
                return new DtoGuardarEncuestaInicialResponse
                {
                    IdEncuestaIni = idEncuestaIni,
                    Estado = hasPending ? EncuestaInicialState.EstadoTemporal : EncuestaInicialState.EstadoDefinitivo,
                    SeccionesPendientes = _sections.Order().ToList(),
                    CamposPendientes = _fields.Order().ToList()
                };
            }
        }
    }
}
