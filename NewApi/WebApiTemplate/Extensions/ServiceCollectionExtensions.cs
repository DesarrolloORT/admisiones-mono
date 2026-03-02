using Microsoft.AspNetCore.Mvc;
using Sanitization.Code;
using WebApiFDP.Security;

namespace WebApiFDP.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar servicios de la aplicación.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configura los controladores MVC con filtros globales de seguridad y validación.
        /// </summary>
        public static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add<SanitizeAttribute>();
                options.Filters.Add<InputRedactionLoggingFilter>();
                options.Filters.Add<JsonSchemaValidationFilter>();
                options.ReturnHttpNotAcceptable = true;
                options.Filters.Add(new ProducesAttribute("application/json"));
            });

            services.AddEndpointsApiExplorer();

            // Registro de validación de esquemas
            services.AddSingleton<IJsonSchemaRegistry, InMemoryJsonSchemaRegistry>();
            services.AddScoped<JsonSchemaValidationFilter>();
            services.AddScoped<InputRedactionLoggingFilter>();

            return services;
        }

        /// <summary>
        /// Configura CORS con los orígenes permitidos.
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            var allowedOrigins = new[]
            {
                "http://localhost:4200",
                "http://localhost:5001/",

                "https://gestion.ort.edu.uy",
                "https://gestionpreprod.ort.edu.uy",
                "https://gestiontesting.ort.edu.uy",
                "https://gestiondesa.ort.edu.uy",

                "https://funcionarios.ort.edu.uy",
                "https://funcionariospreprod64.ort.edu.uy",
                "https://funcionariostesting64.ort.edu.uy",
                "https://funcionariosdesa64.ort.edu.uy",
                "https://funcionarios2.ort.edu.uy"
            };

            const string corsPolicy = "AllowAngularApp";

            services.AddCors(options =>
            {
                options.AddPolicy(name: corsPolicy, policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                          //.WithHeaders("authorization", "content-type", "x-request-id", "x-token")
                          .AllowAnyHeader()
                          .DisallowCredentials();
                });
            });

            return services;
        }
    }
}
