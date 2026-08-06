using AppLogic.Identity;
using AppLogic.Contracts.Text;

namespace UnitTesting.AppLogic.Common.Validation
{
    public class IdentityDocumentRulesTests
    {
        [Fact]
        public void ValidarDocumentoBase_WithUnsupportedType_ReturnsInvalidType()
        {
            var result = IdentityDocumentRules.ValidateBaseDocument("XX", "1234567-2");

            Assert.False(result.IsValid);
            Assert.Equal(IdentityDocumentRules.DocumentValidationError.InvalidDocumentType, result.Error);
            Assert.Equal("Tipo de documento inválido.", result.Message);
        }

        [Fact]
        public void ValidarDocumentoBase_WithValidPassport_ReturnsValid()
        {
            var result = IdentityDocumentRules.ValidateBaseDocument(" PS ", " A12345 ");

            Assert.True(result.IsValid);
            Assert.Equal(IdentityDocumentRules.DocumentValidationError.None, result.Error);
        }

        [Theory]
        [InlineData("  hola  ", "hola")]
        [InlineData(null, "")]
        public void Normalizar_TrimsNullSafely(string? value, string expected)
        {
            Assert.Equal(expected, TextNormalization.Trim(value));
        }

        [Fact]
        public void NormalizarMayusculas_RemovesAccentsAndUppercases()
        {
            Assert.Equal("JOSE PEREZ", TextNormalization.ToUpperWithoutAccents(" José Pérez "));
        }

        [Fact]
        public void FormatoCapital_FormatsText()
        {
            Assert.Equal("Ana Perez", TextNormalization.ToTitleCaseInvariant(" ANA PEREZ "));
            Assert.Equal(string.Empty, TextNormalization.ToTitleCaseInvariant("   "));
        }

        [Theory]
        [InlineData(" si ", "SI")]
        [InlineData(" no ", "NO")]
        [InlineData(null, null)]
        public void NormalizarSiNo_NormalizesOptionalValue(string? value, string? expected)
        {
            Assert.Equal(expected, TextNormalization.NormalizeYesNo(value));
        }

        [Theory]
        [InlineData("CI", "1.234.567-8", "12345678")]
        [InlineData("PS", " ab-123 ", "AB-123")]
        public void NormalizarDocumentoIdentidad_NormalizesByDocumentType(
            string documentType,
            string document,
            string expected)
        {
            Assert.Equal(expected, IdentityDocumentRules.NormalizeIdentityDocument(documentType, document));
        }

        [Theory]
        [InlineData("1.234.567-8", "12345678")]
        [InlineData("1234567-8", "12345678")]
        [InlineData("12345678", "12345678")]
        [InlineData(" 1.234.567-8 ", "12345678")]
        [InlineData("  1 2 3  ", "123")]
        [InlineData("AbC-123", "abc123")]
        [InlineData(null, "unknown")]
        [InlineData("", "unknown")]
        [InlineData("   ", "unknown")]
        public void NormalizarDocumentoParaClave_RemovesSeparators(string? document, string expected)
        {
            Assert.Equal(expected, TextNormalization.NormalizeDocumentForKey(document));
        }

        [Fact]
        public void NormalizarDocumentoParaClave_IsIdempotent()
        {
            var once = TextNormalization.NormalizeDocumentForKey("1.234.567-8");
            var twice = TextNormalization.NormalizeDocumentForKey(once);

            Assert.Equal(once, twice);
        }

        [Fact]
        public void FormatearTextoCapitalizado_CollapsesSpaces()
        {
            Assert.Equal("Ana Perez", TextNormalization.ToTitleCase("  ANA   PEREZ "));
        }
    }
}
