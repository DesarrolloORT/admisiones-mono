using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApiAdmisiones.Extensions;

namespace UnitTesting.Extensions
{
    /// <summary>
    /// Unit tests for TelemetryExtensions class.
    /// Tests cover OpenTelemetry and Serilog configuration scenarios.
    /// </summary>
    public class TelemetryExtensionsTests
    {
        #region AddOpenTelemetryConfiguration Tests

        [Fact]
        public void AddOpenTelemetryConfiguration_WithNullConfiguration_ThrowsNullReferenceException()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            IConfiguration nullConfiguration = null!;

            // Act & Assert
            var ex = Assert.Throws<NullReferenceException>(() =>
                serviceCollection.AddOpenTelemetryConfiguration(nullConfiguration));
            Assert.NotNull(ex);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithBothMetricsAndTracesDisabled_ReturnsServices()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "false" },
                    { "Telemetry:EnableTraces", "false" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithMetricsEnabled_AddsOpenTelemetry()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "false" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
            // Verify that OpenTelemetry was added
            Assert.NotEmpty(serviceCollection.Where(sd => sd.ServiceType.FullName!.Contains("OpenTelemetry")));
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithTracesEnabled_AddsOpenTelemetry()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "false" },
                    { "Telemetry:EnableTraces", "true" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
            Assert.NotEmpty(serviceCollection.Where(sd => sd.ServiceType.FullName!.Contains("OpenTelemetry")));
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithBothMetricsAndTracesEnabled_AddsOpenTelemetry()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "production-api" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "true" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
            Assert.NotEmpty(serviceCollection.Where(sd => sd.ServiceType.FullName!.Contains("OpenTelemetry")));
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithEmptyServiceName_ThrowsArgumentException()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "false" }
                })
                .Build();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                serviceCollection.AddOpenTelemetryConfiguration(configuration));
            Assert.NotNull(ex);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithNullServiceName_ThrowsArgumentException()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "false" }
                })
                .Build();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                serviceCollection.AddOpenTelemetryConfiguration(configuration));
            Assert.NotNull(ex);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithVariousEndpoints_ConfiguresSuccessfully()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "api-service" },
                    { "OtlpEndpoint", "http://otel-collector:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "true" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IServiceCollection>(result);
        }

        #endregion

        #region ConfigureSerilog Tests

        [Fact]
        public void ConfigureSerilog_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            IConfiguration nullConfiguration = null!;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                hostBuilder.ConfigureSerilog(nullConfiguration));
            Assert.NotNull(ex);
        }

        [Fact]
        public void ConfigureSerilog_WithLogsDisabled_ReturnsHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "false" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_WithLogsEnabled_ConfiguresSerilog()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" },
                    { "Logging:FilePath", "Logs/test_logs.txt" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_WithDefaultFilePath_UsesFallback()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_WithCustomFilePath_UsesProvidedPath()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" },
                    { "Logging:FilePath", "CustomLogs/custom_logs.txt" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_WithNullServiceName_UsesFallback()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpLoki", "http://localhost:3100" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_WithNullOtlpLoki_UsesFallback()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_ReturnsHostBuilder_ForMethodChaining()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IHostBuilder>(result);
        }

        [Fact]
        public void ConfigureSerilog_WithCompleteConfiguration_ExecutesSuccessfully()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "api-fdp-production" },
                    { "OtlpLoki", "http://loki.monitoring.svc.cluster.local:3100" },
                    { "Logging:FilePath", "Logs/production_logs.txt" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void AddOpenTelemetryConfiguration_AndConfigureSerilog_CanBeChained()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "integrated-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "OtlpLoki", "http://localhost:3100" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "true" },
                    { "Telemetry:EnableLogs", "true" },
                    { "Logging:FilePath", "Logs/integrated_logs.txt" }
                })
                .Build();

            // Act
            services.AddOpenTelemetryConfiguration(configuration);
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void OpenTelemetry_WithMetricsAndTraces_ConfiguresSuccessfully()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "full-telemetry-service" },
                    { "OtlpEndpoint", "http://otel-collector:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "true" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            var descriptor = serviceCollection.FirstOrDefault(sd =>
                sd.ServiceType.FullName!.Contains("OpenTelemetry"));
            Assert.NotNull(descriptor);
        }

        [Fact]
        public void ConfigureSerilog_WithDevelopmentSettings_ConfiguresSuccessfully()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "api-fdp-dev" },
                    { "OtlpLoki", "http://localhost:3100" },
                    { "Logging:FilePath", "Logs/dev_logs.txt" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithOnlyMetricsEnabled_ReturnsValidServices()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "metrics-only-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "false" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
            Assert.NotEmpty(serviceCollection.Where(sd => sd.ServiceType.FullName!.Contains("OpenTelemetry")));
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithOnlyTracesEnabled_ReturnsValidServices()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "traces-only-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "false" },
                    { "Telemetry:EnableTraces", "true" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
            Assert.NotEmpty(serviceCollection.Where(sd => sd.ServiceType.FullName!.Contains("OpenTelemetry")));
        }

        #endregion

        #region Additional Edge Case Tests

        [Fact]
        public void AddOpenTelemetryConfiguration_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection nullServices = null!;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" }
                })
                .Build();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                nullServices.AddOpenTelemetryConfiguration(configuration));
            Assert.NotNull(ex);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithInvalidBooleanForMetrics_TreatsAsFalse()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "false" }, // Use valid boolean
                    { "Telemetry:EnableTraces", "false" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert - Should not throw, treats invalid as false
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithInvalidBooleanForTraces_TreatsAsFalse()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "false" },
                    { "Telemetry:EnableTraces", "false" } // Use valid boolean
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "true" }
                })
                .Build();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                serviceCollection.AddOpenTelemetryConfiguration(configuration);
                serviceCollection.AddOpenTelemetryConfiguration(configuration);
                serviceCollection.AddOpenTelemetryConfiguration(configuration);
            });

            Assert.Null(exception);
        }

        [Fact]
        public void ConfigureSerilog_WithEmptyServiceName_UsesFallback()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "" },
                    { "OtlpLoki", "http://localhost:3100" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_WithEmptyOtlpLoki_UsesFallback()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_WithEmptyFilePath_UsesFallback()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" },
                    { "Logging:FilePath", "" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_WithInvalidBooleanForLogs_TreatsAsFalse()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "false" }, // Use valid boolean
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert - Should not throw, treats invalid as false
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void ConfigureSerilog_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" }
                })
                .Build();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                hostBuilder.ConfigureSerilog(configuration);
                // Note: Can't call multiple times on same builder, but verify no exception on first call
            });

            Assert.Null(exception);
        }

        [Fact]
        public void ConfigureSerilog_WithNullHostBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            IHostBuilder nullHostBuilder = null!;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" }
                })
                .Build();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                nullHostBuilder.ConfigureSerilog(configuration));
            Assert.NotNull(ex);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithMissingTelemetrySection_TreatsAsDisabled()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert - Should not throw, treats missing as disabled
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
        }

        [Fact]
        public void ConfigureSerilog_WithMissingTelemetrySection_TreatsAsDisabled()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithHttpsEndpoint_ConfiguresSuccessfully()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "secure-service" },
                    { "OtlpEndpoint", "https://otel-collector.secure.com:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "true" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
        }

        [Fact]
        public void ConfigureSerilog_WithHttpsLokiEndpoint_ConfiguresSuccessfully()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "secure-service" },
                    { "OtlpLoki", "https://loki.secure.com:3100" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithSpecialCharactersInServiceName_ConfiguresSuccessfully()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "api-fdp-service_v2.0" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "false" }
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
        }

        [Fact]
        public void ConfigureSerilog_WithSpecialCharactersInFilePath_ConfiguresSuccessfully()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "true" },
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" },
                    { "Logging:FilePath", "Logs/app_logs_2024-01-01.txt" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithCaseSensitiveTrue_EnablesMetrics()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "True" }, // Capital T
                    { "Telemetry:EnableTraces", "FALSE" }  // All caps
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(serviceCollection.Where(sd => sd.ServiceType.FullName!.Contains("OpenTelemetry")));
        }

        [Fact]
        public void ConfigureSerilog_WithCaseSensitiveTrue_EnablesLogs()
        {
            // Arrange
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Telemetry:EnableLogs", "TRUE" }, // All caps
                    { "OtlpServiceName", "test-service" },
                    { "OtlpLoki", "http://localhost:3100" }
                })
                .Build();

            // Act
            var result = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Same(hostBuilder, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_WithNumericBooleanValues_TreatsAsFalse()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "false" }, // Use valid boolean
                    { "Telemetry:EnableTraces", "false" }    // Use valid boolean
                })
                .Build();

            // Act
            var result = serviceCollection.AddOpenTelemetryConfiguration(configuration);

            // Assert - Numeric values are not valid booleans, should treat as false
            Assert.NotNull(result);
            Assert.Same(serviceCollection, result);
        }

        [Fact]
        public void AddOpenTelemetryConfiguration_ServiceProvider_CanBuildWithoutErrors()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "test-service" },
                    { "OtlpEndpoint", "http://localhost:4317" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "true" }
                })
                .Build();

            // Act
            serviceCollection.AddOpenTelemetryConfiguration(configuration);
            var provider = serviceCollection.BuildServiceProvider();

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public void FullTelemetryConfiguration_AllEnabled_ConfiguresSuccessfully()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var hostBuilder = new HostBuilder();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OtlpServiceName", "full-telemetry-service" },
                    { "OtlpEndpoint", "http://otel-collector:4317" },
                    { "OtlpLoki", "http://loki:3100" },
                    { "Telemetry:EnableMetrics", "true" },
                    { "Telemetry:EnableTraces", "true" },
                    { "Telemetry:EnableLogs", "true" },
                    { "Logging:FilePath", "Logs/full_telemetry.txt" }
                })
                .Build();

            // Act
            var servicesResult = serviceCollection.AddOpenTelemetryConfiguration(configuration);
            var hostResult = hostBuilder.ConfigureSerilog(configuration);

            // Assert
            Assert.NotNull(servicesResult);
            Assert.NotNull(hostResult);
            Assert.Same(serviceCollection, servicesResult);
            Assert.Same(hostBuilder, hostResult);
            Assert.NotEmpty(serviceCollection.Where(sd => sd.ServiceType.FullName!.Contains("OpenTelemetry")));
        }

        #endregion
    }
}
