using AppLogic.Interfaces;
using AppLogic.IServices;
using AppLogic.Services;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IGenericRepository;
using BusinessLogic.IServices;
using ConnectionContext;
using DataAccess;
using DataAccess.DevartRepositories;
using DataAccess.GenericAccess.Services;
using DataAccess.Services;
using LdapService.Interfaces;
using LdapService.Services;
using MailORT;
using Microsoft.EntityFrameworkCore;
using ModBandejaAppLogic.Interfaces;
using ModBandejaAppLogic.Services;
using ModBandejaDataAccess;
using ModGenericBaseDataAccess;
using WebApiAdmisiones.Security;

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
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<IUnitOfWorkFactory, EntityFrameworkUnitOfWorkFactory>();
            services.AddScoped<ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWorkFactory,
                               ModBandejaDataAccess.DevartRepositories.EntityFrameworkUnitOfWorkFactory>();
            services.AddScoped<ModGenericBaseBusinessLogic.IDevartRepositories.IUnitOfWorkFactory,
                               ModGenericBaseDataAccess.DevartRepositories.EntityFrameworkUnitOfWorkFactory>();

            // Servicios de autenticación (Core/Autenticacion).
            services.AddScoped<ILdap, Ldap>();

            // Servicios de aplicación.
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ICatalogosService, CatalogosService>();
            services.AddScoped<IInscripcionesService, InscripcionesService>();
            services.AddScoped<IPreinscripcionService, PreinscripcionService>();
            services.AddScoped<IPersonaAdmisionService, PersonaAdmisionService>();
            services.AddScoped<IBecasService, BecasService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<ITokenService, AppLogic.Services.TokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IFondoDeBecaServices, FondoDeBecaServices>();
            services.AddScoped<IBandejaService, BandejaService>();

            // Servicio de correo.
            services.AddScoped<EnvioMail>(_ =>
                new EnvioMail(configuration["SoapSettings:ServiosOffice365Url"] ?? string.Empty));

            return services;
        }
    }
}
