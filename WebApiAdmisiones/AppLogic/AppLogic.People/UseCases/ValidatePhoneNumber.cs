using AppLogic.People.Contracts;
using AppLogic.People.Dtos;
using Utilities;

namespace AppLogic.People.UseCases;

/// <summary>
/// Sin dependencias: el front pide validar un teléfono contra el formato del país antes de
/// enviarlo. No consulta la base ni ningún servicio externo.
/// </summary>
public class ValidatePhoneNumber : IValidatePhoneNumber
{
    public OperationResult<bool> Execute(PhoneNumber phoneNumber, bool isPrimaryPhone)
    {
        const string methodName = nameof(ValidatePhoneNumber);

        // Retorno temprano si el teléfono está vacío o nulo
        if (string.IsNullOrWhiteSpace(phoneNumber.NationalNumber))
        {
            return OperationResult<bool>.Ok(false, methodName);
        }

        var validPhone = PhoneVerification.Validar(phoneNumber.NationalNumber, phoneNumber.Iso2, isPrimaryPhone);
        var esValido = validPhone != null && validPhone.TelefonoValido;

        return OperationResult<bool>.Ok(esValido, methodName);
    }
}
