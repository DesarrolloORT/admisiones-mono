using System.Net;
using System.Text;
using AppLogic.ApiClients.Dtos;
using AppLogic.ApiClients.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTesting.AppLogic.ApiClients
{
    public class InscripcionesyPagosApiClientTests
    {
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
        public async Task PagarCarritosPorInscripcionAsync_WithCustomPaymentType_PostsAllInscripciones()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                [
                  { "clave": "123|10|1|7|555", "valor": "Tu pago con Cuenta Personal se realizó exitosamente." }
                ]
                """));
            var client = CrearClient(handler);
            var result = await client.PagarCarritosPorInscripcionAsync([555], "PAGO/CUENTA");

            Assert.True(result.Success);
            Assert.Single(result.Data!);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("Pagos/Carritos/Pagar?tipoPago=PAGO%2FCUENTA&idsInscripcion=555", request.RequestUri);
            Assert.Equal(string.Empty, request.Body);
        }

        [Fact]
        public async Task PagarCarritosPorInscripcionAsync_WithMultipleInscripciones_PostsAllIdsInOneCall()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                [
                  { "clave": "123|10|1|7|555", "valor": "ok" },
                  { "clave": "123|10|1|8|556", "valor": "ok" }
                ]
                """));
            var client = CrearClient(handler);

            var result = await client.PagarCarritosPorInscripcionAsync([555, 556]);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("Pagos/Carritos/Pagar?tipoPago=PAGO_CUENTA_CORRIENTE", request.RequestUri);
            Assert.Contains("idsInscripcion=555", request.RequestUri);
            Assert.Contains("idsInscripcion=556", request.RequestUri);
            Assert.Equal(string.Empty, request.Body);
        }

        [Fact]
        public async Task ConfirmarPreInscripcionMultipleAsync_WithSuccess_ReturnsResponse()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                {
                  "respuesta": true,
                  "confirmada": true,
                  "inscripcionPendiente": false,
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Analista en TI",
                    "idComienzo": 30,
                    "comienzo": "Marzo 2026",
                    "idTurno": 7,
                    "turno": "Nocturno"
                  },
                  "ofertas": [
                    { "idOferta": 40, "idInscripcion": 55, "fechaVencimientoPago": "2026-06-30T00:00:00", "valorCuota": 1000, "valorSeniaMinima": 250 },
                    { "idOferta": 41, "idInscripcion": 56, "fechaVencimientoPago": "2026-06-30T00:00:00", "valorCuota": 800, "valorSeniaMinima": 200 }
                  ],
                  "estadoCuenta": {
                    "saldoActual": 3210.50
                  }
                }
                """));
            var client = CrearClient(handler);

            var result = await client.ConfirmarPreInscripcionMultipleAsync(new ConfirmarPreInscripcionMultipleApiRequest
            {
                IdProducto = 20,
                IdProceso = 30,
                IdsOfertasSeleccionadas = [40, 41],
                TipoInscripcion = "ONLINE",
                Turno = new DtoTurno { IdTurno = 7 }
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.False(result.Data.InscripcionPendiente);
            Assert.Equal("Analista en TI", result.Data.Resumen!.Carrera);
            Assert.Equal(2, result.Data.Ofertas.Count);
            Assert.Equal(40, result.Data.Ofertas[0].IdOferta);
            Assert.Equal(55, result.Data.Ofertas[0].IdInscripcion);
            Assert.Equal(41, result.Data.Ofertas[1].IdOferta);
            Assert.Equal(56, result.Data.Ofertas[1].IdInscripcion);
            Assert.Equal(3210.50m, result.Data.EstadoCuenta!.SaldoActual);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("ConfirmarPreInscripcionMultiple", request.RequestUri);
            Assert.Contains("tipoInscripcion=ONLINE", request.RequestUri);
            Assert.Contains("idProducto=20", request.RequestUri);
            Assert.Contains("idProceso=30", request.RequestUri);
            Assert.Contains("idsOfertasSeleccionadas=40", request.RequestUri);
            Assert.Contains("idsOfertasSeleccionadas=41", request.RequestUri);
        }

        [Fact]
        public async Task ConfirmarPreInscripcionMultipleAsync_WhenApiRejects_ReturnsFailure()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, "combinacion invalida"));
            var client = CrearClient(handler);

            var result = await client.ConfirmarPreInscripcionMultipleAsync(new ConfirmarPreInscripcionMultipleApiRequest
            {
                IdProducto = 20,
                IdProceso = 30,
                IdsOfertasSeleccionadas = [40],
                TipoInscripcion = "ONLINE",
                Turno = new DtoTurno { IdTurno = 7 }
            });

            Assert.False(result.Success);
            Assert.Equal("CONFIRMAR_PREINSCRIPCION_MULTIPLE_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ObtenerCarritosPorInscripcionAsync_WithSuccess_MapsResponseAndBuildsUrl()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                {
                  "carritos": [
                    { "idCarrito": "123|20|1|30|55", "senia": 1234.50 }
                  ],
                  "estadoCuenta": { "saldoActual": 3210.50 }
                }
                """));
            var client = CrearClient(handler);

            var result = await client.ObtenerCarritosPorInscripcionAsync([55]);

            Assert.True(result.Success);
            Assert.Equal(3210.50m, result.Data!.EstadoCuenta!.SaldoActual);
            var carrito = Assert.Single(result.Data.Carritos);
            Assert.Equal("123|20|1|30|55", carrito.IdCarrito);
            Assert.Equal(1234.50m, carrito.PagoReserva);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("Pagos/Carritos?idsInscripcion=55", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerCarritosPorInscripcionAsync_WithVariasInscripciones_BuildsQueryConTodosLosIds()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                {
                  "carritos": [],
                  "estadoCuenta": { "saldoActual": 0 }
                }
                """));
            var client = CrearClient(handler);

            var result = await client.ObtenerCarritosPorInscripcionAsync([55, 56]);

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("idsInscripcion=55", request.RequestUri);
            Assert.Contains("idsInscripcion=56", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerUrlCrearFacturaPorInscripcionAsync_WithSuccess_PostsAllInscripcionesAndBank()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, "\"https://pagos.test/factura\""));
            var client = CrearClient(handler);

            var result = await client.ObtenerUrlCrearFacturaPorInscripcionAsync([555], "SISTARBANC", "001");

            Assert.True(result.Success);
            Assert.Equal("https://pagos.test/factura", result.Data);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("Pagos/Carritos/UrlCrearFactura?tipoPago=SISTARBANC&banco=001&idsInscripcion=555", request.RequestUri);
            Assert.Equal(string.Empty, request.Body);
        }

        // Los 3 tests siguientes cubren métodos sin consumidores en producción ni tests previos
        // (ObtenerSeniaMinimaAsync, ObtenerCtaCteAsync, ObtenerCursosPagosAsync — ver auditoría,
        // Grupo C de métodos muertos). Se agregan casos mínimos de éxito para validar que el
        // refactor a SendAsync no cambió su URL/comportamiento, sin invertir tiempo extra en
        // cobertura exhaustiva de código sin uso real (no se eliminan por instrucción explícita).

        [Fact]
        public async Task ObtenerSeniaMinimaAsync_WithSuccess_ReturnsResponse()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{ "seniaMinima": 1234.50 }"""));
            var client = CrearClient(handler);

            var result = await client.ObtenerSeniaMinimaAsync(10, 20);

            Assert.True(result.Success);
            Assert.Equal(1234.50m, result.Data!.SeniaMinima);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("Inscripciones/SeniaMinima?idInscripto=10&idProducto=20", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerSeniaMinimaAsync_WhenApiRejects_ReturnsFailure()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, "inscripcion invalida"));
            var client = CrearClient(handler);

            var result = await client.ObtenerSeniaMinimaAsync(10, 20);

            Assert.False(result.Success);
            Assert.Equal("SENIA_MINIMA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Contains("Error al obtener la seña mínima", result.Message);
        }

        [Fact]
        public async Task ObtenerCtaCteAsync_WithSuccess_ReturnsResponse()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{ "saldoActual": 100.50, "saldoVencido": 0, "saldoAVencer": 100.50, "movimientos": [] }"""));
            var client = CrearClient(handler);

            var result = await client.ObtenerCtaCteAsync();

            Assert.True(result.Success);
            Assert.Equal(100.50m, result.Data!.SaldoActual);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("Pagos/CtaCte?estado=SALDO_ACTUAL_Y_MOVIMIENTOS", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerCursosPagosAsync_WithSuccess_ReturnsResponse()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{ "cursos": [], "montoTotal": 0 }"""));
            var client = CrearClient(handler);

            var result = await client.ObtenerCursosPagosAsync();

            Assert.True(result.Success);
            Assert.Empty(result.Data!.Cursos);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("Pagos/Carritos", request.RequestUri);
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
