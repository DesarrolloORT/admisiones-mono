using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;
using System.Text.Json;
using Utilities;

namespace WebApiFDP.Security
{
    /// <summary>
    /// Valida encabezados Accept / Content-Type y luego (si aplica) el body JSON contra un esquema registrado.
    /// Reemplaza al antiguo ContentTypeValidationMiddleware para centralizar validaciones previas al model binding.
    /// Respuestas:
    /// 406 si Accept no permite application/json.
    /// 415 si Content-Type falta o no es application/json cuando se espera body.
    /// 400 para errores de esquema / sintaxis JSON.
    /// </summary>
    public class JsonSchemaValidationFilter : IAsyncResourceFilter
    {
        private static readonly HashSet<string> MethodsWithBody = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete
        };

        private readonly IJsonSchemaRegistry _registry;
        private readonly ILogger<JsonSchemaValidationFilter> _logger;
        public JsonSchemaValidationFilter(IJsonSchemaRegistry registry, ILogger<JsonSchemaValidationFilter> logger)
        {
            _registry = registry;
            _logger = logger;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var req = context.HttpContext.Request;

            // 1. Validar Accept (si viene). Si ninguno de los valores permite JSON -> 406
            if (!ValidateAcceptHeader(req, context))
                return;

            bool methodCanHaveBody = MethodsWithBody.Contains(req.Method);

            // 2. Si el método no puede tener body, continuar sin más validaciones
            if (!methodCanHaveBody)
            {
                await next();
                return;
            }

            // 3. Determinar si hay contenido real en el body
            // IMPORTANTE: Para DELETE sin body (e.g., DELETE /IdiomaPersona/{id}), ContentLength será 0 o null
            // No debe validarse Content-Type ni schema si no hay body
            bool bodyLikelyPresent = (req.ContentLength > 0) ||
                                     (!req.ContentLength.HasValue && req.Headers.ContainsKey("Transfer-Encoding"));

            // 4. Si no hay body obvio, continuar sin más validaciones
            if (!bodyLikelyPresent)
            {
                await next();
                return;
            }

            // 5. Solo si hay body, validar Content-Type y schema
            if (!ValidateContentType(req, context))
                return;

            // 6. Obtener esquema e intentar validar JSON
            var key = JsonSchemaRegistry.BuildKey(req.Method, req.Path);
            if (_registry.TryGet(key, out var schema) && !await ValidateJsonSchema(req, context, schema))
                return;

            await next();
        }

        private bool ValidateAcceptHeader(HttpRequest req, ResourceExecutingContext context)
        {
            if (!req.Headers.TryGetValue("Accept", out var acceptValues))
                return true; // Accept header is optional

            bool allowsJson = acceptValues.Any(v => v != null && AcceptAllowsJson(v));
            if (!allowsJson)
            {
                context.Result = new ObjectResult(
                    OperationResult<object>.IsFailed(
                        "NOT_ACCEPTABLE",
                        nameof(JsonSchemaValidationFilter),
                        "Accept no permite 'application/json'.",
                        406, null))
                { StatusCode = 406 };
                return false;
            }

            return true;
        }

        private bool ValidateContentType(HttpRequest req, ResourceExecutingContext context)
        {
            if (!req.Headers.ContainsKey("Content-Type"))
            {
                context.Result = new ObjectResult(
                    OperationResult<object>.IsFailed(
                        "UNSUPPORTED_MEDIA_TYPE",
                        nameof(JsonSchemaValidationFilter),
                        "Falta encabezado Content-Type.",
                        415, null))
                { StatusCode = 415 };
                return false;
            }

            var contentType = req.ContentType ?? string.Empty;
            if (!IsJsonContentType(contentType))
            {
                context.Result = new ObjectResult(
                    OperationResult<object>.IsFailed(
                        "UNSUPPORTED_MEDIA_TYPE",
                        nameof(JsonSchemaValidationFilter),
                        $"Content-Type '{contentType}' no soportado. Sólo 'application/json'.",
                        415, null))
                { StatusCode = 415 };
                return false;
            }

            return true;
        }

