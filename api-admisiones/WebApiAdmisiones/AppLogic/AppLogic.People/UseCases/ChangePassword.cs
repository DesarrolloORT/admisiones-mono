using AppLogic.Contracts;
using AppLogic.People.Contracts;
using AppLogic.People.Dtos;
using LdapService.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Utilities;

namespace AppLogic.People.UseCases;

/// <summary>
/// Único caso de uso de People que necesita LDAP: la contraseña vive ahí, no en la base
/// de admisiones.
/// </summary>
public class ChangePassword(ILdap ldap, ILogger<ChangePassword> logger) : IChangePassword
{
    private readonly ILdap _ldap = ldap;
    private readonly ILogger<ChangePassword> _logger = logger;

    public async Task<OperationResult<object>> ExecuteAsync(long personId, ChangePasswordRequest request)
    {
        const string methodName = nameof(ChangePassword);

        try
        {
            if (request == null)
            {
                return OperationResult<object>.IsFailed("CAM_PAS_01", methodName, "La solicitud es obligatoria.", 400);
            }

            var passwordValidation = Util.ValidarPassword(request.CurrentPassword, request.NewPassword);
            if (!string.IsNullOrWhiteSpace(passwordValidation))
            {
                return OperationResult<object>.IsFailed("CAM_PAS_02", methodName, passwordValidation, 400);
            }

            var passwordChange = await _ldap.CambiarPasswordAsync(
                personId.ToString(CultureInfo.InvariantCulture),
                request.CurrentPassword,
                request.NewPassword);

            if (!passwordChange.Success)
                return passwordChange.Failure().As<object>(methodName);

            return OperationResult<object>.Ok("Se actualizó tu contraseña", methodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en {Metodo}", methodName);
            return OperationResult<object>.IsFailed("CAM_PAS_99", methodName, "Error al cambiar contraseña.", 500, default!);
        }
    }
}
