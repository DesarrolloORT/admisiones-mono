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
using System.Web.Services.Description;
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
            services.AddScoped<IDbConnectionContext>(sp =>
            {
                string? connectionString = Environment.GetEnvironmentVariable("OracleConnectionStringAdmisiones");
                return new DbConnectionContext(connectionString);
            });

            services.AddScoped<DbConnectionContext>(sp =>
                (DbConnectionContext)sp.GetRequiredService<IDbConnectionContext>());

            // Interceptor de EF Core para logging estandarizado
            services.AddScoped<EfCoreLoggingInterceptor>();

            // DbContexts con configuración por ambiente
            // Production y Preproduction NO habilitan logging sensible de BD
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
                options.UseOracle((System.Data.Common.DbConnection)dbConnectionContext.Connection);
            });

            services.AddDbContext<ModGenericBaseDataAccess.GenericModelContext>((sp, options) =>
            {
                var dbConnectionContext = sp.GetRequiredService<IDbConnectionContext>();
                options.UseOracle((System.Data.Common.DbConnection)dbConnectionContext.Connection);
            });

            // Repositorios y UoW
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<IUnitOfWorkFactory, EntityFrameworkUnitOfWorkFactory>();
            services.AddScoped<ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWorkFactory,
                               ModBandejaDataAccess.DevartRepositories.EntityFrameworkUnitOfWorkFactory>();
            services.AddScoped<ModGenericBaseBusinessLogic.IDevartRepositories.IUnitOfWorkFactory,
                               ModGenericBaseDataAccess.DevartRepositories.EntityFrameworkUnitOfWorkFactory>();

            // Autenticación LDAP
            services.AddScoped<ILdap, Ldap>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IAuthService, AuthService>();

            // Servicios de aplicación
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IGeneralServices, GeneralServices>();
            services.AddScoped<IFondoDeBecaServices, FondoDeBecaServices>();
            services.AddScoped<IBandejaService, BandejaService>();
            services.AddScoped<IProcesoComienzoServices, ProcesoComienzoServices>();

            // Servicio de correo
            services.AddScoped<EnvioMail>(_ =>
                new EnvioMail(configuration["SoapSettings:ServiosOffice365Url"] ?? string.Empty));

            return services;
        }
    }
}
