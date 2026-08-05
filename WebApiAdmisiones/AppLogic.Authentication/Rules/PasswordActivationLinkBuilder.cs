using Microsoft.Extensions.Configuration;

namespace AppLogic.Authentication.Rules;

public static class PasswordActivationLinkBuilder
{
    public static string BuildLink(IConfiguration configuration, string token, string? flow = null)
    {
        var baseUrl = configuration["AdmisionesFrontend:CrearPasswordUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Falta configurar AdmisionesFrontend:CrearPasswordUrl.");
        }

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var link = $"{baseUrl}{separator}token={Uri.EscapeDataString(token)}";
        if (!string.IsNullOrWhiteSpace(flow))
        {
            link = $"{link}&flow={Uri.EscapeDataString(flow)}";
        }

        return link;
    }
}
