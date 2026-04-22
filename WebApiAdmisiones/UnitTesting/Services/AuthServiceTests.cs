using AppLogic.DTOs;
using AppLogic.IServices;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IServices;
using LdapService.Interfaces;
using Moq;
using Utilities;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class AuthServiceTests
    {
        private readonly Mock<ILdap> _ldapMock;
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _ldapMock = new Mock<ILdap>();
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();

            _service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object);
        }

        [Fact]
        public void Constructor_InitializesAllDependencies()
        {
            // Arrange & Act
            var service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_LdapAuthenticationFails_ReturnsFailed()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            long codigoPersona = 12345;
            string password = "wrongpass";
            var ldapFailedResult = OperationResult<bool>.IsFailed(
                "LDAP_01",
                "AutenticarUsuarioLDAPAsync",
                "Credenciales inválidas",
                401,
                false);

            _ldapMock.Setup(x => x.AutenticarUsuarioLDAPAsync(codigoPersona, password))
                .ReturnsAsync(ldapFailedResult);

            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync(codigoPersona, password);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LDAP_01", result.ErrorCode);
            Assert.Equal("Credenciales inválidas", result.Message);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_PersonaNotFoundInDatabase_ReturnsFailed()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            long codigoPersona = 12345;
            string password = "validpass";
            var ldapSuccessResult = OperationResult<bool>.Ok(true, "AutenticarUsuarioLDAPAsync");

            _ldapMock.Setup(x => x.AutenticarUsuarioLDAPAsync(codigoPersona, password))
                .ReturnsAsync(ldapSuccessResult);

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(codigoPersona)).Returns((Persona)null!);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync(codigoPersona, password);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_04", result.ErrorCode);
            Assert.Equal("Usuario autenticado pero no se encontró la persona en la base de datos.", result.Message);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_SuccessfulAuthentication_ReturnsOkWithTokens()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            long codigoPersona = 12345;
            string password = "validpass";
            var ldapSuccessResult = OperationResult<bool>.Ok(true, "AutenticarUsuarioLDAPAsync");

            var persona = new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "Juan",
                SegundoNombre = "Carlos",
                PrimerApellido = "Pérez",
                SegundoApellido = "Gómez",
                TipoPersona = "E",
                Documento = "12345678"
            };

            _ldapMock.Setup(x => x.AutenticarUsuarioLDAPAsync(codigoPersona, password))
                .ReturnsAsync(ldapSuccessResult);

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(codigoPersona)).Returns(persona);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            _tokenServiceMock.Setup(x => x.GenerateAccessToken(persona)).Returns("access_token_123");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token_456");
            _tokenServiceMock.Setup(x => x.HashToken("refresh_token_456")).Returns("hashed_refresh_token");

            _refreshTokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(
                codigoPersona,
                "ADMISIONESWEB",
                "hashed_refresh_token",
                It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync(codigoPersona, password);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("access_token_123", result.Data.AccessToken);
            Assert.Equal("refresh_token_456", result.Data.RefreshToken);
            Assert.Equal("hashed_refresh_token", result.Data.RefreshTokenHash);
            Assert.NotNull(result.Data.Persona);
            Assert.Equal(codigoPersona, result.Data.Persona.CodigoPersona);
            Assert.Equal("Juan", result.Data.Persona.PrimerNombre);
            Assert.Equal("Carlos", result.Data.Persona.SegundoNombre);
            Assert.Equal("Pérez", result.Data.Persona.PrimerApellido);
            Assert.Equal("Gómez", result.Data.Persona.SegundoApellido);
            Assert.Equal("E", result.Data.Persona.TipoPersona);
            Assert.Equal("12345678", result.Data.Persona.Documento);

            _refreshTokenServiceMock.Verify(x => x.SaveRefreshTokenAsync(
                codigoPersona,
                "ADMISIONESWEB",
                "hashed_refresh_token",
                It.Is<DateTime>(d => d > DateTime.UtcNow.AddDays(6) && d < DateTime.UtcNow.AddDays(8))),
                Times.Once);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_ExceptionThrown_ReturnsFailedWithErrorCode()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            long codigoPersona = 12345;
            string password = "validpass";

            _ldapMock.Setup(x => x.AutenticarUsuarioLDAPAsync(codigoPersona, password))
                .ThrowsAsync(new InvalidOperationException("LDAP service unavailable"));

            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync(codigoPersona, password);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_99", result.ErrorCode);
            Assert.Contains("Error al autenticar usuario", result.Message);
            Assert.Contains("LDAP service unavailable", result.Message);
            Assert.Equal(500, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_NullRefreshToken_ReturnsFailed()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));

            // Act
            var result = await _service.RefrescarTokensAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_01", result.ErrorCode);
            Assert.Equal("No se encontró refresh token en las cookies.", result.Message);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_EmptyRefreshToken_ReturnsFailed()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));

            // Act
            var result = await _service.RefrescarTokensAsync(string.Empty);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_01", result.ErrorCode);
            Assert.Equal("No se encontró refresh token en las cookies.", result.Message);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_InvalidRefreshToken_ReturnsFailed()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            string refreshToken = "invalid_token";
            string hashedToken = "hashed_invalid_token";

            _tokenServiceMock.Setup(x => x.HashToken(refreshToken)).Returns(hashedToken);
            _refreshTokenServiceMock.Setup(x => x.GetCodigoPersonaByRefreshTokenAsync("ADMISIONESWEB", hashedToken))
                .ReturnsAsync((long?)null);

            // Act
            var result = await _service.RefrescarTokensAsync(refreshToken);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_03", result.ErrorCode);
            Assert.Equal("Refresh token inválido o expirado.", result.Message);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_PersonaNotFoundInDatabase_ReturnsFailed()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            string refreshToken = "valid_token";
            string hashedToken = "hashed_valid_token";
            long codigoPersona = 12345;

            _tokenServiceMock.Setup(x => x.HashToken(refreshToken)).Returns(hashedToken);
            _refreshTokenServiceMock.Setup(x => x.GetCodigoPersonaByRefreshTokenAsync("ADMISIONESWEB", hashedToken))
                .ReturnsAsync(codigoPersona);

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(codigoPersona)).Returns((Persona)null!);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            // Act
            var result = await _service.RefrescarTokensAsync(refreshToken);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_04", result.ErrorCode);
            Assert.Equal("Usuario no encontrado en la base de datos.", result.Message);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_SuccessfulRefresh_ReturnsOkWithNewTokens()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            string oldRefreshToken = "old_token";
            string oldHashedToken = "old_hashed_token";
            long codigoPersona = 12345;

            var persona = new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "María",
                SegundoNombre = "Elena",
                PrimerApellido = "López",
                SegundoApellido = "Martínez",
                TipoPersona = "E",
                Documento = "98765432"
            };

            _tokenServiceMock.Setup(x => x.HashToken(oldRefreshToken)).Returns(oldHashedToken);
            _refreshTokenServiceMock.Setup(x => x.GetCodigoPersonaByRefreshTokenAsync("ADMISIONESWEB", oldHashedToken))
                .ReturnsAsync(codigoPersona);

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(codigoPersona)).Returns(persona);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            _tokenServiceMock.Setup(x => x.GenerateAccessToken(persona)).Returns("new_access_token");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new_refresh_token");
            _tokenServiceMock.Setup(x => x.HashToken("new_refresh_token")).Returns("new_hashed_token");

            _refreshTokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(
                codigoPersona,
                "ADMISIONESWEB",
                "new_hashed_token",
                It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.RefrescarTokensAsync(oldRefreshToken);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("new_access_token", result.Data.AccessToken);
            Assert.Equal("new_refresh_token", result.Data.RefreshToken);
            Assert.Equal("new_hashed_token", result.Data.RefreshTokenHash);
            Assert.Equal("Tokens renovados correctamente.", result.Data.Message);
            Assert.NotNull(result.Data.Persona);
            Assert.Equal(codigoPersona, result.Data.Persona.CodigoPersona);
            Assert.Equal("María", result.Data.Persona.PrimerNombre);
            Assert.Equal("Elena", result.Data.Persona.SegundoNombre);
            Assert.Equal("López", result.Data.Persona.PrimerApellido);
            Assert.Equal("Martínez", result.Data.Persona.SegundoApellido);
            Assert.Equal("E", result.Data.Persona.TipoPersona);
            Assert.Equal("98765432", result.Data.Persona.Documento);

            _refreshTokenServiceMock.Verify(x => x.SaveRefreshTokenAsync(
                codigoPersona,
                "ADMISIONESWEB",
                "new_hashed_token",
                It.Is<DateTime>(d => d > DateTime.UtcNow.AddDays(6) && d < DateTime.UtcNow.AddDays(8))),
                Times.Once);
        }

        [Fact]
        public async Task RefrescarTokensAsync_ExceptionThrown_ReturnsFailedWithErrorCode()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            string refreshToken = "valid_token";

            _tokenServiceMock.Setup(x => x.HashToken(refreshToken))
                .Throws(new InvalidOperationException("Hash service unavailable"));

            // Act
            var result = await _service.RefrescarTokensAsync(refreshToken);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_99", result.ErrorCode);
            Assert.Contains("Error al refrescar tokens", result.Message);
            Assert.Contains("Hash service unavailable", result.Message);
            Assert.Equal(500, result.HttpCode);
        }
    }
}
