using AppLogic.People.Contracts;
using AppLogic.People.Dtos;
using AppLogic.People.Mapping;
using AppLogic.People.Rules;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.People.UseCases;

public class GetPersonDetails(IUnitOfWorkFactory uowFactory) : IGetPersonDetails
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<PersonDetailsResponse> Execute(long personId)
    {
        const string methodName = nameof(GetPersonDetails);

        using var uow = _uowFactory.Create();
        var person = uow.Personas.GetByKey(personId);
        if (person is null)
        {
            return OperationResult<PersonDetailsResponse>.IsFailed(
                "PER_DAT_01",
                methodName,
                "No se encontró la persona autenticada.",
                404);
        }

        var identidadRestringida = PersonIdentityRules.HasRestrictedIdentity(
            person,
            uow.Inscriptos.TieneInscripcionActiva(personId));

        return OperationResult<PersonDetailsResponse>.Ok(
            PersonMapper.ToDetailsResponse(person, identidadRestringida),
            methodName);
    }
}
