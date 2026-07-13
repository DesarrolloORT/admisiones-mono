using System.Diagnostics;

namespace WebApiAdmisiones.Security.Observability;

public static class ClientTelemetryHeaders
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string ClientSessionId = "X-Client-Session-Id";
    public const string ClientRoute = "X-Client-Route";
    public const string ClientRouteHistory = "X-Client-Route-History";
    public const string ClientDevice = "X-Client-Device";
    public const string ClientEnvironment = "X-Client-Environment";
    public const string ClientService = "X-Client-Service";
    public const string ClientVersion = "X-Client-Version";
    public const string TestRunId = "X-Test-Run-Id";

    private const string Unknown = "unknown";
    private const int MaxLabelLength = 120;
    private const int MaxTagLength = 512;
    private static readonly char[] RouteValueSeparators = ['?', '#'];

    public static string MetricLabel(HttpContext context, string headerName) =>
        HeaderValue(context.Request.Headers[headerName].FirstOrDefault(), Unknown, MaxLabelLength);

    public static string ClientRouteMetricLabel(HttpContext context)
    {
        var route = RouteValue(context.Request.Headers[ClientRoute].FirstOrDefault(), MaxLabelLength);
        return string.IsNullOrWhiteSpace(route) ? Unknown : route;
    }

    public static string ClientDeviceMetricLabel(HttpContext context)
    {
        var device = MetricLabel(context, ClientDevice);
        var detailsStart = device.IndexOf(';');

        return detailsStart >= 0 ? device[..detailsStart].Trim() : device;
    }

    public static bool HasClientTelemetry(HttpContext? context)
    {
        if (context?.Request is null)
        {
            return false;
        }

        return context.Request.Headers.ContainsKey(ClientService)
            || context.Request.Headers.ContainsKey(ClientRoute)
            || context.Request.Headers.ContainsKey(ClientEnvironment)
            || context.Request.Headers.ContainsKey(TestRunId);
    }

    public static string LogValue(HttpContext context, string headerName) =>
        HeaderValue(context.Request.Headers[headerName].FirstOrDefault(), Unknown, MaxTagLength);

    public static string ClientRouteLogValue(HttpContext context)
    {
        var route = RouteValue(context.Request.Headers[ClientRoute].FirstOrDefault(), MaxTagLength);
        return string.IsNullOrWhiteSpace(route) ? Unknown : route;
    }

    public static string ClientRouteHistoryLogValue(HttpContext context)
    {
        var routeHistory = RouteHistoryValue(context.Request.Headers[ClientRouteHistory].FirstOrDefault());
        return string.IsNullOrWhiteSpace(routeHistory) ? Unknown : routeHistory;
    }

    public static void EnrichActivity(Activity activity, HttpRequest request)
    {
        SetTag(activity, request, CorrelationId, "correlation_id");
        SetTag(activity, request, ClientSessionId, "client.session_id");
        SetTag(activity, "client.route", RouteValue(request.Headers[ClientRoute].FirstOrDefault(), MaxTagLength));
        SetTag(activity, "client.route_history", RouteHistoryValue(request.Headers[ClientRouteHistory].FirstOrDefault()));
        SetTag(activity, request, ClientDevice, "client.device");
        SetTag(activity, request, ClientEnvironment, "client.environment");
        SetTag(activity, request, ClientService, "client.service");
        SetTag(activity, request, ClientVersion, "client.version");
        SetTag(activity, request, TestRunId, "test.run_id");
    }

    private static void SetTag(Activity activity, HttpRequest request, string headerName, string tagName)
    {
        var value = HeaderValue(request.Headers[headerName].FirstOrDefault(), string.Empty, MaxTagLength);
        SetTag(activity, tagName, value);
    }

    private static void SetTag(Activity activity, string tagName, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            activity.SetTag(tagName, value);
        }
    }

    private static string HeaderValue(string? value, string fallback, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var sanitized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }

    private static string RouteValue(string? value, int maxLength)
    {
        var route = HeaderValue(value, string.Empty, maxLength);
        var metadataStart = route.IndexOfAny(RouteValueSeparators);

        return metadataStart >= 0 ? route[..metadataStart] : route;
    }

    private static string RouteHistoryValue(string? value)
    {
        var routeHistory = HeaderValue(value, string.Empty, MaxTagLength);
        if (string.IsNullOrWhiteSpace(routeHistory))
        {
            return string.Empty;
        }

        var routes = routeHistory
            .Split(" > ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(route => RouteValue(route, MaxLabelLength));

        return string.Join(" > ", routes);
    }
}
