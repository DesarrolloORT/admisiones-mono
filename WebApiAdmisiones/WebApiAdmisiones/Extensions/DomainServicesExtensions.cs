using AppLogic.IServices;
using AzureService.Interfaces;
using AzureService.Services;
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
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Captcha;
using WebApiAdmisiones.Security.Observability;
using AppLogic.Services.Autenticacion;
using AppLogic.Services.Registro;
using AppLogic.Services.Personas;
using AppLogic.Services.Inscripciones;
using AppLogic.Services.Becas;
using AppLogic.Services.Catalogos;
using AppLogic.IServices.Autenticacion;
using AppLogic.IServices.Becas;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Inscripciones;
using AppLogic.IServices.Personas;
using AppLogic.IServices.Registro;

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
            services.AddScoped<IGeneralService, GeneralService>();
            services.AddScoped<ICatalogosService, CatalogosService>();
            services.AddScoped<IRegistroService, RegistroService>();
            services.AddScoped<IInscripcionesService, InscripcionesService>();
            services.AddScoped<IPersonaService, PersonaService>();
            services.AddScoped<IBecasService, BecasService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPasswordActivationService, PasswordActivationService>();
            services.AddScoped<IHashTokenStore, RedisHashTokenStore>();
            services.AddScoped<IRegistroFlowService, RegistroFlowService>();
            services.AddScoped<IRegistroDocumentoImagenCacheService, RegistroDocumentoImagenCacheService>();
            services.AddHttpClient<IReconocimientoDocumento, ReconocimientoDocumento>(client => client.Timeout = TimeSpan.FromSeconds(45));
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddHttpClient<IRecaptchaService, RecaptchaService>();
            services.AddScoped<IFondoDeBecaServices, FondoDeBecaService>();
            services.AddScoped<IBandejaService, BandejaService>();

            // Servicio de correo.
            services.AddScoped<EnvioMail>(_ =>
                new EnvioMail(configuration["SoapSettings:ServiosOffice365Url"] ?? string.Empty));

            // Abstracciones de infraestructura para los servicios de AppLogic.
            services.AddScoped<IEmailSender, AppLogic.Services.Email.EnvioMailEmailSender>();
            services.AddScoped<AppLogic.IServices.Autenticacion.ITwoFactorSessionStore, AppLogic.Services.Autenticacion.RedisTwoFactorSessionStore>();

            // Servicio de autenticación de dos factores (2FA) por email.
            services.AddScoped<AppLogic.IServices.Autenticacion.IDosFactoresAuthService, AppLogic.Services.Autenticacion.DosFactoresAuthService>();

            // Servicio orquestador del flujo de login (reCAPTCHA + rate limiting + LDAP + 2FA).
            services.AddScoped<AppLogic.IServices.Autenticacion.ILoginFlowService, AppLogic.Services.Autenticacion.LoginFlowService>();

            return services;
        }
    }
}
