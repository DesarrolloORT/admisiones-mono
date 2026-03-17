using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Security
{
    public class LoggingHelperTests
    {
        #region Helper Methods

        private static DefaultHttpContext CreateHttpContext(
            string? method = "GET",
            string? path = "/test/endpoint",
            bool authenticated = false,
            string? issuer = null,
            string? codigoPersona = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var context = new DefaultHttpContext();
            
            if (method != null)
                context.Request.Method = method;
            
            if (path != null)
                context.Request.Path = path;

            if (authenticated)
            {
                var claims = new List<Claim>();
                
                if (issuer != null)
                    claims.Add(new Claim("iss", issuer));
                
                if (codigoPersona != null)
                    claims.Add(new Claim("usuario", codigoPersona));
                
                var identity = new ClaimsIdentity(claims, "TestAuthType");
                context.User = new ClaimsPrincipal(identity);
            }

            if (ipAddress != null)
                context.Request.Headers["X-Forwarded-For"] = ipAddress;
            
            if (userAgent != null)
                context.Request.Headers["User-Agent"] = userAgent;

            return context;
        }

        #endregion

        #region EnsureCorrelationId Tests

        [Fact]
        public void EnsureCorrelationId_NullContext_ReturnsNewGuid()
        {
            // Act
            var result = LoggingHelper.EnsureCorrelationId(null);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
        }

        [Fact]
        public void EnsureCorrelationId_NewContext_CreatesAndStoresGuid()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var result = LoggingHelper.EnsureCorrelationId(context);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            Assert.True(context.Items.ContainsKey(LoggingHelper.CorrelationIdKey));
            Assert.Equal(result, context.Items[LoggingHelper.CorrelationIdKey]);
        }

        [Fact]
        public void EnsureCorrelationId_ExistingGuid_ReturnsSameGuid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var existingGuid = Guid.NewGuid();
            context.Items[LoggingHelper.CorrelationIdKey] = existingGuid;

            // Act
            var result = LoggingHelper.EnsureCorrelationId(context);

            // Assert
            Assert.Equal(existingGuid, result);
        }

        [Fact]
        public void EnsureCorrelationId_InvalidStoredValue_ReturnsNewGuid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Items[LoggingHelper.CorrelationIdKey] = "not-a-guid";

            // Act
            var result = LoggingHelper.EnsureCorrelationId(context);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
        }

        [Fact]
        public void EnsureCorrelationId_CalledMultipleTimes_ReturnsSameGuid()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var first = LoggingHelper.EnsureCorrelationId(context);
            var second = LoggingHelper.EnsureCorrelationId(context);
            var third = LoggingHelper.EnsureCorrelationId(context);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(second, third);
        }

        #endregion

        #region FormatEntrada Tests

        [Fact]
        public void FormatEntrada_NullContext_ReturnsFormattedMessage()
        {
            // Act
            var result = LoggingHelper.FormatEntrada(null, "TestClass");

            // Assert
            Assert.Contains("Tipo: ENTRADA", result);
            Assert.Contains("Clase: TestClass", result);
            Assert.Contains("Origen: Admisiones", result);
            Assert.Contains("Servicio: Desconocido", result);
        }

        [Fact]
        public void FormatEntrada_WithAllParameters_ReturnsFormattedMessage()
        {
            // Arrange
            var context = CreateHttpContext(
                method: "POST",
                path: "/api/datos",
                authenticated: true,
                issuer: "https://admisiones.ort.edu.uy",
                ipAddress: "192.168.1.1",
                userAgent: "TestAgent/1.0");
            var correlationId = Guid.NewGuid();

            // Act
            var result = LoggingHelper.FormatEntrada(
                context,
                "TestController",
                codigoPersona: "12345",
                data: new { mensaje = "test" },
                correlationId: correlationId);

            // Assert
            Assert.Contains("Tipo: ENTRADA", result);
            Assert.Contains("Origen: Admisiones", result);
            Assert.Contains("Clase: TestController", result);
            Assert.Contains("CodigoPersona: 12345", result);
            Assert.Contains("Servicio: POST-api/datos", result);
            Assert.Contains("IP: 192.168.1.1", result);
            Assert.Contains("UA: TestAgent/1.0", result);
            Assert.Contains($"CorrelationId: {correlationId}", result);
        }

        [Fact]
        public void FormatEntrada_WithoutCorrelationId_OmitsCorrelationIdField()
        {
            // Arrange
            var context = CreateHttpContext();

            // Act
            var result = LoggingHelper.FormatEntrada(context, "TestClass");

            // Assert
            Assert.DoesNotContain("CorrelationId:", result);
        }

        #endregion

        #region FormatSalida Tests

        [Fact]
        public void FormatSalida_NullContext_ReturnsFormattedMessage()
        {
            // Act
            var result = LoggingHelper.FormatSalida(null, "TestClass");

            // Assert
            Assert.Contains("Tipo: SALIDA", result);
            Assert.Contains("Clase: TestClass", result);
        }

        [Fact]
        public void FormatSalida_WithData_IncludesSerializedData()
        {
            // Arrange
            var context = CreateHttpContext();
            var data = new { statusCode = 200, message = "OK" };

            // Act
            var result = LoggingHelper.FormatSalida(context, "TestClass", data: data);

            // Assert
            Assert.Contains("Tipo: SALIDA", result);
            Assert.Contains("Datos:", result);
            Assert.Contains("statusCode", result);
        }

        #endregion

        #region FormatError Tests

        [Fact]
        public void FormatError_NullContext_ReturnsFormattedMessage()
        {
            // Act
            var result = LoggingHelper.FormatError(null, "ExceptionMiddleware");

            // Assert
            Assert.Contains("Tipo: ERROR", result);
            Assert.Contains("Clase: ExceptionMiddleware", result);
        }

        [Fact]
        public void FormatError_WithExceptionData_IncludesErrorInfo()
        {
            // Arrange
            var context = CreateHttpContext(
                method: "POST",
                path: "/api/create",
                authenticated: true,
                issuer: "https://admisiones.ort.edu.uy");
            var errorData = new { errorType = "ValidationException", message = "Invalid input" };

            // Act
            var result = LoggingHelper.FormatError(context, "ErrorHandler", data: errorData);

            // Assert
            Assert.Contains("Tipo: ERROR", result);
            Assert.Contains("Origen: Admisiones", result);
            Assert.Contains("ValidationException", result);
            Assert.Contains("Invalid input", result);
        }

        #endregion

        #region GetFormattedOrigin Tests (via FormatLog)

        [Theory]
        [InlineData("https://admisiones.ort.edu.uy", "Admisiones")]
        public void FormatEntrada_DifferentIssuers_ReturnsCorrectOrigin(string? issuer, string expectedOrigin)
        {
            // Arrange
            var context = CreateHttpContext(
                authenticated: issuer != null,
                issuer: issuer);

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test");

            // Assert
            Assert.Contains($"Origen: {expectedOrigin}", result);
        }

        [Fact]
        public void FormatEntrada_UnauthenticatedUser_ReturnsDesconocido()
        {
            // Arrange
            var context = CreateHttpContext(authenticated: false);

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test");

            // Assert
            Assert.Contains("Origen: Admisiones", result);
        }

        #endregion

        #region GetServicePath Tests (via FormatLog)

        [Fact]
        public void FormatEntrada_WithMethodAndPath_FormatsServiceCorrectly()
        {
            // Arrange
            var context = CreateHttpContext(method: "DELETE", path: "/api/users/123");

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test");

            // Assert
            Assert.Contains("Servicio: DELETE-api/users/123", result);
        }

        [Fact]
        public void FormatEntrada_EmptyPath_ShowsDesconocido()
        {
            // Arrange
            var context = CreateHttpContext(method: "GET", path: "");

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test");

            // Assert
            Assert.Contains("Servicio: GET-Desconocido", result);
        }

        [Fact]
        public void FormatEntrada_PathWithLeadingSlash_TrimsSlash()
        {
            // Arrange
            var context = CreateHttpContext(method: "PUT", path: "/DatosLaborales/CargosPersona");

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test");

            // Assert
            Assert.Contains("Servicio: PUT-DatosLaborales/CargosPersona", result);
        }

        #endregion

        #region Data Serialization Tests

        [Fact]
        public void FormatEntrada_NullData_ShowsNull()
        {
            // Arrange
            var context = CreateHttpContext();

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: null);

            // Assert
            Assert.Contains("Datos: null", result);
        }

        [Fact]
        public void FormatEntrada_StringData_ReturnsStringDirectly()
        {
            // Arrange
            var context = CreateHttpContext();

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: "simple string");

            // Assert
            Assert.Contains("Datos: simple string", result);
        }

        [Fact]
        public void FormatEntrada_PrimitiveData_ReturnsToString()
        {
            // Arrange
            var context = CreateHttpContext();

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: 12345);

            // Assert
            Assert.Contains("Datos: 12345", result);
        }

        [Fact]
        public void FormatEntrada_DecimalData_ReturnsToString()
        {
            // Arrange
            var context = CreateHttpContext();
            var decimalValue = 123.45m;

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: decimalValue);

            // Assert - Use culture-invariant comparison
            Assert.Contains($"Datos: {decimalValue}", result);
        }

        [Fact]
        public void FormatEntrada_DateTimeData_ReturnsToString()
        {
            // Arrange
            var context = CreateHttpContext();
            var date = new DateTime(2024, 6, 15, 10, 30, 0);

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: date);

            // Assert
            Assert.Contains("Datos:", result);
            Assert.Contains("2024", result);
        }

        [Fact]
        public void FormatEntrada_GuidData_ReturnsToString()
        {
            // Arrange
            var context = CreateHttpContext();
            var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: guid);

            // Assert
            Assert.Contains("Datos: 12345678-1234-1234-1234-123456789012", result);
        }

        [Fact]
        public void FormatEntrada_ComplexObject_SerializesToJson()
        {
            // Arrange
            var context = CreateHttpContext();
            var data = new { name = "test", count = 5, nested = new { value = "inner" } };

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: data);

            // Assert
            Assert.Contains("Datos:", result);
            Assert.Contains("\"name\":\"test\"", result);
            Assert.Contains("\"count\":5", result);
            Assert.Contains("\"nested\"", result);
        }

        [Fact]
        public void FormatEntrada_ListData_SerializesToJson()
        {
            // Arrange
            var context = CreateHttpContext();
            var data = new List<string> { "item1", "item2", "item3" };

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: data);

            // Assert
            Assert.Contains("[\"item1\",\"item2\",\"item3\"]", result);
        }

        #endregion

        #region GetCodigoPersonaFromContext Tests

        [Fact]
        public void GetCodigoPersonaFromContext_NullContext_ReturnsNull()
        {
            // Act
            var result = LoggingHelper.GetCodigoPersonaFromContext(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCodigoPersonaFromContext_UnauthenticatedUser_ReturnsNull()
        {
            // Arrange
            var context = CreateHttpContext(authenticated: false);

            // Act
            var result = LoggingHelper.GetCodigoPersonaFromContext(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCodigoPersonaFromContext_WithUsuarioClaim_ReturnsValue()
        {
            // Arrange
            var context = CreateHttpContext(authenticated: true, codigoPersona: "USR123");

            // Act
            var result = LoggingHelper.GetCodigoPersonaFromContext(context);

            // Assert
            Assert.Equal("USR123", result);
        }

        [Fact]
        public void GetCodigoPersonaFromContext_WithNameIdentifierClaim_ReturnsValue()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "ID456")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);

            // Act
            var result = LoggingHelper.GetCodigoPersonaFromContext(context);

            // Assert
            Assert.Equal("ID456", result);
        }

        [Fact]
        public void GetCodigoPersonaFromContext_WithNameIdClaim_ReturnsValue()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("nameid", "NAMEID789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);

            // Act
            var result = LoggingHelper.GetCodigoPersonaFromContext(context);

            // Assert
            Assert.Equal("NAMEID789", result);
        }

        [Fact]
        public void GetCodigoPersonaFromContext_UsuarioClaimTakesPriority()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("usuario", "USUARIO_FIRST"),
                new Claim(ClaimTypes.NameIdentifier, "NAMEIDENTIFIER_SECOND"),
                new Claim("nameid", "NAMEID_THIRD")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);

            // Act
            var result = LoggingHelper.GetCodigoPersonaFromContext(context);

            // Assert
            Assert.Equal("USUARIO_FIRST", result);
        }

        #endregion

        #region GetRequestContextInfo Tests

        [Fact]
        public void GetRequestContextInfo_NullContext_ReturnsDesconocidoValues()
        {
            // Act
            var result = LoggingHelper.GetRequestContextInfo(null);

            // Assert
            Assert.Contains("CodigoPersona: Desconocido", result);
            Assert.Contains("IP: Desconocido", result);
            Assert.Contains("UserAgent: Desconocido", result);
            Assert.Contains("HostName: Desconocido", result);
        }

        [Fact]
        public void GetRequestContextInfo_WithAllHeaders_ReturnsFormattedInfo()
        {
            // Arrange
            var context = CreateHttpContext(
                authenticated: true,
                codigoPersona: "USR999",
                ipAddress: "10.0.0.1",
                userAgent: "CustomAgent/2.0");
            context.Request.Headers["X-Client-Host"] = "client-machine-name";

            // Act
            var result = LoggingHelper.GetRequestContextInfo(context);

            // Assert
            Assert.Contains("CodigoPersona: USR999", result);
            Assert.Contains("IP: 10.0.0.1", result);
            Assert.Contains("UserAgent: CustomAgent/2.0", result);
            Assert.Contains("HostName: client-machine-name", result);
        }

        [Fact]
        public void GetRequestContextInfo_WithRemoteIpAddress_UsesRemoteIp()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("172.16.0.1");

            // Act
            var result = LoggingHelper.GetRequestContextInfo(context);

            // Assert
            Assert.Contains("IP: 172.16.0.1", result);
        }

        [Fact]
        public void GetRequestContextInfo_XForwardedForTakesPriority()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            context.Request.Headers["X-Forwarded-For"] = "203.0.113.50";

            // Act
            var result = LoggingHelper.GetRequestContextInfo(context);

            // Assert
            Assert.Contains("IP: 203.0.113.50", result);
        }

        #endregion

        #region Constants Tests

        [Fact]
        public void CorrelationIdKey_HasExpectedValue()
        {
            Assert.Equal("CorrelationId", LoggingHelper.CorrelationIdKey);
        }

        [Fact]
        public void EntradaLoggedKey_HasExpectedValue()
        {
            Assert.Equal("EntradaLogged", LoggingHelper.EntradaLoggedKey);
        }

        [Fact]
        public void SalidaLoggedKey_HasExpectedValue()
        {
            Assert.Equal("SalidaLogged", LoggingHelper.SalidaLoggedKey);
        }

        [Fact]
        public void DbErrorLoggedKey_HasExpectedValue()
        {
            Assert.Equal("DbErrorLogged", LoggingHelper.DbErrorLoggedKey);
        }

        #endregion

        #region Log Format Structure Tests

        [Fact]
        public void FormatEntrada_OutputHasCorrectFieldOrder()
        {
            // Arrange
            var context = CreateHttpContext(
                method: "GET",
                path: "/test",
                authenticated: true,
                issuer: "https://funcionarios.ort.edu.uy",
                codigoPersona: "123",
                ipAddress: "1.2.3.4",
                userAgent: "Agent");
            var correlationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            // Act
            var result = LoggingHelper.FormatEntrada(
                context, "TestClass",
                codigoPersona: "123",
                data: "data",
                correlationId: correlationId);

            // Assert - verify fields appear in expected order
            var tipoIndex = result.IndexOf("Tipo:");
            var origenIndex = result.IndexOf("Origen:");
            var claseIndex = result.IndexOf("Clase:");
            var codigoIndex = result.IndexOf("CodigoPersona:");
            var servicioIndex = result.IndexOf("Servicio:");
            var datosIndex = result.IndexOf("Datos:");
            var ipIndex = result.IndexOf("IP:");
            var uaIndex = result.IndexOf("UA:");
            var corrIndex = result.IndexOf("CorrelationId:");

            Assert.True(tipoIndex < origenIndex);
            Assert.True(origenIndex < claseIndex);
            Assert.True(claseIndex < codigoIndex);
            Assert.True(codigoIndex < servicioIndex);
            Assert.True(servicioIndex < datosIndex);
            Assert.True(datosIndex < ipIndex);
            Assert.True(ipIndex < uaIndex);
            Assert.True(uaIndex < corrIndex);
        }

        [Fact]
        public void FormatEntrada_FieldsAreSeparatedByPipe()
        {
            // Arrange
            var context = CreateHttpContext();

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test");

            // Assert
            Assert.Contains(" | ", result);
            var parts = result.Split(" | ");
            Assert.True(parts.Length >= 8); // Minimum fields without correlation
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void FormatEntrada_EmptyOriginString_HandlesGracefully()
        {
            // Arrange
            var context = CreateHttpContext();

            // Act
            var result = LoggingHelper.FormatEntrada(context, "");

            // Assert
            Assert.Contains("Clase: ", result);
        }

        [Fact]
        public void FormatEntrada_SpecialCharactersInData_SerializesCorrectly()
        {
            // Arrange
            var context = CreateHttpContext();
            var data = new { message = "Test with \"quotes\" and 'apostrophes' and <angle> brackets" };

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: data);

            // Assert
            Assert.Contains("Datos:", result);
            // Should contain the data without throwing
        }

        [Fact]
        public void FormatEntrada_UnicodeCharactersInData_SerializesCorrectly()
        {
            // Arrange
            var context = CreateHttpContext();
            var data = new { name = "José García", emoji = "??" };

            // Act
            var result = LoggingHelper.FormatEntrada(context, "Test", data: data);

            // Assert
            Assert.Contains("José García", result);
            Assert.Contains("??", result);
        }

        #endregion
    }
}
