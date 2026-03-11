using Microsoft.AspNetCore.Http;
using System.Runtime.InteropServices.ObjectiveC;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.Json;
using Utilities;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Nombres de los sistemas que pueden realizar peticiones a la API.
    /// </summary>
    public static class SourceSystems
    {
        public const string Funcionarios = "Funcionarios";
        public const string Gestion = "Gestion";
        public const string Admisiones = "Admisiones";
        public const string Unknown = "Unknown";
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpContext? Context => _httpContextAccessor.HttpContext;

        public long? UserId => long.TryParse(
           Context?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? Context?.User?.FindFirstValue("nameid"),
           out var userId) ? userId : null;

        public string? User => Context?.User?.FindFirstValue("usuario");

        public string? SessionId => Context?.User?.FindFirstValue("sesion");

        public string? Email => Context?.User.FindFirstValue(ClaimTypes.Email);
        public string? IpAddress =>
            Context?.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? Context?.Connection?.RemoteIpAddress?.ToString();
        public string? UserAgent => Context?.Request.Headers.UserAgent.FirstOrDefault();
        public string? HostName => Context?.Request.Headers["X-Client-Host"].FirstOrDefault();

        /// <summary>
        /// Obtiene el nombre del sistema de origen basado en el claim 'iss' (issuer) del token JWT.
        /// </summary>
        public string SourceSystem => GetSourceSystem();

        /// <summary>
        /// Determina el sistema de origen a partir del claim 'iss' del JWT.
        /// </summary>
        /// <returns>Nombre del sistema: Funcionarios, Gestion, Admisiones o Unknown.</returns>
        public string GetSourceSystem()
        {
            var issuerClaim = Context?.User?.FindFirstValue("iss");

            return issuerClaim switch
            {
                "https://funcionarios.ort.edu.uy" => SourceSystems.Funcionarios,
                "https://gestion.ort.edu.uy" => SourceSystems.Gestion,
                "https://admisiones.ort.edu.uy" => SourceSystems.Admisiones,
                _ => SourceSystems.Unknown
            };
        }



        public RequestMetadata GetMetadata() => new(
            UserId,
            User,
            SessionId,
            Email,
            IpAddress,
            UserAgent,
            HostName
        );

        public long GetUserId()
        {
            if (UserId.HasValue)
            {
                return UserId.Value;
            }
            throw new UnauthorizedAccessException("UserId is not available.");
        }


        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public string? GetLogString<T>(string entradaSalida, OperationResult<T>? retorno)
        {


            string strResult = "CódigoPersona: ";
            strResult += UserId.HasValue ? UserId.Value.ToString() : "Desconocido";
            if (Context is not null)
            {
                strResult += string.Concat(" | Método: ", Context.Request.Path.ToString().AsSpan(1, Context.Request.Path.ToString().Length-1));
                strResult += "-" + entradaSalida;
            }
            else
            {
                strResult += " | Método: Desconocido";
            }
            if (retorno is not null)
            {
                strResult += " | Resultado: ";
                strResult += JsonSerializer.Serialize(retorno, JsonOptions);
            }
            else
            {
                strResult += " | Resultado: Desconocido";
            }

            return strResult;
        }


    }

    public interface ICurrentUserService
    {
        long? UserId { get; }
        string? User { get; }
        string? SessionId { get; }
        string? Email { get; }
        string? IpAddress { get; }
        string? UserAgent { get; }
        string? HostName { get; }
        string SourceSystem { get; }

        RequestMetadata GetMetadata();
        long GetUserId();
        string GetSourceSystem();
        string? GetLogString<T>(string entradaSalida, OperationResult<T>? retorno);
    }

    public record RequestMetadata(
    long? UserId,
    string? User,
    string? SessionId,
    string? Email,
    string? IpAddress,
    string? UserAgent,
    string? HostName
);

}
