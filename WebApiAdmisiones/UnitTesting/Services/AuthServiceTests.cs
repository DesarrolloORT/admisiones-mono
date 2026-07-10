using AppLogic.Autenticacion.Requests;
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
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _ldapMock = new Mock<ILdap>();
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();
            _hashTokenStoreMock = new Mock<IHashTokenStore>();

            _service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                _passwordActivationServiceMock.Object,
                _hashTokenStoreMock.Object);
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
                _hashTokenStoreMock.Object);

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
        public async Task CompletarPasswordAsync_WhenPersonaDoesNotExist_ReturnsNotFound()
        {
            var codigoPersona = 12345L;
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(codigoPersona)).Returns((Persona)null!);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            var result = await _service.CompletarPasswordAsync(
                codigoPersona,
                new DtoCompletarPasswordInicialRequest { PasswordNueva = "NuevaPassword1!" });

            Assert.False(result.Success);
            Assert.Equal("INI_PAS_03", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _ldapMock.Verify(
                l => l.ForzarCambiarPasswordAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
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

            var result = await _service.CompletarPasswordAsync(codigoPersona, request);

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
        public async Task CompletarPasswordAsync_WithTemporaryImages_PersistsAndDeletesCache()
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
                    Archivo = [0x25, 0x50, 0x44, 0x46, 1],
                    NombreArchivo = "documento.pdf",
                    ContentType = "application/pdf"
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
            var service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                _passwordActivationServiceMock.Object,
                _hashTokenStoreMock.Object,
                dbConnectionContext: dbConnectionContextMock.Object,
                documentoImagenCacheService: cacheMock.Object);

            var result = await service.CompletarPasswordAsync(codigoPersona, request);

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
    }
}
