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
            try
            {
                await _next(context);
            }
            catch (InputSanitizationException e)
            {
                var codigoPersona = LoggingHelper.GetCodigoPersonaFromContext(context);
                var logMessage = LoggingHelper.FormatError(
                    context,
                    nameof(ExceptionHandlingMiddleware),
                    codigoPersona,
                    "Error de sanitización de entrada");

                _logger.LogError(e, "{LogMessage}", logMessage);

                if (!context.Response.HasStarted)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status409Conflict;

                    var result = OperationResult<string>.IsFailed("SANITIZATION_ERROR", nameof(ExceptionHandlingMiddleware),
                                 "Error inesperado", StatusCodes.Status500InternalServerError, !_env.IsProductionLike() ? e.Message : null);
                    await context.Response.WriteAsJsonAsync(result);
                }
            }
            catch (Exception ex)
            {
                var codigoPersona = LoggingHelper.GetCodigoPersonaFromContext(context);
                var logMessage = LoggingHelper.FormatError(
                    context,
                    nameof(ExceptionHandlingMiddleware),
                    codigoPersona,
                    "Error inesperado");

                _logger.LogError(ex, "{LogMessage}", logMessage);

                if (!context.Response.HasStarted)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    var result = OperationResult<string>.IsFailed("INTERNAL_ERROR", nameof(ExceptionHandlingMiddleware),
                                 "Error inesperado", StatusCodes.Status500InternalServerError, !_env.IsProductionLike() ? ex.Message : null);
                    await context.Response.WriteAsJsonAsync(result);
                }
                else
                {
                    _logger.LogWarning("The response has already started, so the error response could not be written.");
                }
            }
        }
    }
}
