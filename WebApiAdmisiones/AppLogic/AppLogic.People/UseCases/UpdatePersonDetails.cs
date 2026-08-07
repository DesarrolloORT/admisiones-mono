using AppLogic.Contracts.Text;
using AppLogic.People.Contracts;
using AppLogic.People.Dtos;
using AppLogic.People.Rules;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.People.UseCases;

public class UpdatePersonDetails(IUnitOfWorkFactory uowFactory) : IUpdatePersonDetails
{
    private const string MethodName = nameof(UpdatePersonDetails);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<bool> Execute(long personId, UpdatePersonDetailsRequest request)
    {
        using var uow = _uowFactory.Create();
        var person = uow.Personas.GetByKey(personId);
        if (person is null)
        {
            return OperationResult<bool>.IsFailed("PER_ADP_01", MethodName, "No se encontró la persona autenticada.", 404);
        }

        var validation = ValidateRequest(request);
        if (!validation.Success)
        {
            return validation;
        }

        var identidadRestringida = PersonIdentityRules.HasRestrictedIdentity(
            person,
            uow.Inscriptos.TieneInscripcionActiva(personId));
        if (!PersonIdentityRules.AreIdentityChangesAllowed(person, request, identidadRestringida))
        {
            return OperationResult<bool>.IsFailed(
                "PER_ADP_06",
                MethodName,
                "No se pueden modificar datos de identidad para esta persona.",
                400);
        }

        var city = uow.Ciudads.GetByKey(request.CountryId, request.StateId, request.CityId);
        if (city is null)
        {
            return OperationResult<bool>.IsFailed(
                "PER_ADP_05",
                MethodName,
                "No existe la ciudad indicada para el país y estado enviados.",
                400);
        }

        PersonIdentityRules.ApplyIdentityChanges(person, request, identidadRestringida);
        person.CodigoPais = request.CountryId;
        person.CodigoEstado = request.StateId;
        person.CodigoCiudad = request.CityId;
        person.Direccion = TextNormalization.ToTitleCase(request.Address);
        person.Telefono1 = TextNormalization.TrimOrNull(request.PrimaryPhone);
        person.Email = TextNormalization.TrimOrNull(request.Email);

        PersonAuditStamp.Apply(person, personId, uow);
        uow.Personas.Update(person);
        uow.Save();

        return OperationResult<bool>.Ok(true, MethodName);
    }

    private static OperationResult<bool> ValidateRequest(UpdatePersonDetailsRequest request)
    {
        if (request is null)
        {
            return OperationResult<bool>.IsFailed("PER_ADP_02", MethodName, "La solicitud es obligatoria.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.EmailConfirmation))
        {
            return OperationResult<bool>.IsFailed("PER_ADP_03", MethodName, "Faltan parámetros obligatorios.", 400);
        }

        if (!string.Equals(request.Email, request.EmailConfirmation, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<bool>.IsFailed("PER_ADP_04", MethodName, "El mail y su verificación no coinciden.", 400);
        }

        return OperationResult<bool>.Ok(true, MethodName);
    }
}
