using AppLogic.Catalogs.Dtos;
using AppLogic.Contracts.Dtos;
using AppLogic.Enrollments.Survey.Rules;
using BusinessLogic.Entities;

namespace AppLogic.Catalogs.Mapping;

/// <summary>
/// Arma la respuesta de catálogos de la encuesta inicial. Recibe las entidades ya leídas: los combos
/// fijos vienen de <see cref="InitialSurveyOptions"/> y los dinámicos de la base.
/// </summary>
public static class InitialSurveyCatalogMapper
{
    /// <summary>La opción "Otro" no está en T_EMPRESA: se agrega al final del catálogo.</summary>
    private const long OtherUniversityValue = 0;

    public static IReadOnlyList<UniversityCatalog> ToUniversities(IEnumerable<Empresa> companies) =>
        companies
            .Select(e => new UniversityCatalog
            {
                Value = e.CodigoEmpresa,
                Label = e.Nombre ?? string.Empty
            })
            .Append(new UniversityCatalog { Value = OtherUniversityValue, Label = "Otro" })
            .ToList();

    public static IReadOnlyList<HighSchoolYearCatalog> ToHighSchoolYears(IEnumerable<AnioBachiller> anios) =>
        anios
            .Select(a => new HighSchoolYearCatalog
            {
                Value = (long)(a.CantAniosAnioBachiller ?? a.IdAnioBachiller),
                Label = a.NombreAnioBachiller ?? $"{a.CantAniosAnioBachiller}",
                Tracks = a.Titulos
                    .Select(t => new HighSchoolTrackCatalog
                    {
                        Value = t.CodigoTitulo,
                        Label = t.Nombre ?? string.Empty,
                        Track = t.OrientacionTitulo,
                        NewTrack = t.OrientacionNewTitulo
                    })
                    .ToList()
            })
            .ToList();

    public static IReadOnlyList<ComboOption> ToOrtChoiceReasons(IEnumerable<MotivoOpcionesAdmision> reasons) =>
        reasons.Select(m => ComboOption.Of(m.IdMotivo, m.NombreMotivo)).ToList();

    public static IReadOnlyList<ComboOption> ToOrtAdvertisements(IEnumerable<PublicidadOpcionesAdmision> publicidades) =>
        publicidades.Select(p => ComboOption.Of(p.IdPublicidad, p.NombrePublicidad)).ToList();

    public static InitialSurveyCatalogsResponse ToResponse(
        IReadOnlyList<UniversityCatalog> universities,
        IReadOnlyList<HighSchoolYearCatalog> highSchoolYears,
        IReadOnlyList<ComboOption> ortChoiceReasons,
        IReadOnlyList<ComboOption> ortAdvertisements) => new()
        {
            Education = new SurveyEducationCatalogs
            {
                YesNoOptions = InitialSurveyOptions.YesNoOptions,
                LastSecondaryYearLocations = InitialSurveyOptions.LastSecondaryYearLocations,
                PreviousHigherEducationOptions = InitialSurveyOptions.PreviousHigherEducationOptions,
                EducationLevels = InitialSurveyOptions.EducationLevels,
                HighSchoolYears = highSchoolYears,
                Universities = universities
            },
            AcademicDecision = new SurveyAcademicDecisionCatalogs
            {
                UpperSecondaryYears = InitialSurveyOptions.UpperSecondaryYears,
                DecisionSupports = InitialSurveyOptions.DecisionSupports,
                DecisionLevels = InitialSurveyOptions.DecisionLevels,
                Universities = universities,
                OrtChoiceReasons = ortChoiceReasons
            },
            OrtExperience = new SurveyOrtExperienceCatalogs
            {
                YesNoOptions = InitialSurveyOptions.YesNoOptions,
                Ratings = InitialSurveyOptions.Ratings,
                OrtAdvertisements = ortAdvertisements
            }
        };
}
