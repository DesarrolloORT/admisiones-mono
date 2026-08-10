using AppLogic.Contracts.Dtos;
using AppLogic.Contracts.Text;
using AppLogic.People.Contracts;
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

        var validPhone = PhoneNormalization.Validate(phoneNumber.NationalNumber, isPrimaryPhone, phoneNumber.Iso2);
        var esValido = validPhone != null && validPhone.TelefonoValido;

        return OperationResult<bool>.Ok(esValido, methodName);
    }
}
