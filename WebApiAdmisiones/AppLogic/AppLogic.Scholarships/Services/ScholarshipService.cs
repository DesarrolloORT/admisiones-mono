using AppLogic.Contracts.Constants;
using AppLogic.Scholarships.Dtos;
using AppLogic.Scholarships.Interfaces;
using AppLogic.Scholarships.Mapping;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Scholarships.Services;

public class ScholarshipService(IUnitOfWorkFactory uowFactory) : IScholarshipService
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<IEnumerable<ConfirmedEnrollmentResponse>> GetMyConfirmedEnrollments(long personId)
    {
        using var uow = _uowFactory.Create();
        var response = uow.VdInscripcionesFresco1y2s
            .GetInscripcionesFrescoHabilitadas(personId)
            .Where(i => i.EstadoInscripcion == EnrollmentStatus.Confirmed)
            .Select(ConfirmedEnrollmentMapper.ToResponse)
            .ToList();

        return OperationResult<IEnumerable<ConfirmedEnrollmentResponse>>.Ok(response, nameof(GetMyConfirmedEnrollments));
    }
}
