using LdapService.DTOs;
using LdapService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Utilities;

namespace AppLogic.Registration.Services;

/// <summary>
/// Acceso a LDAP para el alta de usuarios del registro. Cuando hay <see cref="IServiceScopeFactory"/>
/// resuelve <see cref="ILdap"/> en un scope propio, porque estas llamadas corren fuera de la
/// transacción de base y no deben quedar atadas al scope de la request. En tests el factory queda
/// en null y se usa el <see cref="ILdap"/> inyectado.
/// </summary>
public class LdapUserDirectory(ILdap ldap, IServiceScopeFactory? serviceScopeFactory = null)
{
    private readonly ILdap _ldap = ldap;
    private readonly IServiceScopeFactory? _serviceScopeFactory = serviceScopeFactory;

    public Task<bool> UserExistsAsync(string usuario) =>
        RunAsync(l => l.ExisteUsuarioLDAP(usuario));

    public Task<OperationResult<bool>> CreateUserAsync(ParamCrearUsuarioLdap request) =>
        RunAsync(l => l.CrearUsuarioAsync(request));

    public Task<OperationResult<bool>> ForcePasswordAsync(string personId, string passwordNueva) =>
        RunAsync(l => l.ForzarCambiarPasswordAsync(personId, passwordNueva));

    private async Task<T> RunAsync<T>(Func<ILdap, Task<T>> operacion)
    {
        if (_serviceScopeFactory == null)
        {
            return await operacion(_ldap);
        }

        using var scope = _serviceScopeFactory.CreateScope();
        return await operacion(scope.ServiceProvider.GetRequiredService<ILdap>());
    }
}
