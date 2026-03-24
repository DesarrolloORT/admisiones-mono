using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace WebApiFDP.Security
{
    /// <summary>
    /// Helper centralizado para generar mensajes de log estandarizados.
    /// Formato: Tipo | Origen (VERBO-UltimaParte) | Clase | CodigoPersona | Servicio | Datos | IP | UA | CorrelationId
    /// Ejemplo de Origen: "GET-ConfirmarDeclaracion3100", "POST-CrearPersona"
    /// </summary>
    public static class LoggingHelper
    {
        private const string Desconocido = "Desconocido";
        
        /// <summary>
        /// Clave para almacenar el CorrelationId en HttpContext.Items.
        /// </summary>
        public const string CorrelationIdKey = "CorrelationId";
        
        /// <summary>
        /// Clave para indicar que ya se logueó la entrada del request.
        /// </summary>
        public const string EntradaLoggedKey = "EntradaLogged";
        
        /// <summary>
        /// Clave para indicar que ya se logueó la salida del request (evita duplicados en middleware).
        /// </summary>
        public const string SalidaLoggedKey = "SalidaLogged";
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Genera un mensaje de log estructurado para entrada de peticiones.
        /// </summary>
        /// <param name="context">Contexto HTTP actual.</param>
        /// <param name="origin">Origen del log (nombre de clase/middleware).</param>
        /// <param name="codigoPersona">Código de la persona autenticada (opcional).</param>
        /// <param name="data">Datos adicionales a loguear (se redactarán automáticamente).</param>
        /// <param name="correlationId">ID de correlación (opcional).</param>
        /// <returns>Mensaje de log formateado.</returns>
        public static string FormatEntrada(
            HttpContext? context,
            string origin,
            string? codigoPersona = null,
            object? data = null,
            Guid? correlationId = null)
        {
            return FormatLog("ENTRADA", context, origin, codigoPersona, data, correlationId);
        }

        /// <summary>
        /// Genera un mensaje de log estructurado para salida de peticiones.
        /// </summary>
        /// <param name="context">Contexto HTTP actual.</param>
        /// <param name="origin">Origen del log (nombre de clase/middleware).</param>
        /// <param name="codigoPersona">Código de la persona autenticada (opcional).</param>
        /// <param name="data">Datos adicionales a loguear (se redactarán automáticamente).</param>
        /// <param name="correlationId">ID de correlación (opcional).</param>
        /// <returns>Mensaje de log formateado.</returns>
        public static string FormatSalida(
            HttpContext? context,
            string origin,
            string? codigoPersona = null,
            object? data = null,
            Guid? correlationId = null)
        {
            return FormatLog("SALIDA", context, origin, codigoPersona, data, correlationId);
        }

        /// <summary>
        /// Genera un mensaje de log estructurado para excepciones o errores.
        /// </summary>
        /// <param name="context">Contexto HTTP actual.</param>
        /// <param name="origin">Origen del log (nombre de clase/middleware).</param>
        /// <param name="codigoPersona">Código de la persona autenticada (opcional).</param>
        /// <param name="data">Datos adicionales a loguear.</param>
        /// <param name="correlationId">ID de correlación (opcional).</param>
        /// <returns>Mensaje de log formateado.</returns>
        public static string FormatError(
            HttpContext? context,
            string origin,
            string? codigoPersona = null,
            object? data = null,
            Guid? correlationId = null)
        {
            return FormatLog("ERROR", context, origin, codigoPersona, data, correlationId);
        }

        private static string FormatLog(
            string tipo,
            HttpContext? context,
            string origin,
            string? codigoPersona,
            object? data,
            Guid? correlationId)
        {
            var parts = new List<string>
            {
                $"Tipo: {tipo}",
                $"Origen: {GetFormattedOrigin(context)}",
                $"Clase: {origin}",
                $"CodigoPersona: {codigoPersona ?? Desconocido}",
                $"Servicio: {GetServicePath(context)}",
                $"Datos: {SerializeData(data)}",
                $"IP: {GetIpAddress(context)}",
                $"UA: {GetUserAgent(context)}"
            };

            if (correlationId.HasValue)
            {
                parts.Add($"CorrelationId: {correlationId.Value}");
            }

            return string.Join(" | ", parts);
        }

        private static string GetServicePath(HttpContext? context)
        {
            if (context?.Request?.Path == null)
                return Desconocido;

            var path = context.Request.Path.ToString();
            return string.IsNullOrWhiteSpace(path) ? Desconocido : path.TrimStart('/');
        }

        /// <summary>
        /// Genera el origen formateado con el verbo HTTP y la última parte del path.
        /// Ejemplo: "GET-ConfirmarDeclaracion3100" en vez de "Declaracion3100/ConfirmarDeclaracion3100"
        /// </summary>
        private static string GetFormattedOrigin(HttpContext? context)
        {
            if (context?.Request == null)
                return Desconocido;

            var httpMethod = context.Request.Method ?? Desconocido;
            var path = context.Request.Path.ToString();

            if (string.IsNullOrWhiteSpace(path))
                return $"{httpMethod}-{Desconocido}";

            // Obtener la última parte del path (después del último '/')
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var lastSegment = segments.Length > 0 ? segments[^1] : Desconocido;

            return $"{httpMethod}-{lastSegment}";
        }

        private static string GetIpAddress(HttpContext? context)
        {
            return context?.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                   ?? context?.Connection?.RemoteIpAddress?.ToString()
                   ?? Desconocido;
        }

        private static string GetUserAgent(HttpContext? context)
        {
            return context?.Request.Headers.UserAgent.FirstOrDefault() ?? Desconocido;
        }

        private static string SerializeData(object? data)
        {
            if (data == null)
                return "null";

            try
            {
                // Si ya es un string, devolverlo directamente
                if (data is string str)
                    return str;

                // Si es un tipo primitivo, convertir a string
                if (data.GetType().IsPrimitive || data is decimal || data is DateTime || data is Guid)
                    return data.ToString() ?? "null";

                // Para objetos complejos, serializar a JSON
                return JsonSerializer.Serialize(data, JsonOptions);
            }
            catch
            {
                return data.ToString() ?? "null";
            }
        }

        /// <summary>
        /// Extrae el código de persona del contexto HTTP desde los claims.
        /// </summary>
        public static string? GetCodigoPersonaFromContext(HttpContext? context)
        {
            if (context?.User?.Identity?.IsAuthenticated != true)
                return null;

            return context.User.Claims
                .FirstOrDefault(c => c.Type == "usuario" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "nameid")?.Value;
        }

        /// <summary>
        /// Extrae información de contexto de la petición para logs de excepción.
        /// </summary>
        public static string GetRequestContextInfo(HttpContext? context)
        {
            var codigoPersona = GetCodigoPersonaFromContext(context);
            var ip = GetIpAddress(context);
            var userAgent = GetUserAgent(context);
            var hostName = context?.Request.Headers["X-Client-Host"].FirstOrDefault() ?? Desconocido;

            return $"CodigoPersona: {codigoPersona ?? Desconocido}, IP: {ip}, UserAgent: {userAgent}, HostName: {hostName}";
        }
    }
}
