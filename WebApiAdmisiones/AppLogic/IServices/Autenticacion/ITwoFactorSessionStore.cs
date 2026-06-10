using AppLogic.DTOs;

namespace AppLogic.IServices.Autenticacion;

public interface ITwoFactorSessionStore
{
    Task SaveAsync(string sessionId, TwoFactorSession session, TimeSpan ttl);
    Task<TwoFactorSession?> GetAsync(string sessionId);
    Task<TimeSpan?> GetTtlAsync(string sessionId);
    Task UpdateAsync(string sessionId, TwoFactorSession session, TimeSpan ttl);
    Task DeleteAsync(string sessionId);
}
