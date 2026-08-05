using AppLogic.People.Contracts;
using AppLogic.People.Dtos;
using AppLogic.People.Mapping;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.People.UseCases;

public class GetMyEnrollments(IUnitOfWorkFactory uowFactory) : IGetMyEnrollments
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<IEnumerable<MyEnrollmentsResponse>> Execute(long personId)
    {
        using var uow = _uowFactory.Create();

        var response = MyEnrollmentsMapper.ToGroupedResponse(
            uow.VdInscripcionesFresco1y2s.GetInscripcionesFrescoHabilitadas(personId),
            uow.VdInscripcionesFresco3y4s.GetInscripcionesFrescoHabilitadas(personId));

        return OperationResult<IEnumerable<MyEnrollmentsResponse>>.Ok(response, nameof(GetMyEnrollments));
    }
}
