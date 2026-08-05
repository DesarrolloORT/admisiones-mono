using System.Text.Json.Serialization;

namespace AppLogic.Enrollments.Survey.Dtos;

/// <summary>Encuesta inicial de la persona, si tiene derecho a completarla.</summary>
public sealed class GetInitialSurveyResponse
{
    /// <summary>La persona tiene un interés registrado que habilita la encuesta.</summary>
    public bool CanAnswerSurvey { get; set; }

    /// <summary>Encuesta ya guardada. Se omite si todavía no completó nada.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InitialSurveyDetails? Survey { get; set; }
}
