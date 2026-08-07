using AppLogic.Enrollments.Survey.Dtos;
using AppLogic.Enrollments.Survey.Rules;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.Enrollments.Survey.Validators;

internal static class InitialSurveyValidation
{
    internal static SaveInitialSurveyResponse ValidateCompletenessFromDb(
        IUnitOfWork uow,
        EncuestaIniAdmision survey,
        long personId)
    {
        var pendientes = new PendingBuilder(survey.IdEncuestaIni);

        ValidateTechnicalData(survey, pendientes);
        ValidateEducation(uow, survey, pendientes);
        ValidateAcademicDecision(uow, survey, personId, pendientes);
        ValidateOrtExperience(uow, survey, personId, pendientes);

        return pendientes.ToResponse();
    }

    private static void ValidateTechnicalData(EncuestaIniAdmision survey, PendingBuilder pendientes)
    {
        pendientes.AddFieldIf(!survey.IdProducto.HasValue, "carreraId");
        pendientes.AddFieldIf(!survey.IdProceso.HasValue, "procesoId");
        pendientes.AddFieldIf(!survey.IdComienzo.HasValue, "idComienzo");
        pendientes.AddFieldIf(!survey.IdTurno.HasValue, "idTurno");
    }

    private static void ValidateEducation(
        IUnitOfWork uow,
        EncuestaIniAdmision survey,
        PendingBuilder pendientes)
    {
        var section = InitialSurveyState.Educacion;
        pendientes.AddSi(!survey.UltimoanioSecundariaEncuestaIni.HasValue, section, "ubicacionUltimoAnioSecundariaId");

        if (survey.UltimoanioSecundariaEncuestaIni == InitialSurveyState.UbicacionUltimoAnioSecundaria.Uruguay)
            pendientes.AddSi(!survey.CodigoInstitucionBac.HasValue || survey.CodigoInstitucionBac <= 0, section, "institucionSecundariaId");
        if (survey.UltimoanioSecundariaEncuestaIni == InitialSurveyState.UbicacionUltimoAnioSecundaria.Exterior)
            pendientes.AddSi(string.IsNullOrWhiteSpace(survey.NombreInstSecEncuestaIni), section, "nombreInstitucionSecundaria");

        pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.CursaSecundariaActualmenteEncuestaIni), section, "cursaSecundariaActualmente");
        if (InitialSurveyState.YesNoToBool(survey.CursaSecundariaActualmenteEncuestaIni) == true)
        {
            var year = InitialSurveyState.LeerLong(survey.UltimoAnioSextoEncuestaIni)
                ?? InitialSurveyState.LeerLong(survey.AniosInstruccionEncuestaIni);
            pendientes.AddSi(!year.HasValue, section, "anioBachillerato");
            if (year.HasValue && InitialSurveyCatalogValidation.HighSchoolYearHasTracks(uow, year.Value))
                pendientes.AddSi(!survey.CodigoTitulo.HasValue || survey.CodigoTitulo <= 0, section, "orientacionBachilleratoId");
        }

        // "SI" (Uruguay/Exterior) o "NO" cuentan como respondido. Las universidades no se validan
        // aquí: Uruguay siempre tiene >=1 (INS_EI_63) y Exterior no lleva (INS_EI_25).
        pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.TieneEducacionSuperiorEncuestaIni), section, "estadoEducacionSuperiorPreviaId");

        var padre = InitialSurveyState.LeerInt(survey.InstruccionPadreEncuestaIni);
        var madre = InitialSurveyState.LeerInt(survey.InstruccionMadreEncuestaIni);
        pendientes.AddSi(!padre.HasValue, section, "nivelFormacionPadreTutorId");
        pendientes.AddSi(!madre.HasValue, section, "nivelFormacionMadreTutorId");
        if (InitialSurveyState.IsHigherEducationLevel(padre))
            pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.InstruccionPadreOrtEncuestaIni), section, "padreTutorEgresadoOrt");
        if (InitialSurveyState.IsHigherEducationLevel(madre))
            pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.InstruccionMadreOrtEncuestaIni), section, "madreTutorEgresadoOrt");
    }

    private static void ValidateAcademicDecision(
        IUnitOfWork uow,
        EncuestaIniAdmision survey,
        long personId,
        PendingBuilder pendientes)
    {
        var section = InitialSurveyState.DecisionAcademica;
        pendientes.AddSi(string.IsNullOrWhiteSpace(survey.DecisionCarreraEncuestaIni), section, "anioDecisionCarreraId");
        pendientes.AddSi(string.IsNullOrWhiteSpace(survey.DecisionUniverEncuestaIni), section, "anioDecisionOrtId");
        pendientes.AddSi(!survey.NivelDecisionEncuestaIni.HasValue, section, "nivelDecisionId");
        pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.InforOtrasAntesEncuestaIni), section, "seInformoEnOtrasUniversidades");

        if (InitialSurveyState.YesNoToBool(survey.InforOtrasAntesEncuestaIni) == true)
            pendientes.AddSi((uow.EmpresaConsideradaAdmisions?.GetByPersona(personId)?.Count ?? 0) == 0, section, "universidadConsideradaIds");

        pendientes.AddSi(!InitialSurveyState.HasDecisionSupport(survey), section, "apoyoDecisionId");
        pendientes.AddSi((uow.MotivoEleccionAdmisions?.GetByPersona(personId)?.Count ?? 0) == 0, section, "motivoEleccionOrtIds");
    }

    private static void ValidateOrtExperience(
        IUnitOfWork uow,
        EncuestaIniAdmision survey,
        long personId,
        PendingBuilder pendientes)
    {
        var section = InitialSurveyState.ExperienciaOrt;
        pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.AsesoramientoOrtEncuestaIni), section, "tuvoAsesoramientoOrt");
        if (InitialSurveyState.YesNoToBool(survey.AsesoramientoOrtEncuestaIni) == true)
            pendientes.AddSi(!survey.ValoracionAsesoramientoOrtEncuestaIni.HasValue, section, "valoracionAsesoramientoOrtId");

        pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.VistaSitioWebOrtEncuestaIni), section, "visitoSitioWebOrt");
        if (InitialSurveyState.YesNoToBool(survey.VistaSitioWebOrtEncuestaIni) == true)
            pendientes.AddSi(!survey.ValoracionSitioWebOrtEncuestaIni.HasValue, section, "valoracionSitioWebOrtId");

        pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.VistaInstalacionesOrtEncuestaIni), section, "visitoInstalacionesOrt");
        if (InitialSurveyState.YesNoToBool(survey.VistaInstalacionesOrtEncuestaIni) == true)
            pendientes.AddSi(!survey.ValoracionInstalacionesOrtEncuestaIni.HasValue, section, "valoracionInstalacionesOrtId");

        pendientes.AddSi(!InitialSurveyState.IsAnsweredYesNo(survey.PublicidadOrtEncuestaIni), section, "recuerdaPublicidadOrt");
        if (InitialSurveyState.YesNoToBool(survey.PublicidadOrtEncuestaIni) == true)
            pendientes.AddSi((uow.PublicidadEleccionAdmisions?.GetByPersona(personId)?.Count ?? 0) == 0, section, "publicidadOrtIds");
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

        internal void AddFieldIf(bool condition, string field)
        {
            if (condition)
                _fields.Add(field);
        }

        internal SaveInitialSurveyResponse ToResponse()
        {
            var hasPending = _fields.Count > 0;
            return new SaveInitialSurveyResponse
            {
                SurveyId = idEncuestaIni,
                Status = hasPending ? InitialSurveyState.EstadoTemporal : InitialSurveyState.EstadoDefinitivo,
                PendingSections = _sections.Order().ToList(),
                PendingFields = _fields.Order().ToList()
            };
        }
    }
}
