using AppLogic.IServices;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using MailORT;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Moq;
using StackExchange.Redis;
using AppLogic.Services;
using AppLogic.DTOs;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class PasswordActivationServiceTests : IDisposable
    {
        private const string Secret = "12345678901234567890123456789012";
        private readonly EnvironmentVariableScope _environment = new(
            ("PASSWORD_ACTIVATION_SECRET_KEY", Secret));

        public void Dispose()
        {
            _environment.Dispose();
        }

        private PasswordActivationService CrearServicio(
            IUnitOfWorkFactory? uowFactory = null,
            TestableEnvioMail? mail = null,
            Mock<IHashTokenStore>? hashStoreMock = null,
            Mock<IDatabase>? redisDbMock = null)
        {
            var redisMock = new Mock<IConnectionMultiplexer>();
            var dbMock = redisDbMock ?? new Mock<IDatabase>();
            redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

            return new PasswordActivationService(
                uowFactory ?? Mock.Of<IUnitOfWorkFactory>(),
                CrearConfiguracion(),
                mail ?? new TestableEnvioMail(),
                hashStoreMock?.Object ?? Mock.Of<IHashTokenStore>(),
                redisMock.Object);
        }

        [Fact]
        public async Task EnviarMailLinkPasswordAsync_StoresHashInRedisAndSendsMail()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            var result = await service.EnviarMailLinkPasswordAsync(persona, "Test");

            Assert.True(result.Success);
            hashStoreMock.Verify(h => h.StoreAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
            Assert.Contains("https://admisiones.test/crear-password?token=", mail.Body);
        }

        [Fact]
        public async Task EnviarMailLinkPasswordAsync_WithNullPersona_ReturnsBadRequest()
        {
            var service = CrearServicio();

            var result = await service.EnviarMailLinkPasswordAsync(null!, "Test");

            Assert.False(result.Success);
            Assert.Equal("ACT_PAS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task EnviarMailLinkPasswordAsync_WithPersonaWithoutEmail_ReturnsBadRequest()
        {
            var persona = CrearPersona();
            persona.Email = " ";
            var service = CrearServicio();

            var result = await service.EnviarMailLinkPasswordAsync(persona, "Test");

            Assert.False(result.Success);
            Assert.Equal("ACT_PAS_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task EnviarMailRecuperacionPasswordAsync_StoresHashInRedisAndSendsRecoveryMail()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            var result = await service.EnviarMailRecuperacionPasswordAsync(persona, "Test");
            var token = ExtraerToken(mail.Body);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.True(result.Success);
            hashStoreMock.Verify(h => h.StoreAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
            Assert.Contains("flow=recovery", mail.Body);
            Assert.Contains("Recuper", mail.Subject);
            Assert.Equal("password-recovery", jwt.Claims.First(c => c.Type == "purpose").Value);
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithValidToken_ReturnsSession()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            string? storedHash = null;
            hashStoreMock.Setup(h => h.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, hash, _) => storedHash = hash)
                .Returns(Task.CompletedTask);
            hashStoreMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(() => storedHash);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            await service.EnviarMailLinkPasswordAsync(persona, "Test");
            var token = ExtraerToken(mail.Body);

            var result = await service.ActivarLinkPasswordAsync(token);

            Assert.True(result.Success);
            Assert.Equal(persona.CodigoPersona, result.Data!.CodigoPersona);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.SessionToken));
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithRecoveryToken_ReturnsSession()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            string? storedHash = null;
            hashStoreMock.Setup(h => h.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, hash, _) => storedHash = hash)
                .Returns(Task.CompletedTask);
            hashStoreMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(() => storedHash);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            await service.EnviarMailRecuperacionPasswordAsync(persona, "Test");
            var token = ExtraerToken(mail.Body);

            var result = await service.ActivarLinkPasswordAsync(token);

            Assert.True(result.Success);
            Assert.Equal(persona.CodigoPersona, result.Data!.CodigoPersona);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.SessionToken));
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithNuevaPersonaToken_ReturnsDocumentoWithoutCodigoPersona()
        {
            var flowId = Guid.NewGuid().ToString("N");
            var documento = "12345672";
            var token = GenerarTokenFlowId(flowId, "nueva-persona-activacion", TimeSpan.FromHours(1));
            var pending = new RegistroPendingPersona
            {
                FlowId = flowId,
                TipoDocumento = "CI",
                Documento = documento,
                Email = "ana@example.com",
                TokenHash = HashTokenForTest(token),
                CreatedAt = DateTime.UtcNow
            };
            var redisDbMock = new Mock<IDatabase>();
            redisDbMock
                .Setup(d => d.StringGetAsync(
                    It.Is<RedisKey>(k => k == $"registro:pending:{flowId}"),
                    It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisValue)JsonSerializer.Serialize(pending, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));

            var service = CrearServicio(redisDbMock: redisDbMock);

            var result = await service.ActivarLinkPasswordAsync(token);

            Assert.True(result.Success);
            Assert.Null(result.Data!.CodigoPersona);
            Assert.Equal(documento, result.Data.Documento);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.SessionToken));
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithTamperedToken_ReturnsFailure()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            string? storedHash = null;
            hashStoreMock.Setup(h => h.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, hash, _) => storedHash = hash)
                .Returns(Task.CompletedTask);
            hashStoreMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(() => storedHash);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            await service.EnviarMailLinkPasswordAsync(persona, "Test");
            var token = ExtraerToken(mail.Body);

            var result = await service.ActivarLinkPasswordAsync(token + "x");

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithEmptyToken_ReturnsBadRequest()
        {
            var service = CrearServicio();

            var result = await service.ActivarLinkPasswordAsync(" ");

            Assert.False(result.Success);
            Assert.Equal("ACT_LINK_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ValidarSessionToken_WithSessionToken_ReturnsDtoValidatedSession()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            string? storedHash = null;
            hashStoreMock.Setup(h => h.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, hash, _) => storedHash = hash)
                .Returns(Task.CompletedTask);
            hashStoreMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(() => storedHash);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            await service.EnviarMailLinkPasswordAsync(persona, "Test");
            var activation = await service.ActivarLinkPasswordAsync(ExtraerToken(mail.Body));

            var result = service.ValidarSessionToken(activation.Data!.SessionToken!);

            Assert.True(result.Success);
            Assert.Equal(persona.CodigoPersona, result.Data!.CodigoPersona);
            Assert.Equal("password-activation-session", result.Data.Purpose);
        }

        [Fact]
        public void ValidarSessionToken_WithEmptyToken_ReturnsUnauthorized()
        {
            var service = CrearServicio();

            var result = service.ValidarSessionToken(" ");

            Assert.False(result.Success);
            Assert.Equal("ACT_SES_01", result.ErrorCode);
            Assert.Equal(401, result.HttpCode);
        }

        private static Persona CrearPersona()
        {
            return new Persona
            {
                CodigoPersona = 12345,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                PrimerNombreMay = "ANA",
                PrimerApellidoMay = "PEREZ",
                TipoPersona = "SGI",
                CodigoVigencia = "SI",
                Email = "ana@example.com"
            };
        }

        private static IConfiguration CrearConfiguracion()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PasswordActivation:SecretKey"] = Secret,
                    ["PasswordActivation:ExpireHours"] = "24",
                    ["PasswordActivation:SessionMinutes"] = "15",
                    ["AdmisionesFrontend:CrearPasswordUrl"] = "https://admisiones.test/crear-password",
                    ["Mail:From"] = "admisiones@example.com"
                })
                .Build();
        }

        private static string ExtraerToken(string body)
        {
            const string marker = "token=";
            var start = body.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(start >= 0, "El body del mail no contiene token.");
            start += marker.Length;
            var end = body.IndexOfAny(['&', '"'], start);
            var encodedToken = end >= 0 ? body[start..end] : body[start..];
            return Uri.UnescapeDataString(encodedToken);
        }

        private static string GenerarTokenFlowId(string flowId, string purpose, TimeSpan duration)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, flowId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim("purpose", purpose)
            };

            var token = new JwtSecurityToken(
                issuer: "WebApiAdmisiones",
                audience: "AdmisionesPassword",
                claims: claims,
                expires: DateTime.UtcNow.Add(duration),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashTokenForTest(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        private class TestableEnvioMail : EnvioMail
        {
            public string Body { get; private set; } = string.Empty;
            public string Subject { get; private set; } = string.Empty;

            public TestableEnvioMail() : base("http://localhost/wsdl")
            {
            }

            public override Task EnviarMail(
                string from,
                List<string> colTOs,
                string subject,
                string body,
                List<string>? colReplyTo = null,
                string sistema = "")
            {
                Subject = subject;
                Body = body;
                return Task.CompletedTask;
            }
        }
    }
}
