using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using BusinessLogic.Entities;
using AppLogic.Autenticacion.Services;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class TokenServiceTests
    {
        private readonly TokenService _service = new();

        [Fact]
        public void GenerateAccessToken_WithValidEnvironment_ReturnsJwtWithExpectedClaims()
        {
            using var scope = new EnvironmentVariableScope(
                ("JWT_SECRET_KEY", "this_is_a_secret_key_with_enough_length_12345"),
                ("JWT_ISSUER_TOKEN_ADMISIONES", "issuer-test"),
                ("JWT_AUDIENCE_TOKEN_ADMISIONES", "audience-test"),
                ("JWT_EXPIRE_MINUTES_ADMISIONES", "15"));

            var before = DateTime.UtcNow;

            var token = _service.GenerateAccessToken(new Persona { CodigoPersona = 12345 });

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal("issuer-test", jwt.Issuer);
            Assert.Contains("audience-test", jwt.Audiences);
            Assert.Equal("12345", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal("12345", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
            Assert.InRange(jwt.ValidTo, before.AddMinutes(14), before.AddMinutes(16));
        }

        [Fact]
        public void GenerateRefreshToken_ReturnsBase64AndRandomValue()
        {
            var token1 = _service.GenerateRefreshToken();
            var token2 = _service.GenerateRefreshToken();

            Assert.NotEmpty(token1);
            Assert.NotEmpty(token2);
            Assert.NotEqual(token1, token2);
            Assert.Equal(64, Convert.FromBase64String(token1).Length);
            Assert.Equal(64, Convert.FromBase64String(token2).Length);
        }

        [Fact]
        public void HashToken_ReturnsExpectedSha256Base64Hash()
        {
            var token = "refresh-token";
            var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

            var result = _service.HashToken(token);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateAccessToken_WithoutSecretKey_ThrowsInvalidOperationException()
        {
            using var scope = new EnvironmentVariableScope(
                ("JWT_SECRET_KEY", null),
                ("JWT_ISSUER_TOKEN_ADMISIONES", "issuer-test"),
                ("JWT_AUDIENCE_TOKEN_ADMISIONES", "audience-test"),
                ("JWT_EXPIRE_MINUTES_ADMISIONES", "15"));

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _service.GenerateAccessToken(new Persona { CodigoPersona = 1 }));

            Assert.Contains("JWT_SECRET_KEY", exception.Message);
        }

        [Fact]
        public void GenerateAccessToken_WithoutExpiration_ThrowsInvalidOperationException()
        {
            using var scope = new EnvironmentVariableScope(
                ("JWT_SECRET_KEY", "this_is_a_secret_key_with_enough_length_12345"),
                ("JWT_ISSUER_TOKEN_ADMISIONES", "issuer-test"),
                ("JWT_AUDIENCE_TOKEN_ADMISIONES", "audience-test"),
                ("JWT_EXPIRE_MINUTES_ADMISIONES", null));

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _service.GenerateAccessToken(new Persona { CodigoPersona = 1 }));

            Assert.Contains("JWT_EXPIRE_MINUTES_ADMISIONES", exception.Message);
        }

        [Fact]
        public void GenerateAccessToken_WithInvalidExpiration_ThrowsInvalidOperationException()
        {
            using var scope = new EnvironmentVariableScope(
                ("JWT_SECRET_KEY", "this_is_a_secret_key_with_enough_length_12345"),
                ("JWT_ISSUER_TOKEN_ADMISIONES", "issuer-test"),
                ("JWT_AUDIENCE_TOKEN_ADMISIONES", "audience-test"),
                ("JWT_EXPIRE_MINUTES_ADMISIONES", "quince"));

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _service.GenerateAccessToken(new Persona { CodigoPersona = 1 }));

            Assert.Contains("valor inv", exception.Message);
        }
    }
}
