using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace WebApiFDP.Security;

public class InputRedactionLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<InputRedactionLoggingFilter> _logger;
    public InputRedactionLoggingFilter(ILogger<InputRedactionLoggingFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext ?? throw new ArgumentNullException(nameof(context), "HttpContext cannot be null");
        var method = httpContext.Request.Method;
        var codigoPersona = LoggingHelper.GetCodigoPersonaFromContext(httpContext);
        var origin = nameof(InputRedactionLoggingFilter);

        // Reutilizar correlationId si ya existe (generado en middleware), o crear uno nuevo
        if (!httpContext.Items.ContainsKey(LoggingHelper.CorrelationIdKey))
        {
            httpContext.Items[LoggingHelper.CorrelationIdKey] = Guid.NewGuid();
        }
        var correlationId = httpContext.Items.TryGetValue(LoggingHelper.CorrelationIdKey, out var storedId)
            && storedId is Guid guidValue
            ? guidValue
            : Guid.NewGuid();
        
        // Marcar que se logueó la entrada (para evitar duplicados en middleware)
        httpContext.Items[LoggingHelper.EntradaLoggedKey] = true;

        // Evitar overhead en GET/HEAD sin argumentos relevantes
        if ((method == HttpMethods.Get || method == HttpMethods.Head) && context.ActionArguments.Count == 0)
        {
            var logMessage = LoggingHelper.FormatEntrada(httpContext, origin, codigoPersona, null, correlationId);
            _logger.LogInformation("{LogMessage}", logMessage);
            await next();
            return;
        }

        var redactedArgs = new Dictionary<string, object?>();
        foreach (var kv in context.ActionArguments)
            redactedArgs[kv.Key] = ResponseRedactionHelper.Redact(kv.Value);

        var logMessageWithData = LoggingHelper.FormatEntrada(httpContext, origin, codigoPersona, redactedArgs, correlationId);
        _logger.LogInformation("{LogMessage}", logMessageWithData);

        await next();
    }
}
