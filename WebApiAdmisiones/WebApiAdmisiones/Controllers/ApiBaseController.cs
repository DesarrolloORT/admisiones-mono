using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    [ApiController]
    public abstract class ApiBaseController<T>(ILogger<T> logger, ICurrentUserService currentUser) : ControllerBase
    {
        protected readonly ILogger<T> _logger = logger;
        protected readonly ICurrentUserService _currentUser = currentUser;

        protected IActionResult ValidateResponse<TData>(OperationResult<TData> retorno)
        {
            var correlationId = LoggingHelper.EnsureCorrelationId(HttpContext);
            var codigoPersona = _currentUser.UserId?.ToString();
            var origin = typeof(T).Name;
            
            // Marcar que la salida será logueada por el controlador (evita duplicados en middleware)
            if (HttpContext?.Items != null)
            {
                HttpContext.Items[LoggingHelper.SalidaLoggedKey] = true;
            }

            if (retorno.Success)
            {
                // Para respuestas exitosas, loguear sin Data (solo metadatos)
                var metadataOnly = new
                {
                    retorno.Success,
                    retorno.HttpCode,
                    retorno.Method,
                    retorno.ErrorCode,
                    retorno.Message
                };

                var logMessage = LoggingHelper.FormatSalida(
                    HttpContext,
                    origin,
                    codigoPersona,
                    metadataOnly,
                    correlationId);

                _logger.LogInformation("{LogMessage}", logMessage);
                return StatusCode(retorno.HttpCode, retorno); // devolvemos el objeto original (no redactado)
            }
            else
            {
                // Para errores, loguear el OperationResult completo con Data (contiene mensaje de error)
                var logMessage = LoggingHelper.FormatSalida(
                    HttpContext,
                    origin,
                    codigoPersona,
                    ResponseRedactionHelper.Redact(retorno),
                    correlationId);

                _logger.LogWarning("{LogMessage}", logMessage);
                return StatusCode(retorno.HttpCode, retorno);
            }
        }
    }
}
