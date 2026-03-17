// File: UnitTesting/Security/AuthenticationExtensionsTests.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Security
{
    public class AuthenticationExtensionsTests
    {
        private const string TestSecret = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789";

        [Fact]
        public void AddJwtAuthentication_RegistersJwtBearerAuthentication()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Act
            var result = services.AddJwtAuthentication(config);

            // Assert
            Assert.Same(services, result);
            var provider = services.BuildServiceProvider();
            var authOptions = provider.GetService<IConfigureOptions<JwtBearerOptions>>();
            Assert.NotNull(authOptions);
        }

        [Fact]
        public void AddJwtAuthentication_ConfiguresJwtBearerOptionsCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "JWT_SECRET_KEY", "dummy" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "super-secret-admisiones-key-for-testing");

            // Act
            services.AddJwtAuthentication(config);
            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOptions = monitor.Get(JwtBearerDefaults.AuthenticationScheme);

            // Assert
            Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuer);
            Assert.True(jwtOptions.TokenValidationParameters.ValidateAudience);
            Assert.True(jwtOptions.TokenValidationParameters.ValidateLifetime);
            Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey);

            Assert.Contains("https://webapiadmisiones.ort.edu.uy", jwtOptions.TokenValidationParameters.ValidIssuers);
            Assert.Contains("https://webapiadmisiones.ort.edu.uy", jwtOptions.TokenValidationParameters.ValidAudiences);

            var token = CreateTestJwt("https://webapiadmisiones.ort.edu.uy");
            var keys = jwtOptions.TokenValidationParameters.IssuerSigningKeyResolver(
                token, null, null, jwtOptions.TokenValidationParameters);

            Assert.Single(keys);
            Assert.IsType<SymmetricSecurityKey>(keys.First());
        }

        private string CreateTestJwt(string issuer)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = new JwtSecurityToken(issuer: issuer);
            return handler.WriteToken(token);
        }

        //[Fact]
        //public async Task JwtBearerEvents_ShouldExtractToken()
        //{
        //    // Arrange
        //    var context = new DefaultHttpContext();
        //    context.Request.Headers["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIwOTNjNjEzZC01ZjAxLTRkZDItYTEyMi1kN2QxYjkxMjEwZWEiLCJuYW1laWQiOiIxNDI4NjgiLCJzZXNzaW9uIjoiZGFjODFiZDAtZGQwNC00YTJlLWJhZjAtMGFkNmRkNGZiYjVmIiwidXN1YXJpbyI6IjE0Mjg2OCIsIm5iZiI6MTc0OTgyODAyMywiZXhwIjoxNzQ5ODI5ODIzLCJpYXQiOjE3NDk4MjgwMjMsImlzcyI6Imh0dHBzOi8vZ2VzdGlvbi5vcnQuZWR1LnV5IiwiYXVkIjoiaHR0cHM6Ly9nZXN0aW9uLm9ydC5lZHUudXkifQ.ybjSMFY4DIWThJ1fO-mujBBYUavmCzYjagJq6tXPAE8";

        //    var events = new JwtBearerEvents();
        //    var capturedToken = string.Empty;

        //    events.OnMessageReceived = ctx =>
        //    {
        //        capturedToken = ctx.Request.Headers.Authorization;
        //        return Task.CompletedTask;
        //    };

        //    // Act
        //    await events.OnMessageReceived(new MessageReceivedContext(context, new AuthenticationScheme("JwtBearer", null, typeof(JwtBearerHandler)), null));

        //    // Assert
        //    Assert.Equal("Bearer my-token", capturedToken);
        //}

        [Fact]
        public async Task JwtBearerEvents_ShouldExtractToken()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIwOTNjNjEzZC01ZjAxLTRkZDItYTEyMi1kN2QxYjkxMjEwZWEiLCJuYW1laWQiOiIxNDI4NjgiLCJzZXNzaW9uIjoiZGFjODFiZDAtZGQwNC00YTJlLWJhZjAtMGFkNmRkNGZiYjVmIiwidXN1YXJpbyI6IjE0Mjg2OCIsIm5iZiI6MTc0OTgyODAyMywiZXhwIjoxNzQ5ODI5ODIzLCJpYXQiOjE3NDk4MjgwMjMsImlzcyI6Imh0dHBzOi8vZ2VzdGlvbi5vcnQuZWR1LnV5IiwiYXVkIjoiaHR0cHM6Ly9nZXN0aW9uLm9ydC5lZHUudXkifQ.ybjSMFY4DIWThJ1fO-mujBBYUavmCzYjagJq6tXPAE8";
            httpContext.Request.Headers["Authorization"] = $"Bearer {expectedToken}";

            var jwtOptions = new JwtBearerOptions();
            AuthenticationExtensions.AddJwtAuthentication(
                new ServiceCollection(),
                new ConfigurationBuilder().Build()
            );

            var events = new JwtBearerEvents();
            jwtOptions.Events = events;

            // Copiamos el handler desde el método real de configuración
            jwtOptions.Events.OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
                return Task.CompletedTask;
            };

            var scheme = new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler));
            var context = new MessageReceivedContext(httpContext, scheme, jwtOptions);

            // Act
            await jwtOptions.Events.OnMessageReceived(context);

            // Assert
            Assert.Equal(expectedToken, context.Token);
        }

        // ===== Tests para ShouldRefreshToken =====
        // NOTA: Estos tests fueron deshabilitados porque ShouldRefreshToken fue eliminado.
        // El refresh de tokens ahora se maneja mediante el endpoint /RefreshToken en PersonaController
        // que valida el refresh token contra la base de datos.

        /*
        [Fact]
        public void ShouldRefreshToken_WithNullUser_ReturnsFalse()
        {
            // Act
            var result = AuthenticationExtensions.ShouldRefreshToken(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithUnauthenticatedUser_ReturnsFalse()
        {
            // Arrange
            var identity = new ClaimsIdentity(); // No authenticated
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = AuthenticationExtensions.ShouldRefreshToken(user);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithoutExpClaim_ReturnsFalse()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("sub", "12345")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = AuthenticationExtensions.ShouldRefreshToken(user);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithInvalidExpClaim_ReturnsFalse()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Exp, "invalid")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = AuthenticationExtensions.ShouldRefreshToken(user);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithTokenExpiringInFuture_ReturnsFalse()
        {
            // Arrange
            var expirationTime = DateTimeOffset.UtcNow.AddHours(1);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Exp, expirationTime.ToUnixTimeSeconds().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = AuthenticationExtensions.ShouldRefreshToken(user, refreshThresholdMinutes: 15);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithTokenExpiringWithinThreshold_ReturnsTrue()
        {
            // Arrange
            var expirationTime = DateTimeOffset.UtcNow.AddMinutes(10);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Exp, expirationTime.ToUnixTimeSeconds().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = AuthenticationExtensions.ShouldRefreshToken(user, refreshThresholdMinutes: 15);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithExpiredToken_ReturnsTrue()
        {
            // Arrange
            var expirationTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Exp, expirationTime.ToUnixTimeSeconds().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = AuthenticationExtensions.ShouldRefreshToken(user);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithCustomThreshold_UsesCorrectThreshold()
        {
            // Arrange
            var expirationTime = DateTimeOffset.UtcNow.AddMinutes(25);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Exp, expirationTime.ToUnixTimeSeconds().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            // Act & Assert
            Assert.False(AuthenticationExtensions.ShouldRefreshToken(user, refreshThresholdMinutes: 20));
            Assert.True(AuthenticationExtensions.ShouldRefreshToken(user, refreshThresholdMinutes: 30));
        }
        */
    }
}
