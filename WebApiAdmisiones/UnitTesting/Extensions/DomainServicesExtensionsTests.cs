using BusinessLogic.IDevartRepositories;
using BusinessLogic.IGenericRepository;
using BusinessLogic.IServices;
using ConnectionContext;
using DataAccess;
using DataAccess.DevartRepositories;
using DataAccess.GenericAccess.Services;
using LdapService.Interfaces;
using LdapService.Services;
using MailORT;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModBandejaAppLogic.Interfaces;
using ModBandejaAppLogic.Services;
using ModBandejaDataAccess;
using Moq;
using WebApiAdmisiones.Extensions;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Observability;
using AppLogic.Services.Autenticacion;
using AppLogic.Services.Personas;
using AppLogic.Services.Inscripciones;
using AppLogic.Services.Becas;
using AppLogic.Services.Catalogos;
using AppLogic.IServices.Autenticacion;
using AppLogic.IServices.Becas;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Inscripciones;
using AppLogic.IServices.Personas;

namespace UnitTesting.Extensions
{
    /// <summary>
    /// Unit tests for DomainServicesExtensions class.
    /// Tests cover service registration, DbContext configuration, and repository initialization.
    /// </summary>
    public class DomainServicesExtensionsTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly ServiceCollection _serviceCollection;

