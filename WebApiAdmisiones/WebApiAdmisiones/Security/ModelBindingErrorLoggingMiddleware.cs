using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Utilities;
using WebApiAdmisiones.Extensions;

namespace WebApiAdmisiones.Security
{
    public class ModelBindingErrorLoggingMiddleware
    {
        private const string LogMessageTemplate = "{LogMessage}";

        private readonly RequestDelegate _next;
        private readonly ILogger<ModelBindingErrorLoggingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ModelBindingErrorLoggingMiddleware(
            RequestDelegate next, 
            ILogger<ModelBindingErrorLoggingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task Invoke(HttpContext context)
        {
#pragma warning disable S2139
            var correlationId = LoggingHelper.EnsureCorrelationId(context);

            try
            {
                var originalBody = context.Response.Body;
                using var memStream = new MemoryStream();
                context.Response.Body = memStream;

                await _next(context);

                memStream.Seek(0, SeekOrigin.Begin);
                var (codigoPersona, entradaLoggedByFilter) = EnsureEntradaLogged(context, correlationId);

                if (await TryHandleBadRequestAsync(memStream, context, originalBody, codigoPersona, correlationId)) return;

                // No copiar contenido para respuestas que no permiten body (204 No Content, 304 Not Modified, etc.)
                if (!IsBodylessStatusCode(context.Response.StatusCode))
                {
                    await memStream.CopyToAsync(originalBody);
                }
                else
                {
                    context.Response.Body = originalBody;
                }
                
                // Si la entrada fue logueada por este middleware (no por el filtro), 
                // también debemos loguear la salida aquí para mantener trazabilidad
                if (!entradaLoggedByFilter && _logger.IsEnabled(LogLevel.Information))
                {
                    var logSalida = LoggingHelper.FormatSalida(
                        context,
                        nameof(ModelBindingErrorLoggingMiddleware),
                        codigoPersona,
                        $"Response StatusCode: {context.Response.StatusCode}",
                        correlationId);
                    _logger.LogInformation(LogMessageTemplate, logSalida);
                }
            }
            catch (Exception ex)
            {
                var codigoPersona = LoggingHelper.GetCodigoPersonaFromContext(context);
                
                // Si no se logueó entrada, loguear entrada aquí antes del error
                if (!context.Items.ContainsKey(LoggingHelper.EntradaLoggedKey))
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                    var logEntrada = LoggingHelper.FormatEntrada(
                        context,
                        nameof(ModelBindingErrorLoggingMiddleware),
                        codigoPersona,
                        "Request con excepción",
                        correlationId);
                    _logger.LogInformation(ex, LogMessageTemplate, logEntrada);
                    }
                    context.Items[LoggingHelper.EntradaLoggedKey] = true;
                }
                
                // No loguear nada adicional aquí - el ExceptionHandlingMiddleware se encarga del log de error.
                // Esto evita logs redundantes que no aportan información nueva.
                throw;
            }
#pragma warning restore S2139
        }

