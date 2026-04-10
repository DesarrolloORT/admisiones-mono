using AppLogic.DTOs;
using AppLogic.IServices;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IGenericRepository;
using BusinessLogic.IServices;
using LdapService.Interfaces;
using Moq;
using Utilities;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class LoginServiceTests
    {
        private readonly Mock<ILdap> _ldapMock;
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IPersonaRepository> _personaRepositoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
        private readonly AuthService _service;

        public LoginServiceTests()
        {
            _ldapMock = new Mock<ILdap>();
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _personaRepositoryMock = new Mock<IPersonaRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();

            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _uowMock.Setup(u => u.Personas).Returns(_personaRepositoryMock.Object);

            _service = new AuthService(
                _ldapMock.Object,
                _uowFactoryMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_WhenLdapFails_ReturnsFailure()
        {
            _ldapMock
                .Setup(l => l.AutenticarUsuarioLDAPAsync(123, "bad-pass"))
                .ReturnsAsync(OperationResult<bool>.IsFailed("LDAP_01", "AutenticarUsuarioLDAPAsync", "Credenciales invalidas.", 401, false));

            var result = await _service.AutenticarUsuarioLDAPAsync(123, "bad-pass");

            Assert.False(result.Success);
            Assert.Equal("LDAP_01", result.ErrorCode);
            Assert.Equal(401, result.HttpCode);
            Assert.Equal("Credenciales invalidas.", result.Message);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_WhenPersonaDoesNotExist_ReturnsNotFound()
        {
            _ldapMock
                .Setup(l => l.AutenticarUsuarioLDAPAsync(123, "pass"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, "AutenticarUsuarioLDAPAsync"));
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns((Persona)null);

            var result = await _service.AutenticarUsuarioLDAPAsync(123, "pass");

            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_04", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_WhenRefreshExpirationIsMissing_ReturnsUnexpectedFailure()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", null));

            _ldapMock
                .Setup(l => l.AutenticarUsuarioLDAPAsync(123, "pass"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, "AutenticarUsuarioLDAPAsync"));
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(CreatePersona(123));
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<Persona>())).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");

            var result = await _service.AutenticarUsuarioLDAPAsync(123, "pass");

            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_99", result.ErrorCode);
            Assert.Contains("JWT_REFRESH_EXPIRE_ADMISIONES", result.Message);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_WhenDataIsValid_ReturnsAuthenticationResponse()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "7"));
            var persona = CreatePersona(123);

            _ldapMock
                .Setup(l => l.AutenticarUsuarioLDAPAsync(123, "pass"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, "AutenticarUsuarioLDAPAsync"));
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(persona);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(persona)).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(123, "ADMISIONESWEB", "refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            var before = DateTime.UtcNow;
            var result = await _service.AutenticarUsuarioLDAPAsync(123, "pass");
            var after = DateTime.UtcNow;

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(123, result.Data.Persona.CodigoPersona);
            Assert.Equal("Ana", result.Data.Persona.PrimerNombre);
            Assert.Equal("Perez", result.Data.Persona.PrimerApellido);
            Assert.Equal("access-token", result.Data.AccessToken);
            Assert.Equal("refresh-token", result.Data.RefreshToken);
            Assert.Equal("refresh-hash", result.Data.RefreshTokenHash);

            _refreshTokenServiceMock.Verify(r => r.SaveRefreshTokenAsync(
                123,
                "ADMISIONESWEB",
                "refresh-hash",
                It.Is<DateTime>(date => date >= before.AddDays(7).AddSeconds(-5) && date <= after.AddDays(7).AddSeconds(5))),
                Times.Once);
        }

        [Fact]
        public async Task AutenticarUsuarioLDAPAsync_WhenLdapThrows_ReturnsUnexpectedFailure()
        {
            _ldapMock
                .Setup(l => l.AutenticarUsuarioLDAPAsync(123, "pass"))
                .ThrowsAsync(new InvalidOperationException("LDAP caido"));

            var result = await _service.AutenticarUsuarioLDAPAsync(123, "pass");

            Assert.False(result.Success);
            Assert.Equal("LOGIN_LDAP_99", result.ErrorCode);
            Assert.Equal(500, result.HttpCode);
            Assert.Contains("LDAP caido", result.Message);
        }

        [Fact]
        public async Task RefrescarTokensAsync_WhenRefreshTokenIsMissing_ReturnsUnauthorized()
        {
            var result = await _service.RefrescarTokensAsync(null, "123");

            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_01", result.ErrorCode);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_WhenCodigoPersonaClaimIsInvalid_ReturnsUnauthorized()
        {
            var result = await _service.RefrescarTokensAsync("refresh-token", "abc");

            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_02", result.ErrorCode);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_WhenRefreshTokenIsInvalid_ReturnsUnauthorized()
        {
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.ValidateRefreshTokenAsync(123, "ADMISIONESWEB", "refresh-hash"))
                .ReturnsAsync(false);

            var result = await _service.RefrescarTokensAsync("refresh-token", "123");

            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_03", result.ErrorCode);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_WhenPersonaDoesNotExist_ReturnsNotFound()
        {
            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.ValidateRefreshTokenAsync(123, "ADMISIONESWEB", "refresh-hash"))
                .ReturnsAsync(true);
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns((Persona)null);

            var result = await _service.RefrescarTokensAsync("refresh-token", "123");

            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_04", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task RefrescarTokensAsync_WhenRefreshExpirationIsInvalid_ReturnsUnexpectedFailure()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "siete"));
            var persona = CreatePersona(123);

            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("current-refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.ValidateRefreshTokenAsync(123, "ADMISIONESWEB", "current-refresh-hash"))
                .ReturnsAsync(true);
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(persona);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(persona)).Returns("new-access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("new-refresh-token")).Returns("new-refresh-hash");

            var result = await _service.RefrescarTokensAsync("refresh-token", "123");

            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_99", result.ErrorCode);
            Assert.Contains("valor inv", result.Message);
        }

        [Fact]
        public async Task RefrescarTokensAsync_WhenDataIsValid_ReturnsRenewedTokens()
        {
            using var scope = new EnvironmentVariableScope(("JWT_REFRESH_EXPIRE_ADMISIONES", "10"));
            var persona = CreatePersona(123);

            _tokenServiceMock.Setup(t => t.HashToken("refresh-token")).Returns("current-refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.ValidateRefreshTokenAsync(123, "ADMISIONESWEB", "current-refresh-hash"))
                .ReturnsAsync(true);
            _personaRepositoryMock.Setup(r => r.GetByKey(123)).Returns(persona);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(persona)).Returns("new-access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh-token");
            _tokenServiceMock.Setup(t => t.HashToken("new-refresh-token")).Returns("new-refresh-hash");
            _refreshTokenServiceMock
                .Setup(r => r.SaveRefreshTokenAsync(123, "ADMISIONESWEB", "new-refresh-hash", It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            var before = DateTime.UtcNow;
            var result = await _service.RefrescarTokensAsync("refresh-token", "123");
            var after = DateTime.UtcNow;

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Tokens renovados correctamente.", result.Data.Message);
            Assert.Equal("new-access-token", result.Data.AccessToken);
            Assert.Equal("new-refresh-token", result.Data.RefreshToken);
            Assert.Equal("new-refresh-hash", result.Data.RefreshTokenHash);
            Assert.Equal(123, result.Data.Persona.CodigoPersona);

            _refreshTokenServiceMock.Verify(r => r.SaveRefreshTokenAsync(
                123,
                "ADMISIONESWEB",
                "new-refresh-hash",
                It.Is<DateTime>(date => date >= before.AddDays(10).AddSeconds(-5) && date <= after.AddDays(10).AddSeconds(5))),
                Times.Once);
        }

        [Fact]
        public async Task RefrescarTokensAsync_WhenTokenServiceThrows_ReturnsUnexpectedFailure()
        {
            _tokenServiceMock
                .Setup(t => t.HashToken("refresh-token"))
                .Throws(new InvalidOperationException("Hash invalido"));

            var result = await _service.RefrescarTokensAsync("refresh-token", "123");

            Assert.False(result.Success);
            Assert.Equal("REFRESH_TOKEN_99", result.ErrorCode);
            Assert.Equal(500, result.HttpCode);
            Assert.Contains("Hash invalido", result.Message);
        }

        private static Persona CreatePersona(long codigoPersona)
        {
            return new Persona
            {
                CodigoPersona = codigoPersona,
                PrimerNombre = "Ana",
                SegundoNombre = "Maria",
                PrimerApellido = "Perez",
                SegundoApellido = "Lopez",
                TipoPersona = "WEB",
                Documento = "12345678"
            };
        }
    }
}
