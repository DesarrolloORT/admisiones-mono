using AppLogic.ApiClients.Interfaces;
using AppLogic.ApiClients.Services;
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

            services.AddInscripcionesyPagosApiClient(CrearConfiguracion());
            using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<IInscripcionesyPagosApiClient>();

            Assert.IsType<InscripcionesyPagosApiClient>(client);
        }

        [Fact]
        public void AddInscripcionesyPagosApiClient_DoesNotRegisterConcreteTypeDirectly()
        {
            // El registro vía AddHttpClient<TClient, TImplementation> resuelve la interfaz,
            // no la clase concreta; los consumers deben depender de IInscripcionesyPagosApiClient.
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();

            services.AddInscripcionesyPagosApiClient(CrearConfiguracion());

            Assert.DoesNotContain(services, sd => sd.ServiceType == typeof(InscripcionesyPagosApiClient));
        }
    }
}
