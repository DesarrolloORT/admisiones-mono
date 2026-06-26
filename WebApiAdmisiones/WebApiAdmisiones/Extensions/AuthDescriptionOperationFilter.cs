using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApiAdmisiones.Extensions;

public class AuthDescriptionOperationFilter : IOperationFilter
{

    public void Apply(OpenApiOperation operation, OperationFilterContext context)

    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var hasAllowAnonymous = metadata.Any(x => x is AllowAnonymousAttribute);
        var hasAuthorize = metadata.Any(x => x is AuthorizeAttribute);

        if (hasAllowAnonymous)
        {
            operation.Summary = $"[Público] {operation.Summary}".Trim();
            operation.Description = AddAuthDescription(
                operation.Description,
                "Endpoint público. No requiere autenticación.");
            return;
        }
        if (hasAuthorize)
        {
            operation.Summary = $"[Privado] {operation.Summary}".Trim();
            operation.Description = AddAuthDescription(
                operation.Description,
                "Endpoint privado. Requiere una cookie de autenticación válida.");
        }
    }
    private static string AddAuthDescription(string? currentDescription, string authDescription)

    {
        if (string.IsNullOrWhiteSpace(currentDescription))
            return authDescription;
        return $"{authDescription}<br/><br/>{currentDescription}";
    }
}