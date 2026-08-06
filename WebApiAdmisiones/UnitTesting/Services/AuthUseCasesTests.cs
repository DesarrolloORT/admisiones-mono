using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using AppLogic.Registration.Dtos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IServices;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using AppLogic.Authentication.Services;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.UseCases;
using AppLogic.Registration.Interfaces;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class AuthUseCasesTests
    {
        private readonly Mock<ILdap> _ldapMock;
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
        private readonly Mock<IPasswordActivationService> _passwordActivationServiceMock;
        private readonly Mock<IHashTokenStore> _hashTokenStoreMock;
        private readonly Mock<IRegistrationFlowService> _registroFlowServiceMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<IIdentityDocumentImageCache> _documentoImagenCacheServiceMock;
        private readonly AuthUseCases _service;

        /// <summary>Composition root de los casos de uso de autenticación para los tests.</summary>
        private sealed record AuthUseCases(
            IAuthenticateWithLdap Authenticate,
            IIssueTokensForPerson IssueTokens,
            IRefreshTokens Refresh,
            IRecoverPassword Recover,
            ICompletePasswordFlow CompletePassword);

        public AuthUseCasesTests()
        {
            _ldapMock = new Mock<ILdap>();
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();
            _hashTokenStoreMock = new Mock<IHashTokenStore>();
            _registroFlowServiceMock = new Mock<IRegistrationFlowService>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _documentoImagenCacheServiceMock = new Mock<IIdentityDocumentImageCache>();

            _service = CreateUseCases(_dbConnectionContextMock.Object, _documentoImagenCacheServiceMock.Object);
        }

        private AuthUseCases CreateUseCases(
            IDbConnectionContext dbConnectionContext,
            IIdentityDocumentImageCache documentImageCache)
        {
            var tokenIssuer = new SessionTokenIssuer(_tokenServiceMock.Object);
            var issueTokens = new IssueTokensForPerson(
                _uowFactoryMock.Object,
                _refreshTokenServiceMock.Object,
                tokenIssuer,
                Mock.Of<ILogger<IssueTokensForPerson>>());

            return new AuthUseCases(
                new AuthenticateWithLdap(_ldapMock.Object, _uowFactoryMock.Object, Mock.Of<ILogger<AuthenticateWithLdap>>()),
                issueTokens,
                new RefreshTokens(
                    _uowFactoryMock.Object,
                    _tokenServiceMock.Object,
                    _refreshTokenServiceMock.Object,
                    tokenIssuer,
                    Mock.Of<ILogger<RefreshTokens>>()),
                new RecoverPassword(_uowFactoryMock.Object, _passwordActivationServiceMock.Object, Mock.Of<ILogger<RecoverPassword>>()),
                new CompletePasswordFlow(
                    _uowFactoryMock.Object,
                    dbConnectionContext,
                    _ldapMock.Object,
                    _passwordActivationServiceMock.Object,
                    _hashTokenStoreMock.Object,
                    _registroFlowServiceMock.Object,
                    documentImageCache,
                    _refreshTokenServiceMock.Object,
                    tokenIssuer,
                    issueTokens,
                    Mock.Of<ILogger<CompletePasswordFlow>>()));
        }

        [Fact]
        public async Task RecuperarPassword_DatosValidos_EnviaLinkYDevuelveMensajeGenerico()
        {
            var request = new RecoverPasswordRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez"
            };

            var person = new Persona
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
            personasRepoMock.Setup(x => x.GetByDocumento("1234567-2")).Returns(person);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(x => x.SendPasswordRecoveryMailAsync(person, nameof(RecoverPassword)))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(RecoverPassword),
                    "Si los datos ingresados son correctos, recibiras un mail con instrucciones para recuperar tu contraseña."));

            var result = await _service.Recover.ExecuteAsync(request);

            Assert.True(result.Success);
            Assert.Null(result.Data);
            Assert.Contains("Si los datos ingresados son correctos", result.Message);
            _passwordActivationServiceMock.Verify(
                x => x.SendPasswordRecoveryMailAsync(person, nameof(RecoverPassword)),
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
            var request = new RecoverPasswordRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez"
            };

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByDocumento("1234567-2")).Returns((Persona)null!);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            var result = await _service.Recover.ExecuteAsync(request);

            Assert.True(result.Success);
            Assert.Null(result.Data);
            Assert.Contains("Si los datos ingresados son correctos", result.Message);
            _passwordActivationServiceMock.Verify(
                x => x.SendPasswordRecoveryMailAsync(It.IsAny<Persona>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RecuperarPassword_ApellidoNoCoincide_DevuelveMensajeGenericoSinEnviarMail()
        {
            var request = new RecoverPasswordRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Gomez"
            };

            var person = new Persona
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
            personasRepoMock.Setup(x => x.GetByDocumento("1234567-2")).Returns(person);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            var result = await _service.Recover.ExecuteAsync(request);

            Assert.True(result.Success);
            Assert.Null(result.Data);
            Assert.Contains("Si los datos ingresados son correctos", result.Message);
            _passwordActivationServiceMock.Verify(
                x => x.SendPasswordRecoveryMailAsync(It.IsAny<Persona>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_LdapAuthenticationFails_ReturnsFailed()
        {
            // Act
            var result = await _service.Authenticate.ExecuteAsync("XX", "1234567-2", "validpass");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_InvalidDocument_ReturnsFailed()
        {
            // Act
            var result = await _service.Authenticate.ExecuteAsync("CI", "invalido", "validpass");

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
            var result = await _service.Authenticate.ExecuteAsync("CI", "1234567-2", "validpass");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_04", result.ErrorCode);
            Assert.Equal("No se encontró la persona en la base de datos.", result.Message);
            Assert.Equal(404, result.HttpCode);
            _ldapMock.Verify(x => x.AutenticarUsuarioLDAPAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_PersonaExtranjera_ReturnsFailed()
        {
            // Arrange
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            var person = new Persona
            {
                CodigoPersona = 12345,
                Documento = "1234567-2",
                TipoDocumento = "CI",
                AlumnoExtranjeroPersona = "SI"
            };
            personasRepoMock.Setup(x => x.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns(person);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            // Act
            var result = await _service.Authenticate.ExecuteAsync("CI", "1234567-2", "validpass");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_05", result.ErrorCode);
            Assert.Equal("No se pudo iniciar sesión.", result.Message);
            Assert.Equal(403, result.HttpCode);
            _ldapMock.Verify(x => x.AutenticarUsuarioLDAPAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_WhenLdapRejectsCredentials_ReturnsLdapFailure()
        {
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            var person = new Persona
            {
                CodigoPersona = 12345,
                Documento = "1234567-2",
                TipoDocumento = "CI"
            };
            personasRepoMock.Setup(x => x.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns(person);
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

            var result = await _service.Authenticate.ExecuteAsync("CI", "1234567-2", "wrongpass");

            Assert.False(result.Success);
            Assert.Equal("LDAP_401", result.ErrorCode);
            Assert.Equal(401, result.HttpCode);
            _tokenServiceMock.Verify(t => t.GenerateAccessToken(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_SuccessfulAuthentication_ReturnsOkWithPersonaIdentityAndNoTokens()
        {
            // SEG-03: AuthenticateWithLdapAsync ya no emite ni persiste tokens. Eso lo hace
            // IssueTokensForPersonAsync, recién cuando el llamador decide que el login está
            // completo (sin 2FA pendiente).
            long personId = 12345;
            string password = "validpass";
            var ldapSuccessResult = OperationResult<bool>.Ok(true, "AuthenticateWithLdapAsync");

            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "Juan",
                SegundoNombre = "Carlos",
                PrimerApellido = "Pérez",
                SegundoApellido = "Gómez",
                TipoPersona = "E",
                Documento = "1234567-2"
            };

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByTipoDocumentoYDocumento("CI", "1234567-2")).Returns(person);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            _ldapMock.Setup(x => x.AutenticarUsuarioLDAPAsync(personId, password))
                .ReturnsAsync(ldapSuccessResult);

            // Act
            var result = await _service.Authenticate.ExecuteAsync("CI", "1234567-2", password);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(personId, result.Data.PersonId);
            Assert.Equal("Juan", result.Data.FirstName);
            Assert.Equal("Carlos", result.Data.MiddleName);
            Assert.Equal("Pérez", result.Data.FirstSurname);
            Assert.Equal("Gómez", result.Data.SecondSurname);
            Assert.Equal("E", result.Data.PersonType);
            Assert.Equal("1234567-2", result.Data.DocumentNumber);

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
            long personId = 12345;

            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "Juan",
                SegundoNombre = "Carlos",
                PrimerApellido = "Pérez",
                SegundoApellido = "Gómez",
                TipoPersona = "E",
                Documento = "1234567-2"
            };

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(personId)).Returns(person);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            _tokenServiceMock.Setup(x => x.GenerateAccessToken(person)).Returns("access_token_123");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token_456");
            _tokenServiceMock.Setup(x => x.HashToken("refresh_token_456")).Returns("hashed_refresh_token");

            _refreshTokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(
                personId,
                "ADMISIONESWEB",
                "hashed_refresh_token",
                It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.IssueTokens.ExecuteAsync(personId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("access_token_123", result.Data.AccessToken);
            Assert.Equal("refresh_token_456", result.Data.RefreshToken);
            Assert.Equal("hashed_refresh_token", result.Data.RefreshTokenHash);
            Assert.NotNull(result.Data.Person);
            Assert.Equal(personId, result.Data.Person.PersonId);

            _refreshTokenServiceMock.Verify(x => x.SaveRefreshTokenAsync(
                personId,
                "ADMISIONESWEB",
                "hashed_refresh_token",
                It.Is<DateTime>(d => d > DateTime.UtcNow.AddDays(6) && d < DateTime.UtcNow.AddDays(8))),
                Times.Once);
            _tokenServiceMock.Verify(x => x.GenerateAccessToken(person), Times.Once);
            _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
            _tokenServiceMock.Verify(x => x.HashToken("refresh_token_456"), Times.Once);
        }

        [Fact]
        public async Task GenerarTokensParaPersonaAsync_WhenRefreshTokenPersistenceFails_ReturnsFailedWithErrorCode()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            const long personId = 12345;
            var person = new Persona { CodigoPersona = personId, Documento = "1234567-2" };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(personId)).Returns(person);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(person)).Returns("access-token");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(x => x.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(x => x.SaveRefreshTokenAsync(personId, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("Persistence unavailable"));

            var result = await _service.IssueTokens.ExecuteAsync(personId);

            Assert.False(result.Success);
            Assert.Equal("GEN_TOK_99", result.ErrorCode);
            Assert.Equal(500, result.HttpCode);
            Assert.DoesNotContain("Persistence unavailable", result.Message);
            _tokenServiceMock.Verify(x => x.GenerateAccessToken(person), Times.Once);
            _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
            _tokenServiceMock.Verify(x => x.HashToken("refresh-token"), Times.Once);
            _refreshTokenServiceMock.Verify(
                x => x.SaveRefreshTokenAsync(personId, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()),
                Times.Once);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_ExceptionThrown_ReturnsFailedWithErrorCode()
        {
            // Arrange
            _uowFactoryMock.Setup(x => x.Create())
                .Throws(new InvalidOperationException("Database unavailable"));

            // Act
            var result = await _service.Authenticate.ExecuteAsync("CI", "1234567-2", "validpass");

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
            var result = await _service.Refresh.ExecuteAsync(null);

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
            var result = await _service.Refresh.ExecuteAsync(string.Empty);

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
            var result = await _service.Refresh.ExecuteAsync(refreshToken);

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
            long personId = 12345;

            _tokenServiceMock.Setup(x => x.HashToken(refreshToken)).Returns(hashedToken);
            _refreshTokenServiceMock.Setup(x => x.GetCodigoPersonaByRefreshTokenAsync("ADMISIONESWEB", hashedToken))
                .ReturnsAsync(personId);

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(personId)).Returns((Persona)null!);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            // Act
            var result = await _service.Refresh.ExecuteAsync(refreshToken);

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
            long personId = 12345;

            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "María",
                SegundoNombre = "Elena",
                PrimerApellido = "López",
                SegundoApellido = "Martínez",
                TipoPersona = "E",
                Documento = "98765432"
            };

            _tokenServiceMock.Setup(x => x.HashToken(oldRefreshToken)).Returns(oldHashedToken);
            _refreshTokenServiceMock.Setup(x => x.GetCodigoPersonaByRefreshTokenAsync("ADMISIONESWEB", oldHashedToken))
                .ReturnsAsync(personId);

            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(x => x.GetByKey(personId)).Returns(person);
            uowMock.Setup(x => x.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(x => x.Create()).Returns(uowMock.Object);

            _tokenServiceMock.Setup(x => x.GenerateAccessToken(person)).Returns("new_access_token");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new_refresh_token");
            _tokenServiceMock.Setup(x => x.HashToken("new_refresh_token")).Returns("new_hashed_token");

            _refreshTokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(
                personId,
                "ADMISIONESWEB",
                "new_hashed_token",
                It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.Refresh.ExecuteAsync(oldRefreshToken);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("new_access_token", result.Data.AccessToken);
            Assert.Equal("new_refresh_token", result.Data.RefreshToken);
            Assert.Equal("new_hashed_token", result.Data.RefreshTokenHash);
            Assert.Equal("Tokens renovados correctamente.", result.Data.Message);
            Assert.NotNull(result.Data.Person);
            Assert.Equal(personId, result.Data.Person.PersonId);
            Assert.Equal("María", result.Data.Person.FirstName);
            Assert.Equal("Elena", result.Data.Person.MiddleName);
            Assert.Equal("López", result.Data.Person.FirstSurname);
            Assert.Equal("Martínez", result.Data.Person.SecondSurname);
            Assert.Equal("E", result.Data.Person.PersonType);
            Assert.Equal("98765432", result.Data.Person.DocumentNumber);

            _refreshTokenServiceMock.Verify(x => x.SaveRefreshTokenAsync(
                personId,
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
            var result = await _service.Refresh.ExecuteAsync(refreshToken);

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
            var personId = 12345L;
            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI",
            };
            var request = new CompleteInitialPasswordRequest
            {
                NewPassword = "NuevaPassword1!"
            };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(personId)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(personId.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(personId.ToString(), request.NewPassword))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "AUTH_LDAP_52",
                    nameof(ILdap.ForzarCambiarPasswordAsync),
                    "El servicio LDAP no pudo forzar el cambio de password.",
                    400,
                    false));
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "password-activation-session", PersonId = personId },
                    nameof(IPasswordActivationService.ValidateSessionToken)));

            var result = (await _service.CompletePassword.ExecuteAsync("token", request)).Result;

            Assert.False(result.Success);
            _hashTokenStoreMock.Verify(h => h.DeleteAsync(personId.ToString()), Times.Never);
            uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistente_WhenPersonaDoesNotExist_ReturnsNotFound()
        {
            var personId = 12345L;
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(personId)).Returns((Persona)null!);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "password-activation-session", PersonId = personId },
                    nameof(IPasswordActivationService.ValidateSessionToken)));

            var result = (await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" })).Result;

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
            var personId = 12345L;
            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI"
            };
            var request = new CompleteInitialPasswordRequest
            {
                NewPassword = "NuevaPassword1!"
            };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(personId)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(personId.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(personId.ToString(), request.NewPassword))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(person)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(personId, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "password-activation-session", PersonId = personId },
                    nameof(IPasswordActivationService.ValidateSessionToken)));

            var result = (await _service.CompletePassword.ExecuteAsync("token", request)).Result;

            Assert.True(result.Success);
            Assert.Equal("access-token", result.Data!.AccessToken);
            Assert.Equal("refresh-token", result.Data.RefreshToken);
            uowMock.Verify(u => u.Save(), Times.Once);
            _hashTokenStoreMock.Verify(h => h.DeleteAsync(personId.ToString()), Times.Once);
            _refreshTokenServiceMock.Verify(
                r => r.SaveRefreshTokenAsync(personId, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()),
                Times.Once);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistente_WithTemporaryImages_PersistsAndDeletesCache()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            var personId = 12345L;
            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI",
                TipoDocumento = "CI",
                Documento = "1234567-2"
            };
            var request = new CompleteInitialPasswordRequest
            {
                NewPassword = "NuevaPassword1!"
            };
            var images = new TemporaryDocumentImages
            {
                ExpirationDate = new DateTime(2030, 1, 1),
                DocumentFront = new TemporaryDocumentFile
                {
                    Content = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                    FileName = "documento.jpg",
                    ContentType = "image/jpeg"
                },
                PersonFace = new TemporaryDocumentFile
                {
                    Content = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                    FileName = "cara.jpg",
                    ContentType = "image/jpeg"
                }
            };
            Imagen? fotoAgregada = null;
            ImagenTemporal? documentoAgregado = null;
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            var imagenRepoMock = new Mock<IImagenRepository>();
            var imagenTemporalRepoMock = new Mock<IImagenTemporalRepository>();
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
            var dbConnectionContextMock = new Mock<IDbConnectionContext>();
            personasRepoMock.Setup(r => r.GetByKey(personId)).Returns(person);
            imagenRepoMock.Setup(r => r.GetFotoByPersona(personId)).Returns(default(Imagen)!);
            imagenRepoMock
                .Setup(r => r.Add(It.IsAny<Imagen>()))
                .Callback<Imagen>(i => fotoAgregada = i);
            imagenTemporalRepoMock.Setup(r => r.GetDocumentoByPersonaAndTipo(personId, 1)).Returns(default(ImagenTemporal)!);
            imagenTemporalRepoMock
                .Setup(r => r.Add(It.IsAny<ImagenTemporal>()))
                .Callback<ImagenTemporal>(i => documentoAgregado = i);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            uowMock.Setup(u => u.Imagens).Returns(imagenRepoMock.Object);
            uowMock.Setup(u => u.ImagenTemporals).Returns(imagenTemporalRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            cacheMock
                .Setup(c => c.GetAsync("CI", "1234567-2"))
                .ReturnsAsync(images);
            dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL))
                .Returns(3000);
            dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN))
                .Returns(4000);
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(personId.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(personId.ToString(), request.NewPassword))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(person)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(personId, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "password-activation-session", PersonId = personId },
                    nameof(IPasswordActivationService.ValidateSessionToken)));
            var service = CreateUseCases(dbConnectionContextMock.Object, cacheMock.Object);

            var result = (await service.CompletePassword.ExecuteAsync("token", request)).Result;

            Assert.True(result.Success);
            Assert.NotNull(documentoAgregado);
            Assert.Equal(3000, documentoAgregado!.IdImagenTemporal);
            Assert.Equal(personId, documentoAgregado.CodigoPersona);
            Assert.Equal("1", documentoAgregado.TipoImagen);
            Assert.Equal("12345_1.jpg", documentoAgregado.NombreImagen);
            Assert.Equal(new DateTime(2030, 1, 1), documentoAgregado.FechaVtoDocumentoPersona);
            Assert.NotNull(fotoAgregada);
            Assert.Equal(4000, fotoAgregada!.IdImagen);
            Assert.Equal(personId, fotoAgregada.CodigoPersona);
            Assert.Equal("3", fotoAgregada.TipoImagen);
            Assert.Equal("12345_3.jpg", fotoAgregada.NombreImagen);
            Assert.Equal(new DateTime(2030, 1, 1), person.FechaVtoDocumentoPersona);
            cacheMock.Verify(c => c.DeleteAsync("CI", "1234567-2"), Times.Once);
            uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_WhenSessionValidationFails_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("bad-token"))
                .Returns(OperationResult<ValidatedSession>.IsFailed(
                    "ACT_SES_01",
                    nameof(IPasswordActivationService.ValidateSessionToken),
                    "Sesión temporal no encontrada.",
                    401,
                    default!));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "bad-token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("ACT_SES_01", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompleteInitialPassword", flow.Result.Method);
            _registroFlowServiceMock.Verify(
                s => s.GetPendingPersonAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_WhenSessionDataIsNull_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(null, nameof(IPasswordActivationService.ValidateSessionToken)));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("ACT_SES_06", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompleteInitialPassword", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaConFlowIdVacio_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "nueva-persona-session", FlowId = "  " },
                    nameof(IPasswordActivationService.ValidateSessionToken)));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("ACT_SES_NUP_01", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompleteInitialPassword", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaSinPendiente_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "nueva-persona-session", FlowId = "flow-1" },
                    nameof(IPasswordActivationService.ValidateSessionToken)));
            _registroFlowServiceMock
                .Setup(s => s.GetPendingPersonAsync("flow-1"))
                .ReturnsAsync((PendingPerson?)null);

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("NUP_COMP_01", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompleteInitialPassword", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaCuandoCreacionFalla_ReturnsFailureWithoutClearingCookie()
        {
            var pending = new PendingPerson { DocumentType = "CI", DocumentNumber = "1234567-2" };
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "nueva-persona-session", FlowId = "flow-1" },
                    nameof(IPasswordActivationService.ValidateSessionToken)));
            _registroFlowServiceMock
                .Setup(s => s.GetPendingPersonAsync("flow-1"))
                .ReturnsAsync(pending);
            _registroFlowServiceMock
                .Setup(s => s.CreatePersonFromPendingAsync(pending, "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<long>.IsFailed(
                    "REG_PERSONA_99",
                    nameof(IRegistrationFlowService.CreatePersonFromPendingAsync),
                    "Error al crear la persona.",
                    500,
                    default));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.False(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("REG_PERSONA_99", flow.Result.ErrorCode);
            Assert.Equal("Error al crear la persona.", flow.Result.Message);
            Assert.Equal(500, flow.Result.HttpCode);
            Assert.Equal("CompleteInitialPassword", flow.Result.Method);
            _registroFlowServiceMock.Verify(s => s.DeletePendingPersonAsync(It.IsAny<string>()), Times.Never);
            _registroFlowServiceMock.Verify(s => s.DeleteFlowSessionAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaExitosa_ClearsCookieAndDeletesPendingData()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            var personId = 12345L;
            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez"
            };
            var pending = new PendingPerson { DocumentType = "CI", DocumentNumber = "1234567-2" };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(personId)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "nueva-persona-session", FlowId = "flow-1" },
                    nameof(IPasswordActivationService.ValidateSessionToken)));
            _registroFlowServiceMock
                .Setup(s => s.GetPendingPersonAsync("flow-1"))
                .ReturnsAsync(pending);
            _registroFlowServiceMock
                .Setup(s => s.CreatePersonFromPendingAsync(pending, "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<long>.Ok(personId, nameof(IRegistrationFlowService.CreatePersonFromPendingAsync)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(person)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(personId, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.True(flow.Result.Success);
            Assert.Equal("access-token", flow.Result.Data!.AccessToken);
            Assert.Equal("refresh-token", flow.Result.Data.RefreshToken);
            Assert.Equal(nameof(IssueTokensForPerson), flow.Result.Method);
            _registroFlowServiceMock.Verify(s => s.DeletePendingPersonAsync("flow-1"), Times.Once);
            _registroFlowServiceMock.Verify(s => s.DeleteFlowSessionAsync("flow-1"), Times.Once);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_NuevaPersonaTokenGenerationFalla_NoLimpiaCookieNiRedis()
        {
            var pending = new PendingPerson { DocumentType = "CI", DocumentNumber = "1234567-2" };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(99L)).Returns((Persona)null!);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "nueva-persona-session", FlowId = "flow-1" },
                    nameof(IPasswordActivationService.ValidateSessionToken)));
            _registroFlowServiceMock
                .Setup(s => s.GetPendingPersonAsync("flow-1"))
                .ReturnsAsync(pending);
            _registroFlowServiceMock
                .Setup(s => s.CreatePersonFromPendingAsync(pending, "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<long>.Ok(99L, nameof(IRegistrationFlowService.CreatePersonFromPendingAsync)));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.False(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("GEN_TOK_01", flow.Result.ErrorCode);
            _registroFlowServiceMock.Verify(s => s.DeletePendingPersonAsync(It.IsAny<string>()), Times.Never);
            _registroFlowServiceMock.Verify(s => s.DeleteFlowSessionAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistenteSinCodigoPersona_ReturnsFailureAndClearsCookie()
        {
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "password-activation-session", PersonId = null },
                    nameof(IPasswordActivationService.ValidateSessionToken)));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.True(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("ACT_SES_03", flow.Result.ErrorCode);
            Assert.Equal(401, flow.Result.HttpCode);
            Assert.Equal("CompleteInitialPassword", flow.Result.Method);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistenteExitosa_ClearsCookie()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            var personId = 12345L;
            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI"
            };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(personId)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(personId.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(personId.ToString(), "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(person)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(personId, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "password-activation-session", PersonId = personId },
                    nameof(IPasswordActivationService.ValidateSessionToken)));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

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
            var personId = 12345L;
            var person = new Persona
            {
                CodigoPersona = personId,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                TipoPersona = "SGI",
                CodigoVigencia = "SI"
            };
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(personId)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            uowMock.Setup(u => u.Save()).Throws(new InvalidOperationException("DB caída"));
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _hashTokenStoreMock
                .Setup(h => h.GetAsync(personId.ToString()))
                .ReturnsAsync("stored-hash");
            _ldapMock
                .Setup(l => l.ForzarCambiarPasswordAsync(personId.ToString(), "NuevaPassword1!"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.ForzarCambiarPasswordAsync)));
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "password-activation-session", PersonId = personId },
                    nameof(IPasswordActivationService.ValidateSessionToken)));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.False(flow.Result.Success);
            Assert.Equal("INI_PAS_99", flow.Result.ErrorCode);
            Assert.Equal(500, flow.Result.HttpCode);
            _hashTokenStoreMock.Verify(
                h => h.DeleteAsync(personId.ToString()),
                Times.Never);
        }

        [Fact]
        public async Task CompletarPasswordFlowAsync_PersonaExistenteCuandoServicioFalla_NoLimpiaCookie()
        {
            var personId = 999L;
            var uowMock = new Mock<IUnitOfWork>();
            var personasRepoMock = new Mock<IPersonaRepository>();
            personasRepoMock.Setup(r => r.GetByKey(personId)).Returns((Persona)null!);
            uowMock.Setup(u => u.Personas).Returns(personasRepoMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            _passwordActivationServiceMock
                .Setup(s => s.ValidateSessionToken("token"))
                .Returns(OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { Purpose = "password-activation-session", PersonId = personId },
                    nameof(IPasswordActivationService.ValidateSessionToken)));

            var flow = await _service.CompletePassword.ExecuteAsync(
                "token",
                new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" });

            Assert.False(flow.ClearActivationCookie);
            Assert.False(flow.Result.Success);
            Assert.Equal("INI_PAS_03", flow.Result.ErrorCode);
            // "CompletarPasswordAsync" es el literal preservado del nombre original del método
            // (ver CompletarPasswordPersonaExistenteOriginMethod) para no cambiar el body público.
            Assert.Equal("CompletarPasswordAsync", flow.Result.Method);
        }
    }
}
