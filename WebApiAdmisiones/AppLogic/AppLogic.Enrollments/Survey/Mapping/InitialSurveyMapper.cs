using AppLogic.People.Constants;
using AppLogic.Enrollments.Survey.Dtos;
using AppLogic.Enrollments.Survey.Rules;
using AppLogic.Enrollments.Survey.Validators;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using System.Security.Cryptography;
using System.Text;
using AppLogic.Contracts.Constants;

namespace AppLogic.Enrollments.Survey.Mapping;

internal static class InitialSurveyMapper
{
    private const long CodigoInstitucionExteriorLegacy = 2898;
    private const long CodigoTituloExteriorSextoLegacy = 5;

    internal static void ApplyTechnicalData(
        EncuestaIniAdmision survey,
        Persona person,
        long personId,
        long? productId,
        long? admissionProcessId,
        long? idComienzo,
        long? idTurno)
    {
        survey.TipoDocumento = person.TipoDocumento;
        survey.Documento = person.Documento;
        survey.CodigoPersona = personId;
        survey.TipoInscripcion = InitialSurveyState.TipoInscripcionSoloEncuesta;
        survey.EstadoEncuestaIniAdmision ??= InitialSurveyState.EstadoTemporal;
        survey.NuevaversionEncuestaIni = SchemaConstants.BooleanFlag.Yes;

        if (productId.HasValue)
        {
            survey.IdProducto = productId.Value;
            survey.ClaveEncuestaIni = BuildSurveyKey(productId.Value, person.Documento);
        }

        if (admissionProcessId.HasValue)
            survey.IdProceso = admissionProcessId.Value;
        if (idComienzo.HasValue)
            survey.IdComienzo = idComienzo.Value;
        if (idTurno.HasValue)
            survey.IdTurno = idTurno.Value;
    }

    /// <summary>
    /// Valor "SI"/"NO" que depende de un flag: si el flag está en false se limpia, y si está en true
    /// solo se actualiza cuando el request trae dato nuevo (si no, conserva lo que ya tenía la encuesta).
    /// </summary>
    private static string? YesNoWhen(bool enabled, bool? newValue, string? current)
    {
        if (!enabled)
            return null;

        return newValue.HasValue ? InitialSurveyState.BoolToYesNo(newValue.Value) : current;
    }

    /// <summary>Misma regla que <see cref="YesNoWhen"/> para las valoraciones numéricas.</summary>
    private static short? RatingWhen(bool enabled, long? newValue, short? current)
    {
        if (!enabled)
            return null;

        return newValue.HasValue ? (short)newValue.Value : current;
    }

    internal static void ApplyEducation(IUnitOfWork uow, EncuestaIniAdmision survey, SaveInitialSurveyRequest request)
    {
        ApplySecondaryInstitution(survey, request);
        ApplyHighSchoolProgress(uow, survey, request);
        ApplyPreviousHigherEducation(survey, request);
        ApplyParentsEducation(survey, request);
    }

    private static void ApplySecondaryInstitution(EncuestaIniAdmision survey, SaveInitialSurveyRequest request)
    {
        if (request.LastSecondaryYearLocationId.HasValue)
        {
            survey.UltimoanioSecundariaEncuestaIni = (short)request.LastSecondaryYearLocationId.Value;
            if (request.LastSecondaryYearLocationId.Value == PersonConstants.Parametros.UruguayCodigoPais)
                survey.NombreInstSecEncuestaIni = null;
            else
                survey.CodigoInstitucionBac = CodigoInstitucionExteriorLegacy;
        }

        if (request.SecondaryInstitutionId.HasValue)
            survey.CodigoInstitucionBac = request.SecondaryInstitutionId.Value;

        if (request.SecondaryInstitutionName != null)
            survey.NombreInstSecEncuestaIni = InitialSurveyState.NormalizeText(request.SecondaryInstitutionName);
    }

