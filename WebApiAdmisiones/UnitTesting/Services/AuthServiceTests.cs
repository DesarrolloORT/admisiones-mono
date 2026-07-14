using AppLogic.Autenticacion.Requests;
using AppLogic.Autenticacion.Dtos;
using AppLogic.Registro.Dtos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IServices;
using ConnectionContext;
using LdapService.Interfaces;
using Moq;
using Utilities;
using AppLogic.Autenticacion.Services;
using AppLogic.Autenticacion.Interfaces;
using AppLogic.Registro.Interfaces;

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
        private readonly Mock<IHashTokenStore> _hashTokenStoreMock;
        private readonly Mock<IRegistroFlowService> _registroFlowServiceMock;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _ldapMock = new Mock<ILdap>();
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();
            _hashTokenStoreMock = new Mock<IHashTokenStore>();
            _registroFlowServiceMock = new Mock<IRegistroFlowService>();

            _service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                _passwordActivationServiceMock.Object,
                _hashTokenStoreMock.Object,
                _registroFlowServiceMock.Object);
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
                _passwordActivationServiceMock.Object,
                _hashTokenStoreMock.Object,
                _registroFlowServiceMock.Object);

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
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(AuthService.RecuperarPassword),
                    "Si los datos ingresados son correctos, recibiras un mail con instrucciones para recuperar tu contraseña."));

            var result = await _service.RecuperarPassword(request);

            Assert.True(result.Success);
            Assert.Null(result.Data);
            Assert.Contains("Si los datos ingresados son correctos", result.Message);
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
            Assert.Null(result.Data);
            Assert.Contains("Si los datos ingresados son correctos", result.Message);
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
            Assert.Null(result.Data);
            Assert.Contains("Si los datos ingresados son correctos", result.Message);
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
        public async Task AutenticarUsuarioLDAPAsync_WhenLdapRejectsCredentials_ReturnsLdapFailure()
        {
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            var persona = new Persona
            {
                CodigoPersona = 12345,
                Documento = "1234567-2",
                TipoDocumento = "CI"
            };
            personasRepoMock.Setup(x => x.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns(persona);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);
            _ldapMock
                .Setup(x => x.AutenticarUsuarioLDAPAsync(12345, "wrongpass"))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "LDAP_401",
                    nameof(ILdap.AutenticarUsuarioLDAPAsync),
                    "Credenciales inválidas.",
                    401,
                    false));

            var result = await _service.AutenticarUsuarioLDAPAsync("CI", "1234567-2", "wrongpass");

            Assert.False(result.Success);
            Assert.Equal("LDAP_401", result.ErrorCode);
            Assert.Equal(401, result.HttpCode);
            _tokenServiceMock.Verify(t => t.GenerateAccessToken(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_SuccessfulAuthentication_ReturnsOkWithPersonaIdentityAndNoTokens()
        {
            // SEG-03: AutenticarUsuarioLDAPAsync ya no emite ni persiste tokens. Eso lo hace
            // GenerarTokensParaPersonaAsync, recién cuando el llamador decide que el login está
            // completo (sin 2FA pendiente).
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

            // Act
            var result = await _service.AutenticarUsuarioLDAPAsync("CI", "1234567-2", password);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(codigoPersona, result.Data.CodigoPersona);
            Assert.Equal("Juan", result.Data.PrimerNombre);
            Assert.Equal("Carlos", result.Data.SegundoNombre);
            Assert.Equal("Pérez", result.Data.PrimerApellido);
            Assert.Equal("Gómez", result.Data.SegundoApellido);
            Assert.Equal("E", result.Data.TipoPersona);
            Assert.Equal("1234567-2", result.Data.Documento);

            _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<Persona>()), Times.Never);
            _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Never);
            _refreshTokenServiceMock.Verify(
                x => x.SaveRefreshTokenAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Fact]
        public async Task GenerarTokensParaPersonaAsync_Success_ReturnsOkWithTokens()
        {
            // Arrange
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            long codigoPersona = 12345;

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
            var result = await _service.GenerarTokensParaPersonaAsync(codigoPersona);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("access_token_123", result.Data.AccessToken);
            Assert.Equal("refresh_token_456", result.Data.RefreshToken);
            Assert.Equal("hashed_refresh_token", result.Data.RefreshTokenHash);
            Assert.NotNull(result.Data.Persona);
            Assert.Equal(codigoPersona, result.Data.Persona.CodigoPersona);

            _refreshTokenServiceMock.Verify(x => x.SaveRefreshTokenAsync(
                codigoPersona,
                "ADMISIONESWEB",
                "hashed_refresh_token",
                It.Is<DateTime>(d => d > DateTime.UtcNow.AddDays(6) && d < DateTime.UtcNow.AddDays(8))),
                Times.Once);
            _tokenServiceMock.Verify(x => x.GenerateAccessToken(persona), Times.Once);
            _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
            _tokenServiceMock.Verify(x => x.HashToken("refresh_token_456"), Times.Once);
        }

        [Fact]
        public async Task GenerarTokensParaPersonaAsync_WhenRefreshTokenPersistenceFails_ReturnsFailedWithErrorCode()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            const long codigoPersona = 12345;
            var persona = new Persona { CodigoPersona = codigoPersona, Documento = "1234567-2" };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(codigoPersona)).Returns(persona);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(persona)).Returns("access-token");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(x => x.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(x => x.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("Persistence unavailable"));

            var result = await _service.GenerarTokensParaPersonaAsync(codigoPersona);

            Assert.False(result.Success);
            Assert.Equal("GEN_TOK_99", result.ErrorCode);
            Assert.Equal(500, result.HttpCode);
            Assert.DoesNotContain("Persistence unavailable", result.Message);
            _tokenServiceMock.Verify(x => x.GenerateAccessToken(persona), Times.Once);
            _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
            _tokenServiceMock.Verify(x => x.HashToken("refresh-token"), Times.Once);
            _refreshTokenServiceMock.Verify(
                x => x.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()),
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
            // El detalle interno de la excepción NO debe filtrarse al cliente.
            Assert.DoesNotContain("Database unavailable", result.Message);
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
            // El detalle interno de la excepción NO debe filtrarse al cliente.
            Assert.DoesNotContain("Hash service unavailable", result.Message);
            Assert.Equal(500, result.HttpCode);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistente_LdapFailure_PreservesHashToken()
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
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "password-activation-session", CodigoPersona = codigoPersona },
                    nameof(IPasswordActivationService.ValidarSessionToken)));

            var result = (await _service.CompletarPasswordFlowAsync("token", request)).Result;

            Assert.False(result.Success);
            Assert.Equal("hash", persona.HashTokenPassword);
            uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistente_WhenPersonaDoesNotExist_ReturnsNotFound()
        {
            var codigoPersona = 12345L;
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns((Persona)null!);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "password-activation-session", CodigoPersona = codigoPersona },
                    nameof(IPasswordActivationService.ValidarSessionToken)));

            var result = (await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" })).Result;

            Assert.False(result.Success);
            Assert.Equal("INI_PAS_03", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _ldapMock.Verify(
                l => l.ForzarCambiarPasswordAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistente_LdapSuccess_ClearsHashAndReturnsTokens()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            var codigoPersona = 12345L;
            var persona = new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI"
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
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(codigoPersona.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(codigoPersona.ToString(), request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(persona)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "password-activation-session", CodigoPersona = codigoPersona },
                    nameof(IPasswordActivationService.ValidarSessionToken)));

            var result = (await _service.CompletarPasswordFlowAsync("token", request)).Result;

            Assert.True(result.Success);
            Assert.Equal("access-token", result.Data!.AccessToken);
            Assert.Equal("refresh-token", result.Data.RefreshToken);
            uowMock.Verify(u => u.Save(), Times.Once);
            _hashTokenStoreMock.Verify(h => h.DeleteAsync(codigoPersona.ToString()), Times.Once);
            _refreshTokenServiceMock.Verify(
                r => r.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()),
                Times.Once);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistente_WithTemporaryImages_PersistsAndDeletesCache()
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
                TipoDocumento = "CI",
                Documento = "1234567-2"
            };
            var request = new DtoCompletarPasswordInicialRequest
            {
                PasswordNueva = "NuevaPassword1!"
            };
            var imagenes = new DtoRegistroDocumentoImagenesTemporales
            {
                FechaVencimiento = new DateTime(2030, 1, 1),
                DocumentoFrente = new DtoRegistroDocumentoArchivoTemporal
                {
                    Archivo = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                    NombreArchivo = "documento.jpg",
                    ContentType = "image/jpeg"
                },
                CaraPersona = new DtoRegistroDocumentoArchivoTemporal
                {
                    Archivo = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                    NombreArchivo = "cara.jpg",
                    ContentType = "image/jpeg"
                }
            };
            Imagen? fotoAgregada = null;
            ImagenTemporal? documentoAgregado = null;
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            var imagenRepoMock = new Mock<IImagenRepository>();
            var imagenTemporalRepoMock = new Mock<IImagenTemporalRepository>();
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            var dbConnectionContextMock = new Mock<IDbConnectionContext>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns(persona);
            imagenRepoMock.Setup(r => r.GetFotoByPersona(codigoPersona)).Returns(default(Imagen)!);
            imagenRepoMock
                .Setup(r => r.Add(It.IsAny<Imagen>()))
                .Callback<Imagen>(i => fotoAgregada = i);
            imagenTemporalRepoMock.Setup(r => r.GetDocumentoByPersonaAndTipo(codigoPersona, 1)).Returns(default(ImagenTemporal)!);
            imagenTemporalRepoMock
                .Setup(r => r.Add(It.IsAny<ImagenTemporal>()))
                .Callback<ImagenTemporal>(i => documentoAgregado = i);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            uowMock.Setup(u => u.Imagens).Returns(imagenRepoMock.Object);
            uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            cacheMock
                .Setup(c => c.ObtenerAsync("CI", "1234567-2"))
                .ReturnsAsync(imagenes);
            dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL))
                .Returns(3000);
            dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN))
                .Returns(4000);
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(codigoPersona.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(codigoPersona.ToString(), request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(persona)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "password-activation-session", CodigoPersona = codigoPersona },
                    nameof(IPasswordActivationService.ValidarSessionToken)));
            var service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                _passwordActivationServiceMock.Object,
                _hashTokenStoreMock.Object,
                _registroFlowServiceMock.Object,
                dbConnectionContext: dbConnectionContextMock.Object,
                documentoImagenCacheService: cacheMock.Object);

            var result = (await service.CompletarPasswordFlowAsync("token", request)).Result;

            Assert.True(result.Success);
            Assert.NotNull(documentoAgregado);
            Assert.Equal(3000, documentoAgregado!.IdImagenTemporal);
            Assert.Equal(codigoPersona, documentoAgregado.CodigoPersona);
            Assert.Equal("1", documentoAgregado.TipoImagen);
            Assert.Equal("12345_1.jpg", documentoAgregado.NombreImagen);
            Assert.Equal(new DateTime(2030, 1, 1), documentoAgregado.FechaVtoDocumentoPersona);
            Assert.NotNull(fotoAgregada);
            Assert.Equal(4000, fotoAgregada!.IdImagen);
            Assert.Equal(codigoPersona, fotoAgregada.CodigoPersona);
            Assert.Equal("3", fotoAgregada.TipoImagen);
            Assert.Equal("12345_3.jpg", fotoAgregada.NombreImagen);
            Assert.Equal(new DateTime(2030, 1, 1), persona.FechaVtoDocumentoPersona);
            cacheMock.Verify(c => c.EliminarAsync("CI", "1234567-2"), Times.Once);
            uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_WhenSessionValidationFails_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("bad-token"))
                .Returns(OperationResult<DtoValidatedSession>.IsFailed(
                    "ACT_SES_01",
                    nameof(IPasswordActivationService.ValidarSessionToken),
                    "Sesión temporal no encontrada.",
                    401,
                    default!));

            var flow = await _service.CompletarPasswordFlowAsync(
                "bad-token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("ACT_SES_01", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompletarPassword", flow.Result.Method);
            _registroFlowServiceMock.Verify(
                s => s.GetPendingPersonaAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_WhenSessionDataIsNull_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(null, nameof(IPasswordActivationService.ValidarSessionToken)));

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("ACT_SES_06", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompletarPassword", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaConFlowIdVacio_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "nueva-persona-session", FlowId = "  " },
                    nameof(IPasswordActivationService.ValidarSessionToken)));

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("ACT_SES_NUP_01", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompletarPassword", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaSinPendiente_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "nueva-persona-session", FlowId = "flow-1" },
                    nameof(IPasswordActivationService.ValidarSessionToken)));
            _registroFlowServiceMock
                .Setup(s => s.GetPendingPersonaAsync("flow-1"))
                .ReturnsAsync((DtoRegistroPendingPersona?)null);

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("NUP_COMP_01", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompletarPassword", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaCuandoCreacionFalla_ReturnsFailureWithoutClearingCookie()
        {
            var pending = new DtoRegistroPendingPersona { TipoDocumento = "CI", Documento = "1234567-2" };
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "nueva-persona-session", FlowId = "flow-1" },
                    nameof(IPasswordActivationService.ValidarSessionToken)));
            _registroFlowServiceMock
                .Setup(s => s.GetPendingPersonaAsync("flow-1"))
                .ReturnsAsync(pending);
            _registroFlowServiceMock
                .Setup(s => s.CompletarNuevaPersona(pending, "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<long>.IsFailed(
                    "REG_PERSONA_99",
                    nameof(IRegistroFlowService.CompletarNuevaPersona),
                    "Error al crear la persona.",
                    500,
                    default));

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.False(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("REG_PERSONA_99", flow.Result.ErrorCode);
            Assert.Equal("Error al crear la persona.", flow.Result.Message);
            Assert.Equal(500, flow.Result.HttpCode);
            Assert.Equal("CompletarPassword", flow.Result.Method);
            _registroFlowServiceMock.Verify(s => s.DeletePendingPersonaAsync(It.IsAny<string>()), Times.Never);
            _registroFlowServiceMock.Verify(s => s.EliminarFlowSessionAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaExitosa_ClearsCookieAndDeletesPendingData()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            var codigoPersona = 12345L;
            var persona = new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez"
            };
            var pending = new DtoRegistroPendingPersona { TipoDocumento = "CI", Documento = "1234567-2" };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "nueva-persona-session", FlowId = "flow-1" },
                    nameof(IPasswordActivationService.ValidarSessionToken)));
            _registroFlowServiceMock
                .Setup(s => s.GetPendingPersonaAsync("flow-1"))
                .ReturnsAsync(pending);
            _registroFlowServiceMock
                .Setup(s => s.CompletarNuevaPersona(pending, "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<long>.Ok(codigoPersona, nameof(IRegistroFlowService.CompletarNuevaPersona)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(persona)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.True(flow.Result.Success);
            Assert.Equal("access-token", flow.Result.Data!.AccessToken);
            Assert.Equal("refresh-token", flow.Result.Data.RefreshToken);
            Assert.Equal("GenerarTokensParaPersonaAsync", flow.Result.Method);
            _registroFlowServiceMock.Verify(s => s.DeletePendingPersonaAsync("flow-1"), Times.Once);
            _registroFlowServiceMock.Verify(s => s.EliminarFlowSessionAsync("flow-1"), Times.Once);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaTokenGenerationFalla_NoLimpiaCookieNiRedis()
        {
            var pending = new DtoRegistroPendingPersona { TipoDocumento = "CI", Documento = "1234567-2" };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(99L)).Returns((Persona)null!);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "nueva-persona-session", FlowId = "flow-1" },
                    nameof(IPasswordActivationService.ValidarSessionToken)));
            _registroFlowServiceMock
                .Setup(s => s.GetPendingPersonaAsync("flow-1"))
                .ReturnsAsync(pending);
            _registroFlowServiceMock
                .Setup(s => s.CompletarNuevaPersona(pending, "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<long>.Ok(99L, nameof(IRegistroFlowService.CompletarNuevaPersona)));

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.False(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("GEN_TOK_01", flow.Result.ErrorCode);
            _registroFlowServiceMock.Verify(s => s.DeletePendingPersonaAsync(It.IsAny<string>()), Times.Never);
            _registroFlowServiceMock.Verify(s => s.EliminarFlowSessionAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistenteSinCodigoPersona_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "password-activation-session", CodigoPersona = null },
                    nameof(IPasswordActivationService.ValidarSessionToken)));

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("ACT_SES_03", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompletarPassword", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistenteExitosa_ClearsCookie()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            var codigoPersona = 12345L;
            var persona = new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI"
            };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(codigoPersona.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(codigoPersona.ToString(), "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(persona)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(codigoPersona, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "password-activation-session", CodigoPersona = codigoPersona },
                    nameof(IPasswordActivationService.ValidarSessionToken)));

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.True(flow.Result.Success);
            Assert.Equal("access-token", flow.Result.Data!.AccessToken);
            // "CompletarPasswordAsync" es el literal preservado del nombre original del método
            // (ver CompletarPasswordPersonaExistenteOriginMethod) para no cambiar el body público.
            Assert.Equal("CompletarPasswordAsync", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistenteCuandoSaveFalla_NoConsumeElLinkDeActivacion()
        {
            // SRV-02: si el Save de DB falla después de cambiar la password en LDAP, el link de
            // activación (hash token en Redis) debe seguir vigente para que el usuario reintente.
            var codigoPersona = 12345L;
            var persona = new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI"
            };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            uowMock.Setup(u => u.Save()).Throws(new InvalidOperationException("DB caída"));
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(codigoPersona.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(codigoPersona.ToString(), "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "password-activation-session", CodigoPersona = codigoPersona },
                    nameof(IPasswordActivationService.ValidarSessionToken)));

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.False(flow.Result.Success);
            Assert.Equal("INI_PAS_99", flow.Result.ErrorCode);
            Assert.Equal(500, flow.Result.HttpCode);
            _hashTokenStoreMock.Verify(
                h => h.DeleteAsync(codigoPersona.ToString()),
                Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistenteCuandoServicioFalla_NoLimpiaCookie()
        {
            var codigoPersona = 999L;
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns((Persona)null!);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken("token"))
                .Returns(OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { Purpose = "password-activation-session", CodigoPersona = codigoPersona },
                    nameof(IPasswordActivationService.ValidarSessionToken)));

            var flow = await _service.CompletarPasswordFlowAsync(
                "token",
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.False(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("INI_PAS_03", flow.Result.ErrorCode);
            // "CompletarPasswordAsync" es el literal preservado del nombre original del método
            // (ver CompletarPasswordPersonaExistenteOriginMethod) para no cambiar el body público.
            Assert.Equal("CompletarPasswordAsync", flow.Result.Method);
        }
    }
}
