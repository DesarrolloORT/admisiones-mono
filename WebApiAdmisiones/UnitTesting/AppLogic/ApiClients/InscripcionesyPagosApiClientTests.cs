using System.Net;
using System.Text;
using AppLogic.ApiClients;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTesting.AppLogic.ApiClients
{
    public class InscripcionesyPagosApiClientTests
    {
        [Fact]
        public async Task ObtenerOfertasParaInscripcionAdmisionesAsync_WithSuccess_MapsResponseAndBuildsUrl()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                {
                  "ofertas": [
                    {
                      "idOferta": 10,
                      "idProducto": 20,
                      "idTurno": 30,
                      "nombreTurno": "Nocturno",
                      "idComienzo": 40,
                      "totalCount": 1,
                      "disponible": true
                    }
                  ],
                  "totalCount": 1
                }
                """));
            var client = CrearClient(handler);

            var result = await client.ObtenerOfertasParaInscripcionAdmisionesAsync(20, 40, 30);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal(10, result.Data.Ofertas.Single().IdOferta);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("OfertasParaInscripcionAdmisiones?idProducto=20&idComienzo=40&idTurno=30", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync_WhenApiRejects_ReturnsFailure()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, "combinacion invalida"));
            var client = CrearClient(handler);

            var result = await client.ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync(20, 99, 30);

            Assert.False(result.Success);
            Assert.Equal("OFERTAS_INSCRIPCION_PROCESO_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Contains("BadRequest", result.Message);
        }

        [Fact]
        public async Task CrearFacturaAsync_WithSuccess_PostsPayloadAndEscapesTipoPago()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{"success":true,"message":"ok","idFactura":99,"numeroFactura":"A123"}"""));
            var client = CrearClient(handler);
            var carritos = new List<ClaveValorCarrito>
            {
                new() { Clave = "id", Valor = "123" }
            };

            var result = await client.CrearFacturaAsync(carritos, "EAN RED");

            Assert.True(result.Success);
            Assert.Equal(99, result.Data!.IdFactura);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("UltCrearFactura?tipoPago=EAN", request.RequestUri);
            Assert.Contains("RED", request.RequestUri);
            Assert.Contains("\"clave\":\"id\"", request.Body);
        }

        [Fact]
        public async Task ObtenerBancosAsync_WhenHttpRequestFails_ReturnsNetworkFailure()
        {
            var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("network down"));
            var client = CrearClient(handler);

            var result = await client.ObtenerBancosAsync();

            Assert.False(result.Success);
            Assert.Equal("API_NETWORK", result.ErrorCode);
            Assert.Equal(503, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcionAsync_WithSuccess_ReturnsResponse()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{"success":true,"message":"confirmada","idInscripcion":55}"""));
            var client = CrearClient(handler);

            var result = await client.ConfirmarPreInscripcionAsync(new ConfirmarPreInscripcionRequest
            {
                IdProducto = 20,
                IdProceso = 30,
                IdOfertaSeleccionada = 40,
                TipoInscripcion = "WEB"
            });

            Assert.True(result.Success);
            Assert.Equal(55, result.Data!.IdInscripcion);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("ConfirmarPreInscripcion", request.RequestUri);
            Assert.Contains("\"idOfertaSeleccionada\":40", request.Body);
        }

        private static InscripcionesyPagosApiClient CrearClient(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://internal.test/")
            };

            return new InscripcionesyPagosApiClient(
                httpClient,
                NullLogger<InscripcionesyPagosApiClient>.Instance);
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

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new CapturedRequest(
                    request.Method,
                    request.RequestUri?.ToString() ?? string.Empty,
                    request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));

                return handler(request);
            }
        }

        private sealed record CapturedRequest(HttpMethod Method, string RequestUri, string Body);
    }
}