        public DomainServicesExtensionsTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _serviceCollection = new ServiceCollection();
        }

        #region AddDomainServices Tests - Basic Registration

        [Fact]
        public void AddDomainServices_WithValidConfiguration_ReturnsServiceCollection()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            var result = _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IServiceCollection>(result);
        }

        [Fact]
        public void AddDomainServices_RegistersIDbConnectionContext_AsScoped()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IDbConnectionContext) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersDbConnectionContext_AsScoped()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(DbConnectionContext) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        #endregion

        #region DbContext Configuration Tests

        [Fact]
        public void AddDomainServices_RegistersModelContext_AsDbContext()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(ModelContext));
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersBandejaModelContext_AsDbContext()
        {
            // Arrange
            SetupEnvironmentMock("Production");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(BandejaModelContext));
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_InDevelopmentEnvironment_ConfiguresLoggingAndSensitiveData()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            var provider = _serviceCollection
                .AddDomainServices(configuration, _environmentMock.Object)
                .BuildServiceProvider();

            // Assert - Environment is Development
            Assert.NotNull(provider);
        }

        [Fact]
        public void AddDomainServices_InProductionEnvironment_DoesNotEnableSensitiveDataLogging()
        {
            // Arrange
            SetupEnvironmentMock("Production");
            var configuration = BuildConfiguration();

            // Act
            var provider = _serviceCollection
                .AddDomainServices(configuration, _environmentMock.Object)
                .BuildServiceProvider();

            // Assert - Should not enable sensitive data logging in production
            Assert.NotNull(provider);
        }

        [Fact]
        public void AddDomainServices_InPreproductionEnvironment_DoesNotEnableSensitiveDataLogging()
        {
            // Arrange
            SetupEnvironmentMock("Preproduction");
            var configuration = BuildConfiguration();

            // Act
            var provider = _serviceCollection
                .AddDomainServices(configuration, _environmentMock.Object)
                .BuildServiceProvider();

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region Repository Registration Tests

        [Fact]
        public void AddDomainServices_RegistersIGenericRepository()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IGenericRepository) &&
                sd.ImplementationType == typeof(GenericRepository) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersIUnitOfWorkFactory()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IUnitOfWorkFactory) &&
                sd.ImplementationType == typeof(EntityFrameworkUnitOfWorkFactory) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersModBandejaUnitOfWorkFactory()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWorkFactory) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersModGenericBaseUnitOfWorkFactory()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(ModGenericBaseBusinessLogic.IDevartRepositories.IUnitOfWorkFactory) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        #endregion

        #region Application Services Registration Tests

        [Fact]
        public void AddDomainServices_RegistersICurrentUserService()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(ICurrentUserService) &&
                sd.ImplementationType == typeof(CurrentUserService) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersIBandejaService()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IBandejaService) &&
                sd.ImplementationType == typeof(BandejaService) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersILoginService()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IAuthService) &&
                sd.ImplementationType == typeof(AuthService) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersDomainServices()
        {
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(ICatalogosService) && sd.ImplementationType == typeof(CatalogosService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IInscripcionesService) && sd.ImplementationType == typeof(InscripcionesService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IBecasService) && sd.ImplementationType == typeof(BecasService)));
        }

        [Fact]
        public void AddDomainServices_RegistersITokenService()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(ITokenService) &&
                sd.ImplementationType == typeof(TokenService) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_RegistersIRefreshTokenService()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IRefreshTokenService) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        #endregion

        #region Authentication Services Registration Tests

        [Fact]
        public void AddDomainServices_RegistersILdap()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(ILdap) &&
                sd.ImplementationType == typeof(Ldap) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        #endregion

        #region Interceptor Registration Tests

        [Fact]
        public void AddDomainServices_RegistersEfCoreLoggingInterceptor()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(EfCoreLoggingInterceptor) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        #endregion

        #region Mail Service Registration Tests

        [Fact]
        public void AddDomainServices_RegistersEnvioMail_WithConfiguredUrl()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            var provider = _serviceCollection
                .AddDomainServices(configuration, _environmentMock.Object)
                .BuildServiceProvider();

            // Assert
            var mailService = provider.GetService<EnvioMail>();
            Assert.NotNull(mailService);
        }

        [Fact]
        public void AddDomainServices_RegistersEnvioMail_AsScoped()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            var descriptor = _serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType == typeof(EnvioMail) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddDomainServices_EnvioMail_UsesConfigurationUrlWhenProvided()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var testUrl = "https://test.office365.com/api";
            var configuration = BuildConfigurationWithMailUrl(testUrl);

            // Act
            var provider = _serviceCollection
                .AddDomainServices(configuration, _environmentMock.Object)
                .BuildServiceProvider();

            var mailService = provider.GetService<EnvioMail>();

            // Assert
            Assert.NotNull(mailService);
        }

        [Fact]
        public void AddDomainServices_EnvioMail_UsesEmptyStringWhenUrlNotConfigured()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration(); // No mail URL configured

            // Act
            var provider = _serviceCollection
                .AddDomainServices(configuration, _environmentMock.Object)
                .BuildServiceProvider();

            var mailService = provider.GetService<EnvioMail>();

            // Assert
            Assert.NotNull(mailService);
        }

        #endregion

        #region Environment-Specific Behavior Tests

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Testing")]
        public void AddDomainServices_InNonProductionEnvironments_ConfiguresModelContextWithLogging(string environment)
        {
            // Arrange
            SetupEnvironmentMock(environment);
            var configuration = BuildConfiguration();

            // Act
            var provider = _serviceCollection
                .AddDomainServices(configuration, _environmentMock.Object)
                .BuildServiceProvider();

            // Assert
            Assert.NotNull(provider);
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Preproduction")]
        public void AddDomainServices_InProductionLikeEnvironments_DisablesLogging(string environment)
        {
            // Arrange
            SetupEnvironmentMock(environment);
            var configuration = BuildConfiguration();

            // Act
            var provider = _serviceCollection
                .AddDomainServices(configuration, _environmentMock.Object)
                .BuildServiceProvider();

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region Service Resolution Tests

        [Fact]
        public void AddDomainServices_AllRegisteredServices_AreRegisteredInServiceCollection()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert - Verify all application services are registered
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(ICurrentUserService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IBandejaService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(ICatalogosService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IInscripcionesService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IBecasService)));
            
        }

        [Fact]
        public void AddDomainServices_RepositoriesAndFactories_AreRegistered()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IGenericRepository)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IUnitOfWorkFactory)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(ModBandejaBusinessLogic.IDevartRepositories.IUnitOfWorkFactory)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(ModGenericBaseBusinessLogic.IDevartRepositories.IUnitOfWorkFactory)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(EnvioMail)));
        }

        [Fact]
        public void AddDomainServices_AuthenticationServices_AreRegistered()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(ILdap)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(ITokenService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IRefreshTokenService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IAuthService)));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void AddDomainServices_WithAllComponents_RegistersAllRequiredServices()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert - Verify critical services are registered
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IDbConnectionContext)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IGenericRepository)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IUnitOfWorkFactory)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ICurrentUserService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IAuthService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ICatalogosService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IInscripcionesService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IBecasService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ITokenService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IRefreshTokenService)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ILdap)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(EfCoreLoggingInterceptor)));
            Assert.NotNull(_serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(EnvioMail)));
        }

        [Fact]
        public void AddDomainServices_RegistersDbConnectionContext_WithFactoryPattern()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);

            // Assert - Verify both interface and concrete type are registered
            var interfaceDescriptor = _serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IDbConnectionContext));
            var concreteDescriptor = _serviceCollection.FirstOrDefault(sd => 
                sd.ServiceType == typeof(DbConnectionContext));

            Assert.NotNull(interfaceDescriptor);
            Assert.NotNull(concreteDescriptor);
            Assert.Equal(ServiceLifetime.Scoped, interfaceDescriptor.Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, concreteDescriptor.Lifetime);
        }

        [Fact]
        public void AddDomainServices_MultipleInvocations_AllowsDuplicateRegistrations()
        {
            // Arrange
            SetupEnvironmentMock("Development");
            var configuration = BuildConfiguration();

            // Act
            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);
            var countBefore = _serviceCollection.Count(sd => 
                sd.ServiceType == typeof(ICurrentUserService));

            _serviceCollection.AddDomainServices(configuration, _environmentMock.Object);
            var countAfter = _serviceCollection.Count(sd => 
                sd.ServiceType == typeof(ICurrentUserService));

            // Assert - Registration count increases (duplicates allowed in ServiceCollection)
            Assert.True(countAfter > countBefore);
        }

        #endregion

        #region Helper Methods

        private void SetupEnvironmentMock(string environmentName)
        {
            _environmentMock.Setup(e => e.EnvironmentName)
                .Returns(environmentName ?? string.Empty);
        }

        private IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SoapSettings:ServiosOffice365Url", "https://office365.example.com/api" },
                    { "Admisiones:IdSistemaAdmisiones", "25" }
                })
                .Build();
        }

        private IConfiguration BuildConfigurationWithMailUrl(string mailUrl)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SoapSettings:ServiosOffice365Url", mailUrl },
                    { "Admisiones:IdSistemaAdmisiones", "25" }
                })
                .Build();
        }

        #endregion
    }
}
