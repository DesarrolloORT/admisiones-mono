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
                [
                  {
                    "idOferta": 10,
                    "idTurno": 30,
                    "nombreTurno": "Nocturno",
                    "horarioReferencia": "Martes 19:00"
                  }
                ]
                """));
            var client = CrearClient(handler);

            var result = await client.ObtenerOfertasParaInscripcionAdmisionesAsync(20, 40, 30);

            Assert.True(result.Success);
            var oferta = Assert.Single(result.Data!);
            Assert.Equal(10, oferta.IdOferta);
            Assert.Equal(30, oferta.Turno.IdTurno);
            Assert.Equal("Nocturno", oferta.Turno.NombreTurno);
            Assert.Equal("Martes 19:00", oferta.HorarioReferencia);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("OfertasParaInscripcionAdmisiones?idProducto=20&idComienzo=40", request.RequestUri);
            Assert.DoesNotContain("idTurno", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync_WithSuccess_MapsOfertaAdmisionesResponse()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                [
                  {
                    "idOferta": 57319,
                    "horarioReferencia": "Lunes y miercoles 08:00",
                    "idTurno": 1,
                    "nombreTurno": "Matutino"
                  }
                ]
                """));
            var client = CrearClient(handler);

            var result = await client.ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync(20, 99);

            Assert.True(result.Success);
            var oferta = Assert.Single(result.Data!);
            Assert.Equal(57319, oferta.IdOferta);
            Assert.Equal(1, oferta.Turno.IdTurno);
            Assert.Equal("Matutino", oferta.Turno.NombreTurno);
            Assert.Equal("Lunes y miercoles 08:00", oferta.HorarioReferencia);
        }

        [Fact]
        public async Task ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync_WhenApiRejects_ReturnsFailure()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, "combinacion invalida"));
            var client = CrearClient(handler);

            var result = await client.ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync(20, 99);

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
                JsonResponse(HttpStatusCode.OK, """
                {
                  "confirmada": true,
                  "message": "confirmada",
                  "idInscripcion": 55,
                  "seniaInscripcion": 1234.50,
                  "fechaVencimientoPago": "2026-06-30T00:00:00",
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Analista en TI",
                    "idComienzo": 30,
                    "comienzo": "Marzo 2026",
                    "idTurno": 7,
                    "turno": "Nocturno"
                  }
                }
                """));
            var client = CrearClient(handler);

            var result = await client.ConfirmarPreInscripcionAsync(new ConfirmarPreInscripcionApiRequest
            {
                IdProducto = 20,
                IdProceso = 30,
                IdOfertaSeleccionada = 40,
                TipoInscripcion = "WEB",
                Turno = new DtoTurno { IdTurno = 7 }
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.Equal(55, result.Data!.IdInscripcion);
            Assert.Equal(1234.50m, result.Data!.SeniaInscripcion);
            Assert.Equal(new DateTime(2026, 6, 30), result.Data!.FechaVencimientoPago);
            Assert.Equal("Analista en TI", result.Data!.Resumen!.Carrera);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("ConfirmarPreInscripcion", request.RequestUri);
            Assert.Contains("tipoInscripcion=WEB", request.RequestUri);
            Assert.Contains("idProducto=20", request.RequestUri);
            Assert.Contains("idProceso=30", request.RequestUri);
            Assert.Contains("idOfertaSeleccionada=40", request.RequestUri);
            Assert.DoesNotContain("\"idOfertaSeleccionada\":40", request.Body);
            Assert.DoesNotContain("\"idProducto\":20", request.Body);
            Assert.Contains("\"idTurno\":7", request.Body);
            Assert.DoesNotContain("\"tipoInscripcion\":\"WEB\"", request.Body);
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