    private static void ApplyHighSchoolProgress(IUnitOfWork uow, EncuestaIniAdmision survey, SaveInitialSurveyRequest request)
    {
        if (request.CurrentlyInSecondary.HasValue)
            survey.CursaSecundariaActualmenteEncuestaIni = InitialSurveyState.BoolToYesNo(request.CurrentlyInSecondary.Value);

        if (request.CurrentlyInSecondary == false)
        {
            survey.AniosInstruccionEncuestaIni = null;
            survey.UltimoAnioSextoEncuestaIni = null;
            survey.CodigoTitulo = null;
        }
        else
        {
            if (request.HighSchoolYear.HasValue)
            {
                var value = ResolveHighSchoolYearCount(uow, request.HighSchoolYear.Value).ToString();
                survey.UltimoAnioSextoEncuestaIni = value;
            }

            if (request.HighSchoolTrackId.HasValue)
                survey.CodigoTitulo = request.HighSchoolTrackId.Value;
            else if (survey.UltimoanioSecundariaEncuestaIni != PersonConstants.Parametros.UruguayCodigoPais
                && request.HighSchoolYear.HasValue
                && ResolveHighSchoolYearCount(uow, request.HighSchoolYear.Value) == 12)
                survey.CodigoTitulo = CodigoTituloExteriorSextoLegacy;
        }

        if (request.RepeatsHighSchoolYear.HasValue)
        {
            survey.VecesSextoEncuestaIni = request.RepeatsHighSchoolYear.Value
                ? request.HighSchoolYearRepeatCount?.ToString()
                : "0";
        }
    }

    private static void ApplyPreviousHigherEducation(EncuestaIniAdmision survey, SaveInitialSurveyRequest request)
    {
        if (!request.PreviousHigherEducationId.HasValue)
            return;

        survey.TieneEducacionSuperiorEncuestaIni = request.PreviousHigherEducationId.Value switch
        {
            InitialSurveyState.EstadoEducacionSuperiorPrevia.Uruguay
                or InitialSurveyState.EstadoEducacionSuperiorPrevia.Exterior => SchemaConstants.BooleanFlag.Yes,
            InitialSurveyState.EstadoEducacionSuperiorPrevia.Ninguna => SchemaConstants.BooleanFlag.No,
            _ => survey.TieneEducacionSuperiorEncuestaIni
        };
    }

    private static void ApplyParentsEducation(EncuestaIniAdmision survey, SaveInitialSurveyRequest request)
    {
        if (request.FatherEducationLevelId.HasValue)
        {
            survey.InstruccionPadreEncuestaIni = request.FatherEducationLevelId.Value.ToString();
            survey.InstruccionPadreOrtEncuestaIni = YesNoWhen(
                InitialSurveyState.IsHigherEducationLevel(request.FatherEducationLevelId),
                request.FatherIsOrtGraduate,
                survey.InstruccionPadreOrtEncuestaIni);
        }

        if (request.MotherEducationLevelId.HasValue)
        {
            survey.InstruccionMadreEncuestaIni = request.MotherEducationLevelId.Value.ToString();
            survey.InstruccionMadreOrtEncuestaIni = YesNoWhen(
                InitialSurveyState.IsHigherEducationLevel(request.MotherEducationLevelId),
                request.MotherIsOrtGraduate,
                survey.InstruccionMadreOrtEncuestaIni);
        }
    }

    internal static void ApplyAcademicDecision(EncuestaIniAdmision survey, SaveInitialSurveyRequest request)
    {
        if (request.CareerDecisionYearId.HasValue)
            survey.DecisionCarreraEncuestaIni = request.CareerDecisionYearId.Value.ToString();
        if (request.OrtDecisionYearId.HasValue)
            survey.DecisionUniverEncuestaIni = request.OrtDecisionYearId.Value.ToString();
        if (request.DecisionLevelId.HasValue)
            survey.NivelDecisionEncuestaIni = (short)request.DecisionLevelId.Value;
        if (request.ResearchedOtherUniversities.HasValue)
            survey.InforOtrasAntesEncuestaIni = InitialSurveyState.BoolToYesNo(request.ResearchedOtherUniversities.Value);
        if (request.OtherUniversitiesInfoLine1 != null)
            survey.InforOtrasLinea1Ini = InitialSurveyState.NormalizeText(request.OtherUniversitiesInfoLine1);
        if (request.OtherUniversitiesInfoLine2 != null)
            survey.InforOtrasLinea2Ini = InitialSurveyState.NormalizeText(request.OtherUniversitiesInfoLine2);
        if (request.DecisionSupportId.HasValue)
            InitialSurveyState.ApplyDecisionSupport(survey, request.DecisionSupportId.Value);
    }

