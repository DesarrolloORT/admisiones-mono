using System.Net;
using System.Text;
using AppLogic.ApiClients;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTesting.AppLogic.Services
{
    public class OfertasInscripcionServiceTests
    {
        [Fact]
        public async Task ObtenerOfertasParaPersonaAsync_WhenPersonaDoesNotExist_ReturnsNotFound()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns((Persona)null!);
            var uow = CrearUow(personaRepo.Object);
            var service = CrearService(uow.Object, new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, "{}")));

            var result = await service.ObtenerOfertasParaPersonaAsync(123, 10, 20, 30);

            Assert.False(result.Success);
            Assert.Equal("OFERTAS_PERSONA_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task ObtenerOfertasParaPersonaAsync_WhenPersonaHasNoCodigoVigencia_ReturnsUnprocessableEntity()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(new Persona
            {
                CodigoPersona = 123,
                CodigoVigencia = " ",
                PrimerNombre = "Ana",
                PrimerApellido = "Perez"
            });
            var uow = CrearUow(personaRepo.Object);
            var service = CrearService(uow.Object, new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, "{}")));

            var result = await service.ObtenerOfertasParaPersonaAsync(123, 10, 20, 30);

            Assert.False(result.Success);
            Assert.Equal("OFERTAS_PERSONA_02", result.ErrorCode);
            Assert.Equal(422, result.HttpCode);
        }

        [Fact]
        public async Task ObtenerOfertasParaPersonaAsync_WhenApiFails_ReturnsApiFailure()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(CrearPersonaValida());
            var uow = CrearUow(personaRepo.Object);
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadGateway, "api caida"));
            var service = CrearService(uow.Object, handler);

            var result = await service.ObtenerOfertasParaPersonaAsync(123, 10, 20, 30);

            Assert.False(result.Success);
            Assert.Equal("OFERTAS_INSCRIPCION_01", result.ErrorCode);
            Assert.Equal(502, result.HttpCode);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task ObtenerOfertasParaPersonaAsync_WithValidPersonaAndApiSuccess_ReturnsOfertas()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(CrearPersonaValida());
            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns(new PersonaAdmite
            {
                CodigoPersona = 123,
                EstadoAdmite = "SI"
            });
            var uow = CrearUow(personaRepo.Object, personaAdmiteRepo.Object);
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                {
                  "ofertas": [
                    { "idOferta": 44, "idProducto": 10, "idTurno": 30, "disponible": true }
                  ],
                  "totalCount": 1
                }
                """));
            var service = CrearService(uow.Object, handler);

            var result = await service.ObtenerOfertasParaPersonaAsync(123, 10, 20, 30);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal(44, result.Data.Ofertas.Single().IdOferta);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("idProducto=10&idComienzo=20&idTurno=30", request.RequestUri);
        }

        private static OfertasInscripcionService CrearService(
            IUnitOfWork uow,
            HttpMessageHandler handler)
        {
            var factory = new Mock<IUnitOfWorkFactory>();
            factory.Setup(f => f.Create()).Returns(uow);
            var apiClient = new InscripcionesyPagosApiClient(
                new HttpClient(handler) { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<InscripcionesyPagosApiClient>.Instance);

            return new OfertasInscripcionService(
                factory.Object,
                apiClient,
                NullLogger<OfertasInscripcionService>.Instance);
        }

        private static Mock<IUnitOfWork> CrearUow(
            IPersonaRepository personaRepository,
            IPersonaAdmiteRepository? personaAdmiteRepository = null)
        {
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(x => x.Personas).Returns(personaRepository);

            var personaAdmiteMock = personaAdmiteRepository ?? Mock.Of<IPersonaAdmiteRepository>(
                r => r.GetByKey(It.IsAny<long>()) == null!);
            uow.Setup(x => x.PersonaAdmites).Returns(personaAdmiteMock);

            return uow;
        }

        private static Persona CrearPersonaValida()
        {
            return new Persona
            {
                CodigoPersona = 123,
                PrimerNombre = "Ana",
                PrimerApellido = "Perez",
                Documento = "1234567-2",
                CodigoVigencia = "SI"
            };
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : HttpMessageHandler
        {
            public List<CapturedRequest> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new CapturedRequest(
                    request.Method,
                    request.RequestUri?.ToString() ?? string.Empty));

                return Task.FromResult(handler(request));
            }
        }

        private sealed record CapturedRequest(HttpMethod Method, string RequestUri);
    }
}