        private async Task<bool> ValidateJsonSchema(HttpRequest req, ResourceExecutingContext context, JsonSchema schema)
        {
            req.EnableBuffering();
            using var reader = new StreamReader(req.Body, leaveOpen: true);
            var raw = await reader.ReadToEndAsync();
            req.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(raw))
            {
                context.Result = new ObjectResult(
                    OperationResult<object>.IsFailed(
                        "SCHEMA_EMPTY_BODY",
                        nameof(JsonSchemaValidationFilter),
                        "Body vacío donde se esperaba JSON.",
                        400, null))
                { StatusCode = 400 };
                return false;
            }

            try
            {
                return ValidateJsonSyntaxAndSchema(context, schema, raw);
            }
            catch (JsonException jex)
            {
                context.Result = new ObjectResult(
                    OperationResult<object>.IsFailed(
                        "SCHEMA_BAD_JSON",
                        nameof(JsonSchemaValidationFilter),
                        "JSON inválido",
                        400, new { jex.Message }))
                { StatusCode = 400 };
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado validando esquema JSON para {Path}", req.Path);
                context.Result = new ObjectResult(
                    OperationResult<object>.IsFailed(
                        "SCHEMA_ERROR",
                        nameof(JsonSchemaValidationFilter),
                        "Error validando esquema",
                        400, null))
                { StatusCode = 400 };
                return false;
            }
        }

        private bool ValidateJsonSyntaxAndSchema(ResourceExecutingContext context, JsonSchema schema, string raw)
        {
            // Validación sintáctica
            JsonDocument.Parse(raw);

            // Validación semántica
            var errors = schema.Validate(raw);
            if (errors.Count > 0)
            {
                var simplified = errors.Select(e => new
                {
                    e.Path,
                    e.Kind,
                    e.Property,
                    e.LineNumber,
                    e.LinePosition,
                    Message = e.ToString()
                }).ToList();

                context.Result = new ObjectResult(
                    OperationResult<object>.IsFailed(
                        "SCHEMA_INVALID",
                        nameof(JsonSchemaValidationFilter),
                        "JSON no cumple esquema",
                        400, simplified))
                { StatusCode = 400 };
                return false;
            }

            return true;
        }

        private static bool AcceptAllowsJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return true;
            var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var media = part.Split(';', 2)[0].Trim();
                if (media.Equals("*/*", StringComparison.Ordinal) || media.Equals("application/json", StringComparison.OrdinalIgnoreCase) || media.Equals("application/*", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool IsJsonContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return false;
            if (!contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) return false;
            return contentType.Length == 16 || contentType[16] == ';';
        }
    }

    public interface IJsonSchemaRegistry
    {
        bool TryGet(string key, out JsonSchema schema);
    }

    public class InMemoryJsonSchemaRegistry : IJsonSchemaRegistry
    {
        private readonly Dictionary<string, JsonSchema> _schemas = new(StringComparer.OrdinalIgnoreCase);
        public InMemoryJsonSchemaRegistry()
        {
            LoadSchemas();
        }
        private void LoadSchemas()
        {
            var ejemploPersona = JsonSchema.FromSampleJson("{\"codigoPersona\":123}");
            ejemploPersona.RequiredProperties.Add("codigoPersona");
            _schemas[BuildKey("POST", "/api/datospersonales")] = ejemploPersona;
        }
        public bool TryGet(string key, out JsonSchema schema) => _schemas.TryGetValue(key, out schema!);
        public static string BuildKey(string method, string path) => method.ToUpperInvariant() + " " + path.ToLowerInvariant();
    }

    public static class JsonSchemaRegistry
    {
        public static string BuildKey(string method, PathString path) => method.ToUpperInvariant() + " " + path.ToString().ToLowerInvariant();
    }
}
