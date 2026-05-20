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
        private readonly Mock<IPasswordActivationService> _passwordActivationServiceMock;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _ldapMock = new Mock<ILdap>();
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();

            _service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                _passwordActivationServiceMock.Object);
        }

        [Fact]
        public void Constructor_InitializesAllDependencies()
        {
            // Arrange & Act
            var service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                _passwordActivationServiceMock.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task RecuperarPassword_DatosValidos_EnviaLinkYDevuelveMensajeGenerico()
        {
            var request = new DtoRecuperarPasswordRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez"
            };

            var persona = new Persona
            {
                CodigoPersona = 12345,
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                PrimerApellidoMay = "PEREZ",
                Email = "ana@example.com"
            };

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByDocumento("1234567-2")).Returns(persona);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(x => x.EnviarMailRecuperacionPasswordAsync(persona, nameof(AuthService.RecuperarPassword)))
                .ReturnsAsync(OperationResult<object?>.Ok(null, nameof(AuthService.RecuperarPassword)));

            var result = await _service.RecuperarPassword(request);

            Assert.True(result.Success);
            Assert.Contains("Si los datos ingresados son correctos", result.Data?.ToString());
            _passwordActivationServiceMock.Verify(
                x => x.EnviarMailRecuperacionPasswordAsync(persona, nameof(AuthService.RecuperarPassword)),
                Times.Once);
            _ldapMock.Verify(
                x => x.EnviarContrasenia(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RecuperarPassword_PersonaNoExiste_DevuelveMensajeGenericoSinEnviarMail()
        {
            var request = new DtoRecuperarPasswordRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez"
            };

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByDocumento("1234567-2")).Returns((Persona)null!);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            var result = await _service.RecuperarPassword(request);

            Assert.True(result.Success);
            Assert.Contains("Si los datos ingresados son correctos", result.Data?.ToString());
            _passwordActivationServiceMock.Verify(
                x => x.EnviarMailRecuperacionPasswordAsync(It.IsAny<Persona>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RecuperarPassword_ApellidoNoCoincide_DevuelveMensajeGenericoSinEnviarMail()
        {
            var request = new DtoRecuperarPasswordRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Gomez"
            };

            var persona = new Persona
            {
                CodigoPersona = 12345,
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                PrimerApellidoMay = "PEREZ",
                Email = "ana@example.com"
            };

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByDocumento("1234567-2")).Returns(persona);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            var result = await _service.RecuperarPassword(request);

            Assert.True(result.Success);
            Assert.Contains("Si los datos ingresados son correctos", result.Data?.ToString());
            _passwordActivationServiceMock.Verify(
                x => x.EnviarMailRecuperacionPasswordAsync(It.IsAny<Persona>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_LdapAuthenticationFails_ReturnsFailed()
        {
            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync("XX", "1234567-2", "validpass");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_InvalidDocument_ReturnsFailed()
        {
            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync("CI", "invalido", "validpass");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_PersonaNotFoundInDatabase_ReturnsFailed()
        {
            // Arrange
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns((Persona)null!);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync("CI", "1234567-2", "validpass");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_04", result.ErrorCode);
            Assert.Equal("No se encontró la persona en la base de datos.", result.Message);
            Assert.Equal(404, result.HttpCode);
            _ldapMock.Verify(x => x.AutenticarUsuarioLDAPAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
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
                Documento = "1234567-2"
            };

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns(persona);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            _ldapMock.Setup(x => x.AutenticarUsuarioLDAPAsync(codigoPersona, password))
                .ReturnsAsync(ldapSuccessResult);

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
            var result = await _service.AutenticarUsuarioLDAPAsync("CI", "1234567-2", password);

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
            Assert.Equal("1234567-2", result.Data.Persona.Documento);

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
            _uowFactoryMock.Setup(x => x.Create())
                .Throws(new InvalidOperationException("Database unavailable"));

            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync("CI", "1234567-2", "validpass");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_99", result.ErrorCode);
            Assert.Contains("Error al autenticar usuario", result.Message);
            Assert.Contains("Database unavailable", result.Message);
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

        [Fact]
        public async Task CambiarPasswordAsync_NullRequest_ReturnsFailed()
        {
            // Act
            var result = await _service.CambiarPasswordAsync(12345, null!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_InvalidPassword_ReturnsFailedWithoutCallingLdap()
        {
            // Arrange
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "short1A!"
            };

            // Act
            var result = await _service.CambiarPasswordAsync(12345, request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_02", result.ErrorCode);
            Assert.Equal("La contraseña debe tener entre 12 y 20 caracteres", result.Message);
            Assert.Equal(400, result.HttpCode);
            _ldapMock.Verify(
                x => x.CambiarPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CambiarPasswordAsync_LdapSuccess_ReturnsOk()
        {
            // Arrange
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CambiarPasswordAsync)));

            // Act
            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Se actualizó tu contraseña", result.Data);
            Assert.Equal(200, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_LdapFailure_PropagatesFailure()
        {
            // Arrange
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "AUTH_LDAP_22",
                    nameof(ILdap.CambiarPasswordAsync),
                    "El servicio LDAP no pudo cambiar la password.",
                    400,
                    false));

            // Act
            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("AUTH_LDAP_22", result.ErrorCode);
            Assert.Equal("El servicio LDAP no pudo cambiar la password.", result.Message);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_ExceptionThrown_ReturnsFailedWithErrorCode()
        {
            // Arrange
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ThrowsAsync(new InvalidOperationException("LDAP service unavailable"));

            // Act
            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_99", result.ErrorCode);
            Assert.Contains("Error al cambiar contraseña", result.Message);
            Assert.Contains("LDAP service unavailable", result.Message);
            Assert.Equal(500, result.HttpCode);
        }

        [Fact]
        public async Task CompletarPasswordAsync_LdapFailure_PreservesHashToken()
        {
            var codigoPersona = 12345L;
            var persona = new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI",
                HashTokenPassword = "hash"
            };
            var request = new DtoCompletarPasswordInicialRequest
            {
                PasswordNueva = "NuevaPassword1!"
            };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(codigoPersona.ToString(), request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "AUTH_LDAP_52",
                    nameof(ILdap.ForzarCambiarPasswordAsync),
                    "El servicio LDAP no pudo forzar el cambio de password.",
                    400,
                    false));

            var result = await _service.CompletarPasswordAsync(codigoPersona, request);

            Assert.False(result.Success);
            Assert.Equal("hash", persona.HashTokenPassword);
            uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordAsync_LdapSuccess_ClearsHashAndReturnsTokens()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            var codigoPersona = 12345L;
            var persona = new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI",
                HashTokenPassword = "hash"
            };
            var request = new DtoCompletarPasswordInicialRequest
            {
                PasswordNueva = "NuevaPassword1!"
            };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(codigoPersona.ToString(), request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(persona)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            var result = await _service.CompletarPasswordAsync(codigoPersona, request);

            Assert.True(result.Success);
            Assert.Null(persona.HashTokenPassword);
            Assert.Equal("access-token", result.Data!.AccessToken);
            Assert.Equal("refresh-token", result.Data.RefreshToken);
            uowMock.Verify(u => u.Save(), Times.Once);
            _refreshTokenServiceMock.Verify(
                r => r.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()),
                Times.Once);
        }
    }
}
