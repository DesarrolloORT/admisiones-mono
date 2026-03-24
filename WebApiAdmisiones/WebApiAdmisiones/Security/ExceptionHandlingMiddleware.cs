using Sanitization.Code;
using System.Security.Claims;
using Utilities;
using WebApiAdmisiones.Extensions;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Middleware para el manejo global de excepciones en la aplicación.
    /// Captura excepciones, registra los errores y devuelve respuestas JSON estandarizadas.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ExceptionHandlingMiddleware"/>.
        /// </summary>
        /// <param name="next">El siguiente delegado en la canalización de solicitudes HTTP.</param>
        /// <param name="logger">El registrador para registrar errores.</param>
        /// <param name="env">El entorno de alojamiento de la aplicación.</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Invoca el middleware para procesar la solicitud HTTP y manejar excepciones.
        /// </summary>
        /// <param name="context">El contexto HTTP actual.</param>
        /// <returns>Una tarea que representa la operación asincrónica.</returns>
        public async Task Invoke(HttpContext context)
        {
            var correlationId = LoggingHelper.EnsureCorrelationId(context);

            try
            {
                await _next(context);
            }
            catch (InputSanitizationException e)
            {
                await HandleSanitizationExceptionAsync(context, e, correlationId);
            }
            catch (Exception ex)
            {
                await HandleGeneralExceptionAsync(context, ex, correlationId);
            }
        }

        private async Task HandleSanitizationExceptionAsync(HttpContext context, InputSanitizationException e, Guid correlationId)
        {
                var codigoPersona = LoggingHelper.GetCodigoPersonaFromContext(context);
                var logMessage = LoggingHelper.FormatError(
                    context,
                    nameof(ExceptionHandlingMiddleware),
                    codigoPersona,
                "Error de sanitización de entrada",
                correlationId);

                _logger.LogError(e, "{LogMessage}", logMessage);

            await WriteErrorResponseAsync(context, "SANITIZATION_ERROR", StatusCodes.Status409Conflict, e.Message);
        }

        private async Task HandleGeneralExceptionAsync(HttpContext context, Exception ex, Guid correlationId)
                {
            var codigoPersona = LoggingHelper.GetCodigoPersonaFromContext(context);
            
            LogGeneralException(context, ex, codigoPersona, correlationId);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "The response has already started, so the error response could not be written.");
                return;
                }

            await WriteErrorResponseAsync(context, "INTERNAL_ERROR", StatusCodes.Status500InternalServerError, ex.Message);
            }

        private void LogGeneralException(HttpContext context, Exception ex, string? codigoPersona, Guid correlationId)
            {
            var dbErrorAlreadyLogged = context.Items.TryGetValue(LoggingHelper.DbErrorLoggedKey, out var dbErrorMsg);
            
            var errorData = dbErrorAlreadyLogged
                ? CreateDbErrorSummary(ex, dbErrorMsg)
                : CreateFullErrorData(ex);
            
                var logMessage = LoggingHelper.FormatError(
                    context,
                    nameof(ExceptionHandlingMiddleware),
                    codigoPersona,
                errorData,
                correlationId);

                _logger.LogError(ex, "{LogMessage}", logMessage);
        }

        private static object CreateDbErrorSummary(Exception ex, object? dbErrorMsg) => new
        {
            ExceptionType = ex.GetType().Name,
            Referencia = "Ver log anterior de EfCoreLoggingInterceptor para detalles del comando SQL",
            DbError = dbErrorMsg?.ToString()
        };

        private static object CreateFullErrorData(Exception ex) => new
                {
            ExceptionType = ex.GetType().Name,
            Message = GetFullExceptionMessage(ex),
            StackTrace = GetRelevantStackTrace(ex)
        };

        private async Task WriteErrorResponseAsync(HttpContext context, string errorCode, int statusCode, string? exceptionMessage)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

                    context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var result = OperationResult<string>.IsFailed(
                errorCode,
                nameof(ExceptionHandlingMiddleware),
                "Error inesperado",
                StatusCodes.Status500InternalServerError,
                !_env.IsProductionLike() ? exceptionMessage : null);

                    await context.Response.WriteAsJsonAsync(result);
                }

        /// <summary>
        /// Obtiene el mensaje completo incluyendo inner exceptions.
        /// </summary>
        private static string GetFullExceptionMessage(Exception ex)
                {
            var messages = new List<string> { ex.Message };
            var inner = ex.InnerException;
            
            while (inner != null)
            {
                messages.Add(inner.Message);
                inner = inner.InnerException;
                }
            
            return string.Join(" -> ", messages);
            }

        /// <summary>
        /// Obtiene solo las líneas del stack trace relevantes al proyecto.
        /// </summary>
        private static string GetRelevantStackTrace(Exception ex)
        {
            if (string.IsNullOrEmpty(ex.StackTrace))
                return string.Empty;

            var relevantLines = ex.StackTrace
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Contains("AppLogic") || 
                               line.Contains("WebApiEmpleos") || 
                               line.Contains("DataAccess"))
                .Take(5);

            return string.Join(" | ", relevantLines.Select(l => l.Trim()));
        }
    }
}
