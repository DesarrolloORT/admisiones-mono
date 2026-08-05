using AppLogic.Authentication.Security;
using UnitTesting.AppLogic.Services;

namespace UnitTesting.AppLogic.Authentication.Security
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class JwtConfigurationTests
    {
        private const string Variable = "JWT_CONFIG_HELPER_TEST_VAR";

        [Fact]
        public void GetRequiredEnvironmentVariable_WhenPresent_ReturnsValue()
        {
            using var scope = new EnvironmentVariableScope((Variable, "some-value"));

            var result = JwtConfiguration.GetRequiredEnvironmentVariable(Variable);

            Assert.Equal("some-value", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetRequiredEnvironmentVariable_WhenMissing_ThrowsInvalidOperationException(string? value)
        {
            using var scope = new EnvironmentVariableScope((Variable, value));

            var exception = Assert.Throws<InvalidOperationException>(
                () => JwtConfiguration.GetRequiredEnvironmentVariable(Variable));

            Assert.Contains(Variable, exception.Message);
        }

        [Fact]
        public void GetRequiredDouble_WhenPresentAndValid_ReturnsParsedValue()
        {
            using var scope = new EnvironmentVariableScope((Variable, "15"));

            var result = JwtConfiguration.GetRequiredDouble(Variable);

            Assert.Equal(15d, result);
        }

        [Fact]
        public void GetRequiredDouble_WithDecimalValue_ParsesUsingInvariantCulture()
        {
            using var scope = new EnvironmentVariableScope((Variable, "15.5"));

            var result = JwtConfiguration.GetRequiredDouble(Variable);

            Assert.Equal(15.5d, result);
        }

        [Fact]
        public void GetRequiredDouble_WhenMissing_ThrowsInvalidOperationException()
        {
            using var scope = new EnvironmentVariableScope((Variable, null));

            var exception = Assert.Throws<InvalidOperationException>(
                () => JwtConfiguration.GetRequiredDouble(Variable));

            Assert.Contains(Variable, exception.Message);
        }

        [Fact]
        public void GetRequiredDouble_WhenInvalidFormat_ThrowsInvalidOperationException()
        {
            using var scope = new EnvironmentVariableScope((Variable, "quince"));

            var exception = Assert.Throws<InvalidOperationException>(
                () => JwtConfiguration.GetRequiredDouble(Variable));

            Assert.Contains(Variable, exception.Message);
        }
    }
}
