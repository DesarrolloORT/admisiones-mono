using AppLogic.Contracts.Text;
using Xunit;

namespace UnitTesting.AppLogic.Text
{
    /// <summary>
    /// El formato guardado tiene que coincidir con FDP (+59899333222). El país lo informa el cliente:
    /// no hay default, así que un número local sin ISO2 no valida.
    /// </summary>
    public class PhoneNormalizationTests
    {
        [Theory]
        [InlineData("99333222", "UY", "+59899333222")]          // nacional sin el 0
        [InlineData("099333222", "UY", "+59899333222")]         // nacional con el 0
        [InlineData(" 099 333-222 ", "UY", "+59899333222")]     // con espacios y guiones
        [InlineData("+59899333222", "UY", "+59899333222")]      // ya en E.164
        [InlineData("+59899333222", null, "+59899333222")]      // E.164 sin ISO2: la librería detecta el país
        [InlineData("0059899333222", "UY", "+59899333222")]     // prefijo internacional 00
        [InlineData("91122223333", "AR", "+5491122223333")]     // celular argentino en formato nacional
        public void Validate_CelularValido_DevuelveE164(string phone, string? iso2, string esperado)
        {
            var result = PhoneNormalization.Validate(phone, isPrimaryPhone: true, iso2);

            Assert.NotNull(result);
            Assert.True(result!.TelefonoValido, result.Error);
            Assert.Equal(esperado, result.TelefonoE164);
        }

        [Fact]
        public void Validate_SinIso2YNumeroLocal_Invalido()
        {
            // Sin ISO2 y sin '+' no hay país de referencia: es el motivo de que el front tenga que
            // mandar el ISO2, igual que FDP.
            var result = PhoneNormalization.Validate("99333222", isPrimaryPhone: true, iso2: null);

            Assert.NotNull(result);
            Assert.False(result!.TelefonoValido);
        }

        [Fact]
        public void Validate_Iso2QueNoCoincideConElNumero_Invalido()
        {
            var result = PhoneNormalization.Validate("+5491122223333", isPrimaryPhone: true, iso2: "UY");

            Assert.NotNull(result);
            Assert.False(result!.TelefonoValido);
        }

        [Fact]
        public void Validate_FijoComoPrincipal_Invalido()
        {
            var result = PhoneNormalization.Validate("24001234", isPrimaryPhone: true, iso2: "UY");

            Assert.NotNull(result);
            Assert.False(result!.TelefonoValido);
        }

        [Fact]
        public void Validate_FijoComoSecundario_DevuelveE164()
        {
            var result = PhoneNormalization.Validate("24001234", isPrimaryPhone: false, iso2: "UY");

            Assert.NotNull(result);
            Assert.True(result!.TelefonoValido, result.Error);
            Assert.Equal("+59824001234", result.TelefonoE164);
        }

        [Fact]
        public void Validate_NumeroBasura_Invalido()
        {
            var result = PhoneNormalization.Validate("222", isPrimaryPhone: true, iso2: "UY");

            Assert.NotNull(result);
            Assert.False(result!.TelefonoValido);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_SinTelefono_DevuelveNull(string? phone)
        {
            // null = no vino teléfono, distinto de "vino y es inválido". Además el validador de Core
            // tira NullReferenceException si le pasan null directo.
            Assert.Null(PhoneNormalization.Validate(phone, isPrimaryPhone: true, iso2: "UY"));
        }
    }
}
