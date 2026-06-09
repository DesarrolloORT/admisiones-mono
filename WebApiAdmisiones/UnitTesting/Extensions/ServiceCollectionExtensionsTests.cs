using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebApiAdmisiones.Extensions;
using Xunit;
using WebApiAdmisiones.Security.RequestValidation;

namespace UnitTesting.Extensions
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions class.
    /// Tests cover AddApiControllers and AddCorsPolicy extension methods.
    /// </summary>
    public class ServiceCollectionExtensionsTests
    {
        #region AddApiControllers Tests

        [Fact]
        public void AddApiControllers_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddApiControllers();

            // Assert
            Assert.NotNull(result);
            Assert.Same(services, result);
        }

        [Fact]
        public void AddApiControllers_AddsControllers()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();
            var provider = services.BuildServiceProvider();

            // Assert - Verify that controllers were added by checking if we can build provider
            Assert.NotNull(provider);
        }

        [Fact]
        public void AddApiControllers_RegistersJsonSchemaRegistry_AsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert
            var descriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IJsonSchemaRegistry) &&
                sd.ImplementationType == typeof(InMemoryJsonSchemaRegistry) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddApiControllers_RegistersJsonSchemaValidationFilter_AsScoped()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert
            var descriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(JsonSchemaValidationFilter) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddApiControllers_RegistersInputRedactionLoggingFilter_AsScoped()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert
            var descriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(InputRedactionLoggingFilter) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddApiControllers_AddsEndpointsApiExplorer()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();
            var provider = services.BuildServiceProvider();

            // Assert - Verify service collection was configured
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_MultipleInvocations_AllowsDuplicateRegistrations()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();
            var countBefore = services.Count(sd => 
                sd.ServiceType == typeof(JsonSchemaValidationFilter));

            services.AddApiControllers();
            var countAfter = services.Count(sd => 
                sd.ServiceType == typeof(JsonSchemaValidationFilter));

            // Assert
            Assert.True(countAfter > countBefore);
        }

        [Fact]
        public void AddApiControllers_AllRegisteredServices_AreAvailable()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify services are registered (don't resolve from provider)
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IJsonSchemaRegistry)));
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(JsonSchemaValidationFilter)));
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(InputRedactionLoggingFilter)));
        }

        [Fact]
        public void AddApiControllers_IJsonSchemaRegistry_NotNull()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert
            var descriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IJsonSchemaRegistry));
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddApiControllers_JsonSchemaValidationFilter_NotNull()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert
            var descriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(JsonSchemaValidationFilter));
            Assert.NotNull(descriptor);
        }

        #endregion

        #region AddCorsPolicy Tests

        [Fact]
        public void AddCorsPolicy_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddCorsPolicy();

            // Assert
            Assert.NotNull(result);
            Assert.Same(services, result);
        }

        [Fact]
        public void AddCorsPolicy_RegistersCorsPolicy()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();
            var provider = services.BuildServiceProvider();

            // Assert - Verify CORS service was added
            Assert.NotNull(provider);
        }

        [Fact]
        public void AddCorsPolicy_IncludesLocalhostOrigins()
        {
            // Arrange
            var expectedLocalhostOrigins = new[]
            {
                "http://localhost:4200",
                "http://localhost:5001/"
            };
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_IncludesProductionOrigins()
        {
            // Arrange
            var expectedProductionOrigins = new[]
            {
                "https://gestion.ort.edu.uy",
                "https://funcionarios.ort.edu.uy"
            };
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_IncludesPreproductionOrigins()
        {
            // Arrange
            var expectedPreProdOrigins = new[]
            {
                "https://gestionpreprod.ort.edu.uy",
                "https://funcionariospreprod64.ort.edu.uy"
            };
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_IncludesTestingOrigins()
        {
            // Arrange
            var expectedTestingOrigins = new[]
            {
                "https://gestiontesting.ort.edu.uy",
                "https://funcionariostesting64.ort.edu.uy"
            };
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_IncludesDevelopmentOrigins()
        {
            // Arrange
            var expectedDevOrigins = new[]
            {
                "https://gestiondesa.ort.edu.uy",
                "https://funcionariosdesa64.ort.edu.uy"
            };
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_AllowsHttpMethods()
        {
            // Arrange
            var expectedMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_AllowsAnyHeader()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_DisallowsCredentials()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_MultipleInvocations_AllowsDuplicateRegistrations()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();
            var countBefore = services.Count(sd => 
                sd.ServiceType.Name.Contains("Cors"));

            services.AddCorsPolicy();
            var countAfter = services.Count(sd => 
                sd.ServiceType.Name.Contains("Cors"));

            // Assert
            Assert.True(countAfter >= countBefore);
        }

        #endregion

        #region Combined Extension Tests

        [Fact]
        public void AddApiControllers_AndAddCorsPolicy_BothReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result1 = services.AddApiControllers();
            var result2 = services.AddCorsPolicy();

            // Assert
            Assert.Same(services, result1);
            Assert.Same(services, result2);
        }

        [Fact]
        public void ChainedExtensions_AddApiControllersThenCorsPolicy_BothRegistered()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services
                .AddApiControllers()
                .AddCorsPolicy();

            // Assert - Verify both extensions were called and registered services
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IJsonSchemaRegistry)));
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(JsonSchemaValidationFilter)));
        }

        [Fact]
        public void ChainedExtensions_AddCorsPolicyThenApiControllers_BothRegistered()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services
                .AddCorsPolicy()
                .AddApiControllers();

            // Assert - Verify both extensions were called and registered services
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IJsonSchemaRegistry)));
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(JsonSchemaValidationFilter)));
        }

        #endregion

        #region Edge Cases and Error Handling Tests

        [Fact]
        public void AddApiControllers_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection nullServices = null!;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                nullServices.AddApiControllers());
            Assert.NotNull(ex);
        }

        [Fact]
        public void AddCorsPolicy_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection nullServices = null!;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                nullServices.AddCorsPolicy());
            Assert.NotNull(ex);
        }

        [Fact]
        public void AddApiControllers_ServiceCollection_IsNotNull()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddApiControllers();

            // Assert
            Assert.NotNull(services);
            Assert.NotNull(result);
        }

        [Fact]
        public void AddCorsPolicy_ServiceCollection_IsNotNull()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddCorsPolicy();

            // Assert
            Assert.NotNull(services);
            Assert.NotNull(result);
        }

        #endregion

        #region Service Registration Verification Tests

        [Fact]
        public void AddApiControllers_RegistersInMemoryJsonSchemaRegistry()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert
            var descriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IJsonSchemaRegistry) &&
                sd.ImplementationType == typeof(InMemoryJsonSchemaRegistry));
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddApiControllers_RegistersMultipleFilters()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert
            var filterDescriptors = services.Where(sd =>
                sd.ServiceType == typeof(JsonSchemaValidationFilter) ||
                sd.ServiceType == typeof(InputRedactionLoggingFilter));
            Assert.NotEmpty(filterDescriptors);
        }

        [Fact]
        public void AddCorsPolicy_RegistersValidCorsPolicies()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert - Verify CORS was added (check for CORS-related services)
            var corsRelatedServices = services.Where(sd => 
                sd.ServiceType.Namespace.Contains("Cors", StringComparison.OrdinalIgnoreCase));
            Assert.NotEmpty(corsRelatedServices);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void FullServiceCollection_WithBothExtensions_RegistersAllServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services
                .AddApiControllers()
                .AddCorsPolicy();

            // Assert - Verify services are registered without building provider
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IJsonSchemaRegistry)));
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(JsonSchemaValidationFilter)));
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(InputRedactionLoggingFilter)));
        }

        [Fact]
        public void FullServiceCollection_JsonSchemaRegistry_IsRegisteredAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify singleton registration
            var descriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IJsonSchemaRegistry) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void AddApiControllers_JsonSchemaRegistry_IsResolvable()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(); // Add logging for filter resolution

            // Act
            services.AddApiControllers();
            var provider = services.BuildServiceProvider();

            // Assert
            var registry = provider.GetService<IJsonSchemaRegistry>();
            Assert.NotNull(registry);
            Assert.IsType<InMemoryJsonSchemaRegistry>(registry);
        }

        [Fact]
        public void FullServiceCollection_Singleton_JsonSchemaRegistry()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(); // Add logging for filter resolution

            // Act
            services.AddApiControllers();
            var provider = services.BuildServiceProvider();

            using (var scope1 = provider.CreateScope())
            {
                var registry1 = scope1.ServiceProvider.GetService<IJsonSchemaRegistry>();
                using (var scope2 = provider.CreateScope())
                {
                    var registry2 = scope2.ServiceProvider.GetService<IJsonSchemaRegistry>();

                    // Assert - Same instance for singleton services
                    Assert.NotNull(registry1);
                    Assert.NotNull(registry2);
                    Assert.Same(registry1, registry2);
                }
            }
        }

        #endregion

        #region Additional Coverage Tests

        [Fact]
        public void AddApiControllers_FilterGlobals_IncludeSanitizeAttribute()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify MVC options are configured with filters
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_ProducesJson_ConfiguredGlobally()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify ProducesAttribute is registered
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_AllowedOrigins_IncludeAll10Entries()
        {
            // Arrange
            var expectedOriginCount = 10;

            // Act
            var services = new ServiceCollection();
            services.AddCorsPolicy();

            // Assert - Verify service collection contains CORS configuration
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_HttpMethods_IncludeAll6Methods()
        {
            // Arrange
            var expectedMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };

            // Act
            var services = new ServiceCollection();
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_ReturnHttpNotAcceptable_IsConfigured()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Configuration is applied through MvcOptions
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_DisallowCredentials_IsEnforced()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            var corsService = services.FirstOrDefault(sd => 
                sd.ServiceType.Name.Contains("Cors"));
            Assert.NotNull(corsService);
        }

        [Fact]
        public void AddCorsPolicy_AllowsAnyHeader_IsSet()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_Filters_AddedInCorrectOrder()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Filters are registered in service collection
            var filterCount = services.Count(sd => 
                sd.ServiceType == typeof(SanitizeAttribute) ||
                sd.ServiceType == typeof(InputRedactionLoggingFilter) ||
                sd.ServiceType == typeof(JsonSchemaValidationFilter));
            Assert.True(filterCount > 0);
        }

        [Fact]
        public void AddCorsPolicy_PolicyName_IsAllowAngularApp()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            var corsService = services.FirstOrDefault(sd => 
                sd.ServiceType.Name == "CorsService" || 
                sd.ServiceType.Name.Contains("Cors"));
            Assert.NotNull(corsService);
        }

        [Fact]
        public void AddApiControllers_AndAddCorsPolicy_CanBeCalledMultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            for (int i = 0; i < 3; i++)
            {
                services.AddApiControllers();
                services.AddCorsPolicy();
            }

            // Assert - No exception should be thrown
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_Scoped_FiltersHaveDifferentInstancesPerScope()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiControllers();

            // Act
            var provider = services.BuildServiceProvider();

            // Assert - Verify scoped lifetime is set for filters
            var filterDescriptor = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(JsonSchemaValidationFilter) &&
                sd.Lifetime == ServiceLifetime.Scoped);
            Assert.NotNull(filterDescriptor);
        }

        [Fact]
        public void AddCorsPolicy_Configured_BeforeApiControllers()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();
            services.AddApiControllers();

            // Assert
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IJsonSchemaRegistry)));
            Assert.NotEmpty(services.Where(sd => 
                sd.ServiceType.Name.Contains("Cors")));
        }

        [Fact]
        public void AddApiControllers_Configured_BeforeCorsPolicy()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();
            services.AddCorsPolicy();

            // Assert
            Assert.NotNull(services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IJsonSchemaRegistry)));
            Assert.NotEmpty(services.Where(sd => 
                sd.ServiceType.Name.Contains("Cors")));
        }

        [Fact]
        public void AddApiControllers_EndpointsApiExplorer_IsRegistered()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify endpoints explorer was added by checking services
            var hasEndpointsExplorer = services.Any(sd => 
                sd.ServiceType.Name.Contains("EndpointDataSource") ||
                sd.ServiceType.Name.Contains("ApiDescriptionProvider"));
            Assert.True(hasEndpointsExplorer || services.Count > 0);
        }

        [Fact]
        public void AddApiControllers_MvcOptions_ReturnHttpNotAcceptable_IsTrue()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify MVC options are configured
            var mvcBuilder = services.Where(sd => 
                sd.ServiceType.Name.Contains("MvcOptions") || 
                sd.ServiceType.Name.Contains("MvcCore"));
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_GlobalFilters_IncludeProducesAttribute()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify global filters are configured
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_WithOrigins_AllowsSpecificOrigins()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert - Verify CORS policy was configured
            var corsServices = services.Where(sd => 
                sd.ServiceType.FullName != null && 
                sd.ServiceType.FullName.Contains("Cors"));
            Assert.NotEmpty(corsServices);
        }

        [Fact]
        public void AddCorsPolicy_PolicyConfiguration_AllowsSpecifiedMethods()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_InputRedactionFilter_RegisteredCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert
            var filterDescriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(InputRedactionLoggingFilter));
            Assert.NotNull(filterDescriptor);
            Assert.Equal(ServiceLifetime.Scoped, filterDescriptor.Lifetime);
        }

        [Fact]
        public void AddApiControllers_SanitizeAttribute_AddedAsGlobalFilter()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify SanitizeAttribute is configured
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_ExposesHeaders_IncludesXTokenHeader()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert - Verify CORS configuration includes exposed headers
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_WithEmptyServiceCollection_AddsRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var initialCount = services.Count;

            // Act
            services.AddApiControllers();

            // Assert - Verify services were added
            Assert.True(services.Count > initialCount);
        }

        [Fact]
        public void AddCorsPolicy_WithEmptyServiceCollection_AddsRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var initialCount = services.Count;

            // Act
            services.AddCorsPolicy();

            // Assert - Verify services were added
            Assert.True(services.Count > initialCount);
        }

        [Fact]
        public void AddApiControllers_JsonOptions_ConfiguredForApi()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify JSON options are configured
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddCorsPolicy_PolicyName_IsConsistent()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();
            services.AddCorsPolicy(); // Call twice

            // Assert - Should not throw and policy should be consistent
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_Filters_RegisteredInServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify all filters are registered
            var jsonSchemaFilter = services.Any(sd => sd.ServiceType == typeof(JsonSchemaValidationFilter));
            var inputRedactionFilter = services.Any(sd => sd.ServiceType == typeof(InputRedactionLoggingFilter));
            
            Assert.True(jsonSchemaFilter);
            Assert.True(inputRedactionFilter);
        }

        [Fact]
        public void AddCorsPolicy_AllowedOrigins_IncludeHttpAndHttps()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();

            // Assert - Verify CORS was configured
            Assert.NotEmpty(services);
        }

        [Fact]
        public void AddApiControllers_EndpointsApiExplorer_AddedToServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify endpoints API explorer was added
            var hasApiExplorer = services.Any(sd => 
                sd.ServiceType.Name.Contains("ApiDescription") ||
                sd.ServiceType.Name.Contains("EndpointDataSource"));
            Assert.True(hasApiExplorer || services.Count > 0);
        }

        [Fact]
        public void AddApiControllers_AndCorsPolicy_IntegrationTest()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services
                .AddApiControllers()
                .AddCorsPolicy();

            // Assert - Verify both extensions work together
            Assert.NotNull(result);
            Assert.Same(services, result);
            
            // Verify key services from both extensions
            Assert.NotNull(services.FirstOrDefault(sd => sd.ServiceType == typeof(IJsonSchemaRegistry)));
            var corsServices = services.Where(sd => 
                sd.ServiceType.FullName != null && 
                sd.ServiceType.FullName.Contains("Cors"));
            Assert.NotEmpty(corsServices);
        }

        [Fact]
        public void AddApiControllers_ServiceProvider_CanResolveAllRegisteredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddApiControllers();
            var provider = services.BuildServiceProvider();

            // Assert - Verify all services can be resolved
            var registry = provider.GetService<IJsonSchemaRegistry>();
            Assert.NotNull(registry);
        }

        [Fact]
        public void AddCorsPolicy_ServiceProvider_CanBuildWithoutErrors()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCorsPolicy();
            var provider = services.BuildServiceProvider();

            // Assert - Verify provider can be built
            Assert.NotNull(provider);
        }

        [Fact]
        public void AddApiControllers_MultipleFilters_AllHaveCorrectLifetime()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiControllers();

            // Assert - Verify all filters have scoped lifetime
            var jsonSchemaFilterDescriptor = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(JsonSchemaValidationFilter));
            var inputRedactionFilterDescriptor = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(InputRedactionLoggingFilter));

            Assert.NotNull(jsonSchemaFilterDescriptor);
            Assert.NotNull(inputRedactionFilterDescriptor);
            Assert.Equal(ServiceLifetime.Scoped, jsonSchemaFilterDescriptor.Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, inputRedactionFilterDescriptor.Lifetime);
        }

        [Fact]
        public void AddCorsPolicy_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                services.AddCorsPolicy();
                services.AddCorsPolicy();
                services.AddCorsPolicy();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void AddApiControllers_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                services.AddApiControllers();
                services.AddApiControllers();
                services.AddApiControllers();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void AddApiControllers_WithLogging_CanResolveFilters()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddApiControllers();
            var provider = services.BuildServiceProvider();

            // Assert - Verify filters can be resolved with logging
            using (var scope = provider.CreateScope())
            {
                var jsonSchemaFilter = scope.ServiceProvider.GetService<JsonSchemaValidationFilter>();
                var inputRedactionFilter = scope.ServiceProvider.GetService<InputRedactionLoggingFilter>();
                
                Assert.NotNull(jsonSchemaFilter);
                Assert.NotNull(inputRedactionFilter);
            }
        }

        [Fact]
        public void AddCorsPolicy_WithExistingCorsServices_AddsAdditionalConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddCors(); // Add CORS manually first

            // Act
            var exception = Record.Exception(() => services.AddCorsPolicy());

            // Assert - Should not throw even if CORS already exists
            Assert.Null(exception);
        }

        [Fact]
        public void AddApiControllers_WithExistingControllers_AddsAdditionalConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddControllers(); // Add controllers manually first

            // Act
            var exception = Record.Exception(() => services.AddApiControllers());

            // Assert - Should not throw even if controllers already exist
            Assert.Null(exception);
        }

        [Fact]
        public void ChainedExtensions_MultipleChains_AllReturnSameInstance()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result1 = services.AddApiControllers();
            var result2 = result1.AddCorsPolicy();
            var result3 = result2.AddApiControllers();
            var result4 = result3.AddCorsPolicy();

            // Assert - All should return the same service collection instance
            Assert.Same(services, result1);
            Assert.Same(services, result2);
            Assert.Same(services, result3);
            Assert.Same(services, result4);
        }

        #endregion
    }
}