        private (string? codigoPersona, bool entradaLoggedByFilter) EnsureEntradaLogged(HttpContext context, Guid correlationId)
        {
            var codigoPersona = LoggingHelper.GetCodigoPersonaFromContext(context);
            var entradaLoggedByFilter = context.Items.ContainsKey(LoggingHelper.EntradaLoggedKey);

            if (!entradaLoggedByFilter)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                var logEntrada = LoggingHelper.FormatEntrada(
                    context,
                    nameof(ModelBindingErrorLoggingMiddleware),
                    codigoPersona,
                    "Request procesado por middleware (no llegó al filtro)",
                    correlationId);
                _logger.LogInformation(LogMessageTemplate, logEntrada);
                }
                context.Items[LoggingHelper.EntradaLoggedKey] = true;
            }

            return (codigoPersona, entradaLoggedByFilter);
        }

        private async Task<bool> TryHandleBadRequestAsync(
            MemoryStream memStream,
            HttpContext context,
            Stream originalBody,
            string? codigoPersona,
            Guid correlationId)
        {
            if (context.Response.StatusCode != StatusCodes.Status400BadRequest) return false;

            var responseBody = await new StreamReader(memStream).ReadToEndAsync();
            var salidaLoggedByController = context.Items.ContainsKey(LoggingHelper.SalidaLoggedKey);

            if (!salidaLoggedByController)
            {
                var logMessage = LoggingHelper.FormatSalida(
                    context,
                    nameof(ModelBindingErrorLoggingMiddleware),
                    codigoPersona,
                    $"DataAnnotation error response: {responseBody}",
                    correlationId);

                _logger.LogWarning(LogMessageTemplate, logMessage);
            }

            memStream.Seek(0, SeekOrigin.Begin);

            if (_environment.IsProductionLike())
                await HandleBadRequestProductionLikeAsync(responseBody, memStream, context, originalBody);
            else
                await HandleBadRequestDevelopmentAsync(responseBody, memStream, context, originalBody);

            return true;
        }

        /// <summary>
        /// Maneja respuestas 400 en ambientes Production y Preproduction.
        /// Mantiene respuestas que ya son OperationResult y reemplaza cualquier otra por un OperationResult genérico sin filtrar detalles.
        /// </summary>
        private static async Task HandleBadRequestProductionLikeAsync(string responseBody, MemoryStream memStream, HttpContext context, Stream originalBody)
        {
            bool esOperationResult = IsOperationResult(responseBody);

            if (esOperationResult)
            {
                // Devolver tal cual vino
                context.Response.Body = originalBody;
                memStream.Seek(0, SeekOrigin.Begin);
                await memStream.CopyToAsync(originalBody);
                return;
            }

            // Enmascarar (ProblemDetails u otro formato desconocido) para no exponer estructura interna.
            var opGenerico = OperationResult<object>.IsFailed("MBM_DA_01", "Invoke", "Solicitud denegada.", 400, null);
            var json = JsonSerializer.Serialize(opGenerico);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Response.Body = originalBody;
            await context.Response.Body.WriteAsync(bytes);
        }

        /// <summary>
        /// Maneja respuestas 400 en Development y Testing: sólo envuelve ProblemDetails generados por el binder.
        /// </summary>
        private async Task HandleBadRequestDevelopmentAsync(string responseBody, MemoryStream memStream, HttpContext context, Stream originalBody)
        {
            var parsed = ParseBadRequestBody(responseBody);

            if (!parsed.IsProblemDetails || parsed.IsOperationResultFormat)
            {
                context.Response.Body = originalBody;
                memStream.Seek(0, SeekOrigin.Begin);
                await memStream.CopyToAsync(originalBody);
                return;
            }

            var opResult = OperationResult<Dictionary<string, string[]>>.IsFailed(
                "MBM_DA_02",
                "Invoke",
                string.IsNullOrWhiteSpace(parsed.Title) ? "Errores de validación." : parsed.Title!,
                400,
                parsed.Errors ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

            var json = JsonSerializer.Serialize(opResult);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Response.Body = originalBody;
            await context.Response.Body.WriteAsync(bytes);
        }

        private static bool IsOperationResult(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return false;
                return root.TryGetProperty("success", out _) || root.TryGetProperty("Success", out _);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determina si el status code no permite contenido en el body de la respuesta.
        /// </summary>
        private static bool IsBodylessStatusCode(int statusCode)
        {
            // 204 No Content, 205 Reset Content, 304 Not Modified no permiten body
            return statusCode == StatusCodes.Status204NoContent
                || statusCode == StatusCodes.Status205ResetContent
                || statusCode == StatusCodes.Status304NotModified;
        }

        private sealed class ParsedBadRequest
        {
            public bool IsOperationResultFormat { get; init; }
            public bool IsProblemDetails { get; init; }
            public Dictionary<string, string[]>? Errors { get; init; }
            public string? Title { get; init; }
        }

        private ParsedBadRequest ParseBadRequestBody(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return new ParsedBadRequest { IsOperationResultFormat = false, IsProblemDetails = false };

                bool isOp = root.TryGetProperty("success", out _) || root.TryGetProperty("Success", out _);
                Dictionary<string, string[]>? errors = null;
                bool isProblem = false;
                string? title = null;

                if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                {
                    isProblem = true;
                    errors = ExtractErrors(errorsElement);
                }
                if (root.TryGetProperty("title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String)
                {
                    title = titleElement.GetString();
                }

                return new ParsedBadRequest
                {
                    IsOperationResultFormat = isOp,
                    IsProblemDetails = isProblem,
                    Errors = errors,
                    Title = title
                };
            }
            catch (Exception ex)
            {
                var correlationId = LoggingHelper.EnsureCorrelationId(null);
                var logMessage = LoggingHelper.FormatError(
                    null,
                    nameof(ModelBindingErrorLoggingMiddleware),
                    null,
                    "No se pudo parsear el cuerpo de error 400. Se devolverá sin envolver.",
                    correlationId);
                
                _logger.LogWarning(ex, "{LogMessage}", logMessage);
                return new ParsedBadRequest { IsOperationResultFormat = false, IsProblemDetails = false };
            }
        }

        private static Dictionary<string, string[]> ExtractErrors(JsonElement errorsElement)
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in errorsElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array) continue;
                var list = new List<string>();
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        list.Add(item.GetString() ?? string.Empty);
                }
                dict[prop.Name] = list.ToArray();
            }
            return dict;
        }
    }
}
