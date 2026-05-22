using AppLogic.Utilities;

namespace UnitTesting.AppLogic.Utilities
{
    public class DocumentUtilsTests
    {
        [Fact]
        public void ValidarDocumentoBase_WithUnsupportedType_ReturnsInvalidType()
        {
            var result = DocumentUtils.ValidarDocumentoBase("XX", "1234567-2");

            Assert.False(result.IsValid);
            Assert.Equal(DocumentUtils.DocumentValidationError.InvalidDocumentType, result.Error);
            Assert.Equal("Tipo de documento inválido.", result.Message);
        }

        [Fact]
        public void ValidarDocumentoBase_WithValidPassport_ReturnsValid()
        {
            var result = DocumentUtils.ValidarDocumentoBase(" PS ", " A12345 ");

            Assert.True(result.IsValid);
            Assert.Equal(DocumentUtils.DocumentValidationError.None, result.Error);
        }

        [Theory]
        [InlineData("  hola  ", "hola")]
        [InlineData(null, "")]
        public void Normalizar_TrimsNullSafely(string? value, string expected)
        {
            Assert.Equal(expected, DocumentUtils.Normalizar(value));
        }

        [Fact]
        public void NormalizarMayusculas_RemovesAccentsAndUppercases()
        {
            Assert.Equal("JOSE PEREZ", DocumentUtils.NormalizarMayusculas(" José Pérez "));
        }

        [Fact]
        public void FormatoCapital_FormatsText()
        {
            Assert.Equal("Ana Perez", DocumentUtils.FormatoCapital(" ANA PEREZ "));
            Assert.Equal(string.Empty, DocumentUtils.FormatoCapital("   "));
        }
    }
}
