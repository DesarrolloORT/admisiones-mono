using System.Collections.Generic;

namespace AppLogic.Enrollments.Survey.Dtos;

/// <summary>
/// Campos de la encuesta inicial de admisión. Todos son opcionales porque la encuesta se guarda
/// de forma parcial: recién al confirmar la preinscripción se exige que esté completa.
/// </summary>
public class SaveInitialSurveyRequest
{
    /// <summary>Carrera (producto) elegida.</summary>
    public long? DegreeProgramId { get; set; }

    /// <summary>Proceso de admisión elegido.</summary>
    public long? AdmissionProcessId { get; set; }

    /// <summary>Orientación del bachillerato (título).</summary>
    public long? HighSchoolTrackId { get; set; }

    /// <summary>Último año de bachillerato cursado.</summary>
    public long? HighSchoolYear { get; set; }

    /// <summary>La persona está cursando secundaria actualmente.</summary>
    public bool? CurrentlyInSecondary { get; set; }

    /// <summary>Cantidad de veces que recursa el año de bachillerato.</summary>
    public int? HighSchoolYearRepeatCount { get; set; }

    /// <summary>La persona recursa el año de bachillerato.</summary>
    public bool? RepeatsHighSchoolYear { get; set; }

    /// <summary>Nivel de formación del padre o tutor.</summary>
    public int? FatherEducationLevelId { get; set; }

    /// <summary>Nivel de formación de la madre o tutora.</summary>
    public int? MotherEducationLevelId { get; set; }

    /// <summary>Año en que decidió la carrera.</summary>
    public int? CareerDecisionYearId { get; set; }

    /// <summary>Año en que decidió estudiar en ORT.</summary>
    public int? OrtDecisionYearId { get; set; }

    /// <summary>Se informó sobre otras universidades.</summary>
    public bool? ResearchedOtherUniversities { get; set; }

    /// <summary>Detalle libre sobre otras universidades, primera línea.</summary>
    public string? OtherUniversitiesInfoLine1 { get; set; }

    /// <summary>Detalle libre sobre otras universidades, segunda línea.</summary>
    public string? OtherUniversitiesInfoLine2 { get; set; }

    /// <summary>Con quién compartió la decisión.</summary>
    public int? DecisionSupportId { get; set; }

    /// <summary>Institución de secundaria, cuando está catalogada.</summary>
    public long? SecondaryInstitutionId { get; set; }

    /// <summary>Nombre libre de la institución de secundaria, cuando no está catalogada.</summary>
    public string? SecondaryInstitutionName { get; set; }

    /// <summary>Dónde cursó el último año de secundaria (Uruguay o exterior).</summary>
    public long? LastSecondaryYearLocationId { get; set; }

    /// <summary>Estado de la educación superior previa.</summary>
    public long? PreviousHigherEducationId { get; set; }

    /// <summary>Qué tan avanzada estaba la decisión.</summary>
    public long? DecisionLevelId { get; set; }

    /// <summary>Recibió asesoramiento de ORT.</summary>
    public bool? HadOrtAdvisory { get; set; }

    /// <summary>Valoración del asesoramiento de ORT.</summary>
    public long? OrtAdvisoryRatingId { get; set; }

    /// <summary>Visitó el sitio web de ORT.</summary>
    public bool? VisitedOrtWebsite { get; set; }

    /// <summary>Valoración del sitio web de ORT.</summary>
    public long? OrtWebsiteRatingId { get; set; }

    /// <summary>Visitó las instalaciones de ORT.</summary>
    public bool? VisitedOrtFacilities { get; set; }

    /// <summary>Valoración de las instalaciones de ORT.</summary>
    public long? OrtFacilitiesRatingId { get; set; }

    /// <summary>Recuerda haber visto publicidad de ORT.</summary>
    public bool? RecallsOrtAdvertising { get; set; }

    /// <summary>La madre o tutora es egresada de ORT.</summary>
    public bool? MotherIsOrtGraduate { get; set; }

    /// <summary>El padre o tutor es egresado de ORT.</summary>
    public bool? FatherIsOrtGraduate { get; set; }

    /// <summary>Universidades consideradas, elegidas del catálogo.</summary>
    public List<long>? ConsideredUniversityIds { get; set; }

    /// <summary>Universidades consideradas, ingresadas como texto libre.</summary>
    public List<string>? ConsideredUniversityOthers { get; set; }

    /// <summary>Universidades de educación superior previa, elegidas del catálogo.</summary>
    public List<long>? HigherEducationUniversityIds { get; set; }

    /// <summary>Universidades de educación superior previa, ingresadas como texto libre.</summary>
    public List<string>? HigherEducationUniversityOthers { get; set; }

    /// <summary>Medios en los que vio publicidad de ORT.</summary>
    public List<long>? OrtAdvertisingIds { get; set; }

    /// <summary>Motivos por los que eligió ORT.</summary>
    public List<long>? OrtChoiceReasonIds { get; set; }
}
