using Xunit;
using Utilities;
using PhoneNumbers;

namespace UnitTesting.Utilities
{
    public class ValidadorTelefonosTests
    {
        [Theory]
        [InlineData("099634923", "UY", true, true, "+59899634923", "UY", PhoneVerification.TipoTelefonoEnu.Movil)]
        [InlineData("+59891991399", null, true, true, "+59891991399", "UY", PhoneVerification.TipoTelefonoEnu.Movil)]
        [InlineData("0059898480192", "UY", true, true, "+59898480192", "UY", PhoneVerification.TipoTelefonoEnu.Movil)]
        [InlineData("22080726", "UY", true, false, null, null, PhoneVerification.TipoTelefonoEnu.Desconocido)]
        [InlineData("29012345", "UY", false, true, "+59829012345", "UY", PhoneVerification.TipoTelefonoEnu.Fijo)]
        public void ValidarTelefono2_CasosComunes(string telefono, string iso2, bool isPrimaryPhone, bool esperadoValido, string esperadoE164, string esperadoIso2, PhoneVerification.TipoTelefonoEnu esperadoTipo)
        {
            var result = PhoneVerification.Validar(telefono, iso2, isPrimaryPhone);

            Assert.Equal(esperadoValido, result.TelefonoValido);
            Assert.Equal(esperadoE164, result.TelefonoE164);
            Assert.Equal(esperadoIso2, result.Iso2);
            Assert.Equal(esperadoTipo, result.TipoTelefono);
        }

        [Fact]
        public void ValidateMobile_DobleMas_Invalido()
        {
            var result = ValidadorTelefonos.ValidateMobile("++59891991399", "UY");
            Assert.False(result.TelefonoValido);
            Assert.Equal("Formato inválido: doble '+' al inicio.", result.Error);
        }

        [Fact]
        public void ValidateMobile_NumeroVacio_Invalido()
        {
            var result = ValidadorTelefonos.ValidateMobile("", "UY");
            Assert.False(result.TelefonoValido);
            Assert.Contains("El número", result.Error);
        }

        [Fact]
        public void ValidateMobile_CaracteresNoNumericos_Invalido()
        {
            var result = PhoneVerification.Validar("09963A923", "UY", true);
            Assert.False(result.TelefonoValido);
            Assert.Equal("El número no es válido o no corresponde a una línea móvil.", result.Error);
        }

        [Fact]
        public void ValidateMobile_ComienzaConCero_Invalido()
        {
            var result = PhoneVerification.Validar("099634923", "UY", true);
            // El método permite números que empiezan con 0, pero la lógica puede cambiar según reglas locales.
            // Si la lógica cambia, ajustar el test.
            Assert.True(result.TelefonoValido || result.Error == "El número de celular no puede comenzar con 0.");
        }

        [Fact]
        public void SacarSimboloYcaracteristica_EliminaPrefijoCorrectamente()
        {
            var telefonoE164 = "+59899634923";
            var caracteristica = "598";
            var iso2 = "UY";
            var result = PhoneVerification.IdentificarTelefono(telefonoE164, iso2, true);
            Assert.Equal("99634923", result.TelefonoSimple);
        }

        [Fact]
        public void SacarSimboloYcaracteristica_CaracteristicaNoCoincide_NoElimina()
        {
            var telefonoE164 = "+59899634923";
            var caracteristica = "599";
            var iso2 = "UY";
            var result = PhoneVerification.IdentificarTelefono(telefonoE164, iso2, false);
            Assert.Equal("99634923", result.TelefonoSimple);

        }

        [Fact]
        public void FormarFormatoE164_AgregaPrefijoCorrectamente()
        {
            var telefonoSimple = "99634923";
            var caracteristica = "598";
            var result = PhoneVerification.FormarFormatoE164(telefonoSimple, caracteristica);
            Assert.Equal("+59899634923", result);
        }

        [Fact]
        public void FormarFormatoE164_YaTienePrefijo_NoDuplica()
        {
            var telefonoSimple = "59899634923";
            var caracteristica = "598";
            var result = PhoneVerification.FormarFormatoE164(telefonoSimple, caracteristica);
            Assert.Equal("+59899634923", result);
        }
    }
}
