// File: UnitTesting/Utilities/UtilTests.cs
using System;
using System.Collections.Generic;
using Utilities;
using Xunit;

namespace UnitTesting.Utilities
{
    public class UtilTests
    {
        [Theory]
        [InlineData("usuario@dominio.com", true)]
        [InlineData("nombre.apellido@empresa.org", true)]
        [InlineData("usuario+etiqueta@dominio.co", true)]
        [InlineData("usuario@sub.dominio.com", true)]
        [InlineData("usuario@dominio..com", false)]
        [InlineData("@dominio.com", false)]
        [InlineData("usuario@dominio", false)]
        [InlineData("usuario@dominio.c", false)]
        [InlineData("usuario@@dominio.com", false)]
        public void EsCorreoValido_ValidaCorreos(string correo, bool esperado)
        {
            var result = Util.EsCorreoValido(correo);
            Assert.Equal(esperado, result);
        }

        [Fact]
        public void RemplazarTildes_ReemplazaCorrectamente()
        {
            var texto = "áéíóú ÁÉÍÓÚ";
            var esperado = "aeiou aeiou";
            var result = Util.RemplazarTildes(texto);
            Assert.Equal(esperado, result);
        }

        [Theory]
        [InlineData("1234567-2", "")]
        [InlineData("1234567-1", "El dígito verificador no es correcto.")]
        [InlineData("1234567#8", "El número de documento de la persona debe ser numérico (sin puntos) seguido de un guión (-) y el dígito verificador.")]
        [InlineData("1234567-8-9", "El número de documento de la persona debe ser numérico (sin puntos) seguido de un guión (-) y el dígito verificador.")]
        [InlineData("1234567-a", "La cédula solo puede contener números y un guión.")]
        public void ValidoCI_ValidaCedula(string ci, string esperado)
        {
            var result = Util.ValidoCI(ci);
            Assert.Equal(esperado, result);
        }

        [Theory]
        [InlineData("Password123!", "Password123!", "La nueva contraseña no puede ser igual a la actual.")]
        [InlineData("", "Password123!", "Parámetros incorrectos")]
        [InlineData("Password123!", "", "Parámetros incorrectos")]
        [InlineData("Password123!", "short1A!", "La contraseña debe tener entre 12 y 20 caracteres")]
        [InlineData("Password123!", "Password123456", "Parámetros incorrectos. Caracteres no válidos.")]
        [InlineData("Password123!", "NuevaPassword1!", "")]
        public void ValidarPassword_ValidaPassword(string actual, string nuevo, string esperado)
        {
            var result = Util.ValidarPassword(actual, nuevo);
            Assert.Equal(esperado, result);
        }

        [Theory]
        [InlineData("NuevaPassword1!")]
        [InlineData("")]
        [InlineData("short1A!")]
        [InlineData("Password123456")]
        public void ValidarPasswordNueva_ValidaPasswordInicial(string nuevo)
        {
            var result = Util.ValidarPasswordNueva(nuevo);
            Assert.Equal(Util.ValidarPassword("ActualPassword1!", nuevo), result);
        }

        [Fact]
        public void ConvertirTextoHTML_ReemplazaSaltosYTabulaciones()
        {
            var texto = "Línea1\nLínea2\tLínea3\rLínea4";
            var esperado = "Línea1<br>Línea2&nbsp;Línea3<br>Línea4";
            var result = Util.ConvertirTextoHTML(texto);
            Assert.Equal(esperado, result);
        }

        [Theory]
        [InlineData(1.0000000001, 1.0000000001, true)]
        [InlineData(1.0000001, 1.0000002, false)]
        public void AreDoublesEqual_ValidaDoubles(double v1, double v2, bool esperado)
        {
            var result = Util.AreDoublesEqual(v1, v2);
            Assert.Equal(esperado, result);
        }

        [Fact]
        public void FirmarYVerificarFirma_ObjetoSimple()
        {
            var obj = new { Nombre = "Juan", Edad = 30 };
            var firma = Util.Firmar(obj);
            var esValida = Util.VerificarFirma(obj, firma);
            Assert.True(esValida);
        }

        [Fact]
        public void FirmarYVerificarFirma_ListaObjetos()
        {
            var lista = new List<object> { new { Nombre = "Ana" }, new { Nombre = "Luis" } };
            var firma = Util.Firmar(lista);
            var esValida = Util.VerificarFirma(lista, firma);
            Assert.True(esValida);
        }

        [Theory]
        [InlineData("1", "Enero")]
        [InlineData("2", "Febrero")]
        [InlineData("12", "Diciembre")]
        [InlineData("13", "Número de mes inválido")]
        [InlineData("00", "Número de mes inválido")]
        public void ObtenerNombreMes_DevuelveNombreCorrecto(string numeroMes, string esperado)
        {
            var result = Util.ObtenerNombreMes(numeroMes);
            Assert.Equal(esperado, result);
        }
    }
}
