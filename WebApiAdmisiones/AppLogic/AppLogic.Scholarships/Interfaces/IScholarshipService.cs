using AppLogic.Scholarships.Dtos;
using Utilities;

namespace AppLogic.Scholarships.Interfaces;

public interface IScholarshipService
{
    /// <summary>Inscripciones confirmadas de la persona, base del circuito de becas.</summary>
    OperationResult<IEnumerable<ConfirmedEnrollmentResponse>> GetMyConfirmedEnrollments(long personId);
}
