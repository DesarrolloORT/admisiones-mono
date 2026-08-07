namespace AppLogic.Enrollments.Survey.Dtos;

/// <summary>Resultado de guardar la encuesta inicial, con lo que todavía falta completar.</summary>
public sealed class SaveInitialSurveyResponse
{
    /// <summary>Código de la encuesta guardada.</summary>
    public long SurveyId { get; set; }

    /// <summary>Estado de la encuesta: TEMPORAL o DEFINITIVO.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Secciones que todavía tienen campos sin completar.</summary>
    public List<string> PendingSections { get; set; } = [];

    /// <summary>Campos que todavía faltan completar.</summary>
    public List<string> PendingFields { get; set; } = [];
}
