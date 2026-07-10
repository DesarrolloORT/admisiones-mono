using AppLogic.Autenticacion.Dtos;

namespace AppLogic.Autenticacion.Interfaces;

public interface ITwoFactorSessionStore
{
    Task SaveAsync(string sessionId, DtoTwoFactorSession session, TimeSpan ttl);
    Task<DtoTwoFactorSession?> GetAsync(string sessionId);
    Task<TimeSpan?> GetTtlAsync(string sessionId);
    Task UpdateAsync(string sessionId, DtoTwoFactorSession session, TimeSpan ttl);
    Task DeleteAsync(string sessionId);
}
