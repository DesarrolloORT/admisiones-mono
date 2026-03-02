// File: UnitTesting/Security/CurrentUserServiceSourceSystemTests.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using WebApiFDP.Security;
using Xunit;

namespace UnitTesting.Security
{
    /// <summary>
    /// Tests para la funcionalidad de SourceSystem en CurrentUserService.
    /// </summary>
    public class CurrentUserServiceSourceSystemTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly DefaultHttpContext _httpContext;

        public CurrentUserServiceSourceSystemTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(_httpContext);
        }

        private void SetClaims(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _httpContext.User = principal;
        }

        [Theory]
        [InlineData("https://funcionarios.ort.edu.uy", "Funcionarios")]
        [InlineData("https://gestion.ort.edu.uy", "Gestion")]
        [InlineData("https://admisiones.ort.edu.uy", "Admisiones")]
        [InlineData("https://unknown.ort.edu.uy", "Unknown")]
        [InlineData(null, "Unknown")]
        [InlineData("", "Unknown")]
        public void GetSourceSystem_ReturnsCorrectSystem_BasedOnIssuerClaim(string issuerClaim, string expectedSystem)
        {
            // Arrange
            if (!string.IsNullOrEmpty(issuerClaim))
            {
                SetClaims(new Claim("iss", issuerClaim));
            }
            else
            {
                SetClaims();
            }

            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            // Act
            var result = service.GetSourceSystem();

            // Assert
            Assert.Equal(expectedSystem, result);
        }

        [Theory]
        [InlineData("https://funcionarios.ort.edu.uy", "Funcionarios")]
        [InlineData("https://gestion.ort.edu.uy", "Gestion")]
        [InlineData("https://admisiones.ort.edu.uy", "Admisiones")]
        public void SourceSystem_Property_ReturnsCorrectValue(string issuerClaim, string expectedSystem)
        {
            // Arrange
            SetClaims(new Claim("iss", issuerClaim));
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            // Act
            var result = service.SourceSystem;

            // Assert
            Assert.Equal(expectedSystem, result);
        }

        [Fact]
        public void SourceSystem_ReturnsUnknown_WhenNoIssuerClaim()
        {
            // Arrange
            SetClaims(new Claim(ClaimTypes.NameIdentifier, "123"));
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            // Act
            var result = service.SourceSystem;

            // Assert
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void SourceSystem_Constants_HaveCorrectValues()
        {
            // Assert
            Assert.Equal("Funcionarios", SourceSystems.Funcionarios);
            Assert.Equal("Gestion", SourceSystems.Gestion);
            Assert.Equal("Admisiones", SourceSystems.Admisiones);
            Assert.Equal("Unknown", SourceSystems.Unknown);
        }
    }
}
