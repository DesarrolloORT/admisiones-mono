using AppLogic.Autenticacion.Helpers;

namespace UnitTesting.AppLogic.Helpers
{
    public class EmailMaskingHelperTests
    {
        [Theory]
        [InlineData("gabriele@ort.edu.uy", "g******e@ort.******")]
        [InlineData("  gabriele@ort.edu.uy  ", "g******e@ort.******")]
        [InlineData("a@example.com", "a******@example.******")]
        [InlineData("ab@example.com", "a******b@example.******")]
        public void MaskEmail_WithValidEmail_ReturnsMaskedEmail(string email, string expected)
        {
            var result = EmailMaskingHelper.Mask(email);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("gabriele")]
        [InlineData("@ort.edu.uy")]
        [InlineData("gabriele@")]
        [InlineData("gabriele@ort")]
        [InlineData("gabriele@ort.")]
        [InlineData("gabriele@@ort.edu.uy")]
        public void MaskEmail_WithInvalidEmail_ReturnsOnlyMask(string? email)
        {
            var result = EmailMaskingHelper.Mask(email);

            Assert.Equal("******", result);
        }
    }
}
