using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using AppLogic.Platform.Email;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Moq;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using AppLogic.Authentication.Services;
using AppLogic.Authentication.Interfaces;

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
            IEmailSender? emailSender = null,
            Mock<IHashTokenStore>? hashStoreMock = null,
            Mock<IPendingPersonStore>? pendingPersonaStoreMock = null)
        {
            return new PasswordActivationService(
                uowFactory ?? Mock.Of<IUnitOfWorkFactory>(),
                CrearConfiguracion(),
                emailSender ?? new TestEmailSender(),
                hashStoreMock?.Object ?? Mock.Of<IHashTokenStore>(),
                pendingPersonaStoreMock?.Object ?? Mock.Of<IPendingPersonStore>(),
                Mock.Of<ILogger<PasswordActivationService>>());
        }

        [Fact]
        public async Task EnviarMailLinkPasswordAsync_StoresHashInRedisAndSendsMail()
        {
            var person = CreatePerson();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestEmailSender();

            personaRepoMock.Setup(r => r.GetByKey(person.CodigoPersona)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            var result = await service.SendPasswordLinkMailAsync(person, "Test");

            Assert.True(result.Success);
            hashStoreMock.Verify(h => h.StoreAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
            Assert.Equal(1, mail.SendCount);
            Assert.Equal("ana@example.com", mail.To);
            Assert.Equal("Crea tu contraseña de Admisiones", mail.Subject);
            Assert.Contains("https://admisiones.test/crear-password?token=", mail.Body);
        }

        [Fact]
        public async Task EnviarMailLinkPasswordAsync_WithNullPersona_ReturnsBadRequest()
        {
            var service = CrearServicio();

            var result = await service.SendPasswordLinkMailAsync(null!, "Test");

            Assert.False(result.Success);
            Assert.Equal("ACT_PAS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task EnviarMailLinkPasswordAsync_WithPersonaWithoutEmail_ReturnsBadRequest()
        {
            var person = CreatePerson();
            person.Email = " ";
            var service = CrearServicio();

            var result = await service.SendPasswordLinkMailAsync(person, "Test");

            Assert.False(result.Success);
            Assert.Equal("ACT_PAS_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task EnviarMailNuevaPersonaAsync_SendsExpectedMailOnce()
        {
            var emailSender = new TestEmailSender();
            var service = CrearServicio(emailSender: emailSender);

            var result = await service.SendNewPersonMailAsync("flow-1", " nueva@example.com ", "token-1");

            Assert.True(result.Success);
            Assert.Equal(1, emailSender.SendCount);
            Assert.Equal("nueva@example.com", emailSender.To);
            Assert.Equal("Crea tu contraseña de Admisiones", emailSender.Subject);
            Assert.Contains("flow=registration", emailSender.Body);
            Assert.Contains("token-1", emailSender.Body);
        }

        [Fact]
        public async Task EnviarMailNuevaPersonaAsync_WhenEmailSenderFails_PreservesErrorBehavior()
        {
            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock
                .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Mail unavailable"));
            var service = CrearServicio(emailSender: emailSenderMock.Object);

            var result = await service.SendNewPersonMailAsync("flow-1", "nueva@example.com", "token-1");

            Assert.False(result.Success);
            Assert.Equal("ACT_NUP_99", result.ErrorCode);
            Assert.Equal("Error al enviar link de activación para nueva persona.", result.Message);
            Assert.Equal(500, result.HttpCode);
            emailSenderMock.Verify(
                s => s.SendAsync("nueva@example.com", "Crea tu contraseña de Admisiones", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task EnviarMailRecuperacionPasswordAsync_StoresHashInRedisAndSendsRecoveryMail()
        {
            var person = CreatePerson();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestEmailSender();

            personaRepoMock.Setup(r => r.GetByKey(person.CodigoPersona)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            var result = await service.SendPasswordRecoveryMailAsync(person, "Test");
            var token = ExtraerToken(mail.Body);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.True(result.Success);
            hashStoreMock.Verify(h => h.StoreAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
            Assert.Contains("flow=recovery", mail.Body);
            Assert.Equal(1, mail.SendCount);
            Assert.Equal("ana@example.com", mail.To);
            Assert.Equal("Recuperá tu contraseña de Admisiones", mail.Subject);
            Assert.Equal("password-recovery", jwt.Claims.First(c => c.Type == "purpose").Value);
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithValidToken_ReturnsSession()
        {
            var person = CreatePerson();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestEmailSender();

            personaRepoMock.Setup(r => r.GetByKey(person.CodigoPersona)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            string? storedHash = null;
            hashStoreMock.Setup(h => h.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, hash, _) => storedHash = hash)
                .Returns(Task.CompletedTask);
            hashStoreMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(() => storedHash);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            await service.SendPasswordLinkMailAsync(person, "Test");
            var token = ExtraerToken(mail.Body);

            var result = await service.ActivatePasswordLinkAsync(token);

            Assert.True(result.Success);
            Assert.Equal(person.CodigoPersona, result.Data!.PersonId);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.SessionToken));
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithRecoveryToken_ReturnsSession()
        {
            var person = CreatePerson();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestEmailSender();

            personaRepoMock.Setup(r => r.GetByKey(person.CodigoPersona)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            string? storedHash = null;
            hashStoreMock.Setup(h => h.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, hash, _) => storedHash = hash)
                .Returns(Task.CompletedTask);
            hashStoreMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(() => storedHash);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            await service.SendPasswordRecoveryMailAsync(person, "Test");
            var token = ExtraerToken(mail.Body);

            var result = await service.ActivatePasswordLinkAsync(token);

            Assert.True(result.Success);
            Assert.Equal(person.CodigoPersona, result.Data!.PersonId);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.SessionToken));
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithNuevaPersonaToken_ReturnsDocumentoWithoutCodigoPersona()
        {
            var flowId = Guid.NewGuid().ToString("N");
            var document = "12345672";
            var token = GenerateFlowIdToken(flowId, "nueva-persona-activacion", TimeSpan.FromHours(1));
            var pending = new PendingPerson
            {
                FlowId = flowId,
                DocumentType = "CI",
                DocumentNumber = document,
                Email = "ana@example.com",
                TokenHash = HashTokenForTest(token),
                CreatedAt = DateTime.UtcNow
            };
            var pendingPersonaStoreMock = new Mock<IPendingPersonStore>();
            pendingPersonaStoreMock
                .Setup(s => s.GetRawAsync(flowId))
                .ReturnsAsync(JsonSerializer.Serialize(pending, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));

            var service = CrearServicio(pendingPersonaStoreMock: pendingPersonaStoreMock);

            var result = await service.ActivatePasswordLinkAsync(token);

            Assert.True(result.Success);
            Assert.Null(result.Data!.PersonId);
            Assert.Equal(document, result.Data.DocumentNumber);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.SessionToken));
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithTamperedToken_ReturnsFailure()
        {
            var person = CreatePerson();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestEmailSender();

            personaRepoMock.Setup(r => r.GetByKey(person.CodigoPersona)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            string? storedHash = null;
            hashStoreMock.Setup(h => h.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, hash, _) => storedHash = hash)
                .Returns(Task.CompletedTask);
            hashStoreMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(() => storedHash);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            await service.SendPasswordLinkMailAsync(person, "Test");
            var token = ExtraerToken(mail.Body);

            var result = await service.ActivatePasswordLinkAsync(token + "x");

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithEmptyToken_ReturnsBadRequest()
        {
            var service = CrearServicio();

            var result = await service.ActivatePasswordLinkAsync(" ");

            Assert.False(result.Success);
            Assert.Equal("ACT_LINK_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ValidarSessionToken_WithSessionToken_ReturnsDtoValidatedSession()
        {
            var person = CreatePerson();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var hashStoreMock = new Mock<IHashTokenStore>();
            var mail = new TestEmailSender();

            personaRepoMock.Setup(r => r.GetByKey(person.CodigoPersona)).Returns(person);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            string? storedHash = null;
            hashStoreMock.Setup(h => h.StoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, hash, _) => storedHash = hash)
                .Returns(Task.CompletedTask);
            hashStoreMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(() => storedHash);

            var service = CrearServicio(uowFactoryMock.Object, mail, hashStoreMock);

            await service.SendPasswordLinkMailAsync(person, "Test");
            var activation = await service.ActivatePasswordLinkAsync(ExtraerToken(mail.Body));

            var result = service.ValidateSessionToken(activation.Data!.SessionToken!);

            Assert.True(result.Success);
            Assert.Equal(person.CodigoPersona, result.Data!.PersonId);
            Assert.Equal("password-activation-session", result.Data.Purpose);
        }

        [Fact]
        public void ValidarSessionToken_WithEmptyToken_ReturnsUnauthorized()
        {
            var service = CrearServicio();

            var result = service.ValidateSessionToken(" ");

            Assert.False(result.Success);
            Assert.Equal("ACT_SES_01", result.ErrorCode);
            Assert.Equal(401, result.HttpCode);
        }

        private static Persona CreatePerson()
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

        private static string GenerateFlowIdToken(string flowId, string purpose, TimeSpan duration)
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

        private sealed class TestEmailSender : IEmailSender
        {
            public string Body { get; private set; } = string.Empty;
            public string Subject { get; private set; } = string.Empty;
            public string To { get; private set; } = string.Empty;
            public int SendCount { get; private set; }

            public Task SendAsync(string to, string subject, string body)
            {
                To = to;
                Subject = subject;
                Body = body;
                SendCount++;
                return Task.CompletedTask;
            }
        }
    }
}
