using AppLogic.Authentication.DependencyInjection;
using AppLogic.Enrollments.DependencyInjection;
using AppLogic.Identity.DependencyInjection;
using AppLogic.Integrations.Tivenos.DependencyInjection;
using AppLogic.People.DependencyInjection;
using AppLogic.Registration.DependencyInjection;
using AppLogic.Scholarships.DependencyInjection;
using AppLogic.Platform.Email;
using AzureService.Interfaces;
using AzureService.Services;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IServices;
using ConnectionContext;
using DataAccess;
using DataAccess.DevartRepositories;
using DataAccess.Services;
using LdapService.Interfaces;
using LdapService.Services;
using MailORT;
using Microsoft.EntityFrameworkCore;
using ModBandejaAppLogic.Interfaces;
using ModBandejaAppLogic.Services;
using ModBandejaDataAccess;
using ModGenericBaseDataAccess;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Captcha;
using WebApiAdmisiones.Security.Cache;
using WebApiAdmisiones.Security.Observability;
using AppLogic.Catalogs.Services;
using AppLogic.Catalogs.Interfaces;

namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar los servicios de dominio y acceso a datos.
    /// </summary>
    public static class DomainServicesExtensions
    {
        /// <summary>
        /// Registra los servicios de dominio, repositorios y contextos de base de datos.
        /// </summary>
        public static IServiceCollection AddDomainServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            // Conexión a partir del environment.
            services.AddScoped<IDbConnectionContext>(sp =>
            {
                string? connectionString = Environment.GetEnvironmentVariable("OracleConnectionStringAdmisiones");
                return new DbConnectionContext(connectionString);
            });

            services.AddScoped<DbConnectionContext>(sp =>
                (DbConnectionContext)sp.GetRequiredService<IDbConnectionContext>());

            // Interceptor de EF Core para logging estandarizado.
            services.AddScoped<EfCoreLoggingInterceptor>();

            // DbContexts con configuración por ambiente.
            // Production y Preproduction no habilitan logging sensible de BD.
            services.AddDbContext<ModelContext>((sp, options) =>
            {
                var dbConnectionContext = sp.GetRequiredService<IDbConnectionContext>();
                var efCoreInterceptor = sp.GetRequiredService<EfCoreLoggingInterceptor>();
                options.UseOracle((System.Data.Common.DbConnection)dbConnectionContext.Connection)
                       .AddInterceptors(efCoreInterceptor);
                if (!environment.IsProductionLike())
                {
                    options.EnableSensitiveDataLogging()
                           .EnableDetailedErrors();
                }
            });

            services.AddDbContext<BandejaModelContext>((sp, options) =>
            {
                var dbConnectionContext = sp.GetRequiredService<IDbConnectionContext>();
                var efCoreInterceptor = sp.GetRequiredService<EfCoreLoggingInterceptor>();

                options.UseOracle((System.Data.Common.DbConnection)dbConnectionContext.Connection)
                       .AddInterceptors(efCoreInterceptor);
            });

            services.AddDbContext<GenericModelContext>((sp, options) =>
            {
                var dbConnectionContext = sp.GetRequiredService<IDbConnectionContext>();
                var efCoreInterceptor = sp.GetRequiredService<EfCoreLoggingInterceptor>();

                options.UseOracle((System.Data.Common.DbConnection)dbConnectionContext.Connection)
                       .AddInterceptors(efCoreInterceptor);
            });

            // Repositorios y UoW.
            services.AddScoped<IUnitOfWorkFactory, EntityFrameworkUnitOfWorkFactory>();
            services.AddScoped<ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWorkFactory,
                               ModBandejaDataAccess.DevartRepositories.EntityFrameworkUnitOfWorkFactory>();
            services.AddScoped<ModGenericBaseBusinessLogic.IDevartRepositories.IUnitOfWorkFactory,
                               ModGenericBaseDataAccess.DevartRepositories.EntityFrameworkUnitOfWorkFactory>();

            // Servicios de autenticación (Core/Autenticacion).
            services.AddScoped<ILdap, Ldap>();

            // Cada módulo de AppLogic registra sus propios servicios: qué clase implementa cada
            // contrato es asunto del módulo, no del host.
            services.AddIdentityModule();
            services.AddTivenosIntegration();
            services.AddPeople();
            services.AddAuthenticationModule();
            services.AddScholarships();
            services.AddRegistration();
            services.AddEnrollments();

            // Servicios que dependen de infraestructura del host y por eso no pueden vivir en el módulo.
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<CatalogService>();
            services.AddScoped<ICatalogService>(sp => new CatalogCacheDecorator(
                sp.GetRequiredService<CatalogService>(),
                sp.GetRequiredService<IRedisCacheService>(),
                sp.GetRequiredService<IConfiguration>()));
            services.AddHttpClient<IReconocimientoDocumento, ReconocimientoDocumento>(client => client.Timeout = TimeSpan.FromSeconds(45));
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddHttpClient<IRecaptchaService, RecaptchaService>();
            services.AddScoped<IBandejaService, BandejaService>();

            // Servicio de correo.
            services.AddScoped<EnvioMail>(_ =>
                new EnvioMail(configuration["SoapSettings:ServiosOffice365Url"] ?? string.Empty));

            // Abstracción de mail: la implementación necesita EnvioMail, que se arma con configuración del host.
            services.AddScoped<IEmailSender, OrtEmailSender>();

            return services;
        }
    }
}
