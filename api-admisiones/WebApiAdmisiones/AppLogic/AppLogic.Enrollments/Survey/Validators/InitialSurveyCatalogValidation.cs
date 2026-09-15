using AppLogic.Enrollments.Survey.Dtos;
using AppLogic.Enrollments.Survey.Rules;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using AppLogic.Enrollments.Constants;

namespace AppLogic.Enrollments.Survey.Validators;

public static class InitialSurveyCatalogValidation
{
    /// <summary>IdNivelProducto que identifica una carrera universitaria (a diferencia de terciarias u otros niveles).</summary>
    private const long NivelProductoUniversitario = 1;

    /// <summary>Cuarto año de bachillerato, identificado según cuál campo resolvió el año: IdAnioBachiller usa la numeración corta (1-6), CantAniosAnioBachiller la cantidad de años acumulados (hasta 12).</summary>
    private const long IdAnioBachillerCuartoAnio = 4;
    private const long CantAniosBachilleratoCuartoAnio = 10;

    public static InitialSurveyRejection ValidatePartialRequest(
        IUnitOfWork uow,
        SaveInitialSurveyRequest request)
    {
        if (request == null)
            return InitialSurveyRejection.InvalidRequest;

        var fixedValidation = ValidateFixedOptions(request);
        if (fixedValidation != InitialSurveyRejection.None)
            return fixedValidation;

        var dynamicValidation = ValidateDynamicCatalogs(uow, request);
        if (dynamicValidation != InitialSurveyRejection.None)
            return dynamicValidation;

        return ValidateInternalConsistency(request);
    }

