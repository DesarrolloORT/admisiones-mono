using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Interceptor de Entity Framework Core para formatear los logs de comandos SQL
    /// usando el formato estándar de LoggingHelper.
    /// </summary>
    public class EfCoreLoggingInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<EfCoreLoggingInterceptor> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string LogMessageTemplate = "{LogMessage}";

        public EfCoreLoggingInterceptor(
            ILogger<EfCoreLoggingInterceptor> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Se ejecuta cuando un comando falla.
        /// </summary>
        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            LogCommandError(command, eventData);
            base.CommandFailed(command, eventData);
        }

        /// <summary>
        /// Se ejecuta cuando un comando async falla.
        /// </summary>
        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            LogCommandError(command, eventData);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        private void LogCommandError(DbCommand command, CommandErrorEventData eventData)
        {
            var context = _httpContextAccessor.HttpContext;
            var correlationId = LoggingHelper.EnsureCorrelationId(context);
            var codigoPersona = LoggingHelper.GetCodigoPersonaFromContext(context);

            var errorData = new
            {
                CommandText = TruncateCommand(command.CommandText),
                Duration = $"{eventData.Duration.TotalMilliseconds:F2}ms",
                ErrorType = eventData.Exception?.GetType().Name,
                ErrorMessage = eventData.Exception?.Message
            };

            var logMessage = LoggingHelper.FormatError(
                context,
                nameof(EfCoreLoggingInterceptor),
                codigoPersona,
                errorData,
                correlationId);

            // Loguear sin pasar la excepción para evitar stack trace duplicado
            // (el stack trace completo es innecesario aquí - el error de BD es suficiente)
            _logger.LogError(LogMessageTemplate, logMessage);
            
            // Marcar que ya se logueó el error de BD para evitar duplicados en ExceptionHandlingMiddleware
            if (context != null)
            {
                context.Items[LoggingHelper.DbErrorLoggedKey] = errorData.ErrorMessage;
            }
        }

        /// <summary>
        /// Trunca el comando SQL si es muy largo para evitar logs excesivos.
        /// </summary>
        private static string TruncateCommand(string? commandText, int maxLength = 500)
        {
            if (string.IsNullOrEmpty(commandText))
                return string.Empty;

            if (commandText.Length <= maxLength)
                return commandText;

            return commandText[..maxLength] + "... [TRUNCATED]";
        }
    }
}
