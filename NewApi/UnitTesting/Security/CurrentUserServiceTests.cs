// File: UnitTesting/Security/CurrentUserServiceTests.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Utilities;
using WebApiFDP.Security;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Xunit.Sdk;
using Google.Protobuf.WellKnownTypes;

namespace UnitTesting.Security
{
    public class CurrentUserServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly DefaultHttpContext _httpContext;

        public CurrentUserServiceTests()
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

        [Fact]
        public void UserId_ReturnsValue_WhenClaimPresent()
        {
            SetClaims(new Claim(ClaimTypes.NameIdentifier, "123"));
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal(123, service.UserId);
        }

        [Fact]
        public void UserId_ReturnsNull_WhenClaimMissing()
        {
            SetClaims();
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Null(service.UserId);
        }

        [Fact]
        public void User_ReturnsUsuarioClaim()
        {
            SetClaims(new Claim("usuario", "testuser"));
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal("testuser", service.User);
        }

        [Fact]
        public void SessionId_ReturnsSesionClaim()
        {
            SetClaims(new Claim("sesion", "abc123"));
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal("abc123", service.SessionId);
        }

        [Fact]
        public void Email_ReturnsEmailClaim()
        {
            SetClaims(new Claim(ClaimTypes.Email, "a@b.com"));
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal("a@b.com", service.Email);
        }

        [Fact]
        public void IpAddress_ReturnsXForwardedForHeader()
        {
            _httpContext.Request.Headers["X-Forwarded-For"] = "1.2.3.4";
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal("1.2.3.4", service.IpAddress);
        }

        [Fact]
        public void IpAddress_ReturnsRemoteIpAddress_IfNoHeader()
        {
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("5.6.7.8");
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal("5.6.7.8", service.IpAddress);
        }

        [Fact]
        public void UserAgent_ReturnsUserAgentHeader()
        {
            _httpContext.Request.Headers["User-Agent"] = "TestAgent";
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal("TestAgent", service.UserAgent);
        }

        [Fact]
        public void HostName_ReturnsXClientHostHeader()
        {
            _httpContext.Request.Headers["X-Client-Host"] = "host123";
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal("host123", service.HostName);
        }

        [Fact]
        public void GetMetadata_ReturnsAllProperties()
        {
            SetClaims(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim("usuario", "u"),
                new Claim("sesion", "s"),
                new Claim(ClaimTypes.Email, "e@x.com")
            );
            _httpContext.Request.Headers["X-Forwarded-For"] = "ip";
            _httpContext.Request.Headers["User-Agent"] = "ua";
            _httpContext.Request.Headers["X-Client-Host"] = "hn";

            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            var meta = service.GetMetadata();

            Assert.Equal(42, meta.UserId);
            Assert.Equal("u", meta.User);
            Assert.Equal("s", meta.SessionId);
            Assert.Equal("e@x.com", meta.Email);
            Assert.Equal("ip", meta.IpAddress);
            Assert.Equal("ua", meta.UserAgent);
            Assert.Equal("hn", meta.HostName);
        }

        [Fact]
        public void GetUserId_ReturnsValue_WhenPresent()
        {
            SetClaims(new Claim(ClaimTypes.NameIdentifier, "99"));
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Equal(99, service.GetUserId());
        }

        [Fact]
        public void GetUserId_Throws_WhenNotPresent()
        {
            SetClaims();
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            Assert.Throws<UnauthorizedAccessException>(() => service.GetUserId());
        }

        [Fact]
        public void GetLogString_ReturnsExpectedString_WithContextAndResult()
        {
            SetClaims(new Claim(ClaimTypes.NameIdentifier, "7"));
            _httpContext.Request.Path = "/api/test";
            var service = new CurrentUserService(_mockHttpContextAccessor.Object);

            var opResult = OperationResult<string>.Ok("data", "TestMethod");
            var log = service.GetLogString("IN", opResult);
            var expectedMethod = "CódigoPersona: 7 | Método: api/test-IN | Resultado: {\"Success\":true,\"HttpCode\":200,\"ErrorCode\":\"\",\"Method\":\"TestMethod\",\"Message\":\"\",\"Data\":\"data\"}";

            Assert.Equal(expectedMethod, log);
        }

        [Fact]
        public void GetLogString_ReturnsExpectedString_WithoutContext()
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns((HttpContext)null);
            var service = new CurrentUserService(accessor.Object);

            var log = service.GetLogString<string>("OUT", null);

            Assert.Contains("Desconocido", log);
            Assert.Contains("Método: Desconocido", log);
            Assert.Contains("Resultado: Desconocido", log);
        }
    }
}
