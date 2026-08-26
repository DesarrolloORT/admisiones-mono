namespace AppLogic.Enrollments.Survey.Dtos;

/// <summary>Encuesta guardada: los mismos campos del request más su identidad y estado.</summary>
public sealed class InitialSurveyDetails : SaveInitialSurveyRequest
{
    /// <summary>Código de la encuesta.</summary>
    public long SurveyId { get; set; }

    /// <summary>Estado de la encuesta: TEMPORAL o DEFINITIVO.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Departamento (CODIGO_ESTADO) de la institución de secundaria.
    /// </summary>
    public long? SecondaryInstitutionStateId { get; set; }
}