    private static InitialSurveyRejection ValidateFixedOptions(
        SaveInitialSurveyRequest request)
    {
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.LastSecondaryYearLocations, request.LastSecondaryYearLocationId))
            return InitialSurveyRejection.InvalidLastSecondaryYearLocation;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.PreviousHigherEducationOptions, request.PreviousHigherEducationId))
            return InitialSurveyRejection.InvalidPreviousHigherEducation;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.EducationLevels, request.FatherEducationLevelId))
            return InitialSurveyRejection.InvalidFatherEducationLevel;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.EducationLevels, request.MotherEducationLevelId))
            return InitialSurveyRejection.InvalidMotherEducationLevel;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.UpperSecondaryYears, request.CareerDecisionYearId))
            return InitialSurveyRejection.InvalidCareerDecisionYear;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.UpperSecondaryYears, request.OrtDecisionYearId))
            return InitialSurveyRejection.InvalidOrtDecisionYear;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.DecisionSupports, request.DecisionSupportId))
            return InitialSurveyRejection.InvalidDecisionSupport;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.DecisionLevels, request.DecisionLevelId))
            return InitialSurveyRejection.InvalidDecisionLevel;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.Ratings, request.OrtAdvisoryRatingId))
            return InitialSurveyRejection.InvalidAdvisoryRating;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.Ratings, request.OrtWebsiteRatingId))
            return InitialSurveyRejection.InvalidWebsiteRating;
        if (!InitialSurveyOptions.Contains(InitialSurveyOptions.Ratings, request.OrtFacilitiesRatingId))
            return InitialSurveyRejection.InvalidFacilitiesRating;

        if (request.AdmissionProcessId is <= 0)
            return InitialSurveyRejection.InvalidAdmissionProcess;
        if (request.HighSchoolTrackId is <= 0)
            return InitialSurveyRejection.InvalidDegreeTitle;

        return InitialSurveyRejection.None;
    }

    // Los grupos se evalúan en cadena y no en un array: cada uno consulta la base, así que el
    // primer rechazo tiene que cortar antes de que los siguientes vayan a la DB.
    private static InitialSurveyRejection ValidateDynamicCatalogs(
        IUnitOfWork uow,
        SaveInitialSurveyRequest request)
    {
        if (request.DegreeProgramId.HasValue
            && !uow.Productos.EsProductoValidoParaInteres(request.DegreeProgramId.Value))
            return InitialSurveyRejection.InvalidProduct;

        var product = request.DegreeProgramId.HasValue
            ? uow.Productos.GetByKey(request.DegreeProgramId.Value)
            : null;

        var rejection = ValidateAvailableProcess(uow, request);
        if (rejection != InitialSurveyRejection.None)
            return rejection;

        rejection = ValidateHighSchool(uow, request, product);
        if (rejection != InitialSurveyRejection.None)
            return rejection;

        rejection = ValidateSecondaryInstitution(uow, request);
        if (rejection != InitialSurveyRejection.None)
            return rejection;

        rejection = ValidateUniversities(uow, request);
        if (rejection != InitialSurveyRejection.None)
            return rejection;

        return ValidateMultiOptionCatalogs(uow, request);
    }

    private static InitialSurveyRejection ValidateAvailableProcess(
        IUnitOfWork uow,
        SaveInitialSurveyRequest request)
    {
        var availableProcesses = uow.VdProcesosDisponibles1y2s;
        if (request.DegreeProgramId.HasValue
            && request.AdmissionProcessId.HasValue
            && availableProcesses != null
            && !availableProcesses.GetProcesosDisponibles(request.DegreeProgramId.Value).Any(p => p.IdProceso == request.AdmissionProcessId.Value))
        {
            return InitialSurveyRejection.NoEnabledProcessForProduct;
        }

        return InitialSurveyRejection.None;
    }

    private static InitialSurveyRejection ValidateHighSchool(
        IUnitOfWork uow,
        SaveInitialSurveyRequest request,
        Producto? product)
    {
        // Sin dato explícito de "cursa secundaria" se asume que el bachillerato aplica.
        if (request.CurrentlyInSecondary == false)
            return InitialSurveyRejection.None;

        if (request.HighSchoolYear.HasValue && ResolveHighSchoolYear(uow, request.HighSchoolYear.Value) == null)
            return InitialSurveyRejection.InvalidHighSchoolYear;

        if (product?.IdNivelProducto == NivelProductoUniversitario
            && request.HighSchoolYear.HasValue
            && IsFourthHighSchoolYear(uow, request.HighSchoolYear.Value))
            return InitialSurveyRejection.UniversityRequiresFifthOrSixthYear;

        if (request.HighSchoolTrackId.HasValue && !IsCatalogedTrack(uow, request.HighSchoolTrackId.Value))
            return InitialSurveyRejection.UncatalogedDegreeTitle;

        return InitialSurveyRejection.None;
    }

    private static InitialSurveyRejection ValidateSecondaryInstitution(
        IUnitOfWork uow,
        SaveInitialSurveyRequest request)
    {
        if (request.LastSecondaryYearLocationId != InitialSurveyState.UbicacionUltimoAnioSecundaria.Uruguay)
            return InitialSurveyRejection.None;

        if (request.SecondaryInstitutionId is <= 0)
            return InitialSurveyRejection.InvalidSecondaryInstitutionId;
        if (request.SecondaryInstitutionId.HasValue && uow.Empresas.GetByKey(request.SecondaryInstitutionId.Value) == null)
            return InitialSurveyRejection.UnknownSecondaryInstitution;

        return InitialSurveyRejection.None;
    }

    private static InitialSurveyRejection ValidateUniversities(
        IUnitOfWork uow,
        SaveInitialSurveyRequest request)
    {
        var companies = ValidateCompanies(uow, request.ConsideredUniversityIds, request.ConsideredUniversityOthers);
        if (companies != InitialSurveyRejection.None)
            return companies;

        if (request.PreviousHigherEducationId != InitialSurveyState.EstadoEducacionSuperiorPrevia.Uruguay)
            return InitialSurveyRejection.None;

        return ValidateCompanies(uow, request.HigherEducationUniversityIds, request.HigherEducationUniversityOthers);
    }

    private static InitialSurveyRejection ValidateMultiOptionCatalogs(
        IUnitOfWork uow,
        SaveInitialSurveyRequest request)
    {
        if (request.OrtChoiceReasonIds is { Count: > 0 })
        {
            var reasons = uow.MotivoOpcionesAdmisions.GetAll();
            if (request.OrtChoiceReasonIds.Any(id => id <= 0 || !reasons.Any(m => m.IdMotivo == id)))
                return InitialSurveyRejection.InvalidChoiceReason;
        }

        if (request.OrtAdvertisingIds is { Count: > 0 })
        {
            var publicidades = uow.PublicidadOpcionesAdmisions.GetAll();
            if (request.OrtAdvertisingIds.Any(id => id <= 0 || !publicidades.Any(p => p.IdPublicidad == id)))
                return InitialSurveyRejection.InvalidAdvertising;
        }

        return InitialSurveyRejection.None;
    }

    private static InitialSurveyRejection ValidateInternalConsistency(
        SaveInitialSurveyRequest request)
    {
        var rejection = ValidateHigherEducationConsistency(request);
        if (rejection != InitialSurveyRejection.None)
            return rejection;

        rejection = ValidateRatingsConsistency(request);
        if (rejection != InitialSurveyRejection.None)
            return rejection;

        return ValidateParentsConsistency(request);
    }

    private static InitialSurveyRejection ValidateHigherEducationConsistency(SaveInitialSurveyRequest request)
    {
        if (request.RepeatsHighSchoolYear == true
            && (!request.HighSchoolYearRepeatCount.HasValue || request.HighSchoolYearRepeatCount.Value < 1))
            return InitialSurveyRejection.InvalidRepeatCount;

        if (request.PreviousHigherEducationId == InitialSurveyState.EstadoEducacionSuperiorPrevia.Uruguay
            && (request.HigherEducationUniversityIds == null || request.HigherEducationUniversityIds.Count == 0)
            && !HasOthers(request.HigherEducationUniversityOthers))
            return InitialSurveyRejection.HigherEducationUniversityRequired;
        if (request.PreviousHigherEducationId != InitialSurveyState.EstadoEducacionSuperiorPrevia.Uruguay
            && (request.HigherEducationUniversityIds?.Count > 0 || HasOthers(request.HigherEducationUniversityOthers)))
            return InitialSurveyRejection.InvalidSelectedUniversity;

        return InitialSurveyRejection.None;
    }

    private static InitialSurveyRejection ValidateRatingsConsistency(SaveInitialSurveyRequest request)
    {
        if (request.HadOrtAdvisory == true && !request.OrtAdvisoryRatingId.HasValue)
            return InitialSurveyRejection.AdvisoryRatingRequired;
        if (request.VisitedOrtWebsite == true && !request.OrtWebsiteRatingId.HasValue)
            return InitialSurveyRejection.WebsiteRatingRequired;
        if (request.VisitedOrtFacilities == true && !request.OrtFacilitiesRatingId.HasValue)
            return InitialSurveyRejection.FacilitiesRatingRequired;

        return InitialSurveyRejection.None;
    }

    private static InitialSurveyRejection ValidateParentsConsistency(SaveInitialSurveyRequest request)
    {
        if (InitialSurveyState.IsHigherEducationLevel(request.FatherEducationLevelId) && !request.FatherIsOrtGraduate.HasValue)
            return InitialSurveyRejection.FatherOrtGraduateRequired;
        if (InitialSurveyState.IsHigherEducationLevel(request.MotherEducationLevelId) && !request.MotherIsOrtGraduate.HasValue)
            return InitialSurveyRejection.MotherOrtGraduateRequired;

        return InitialSurveyRejection.None;
    }

    private static InitialSurveyRejection ValidateCompanies(
        IUnitOfWork uow,
        List<long>? companies,
        List<string>? otros)
    {
        if ((companies == null || companies.Count == 0) && !HasOthers(otros))
            return InitialSurveyRejection.None;

        var universidades = uow.Empresas.GetUniversidades();
        var hasOtherSelected = companies?.Contains(0) == true;
        var hasOtherName = HasOthers(otros);

        if (companies?.Any(id => id < 0) == true)
            return InitialSurveyRejection.InvalidSelectedUniversity;
        if (hasOtherSelected != hasOtherName)
            return InitialSurveyRejection.InvalidSelectedUniversity;
        if (companies?.Where(id => id > 0).Any(id => !universidades.Any(u => u.CodigoEmpresa == id)) == true)
            return InitialSurveyRejection.UnknownSelectedUniversity;

        return InitialSurveyRejection.None;
    }

    internal static AnioBachiller? ResolveHighSchoolYear(IUnitOfWork uow, long value)
    {
        return uow.AnioBachillers.GetAllWithRelated()
            .FirstOrDefault(a => IsHighSchoolYear(a, value));
    }

    internal static bool HighSchoolYearHasTracks(IUnitOfWork uow, long value)
        => ResolveHighSchoolYear(uow, value)?.Titulos.Count > 0;

    private static bool IsCatalogedTrack(IUnitOfWork uow, long codigoTitulo)
    {
        return uow.AnioBachillers.GetAllWithRelated()
            .SelectMany(a => a.Titulos)
            .Any(t => t.CodigoTitulo == codigoTitulo);
    }

    private static bool IsHighSchoolYear(AnioBachiller year, long value)
    {
        return year.IdAnioBachiller == value
            || year.CantAniosAnioBachiller == value;
    }

    private static bool IsFourthHighSchoolYear(IUnitOfWork uow, long value)
    {
        var year = ResolveHighSchoolYear(uow, value);
        return value is IdAnioBachillerCuartoAnio or CantAniosBachilleratoCuartoAnio
            || year?.IdAnioBachiller == IdAnioBachillerCuartoAnio
            || year?.CantAniosAnioBachiller == CantAniosBachilleratoCuartoAnio;
    }

    private static bool HasOthers(List<string>? otros)
        => otros?.Any(o => !string.IsNullOrWhiteSpace(o)) == true;
}
