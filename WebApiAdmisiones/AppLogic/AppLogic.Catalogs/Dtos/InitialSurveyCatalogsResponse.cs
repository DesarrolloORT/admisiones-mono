using AppLogic.Contracts.Dtos;

namespace AppLogic.Catalogs.Dtos;

/// <summary>Universidad elegible como "otra universidad considerada" o donde cursó estudios previos.</summary>
public sealed class UniversityCatalog
{
    /// <summary>Código de la universidad en T_EMPRESA (0 = "Otro").</summary>
    public long Value { get; init; }

    /// <summary>Nombre de la universidad.</summary>
    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// Orientación de bachillerato (científico, humanístico, …). <c>Track</c> es el término que ya usa
/// <c>SaveInitialSurveyRequest.HighSchoolTrackId</c>.
/// </summary>
public sealed class HighSchoolTrackCatalog
{
    /// <summary>Código del título.</summary>
    public long Value { get; init; }

    /// <summary>Nombre del título.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Código de la orientación en el plan viejo.</summary>
    public string? Track { get; init; }

    /// <summary>Código de la orientación en el plan nuevo.</summary>
    public string? NewTrack { get; init; }
}

/// <summary>Año de bachillerato con sus orientaciones disponibles.</summary>
public sealed class HighSchoolYearCatalog
{
    /// <summary>Cantidad de años de bachillerato.</summary>
    public long Value { get; init; }

    /// <summary>Nombre del año de bachillerato.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Orientaciones disponibles para ese año.</summary>
    public IReadOnlyList<HighSchoolTrackCatalog> Tracks { get; init; } = [];
}

/// <summary>Combos de la sección "Educación" de la encuesta inicial.</summary>
public sealed class SurveyEducationCatalogs
{
    /// <summary>Opciones Sí / No.</summary>
    public IReadOnlyList<ComboOption> YesNoOptions { get; init; } = [];

    /// <summary>Dónde cursó el último año de secundaria (Uruguay / exterior).</summary>
    public IReadOnlyList<ComboOption> LastSecondaryYearLocations { get; init; } = [];

    /// <summary>Años de bachillerato con sus orientaciones.</summary>
    public IReadOnlyList<HighSchoolYearCatalog> HighSchoolYears { get; init; } = [];

    /// <summary>Estados posibles de la educación superior previa.</summary>
    public IReadOnlyList<ComboOption> PreviousHigherEducationOptions { get; init; } = [];

    /// <summary>Universidades donde pudo haber cursado estudios superiores previos.</summary>
    public IReadOnlyList<UniversityCatalog> Universities { get; init; } = [];

    /// <summary>Niveles de formación de padre y madre.</summary>
    public IReadOnlyList<ComboOption> EducationLevels { get; init; } = [];
}

/// <summary>Combos de la sección "Decisión académica" de la encuesta inicial.</summary>
public sealed class SurveyAcademicDecisionCatalogs
{
    /// <summary>Años de educación media superior en los que decidió la carrera o eligió ORT.</summary>
    public IReadOnlyList<ComboOption> UpperSecondaryYears { get; init; } = [];

    /// <summary>Quién lo acompañó en la decisión.</summary>
    public IReadOnlyList<ComboOption> DecisionSupports { get; init; } = [];

    /// <summary>Qué tan decidido está.</summary>
    public IReadOnlyList<ComboOption> DecisionLevels { get; init; } = [];

    /// <summary>Universidades que consideró además de ORT.</summary>
    public IReadOnlyList<UniversityCatalog> Universities { get; init; } = [];

    /// <summary>Motivos por los que eligió ORT.</summary>
    public IReadOnlyList<ComboOption> OrtChoiceReasons { get; init; } = [];
}

/// <summary>Combos de la sección "Experiencia ORT" de la encuesta inicial.</summary>
public sealed class SurveyOrtExperienceCatalogs
{
    /// <summary>Opciones Sí / No.</summary>
    public IReadOnlyList<ComboOption> YesNoOptions { get; init; } = [];

    /// <summary>Escala de valoración (1 a 5).</summary>
    public IReadOnlyList<ComboOption> Ratings { get; init; } = [];

    /// <summary>Piezas de publicidad de ORT que puede recordar.</summary>
    public IReadOnlyList<ComboOption> OrtAdvertisements { get; init; } = [];
}

public sealed class InitialSurveyCatalogsResponse
{
    /// <summary>Combos de la sección "Educación".</summary>
    public SurveyEducationCatalogs Education { get; init; } = new();

    /// <summary>Combos de la sección "Decisión académica".</summary>
    public SurveyAcademicDecisionCatalogs AcademicDecision { get; init; } = new();

    /// <summary>Combos de la sección "Experiencia ORT".</summary>
    public SurveyOrtExperienceCatalogs OrtExperience { get; init; } = new();
}
