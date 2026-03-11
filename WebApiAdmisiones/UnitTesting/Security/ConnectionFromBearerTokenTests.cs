using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Security
{
    [Collection("NoParallelEnvTests")]
    public class ConnectionFromBearerTokenTests : IDisposable
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public ConnectionFromBearerTokenTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockConfiguration = new Mock<IConfiguration>();
        }

        private static string CreateJwtToken(string issuer = null)
        {
            var handler = new JwtSecurityTokenHandler();
            var claims = new List<Claim>();
            if (issuer != null)
                claims.Add(new Claim("iss", issuer));
            var token = new JwtSecurityToken(issuer: issuer, claims: claims);
            return handler.WriteToken(token);
        }

        [Fact]
        public void GetConnectionString_NoAuthorizationHeader_ReturnsEmpty()
        {
            var context = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(context);

            var service = new ConnectionFromBearerToken(_mockHttpContextAccessor.Object, _mockConfiguration.Object);
            var result = service.GetConnectionString();

            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData("https://funcionarios.ort.edu.uy", "OracleConnectionStringFuncionarios", "FUNC_CONN")]
        [InlineData("https://gestion.ort.edu.uy", "OracleConnectionStringGestion", "GEST_CONN")]
        [InlineData("https://admisiones.ort.edu.uy", "OracleConnectionStringAdmisiones", "ADM_CONN")]
        public void GetConnectionString_KnownIssuer_ReturnsEnvConnectionString(string issuer, string envVar, string expectedConn)
        {
            try
            {
                var token = CreateJwtToken(issuer);
                var context = new DefaultHttpContext();
                context.Request.Headers["Authorization"] = $"Bearer {token}";
                _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(context);

                System.Environment.SetEnvironmentVariable(envVar, expectedConn);

                var service = new ConnectionFromBearerToken(_mockHttpContextAccessor.Object, _mockConfiguration.Object);
                var result = service.GetConnectionString();

                // Assert that result matches the actual environment variable value
                Assert.Equal(System.Environment.GetEnvironmentVariable(envVar), result);
            }
            finally
            {
                // 🧼 Limpieza del entorno para evitar efectos colaterales
                System.Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        [Fact]
        public void GetConnectionString_UnknownIssuer_ReturnsConfigConnectionString()
        {
            var token = CreateJwtToken("https://otro.issuer.com");
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = $"Bearer {token}";
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(context);

            _mockConfiguration.Setup(c => c["OracleConnectionString"]).Returns("DEFAULT_CONN");

            var service = new ConnectionFromBearerToken(_mockHttpContextAccessor.Object, _mockConfiguration.Object);
            var result = service.GetConnectionString();

            Assert.Equal("DEFAULT_CONN", result);
        }

        [Fact]
        public void GetConnectionString_NoIssuerClaim_ReturnsDefaultEnvConnectionString()
        {
            try
            {
                var token = CreateJwtToken(null);
                var context = new DefaultHttpContext();
                context.Request.Headers["Authorization"] = $"Bearer {token}";
                _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(context);

                System.Environment.SetEnvironmentVariable("OracleConnectionString", "DEFAULT_ENV_CONN");

                var service = new ConnectionFromBearerToken(_mockHttpContextAccessor.Object, _mockConfiguration.Object);
                var result = service.GetConnectionString();

                Assert.Equal("DEFAULT_ENV_CONN", result);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OracleConnectionString", null);
            }
        }

        public void Dispose()
        {
            // Clean up environment variables to avoid cross-test contamination
            System.Environment.SetEnvironmentVariable("OracleConnectionString", null);
            System.Environment.SetEnvironmentVariable("OracleConnectionStringFuncionarios", null);
            System.Environment.SetEnvironmentVariable("OracleConnectionStringGestion", null);
            System.Environment.SetEnvironmentVariable("OracleConnectionStringAdmisiones", null);
        }
    }
}
