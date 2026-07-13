using System.IdentityModel.Tokens.Jwt;
using AppLogic.Autenticacion.Services;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class TokenServiceInternalApiTests
    {
        [Fact]
        public void GenerateServiceToken_WithSecret_ReturnsJwtWithExpectedClaims()
        {
            using var scope = new EnvironmentVariableScope(
                ("SECRET_KEY_API_INSCR_PAGOS", "12345678901234567890123456789012"));
            var before = DateTime.UtcNow;
            var service = new TokenServiceInternalApi();

            var token = service.GenerateServiceToken(
                "api-inscripciones-pagos",
                "inscripciones.write",
                "pagos.read");

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal("api-admisiones", jwt.Issuer);
            Assert.Contains("api-inscripciones-pagos", jwt.Audiences);
            Assert.Equal("api-admisiones", jwt.Claims.First(c => c.Type == "service_name").Value);
            Assert.Equal("api-inscripciones-pagos", jwt.Claims.First(c => c.Type == "target_api").Value);
            Assert.Equal("inscripciones.write pagos.read", jwt.Claims.First(c => c.Type == "scope").Value);
            Assert.InRange(jwt.ValidTo, before.AddMinutes(4), before.AddMinutes(6));
        }

        [Fact]
        public void GenerateServiceToken_WithoutSecret_ThrowsInvalidOperationException()
        {
            using var scope = new EnvironmentVariableScope(("SECRET_KEY_API_INSCR_PAGOS", null));
            var service = new TokenServiceInternalApi();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                service.GenerateServiceToken("api-inscripciones-pagos"));

            Assert.Contains("SECRET_KEY_API_INSCR_PAGOS", exception.Message);
        }
    }
}
