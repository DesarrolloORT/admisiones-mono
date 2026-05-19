using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using MailORT;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class PasswordActivationServiceTests
    {
        private const string Secret = "12345678901234567890123456789012";

        [Fact]
        public async Task EnviarMailLinkPasswordAsync_StoresHashAndSendsMail()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            var service = new PasswordActivationService(
                uowFactoryMock.Object,
                CrearConfiguracion(),
                mail);

            var result = await service.EnviarMailLinkPasswordAsync(persona, "Test");

            Assert.True(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(persona.HashTokenPassword));
            Assert.Contains("https://admisiones.test/crear-password?token=", mail.Body);
            personaRepoMock.Verify(r => r.Update(persona), Times.Once);
            uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithValidToken_ReturnsSession()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            var service = new PasswordActivationService(
                uowFactoryMock.Object,
                CrearConfiguracion(),
                mail);

            await service.EnviarMailLinkPasswordAsync(persona, "Test");
            var token = ExtraerToken(mail.Body);

            var result = await service.ActivarLinkPasswordAsync(token);

            Assert.True(result.Success);
            Assert.Equal(persona.CodigoPersona, result.Data!.CodigoPersona);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.SessionToken));
        }

        [Fact]
        public async Task ActivarLinkPasswordAsync_WithTamperedToken_ReturnsFailure()
        {
            var persona = CrearPersona();
            var personaRepoMock = new Mock<IPersonaRepository>();
            var uowMock = new Mock<IUnitOfWork>();
            var uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            var mail = new TestableEnvioMail();

            personaRepoMock.Setup(r => r.GetByKey(persona.CodigoPersona)).Returns(persona);
            uowMock.Setup(u => u.Personas).Returns(personaRepoMock.Object);
            uowFactoryMock.Setup(f => f.Create()).Returns(uowMock.Object);

            var service = new PasswordActivationService(
                uowFactoryMock.Object,
                CrearConfiguracion(),
                mail);

            await service.EnviarMailLinkPasswordAsync(persona, "Test");
            var token = ExtraerToken(mail.Body);

            var result = await service.ActivarLinkPasswordAsync(token + "x");

            Assert.False(result.Success);
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
            var end = body.IndexOf('"', start);
            var encodedToken = end >= 0 ? body[start..end] : body[start..];
            return Uri.UnescapeDataString(encodedToken);
        }

        private class TestableEnvioMail : EnvioMail
        {
            public string Body { get; private set; } = string.Empty;

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
                Body = body;
                return Task.CompletedTask;
            }
        }
    }
}
