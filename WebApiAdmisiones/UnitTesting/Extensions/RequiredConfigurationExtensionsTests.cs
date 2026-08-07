using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using WebApiAdmisiones.Extensions;
using Xunit;

namespace UnitTesting.Extensions
{
    public class RequiredConfigurationExtensionsTests
    {
        private static IConfiguration Build(Dictionary<string, string?> values) =>
            new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        private static Dictionary<string, string?> Completa() => new()
        {
            ["ApiServicioInterno:RutaApi"] = "https://interno.ort.edu.uy/",
            ["ApiServicioInterno:Usuario"] = "WS_INTERNO",
            ["ApiServicioInterno:Password"] = "una-password"
        };

        [Fact]
        public void ValidateRequiredSecrets_ConTodasLasClaves_NoLanza()
        {
            var configuration = Build(Completa());

            var result = configuration.ValidateRequiredSecrets();

            Assert.Same(configuration, result);
        }

        [Theory]
        [InlineData("ApiServicioInterno:RutaApi", "ApiServicioInterno__RutaApi")]
        [InlineData("ApiServicioInterno:Usuario", "ApiServicioInterno__Usuario")]
        [InlineData("ApiServicioInterno:Password", "ApiServicioInterno__Password")]
        public void ValidateRequiredSecrets_ConClaveFaltante_LanzaConElNombreDeLaVariable(
            string claveFaltante,
            string variableEsperada)
        {
            var values = Completa();
            values.Remove(claveFaltante);

            var ex = Assert.Throws<InvalidOperationException>(
                () => Build(values).ValidateRequiredSecrets());

            Assert.Contains(variableEsperada, ex.Message);
        }

        [Fact]
        public void ValidateRequiredSecrets_ConClaveVacia_LanzaIgual()
        {
            var values = Completa();
            values["ApiServicioInterno:Password"] = "   ";

            var ex = Assert.Throws<InvalidOperationException>(
                () => Build(values).ValidateRequiredSecrets());

            Assert.Contains("ApiServicioInterno__Password", ex.Message);
        }

        [Fact]
        public void ValidateRequiredSecrets_ConVariasFaltantes_LasListaTodasJuntas()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Build([]).ValidateRequiredSecrets());

            Assert.Contains("ApiServicioInterno__RutaApi", ex.Message);
            Assert.Contains("ApiServicioInterno__Usuario", ex.Message);
            Assert.Contains("ApiServicioInterno__Password", ex.Message);
        }
    }
}
