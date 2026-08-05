using AppLogic.Identity.Dtos;
using AppLogic.People.Dtos;
using Utilities;

namespace AppLogic.People.Contracts;

/// <summary>Datos personales de la persona autenticada.</summary>
public interface IGetPersonDetails
{
    OperationResult<PersonDetailsResponse> Execute(long personId);
}

/// <summary>Actualiza domicilio, contacto y —si la identidad no está restringida— datos de identidad.</summary>
public interface IUpdatePersonDetails
{
    OperationResult<bool> Execute(long personId, UpdatePersonDetailsRequest request);
}

/// <summary>Valida un teléfono contra el formato del país. Sin dependencias: es una función pura.</summary>
public interface IValidatePhoneNumber
{
    OperationResult<bool> Execute(PhoneNumber phoneNumber, bool isPrimaryPhone);
}

/// <summary>Inscripciones de la persona agrupadas por producto y proceso.</summary>
public interface IGetMyEnrollments
{
    OperationResult<IEnumerable<MyEnrollmentsResponse>> Execute(long personId);
}

/// <summary>Cambia la contraseña en LDAP. No toca la base de admisiones.</summary>
public interface IChangePassword
{
    Task<OperationResult<object>> ExecuteAsync(long personId, ChangePasswordRequest request);
}

/// <summary>Foto de perfil de la persona.</summary>
public interface IGetPersonPhoto
{
    OperationResult<byte[]> Execute(long personId);
}

/// <summary>Alta o reemplazo de la foto de perfil.</summary>
public interface IUploadPersonPhoto
{
    OperationResult<bool> Execute(long personId, byte[] fileContent, string fileName);
}

/// <summary>Frente y dorso del documento de identidad, temporales o definitivos.</summary>
public interface IGetPersonIdentityDocument
{
    OperationResult<PersonIdentityDocumentResponse> Execute(long personId);
}

/// <summary>Alta o reemplazo de las imágenes del documento de identidad.</summary>
public interface IUploadPersonIdentityDocument
{
    OperationResult<bool> Execute(long personId, DateTime fecha, IdentityDocumentFile frente, IdentityDocumentFile dorso);
}
