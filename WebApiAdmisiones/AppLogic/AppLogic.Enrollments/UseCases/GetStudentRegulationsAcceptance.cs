using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.UseCases;

public class GetStudentRegulationsAcceptance(IUnitOfWorkFactory uowFactory) : IGetStudentRegulationsAcceptance
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<StudentRegulationsAcceptanceResponse> Execute(long personId)
    {
        using var uow = _uowFactory.Create();
        var acceptance = uow.AceptacionReglamentoEsts.GetPrimeraByPersona(personId);
        var response = new StudentRegulationsAcceptanceResponse
        {
            AcceptedStudentRegulations = acceptance != null,
            AcceptanceDate = acceptance?.FechaIngreso
        };

        return OperationResult<StudentRegulationsAcceptanceResponse>.Ok(
            response,
            nameof(GetStudentRegulationsAcceptance));
    }
}
