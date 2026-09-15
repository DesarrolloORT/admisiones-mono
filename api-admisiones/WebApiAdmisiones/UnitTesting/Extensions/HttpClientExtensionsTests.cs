using AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;
using AppLogic.Integrations.EnrollmentsAndPayments.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApiAdmisiones.Extensions;
using Xunit;

namespace UnitTesting.Extensions
{
    public class HttpClientExtensionsTests
    {
        private static IConfiguration CrearConfiguracion()
            => new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiClients:InscripcionesYPagos:BaseUrl"] = "https://internal.test/"
                })
                .Build();

        [Fact]
        public void AddInscripcionesyPagosApiClient_RegistersInterface_ResolvableAsInscripcionesyPagosApiClient()
        {
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();

            services.AddEnrollmentsAndPaymentsApiClient(CrearConfiguracion());
            using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<IEnrollmentsAndPaymentsApiClient>();

            Assert.IsType<EnrollmentsAndPaymentsApiClient>(client);
        }

        [Fact]
        public void AddInscripcionesyPagosApiClient_DoesNotRegisterConcreteTypeDirectly()
        {
            // El registro vía AddHttpClient<TClient, TImplementation> resuelve la interfaz,
            // no la clase concreta; los consumers deben depender de IEnrollmentsAndPaymentsApiClient.
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();

            services.AddEnrollmentsAndPaymentsApiClient(CrearConfiguracion());

            Assert.DoesNotContain(services, sd => sd.ServiceType == typeof(EnrollmentsAndPaymentsApiClient));
        }
    }
}
