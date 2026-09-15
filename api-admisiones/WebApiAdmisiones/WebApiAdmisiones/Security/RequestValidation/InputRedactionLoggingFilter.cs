using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using WebApiAdmisiones.Security.Observability;

namespace WebApiAdmisiones.Security.RequestValidation;

public class InputRedactionLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<InputRedactionLoggingFilter> _logger;
    public InputRedactionLoggingFilter(ILogger<InputRedactionLoggingFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext ?? throw new ArgumentNullException(nameof(context), "HttpContext cannot be null");
        var method = httpContext.Request.Method;
        var personId = LoggingHelper.GetCodigoPersonaFromContext(httpContext);
        var origin = nameof(InputRedactionLoggingFilter);

        var correlationId = LoggingHelper.EnsureCorrelationId(httpContext);
        
        // Marcar que se logueó la entrada (para evitar duplicados en middleware)
        httpContext.Items[LoggingHelper.EntradaLoggedKey] = true;

        // Evitar overhead en GET/HEAD sin argumentos relevantes
        if ((method == HttpMethods.Get || method == HttpMethods.Head) && context.ActionArguments.Count == 0)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
            var logMessage = LoggingHelper.FormatEntrada(httpContext, origin, personId, null, correlationId);
            _logger.LogInformation("{LogMessage}", logMessage);
            }
            await next();
            return;
        }

        var redactedArgs = new Dictionary<string, object?>();
        foreach (var kv in context.ActionArguments)
            redactedArgs[kv.Key] = ResponseRedactionHelper.Redact(kv.Value);

        if (_logger.IsEnabled(LogLevel.Information))
        {
        var logMessageWithData = LoggingHelper.FormatEntrada(httpContext, origin, personId, redactedArgs, correlationId);
        _logger.LogInformation("{LogMessage}", logMessageWithData);
        }
        await next();
    }
}
