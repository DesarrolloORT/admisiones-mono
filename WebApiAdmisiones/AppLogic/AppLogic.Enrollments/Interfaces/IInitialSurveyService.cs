using AppLogic.Enrollments.Survey.Dtos;
using Utilities;

namespace AppLogic.Enrollments.Interfaces;

/// <summary>
/// Servicio de encuesta inicial de admisión: obtención y guardado (parcial o definitivo).
/// Expone únicamente las operaciones consumidas por <c>InscripcionesService</c>.
/// </summary>
public interface IInitialSurveyService
{
    OperationResult<GetInitialSurveyResponse> GetInitialSurvey(long personId);

    OperationResult<SaveInitialSurveyResponse> SaveInitialSurvey(
        long personId,
        SaveInitialSurveyRequest request);
}
