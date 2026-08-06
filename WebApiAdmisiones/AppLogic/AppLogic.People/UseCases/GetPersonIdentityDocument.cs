using AppLogic.Contracts;
using AppLogic.Identity.Constants;
using AppLogic.Identity.Services;
using AppLogic.People.Contracts;
using AppLogic.People.Dtos;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.People.UseCases;

public class GetPersonIdentityDocument(IUnitOfWorkFactory uowFactory) : IGetPersonIdentityDocument
{
    private const string MethodName = nameof(GetPersonIdentityDocument);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<PersonIdentityDocumentResponse> Execute(long personId)
    {
        using var uow = _uowFactory.Create();
        var person = uow.Personas.GetByKey(personId);
        var finalDocumentExpirationDate = person?.FechaVtoDocumentoPersona;

        var frente = IdentityDocumentService.GetOptionalDocumentForQuery(
            uow,
            personId,
            IdentityDocumentConstants.ImageType.Front,
            finalDocumentExpirationDate,
            MethodName);
        if (!frente.Success)
            return frente.Failure().As<PersonIdentityDocumentResponse>(MethodName);

        var dorso = IdentityDocumentService.GetOptionalDocumentForQuery(
            uow,
            personId,
            IdentityDocumentConstants.ImageType.Back,
            finalDocumentExpirationDate,
            MethodName);
        if (!dorso.Success)
            return dorso.Failure().As<PersonIdentityDocumentResponse>(MethodName);

        if (frente.Data is null && dorso.Data is null)
        {
            return OperationResult<PersonIdentityDocumentResponse>.IsFailed(
                "GEN_DA_02",
                MethodName,
                "Documento no encontrado.",
                404);
        }

        return OperationResult<PersonIdentityDocumentResponse>.Ok(
            new PersonIdentityDocumentResponse
            {
                Front = frente.Data?.File,
                Back = dorso.Data?.File,
                ExpirationDate = frente.Data?.ExpirationDate ?? dorso.Data?.ExpirationDate
            },
            MethodName);
    }
}