    internal static void ApplyOrtExperience(EncuestaIniAdmision survey, SaveInitialSurveyRequest request)
    {
        if (request.HadOrtAdvisory.HasValue)
        {
            survey.AsesoramientoOrtEncuestaIni = InitialSurveyState.BoolToYesNo(request.HadOrtAdvisory.Value);
            survey.ValoracionAsesoramientoOrtEncuestaIni = RatingWhen(
                request.HadOrtAdvisory.Value,
                request.OrtAdvisoryRatingId,
                survey.ValoracionAsesoramientoOrtEncuestaIni);
        }

        if (request.VisitedOrtWebsite.HasValue)
        {
            survey.VistaSitioWebOrtEncuestaIni = InitialSurveyState.BoolToYesNo(request.VisitedOrtWebsite.Value);
            survey.ValoracionSitioWebOrtEncuestaIni = RatingWhen(
                request.VisitedOrtWebsite.Value,
                request.OrtWebsiteRatingId,
                survey.ValoracionSitioWebOrtEncuestaIni);
        }

        if (request.VisitedOrtFacilities.HasValue)
        {
            survey.VistaInstalacionesOrtEncuestaIni = InitialSurveyState.BoolToYesNo(request.VisitedOrtFacilities.Value);
            survey.ValoracionInstalacionesOrtEncuestaIni = RatingWhen(
                request.VisitedOrtFacilities.Value,
                request.OrtFacilitiesRatingId,
                survey.ValoracionInstalacionesOrtEncuestaIni);
        }

        if (request.RecallsOrtAdvertising.HasValue)
            survey.PublicidadOrtEncuestaIni = InitialSurveyState.BoolToYesNo(request.RecallsOrtAdvertising.Value);
    }

