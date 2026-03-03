using AppLogic.Interfaces;
using AppLogic.Services;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IGenericRepository;
using ConnectionContext;
using DataAccess;
using DataAccess.DevartRepositories;
using DataAccess.GenericAccess.Services;
using MailORT;
using Microsoft.EntityFrameworkCore;
using ModBandejaAppLogic.Interfaces;
using ModBandejaAppLogic.Services;
using ModBandejaDataAccess;
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
            // Contexto de conexión desde JWT
            services.AddScoped<ConnectionFromBearerToken>();

            services.AddScoped<IDbConnectionContext>(sp =>
            {
                var connectionFromToken = sp.GetRequiredService<ConnectionFromBearerToken>();
                var connectionString = connectionFromToken.GetConnectionString();
                return new DbConnectionContext(connectionString);
            });

            services.AddScoped<DbConnectionContext>(sp =>
                (DbConnectionContext)sp.GetRequiredService<IDbConnectionContext>());

            // DbContexts con configuración por ambiente
            // Production y Preproduction NO habilitan logging sensible de BD
            services.AddDbContext<ModelContext>((sp, options) =>
            {
                var dbConnectionContext = sp.GetRequiredService<IDbConnectionContext>();
                options.UseOracle((System.Data.Common.DbConnection)dbConnectionContext.Connection);
                
                if (!environment.IsProductionLike())
                {
                    options.LogTo(Console.WriteLine,
                                 new[] { DbLoggerCategory.Database.Command.Name },
                                 LogLevel.Information)
                           .EnableSensitiveDataLogging()
                           .EnableDetailedErrors();
                }
            });

            services.AddDbContext<BandejaModelContext>((sp, options) =>
            {
                var dbConnectionContext = sp.GetRequiredService<IDbConnectionContext>();
                options.UseOracle((System.Data.Common.DbConnection)dbConnectionContext.Connection);
            });

            // Repositorios y UoW
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<IUnitOfWorkFactory, EntityFrameworkUnitOfWorkFactory>();
            services.AddScoped<ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWorkFactory,
                               ModBandejaDataAccess.DevartRepositories.EntityFrameworkUnitOfWorkFactory>();

            // Servicios de aplicación
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IGeneralServices, GeneralServices>();
            services.AddScoped<IBandejaService, BandejaService>();
            services.AddScoped<IProcesoComienzoServices, ProcesoComienzoServices>();

            // Servicio de correo
            services.AddScoped<EnvioMail>(_ =>
                new EnvioMail(configuration["SoapSettings:ServiosOffice365Url"] ?? string.Empty));

            return services;
        }
    }
}
