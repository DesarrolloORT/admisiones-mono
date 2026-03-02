using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar telemetría (OpenTelemetry y Serilog).
    /// </summary>
    public static class TelemetryExtensions
    {
        /// <summary>
        /// Configura OpenTelemetry para métricas y trazas según la configuración.
        /// </summary>
        public static IServiceCollection AddOpenTelemetryConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var otlpServiceName = configuration["OtlpServiceName"] ?? string.Empty;
            var otlpEndpoint = configuration["OtlpEndpoint"] ?? string.Empty;
            var enableMetrics = configuration.GetValue<bool>("Telemetry:EnableMetrics");
            var enableTraces = configuration.GetValue<bool>("Telemetry:EnableTraces");

            if (!enableMetrics && !enableTraces)
                return services;

            var otBuilder = services.AddOpenTelemetry();

            if (enableMetrics)
            {
                otBuilder.WithMetrics(mb => mb
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(otlpServiceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otlpEndpoint);
                        o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    }));
            }

            if (enableTraces)
            {
                otBuilder.WithTracing(tb => tb
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(otlpServiceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otlpEndpoint);
                        o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    }));
            }

            return services;
        }

        /// <summary>
        /// Configura Serilog con sinks de consola, archivo y OpenTelemetry (Loki).
        /// </summary>
        public static IHostBuilder ConfigureSerilog(
            this IHostBuilder hostBuilder,
            IConfiguration configuration)
        {
            var enableLogs = configuration.GetValue<bool>("Telemetry:EnableLogs");
            
            if (!enableLogs)
                return hostBuilder;

            var otlpServiceName = configuration["OtlpServiceName"] ?? string.Empty;
            var otlpLoki = configuration["OtlpLoki"] ?? string.Empty;
            var filePath = configuration["Logging:FilePath"] ?? "Logs/loki_logs.txt";

            return hostBuilder.UseSerilog((context, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.WithProperty("app", otlpServiceName)
                    .Enrich.WithProperty("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        filePath,
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.OpenTelemetry(o =>
                    {
                        o.Endpoint = otlpLoki;
                        o.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;
                        o.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = otlpServiceName };
                    });

                Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine($"SelfLog: {msg}"));
                Log.Information("Prueba de conexión a Loki con ID único {UniqueId}", Guid.NewGuid());
            });
        }
    }
}