    internal static InitialSurveyDetails MapForRead(
        IUnitOfWork uow,
        EncuestaIniAdmision survey,
        long personId,
        SaveInitialSurveyResponse pendientes)
    {
        var vecesRecursa = InitialSurveyState.LeerInt(survey.VecesSextoEncuestaIni);
        var higherEducation = uow.EducacionSuperiorAdmisions?.GetByPersona(personId);
        var higherEducationStatus = LeerEstadoEducacionSuperior(survey.TieneEducacionSuperiorEncuestaIni, higherEducation?.Count > 0);

        return new InitialSurveyDetails
        {
            SurveyId = survey.IdEncuestaIni,
            Status = survey.EstadoEncuestaIniAdmision ?? InitialSurveyState.EstadoTemporal,
            DegreeProgramId = survey.IdProducto,
            AdmissionProcessId = survey.IdProceso,
            CurrentlyInSecondary = InitialSurveyState.YesNoToBool(survey.CursaSecundariaActualmenteEncuestaIni),
            HighSchoolTrackId = survey.CodigoTitulo,
            HighSchoolYear = InitialSurveyState.LeerLong(survey.UltimoAnioSextoEncuestaIni)
                ?? InitialSurveyState.LeerLong(survey.AniosInstruccionEncuestaIni),
            HighSchoolYearRepeatCount = vecesRecursa > 0 ? vecesRecursa : null,
            RepeatsHighSchoolYear = vecesRecursa.HasValue ? vecesRecursa > 0 : null,
            FatherEducationLevelId = InitialSurveyState.LeerInt(survey.InstruccionPadreEncuestaIni),
            MotherEducationLevelId = InitialSurveyState.LeerInt(survey.InstruccionMadreEncuestaIni),
            CareerDecisionYearId = InitialSurveyState.LeerInt(survey.DecisionCarreraEncuestaIni),
            OrtDecisionYearId = InitialSurveyState.LeerInt(survey.DecisionUniverEncuestaIni),
            ResearchedOtherUniversities = InitialSurveyState.YesNoToBool(survey.InforOtrasAntesEncuestaIni),
            OtherUniversitiesInfoLine1 = survey.InforOtrasLinea1Ini,
            OtherUniversitiesInfoLine2 = survey.InforOtrasLinea2Ini,
            DecisionSupportId = InitialSurveyState.LeerApoyoDecision(survey),
            SecondaryInstitutionId = survey.CodigoInstitucionBac,
            SecondaryInstitutionName = survey.NombreInstSecEncuestaIni,
            LastSecondaryYearLocationId = survey.UltimoanioSecundariaEncuestaIni,
            PreviousHigherEducationId = higherEducationStatus,
            DecisionLevelId = survey.NivelDecisionEncuestaIni,
            HadOrtAdvisory = InitialSurveyState.YesNoToBool(survey.AsesoramientoOrtEncuestaIni),
            OrtAdvisoryRatingId = survey.ValoracionAsesoramientoOrtEncuestaIni,
            VisitedOrtWebsite = InitialSurveyState.YesNoToBool(survey.VistaSitioWebOrtEncuestaIni),
            OrtWebsiteRatingId = survey.ValoracionSitioWebOrtEncuestaIni,
            VisitedOrtFacilities = InitialSurveyState.YesNoToBool(survey.VistaInstalacionesOrtEncuestaIni),
            OrtFacilitiesRatingId = survey.ValoracionInstalacionesOrtEncuestaIni,
            RecallsOrtAdvertising = InitialSurveyState.YesNoToBool(survey.PublicidadOrtEncuestaIni),
            MotherIsOrtGraduate = InitialSurveyState.YesNoToBool(survey.InstruccionMadreOrtEncuestaIni),
            FatherIsOrtGraduate = InitialSurveyState.YesNoToBool(survey.InstruccionPadreOrtEncuestaIni),
            ConsideredUniversityIds = uow.EmpresaConsideradaAdmisions?.GetByPersona(personId)?.Where(e => e.CodigoEmpresa.HasValue).Select(e => e.CodigoEmpresa!.Value).ToList(),
            ConsideredUniversityOthers = uow.EmpresaConsideradaAdmisions?.GetByPersona(personId)?.Where(e => !string.IsNullOrWhiteSpace(e.NombreOtraEmpresa)).Select(e => e.NombreOtraEmpresa!.Trim()).ToList(),
            HigherEducationUniversityIds = higherEducationStatus == InitialSurveyState.EstadoEducacionSuperiorPrevia.Uruguay ? higherEducation?.Where(e => e.CodigoEmpresa.HasValue).Select(e => e.CodigoEmpresa!.Value).ToList() : null,
            HigherEducationUniversityOthers = higherEducationStatus == InitialSurveyState.EstadoEducacionSuperiorPrevia.Uruguay ? higherEducation?.Where(e => !string.IsNullOrWhiteSpace(e.NombreOtraEmpresa)).Select(e => e.NombreOtraEmpresa!.Trim()).ToList() : null,
            OrtAdvertisingIds = uow.PublicidadEleccionAdmisions?.GetByPersona(personId)?.Select(p => p.IdPublicidad).ToList(),
            OrtChoiceReasonIds = uow.MotivoEleccionAdmisions?.GetByPersona(personId)?.Select(m => m.IdMotivo).ToList()
        };
    }

    private static long? LeerEstadoEducacionSuperior(string? value, bool tieneUniversidades)
    {
        // "SI" cubre Uruguay y Exterior; se distingue por la presencia de universidades
        // (invariante garantizado por INS_EI_63 / INS_EI_25 en el guardado).
        return InitialSurveyState.YesNoToBool(value) switch
        {
            true => tieneUniversidades
                ? InitialSurveyState.EstadoEducacionSuperiorPrevia.Uruguay
                : InitialSurveyState.EstadoEducacionSuperiorPrevia.Exterior,
            false => InitialSurveyState.EstadoEducacionSuperiorPrevia.Ninguna,
            _ => null
        };
    }

    private static decimal ResolveHighSchoolYearCount(IUnitOfWork uow, long value)
        => InitialSurveyCatalogValidation.ResolveHighSchoolYear(uow, value)?.CantAniosAnioBachiller ?? value;

    private static string BuildSurveyKey(long productId, string? document)
    {
        var input = $"{productId}/{(document ?? string.Empty).Trim().ToUpperInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..30];
    }
}
