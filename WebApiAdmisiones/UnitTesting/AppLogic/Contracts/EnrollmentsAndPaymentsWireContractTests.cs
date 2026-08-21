using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using AppLogic.Integrations.EnrollmentsAndPayments.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTesting.AppLogic.Contracts
{
    /// <summary>
    /// Contrato de cable con la API interna de Inscripciones y Pagos.
    /// <para>
    /// Los DTOs de <c>AppLogic.Integrations.EnrollmentsAndPayments</c> están en español a propósito:
    /// modelan el formato que emite un sistema ajeno. Renombrar una propiedad —o una clave de query
    /// string— rompe la integración <b>en silencio</b>: compila, no lanza, y los campos quedan en su
    /// valor por defecto.
    /// </para>
    /// <para>
    /// Ya pasó: un rename masivo cambió <c>idProducto</c> por <c>productId</c> en las URLs y en los
    /// fixtures, y toda la suite siguió verde porque los asserts no cubrían esos campos.
    /// </para>
    /// <para>
    /// Estos tests deserializan el JSON real y verifican <b>todas</b> las propiedades por reflexión:
    /// si alguien agrega una al DTO y no la agrega al fixture, el test falla. Esa es la parte que
    /// evita que el contrato se degrade con el tiempo.
    /// </para>
    /// </summary>
    public class EnrollmentsAndPaymentsWireContractTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Deserialización: el JSON que emite el sistema remoto llena TODO el DTO
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ConfirmarPreInscripcion_LlenaTodasLasPropiedadesDelDto()
        {
            var handler = Stub("""
            {
              "respuesta": true,
              "confirmada": true,
              "inscripcionPendiente": true,
              "resumen": {
                "idOferta": 40,
                "idProducto": 20,
                "carrera": "Analista en TI",
                "idComienzo": 30,
                "comienzo": "Marzo 2026",
                "idTurno": 7,
                "turno": "Nocturno"
              },
              "ofertas": [
                {
                  "idOferta": 40,
                  "idInscripcion": 55,
                  "fechaVencimientoPago": "2026-06-30T00:00:00",
                  "valorCuota": 1000,
                  "valorSeniaMinima": 250
                }
              ],
              "estadoCuenta": { "saldoActual": 3210.50 }
            }
            """);

            var result = await CrearClient(handler).ConfirmMultiplePreEnrollmentAsync(
                new ConfirmarPreInscripcionMultipleApiRequest
                {
                    IdProducto = 20,
                    IdProceso = 30,
                    IdsOfertasSeleccionadas = [40]
                });

            Assert.True(result.Success);
            var data = result.Data!;

            AssertTodasLasPropiedadesLlenas(data);
            AssertTodasLasPropiedadesLlenas(data.Resumen!);
            AssertTodasLasPropiedadesLlenas(Assert.Single(data.Ofertas));
            AssertTodasLasPropiedadesLlenas(data.EstadoCuenta!);

            // Valores concretos, para que no alcance con "no es el default".
            Assert.Equal(20, data.Resumen!.IdProducto);
            Assert.Equal("Analista en TI", data.Resumen.Carrera);
            Assert.Equal(30, data.Resumen.IdComienzo);
            Assert.Equal("Marzo 2026", data.Resumen.Comienzo);
            Assert.Equal(7, data.Resumen.IdTurno);
            Assert.Equal("Nocturno", data.Resumen.Turno);
            Assert.Equal(3210.50m, data.EstadoCuenta!.SaldoActual);

            var oferta = Assert.Single(data.Ofertas);
            Assert.Equal(40, oferta.IdOferta);
            Assert.Equal(55, oferta.IdInscripcion);
            Assert.Equal(new DateTime(2026, 6, 30), oferta.FechaVencimientoPago);
            Assert.Equal(1000, oferta.ValorCuota);
            Assert.Equal(250, oferta.ValorSeniaMinima);
        }

        [Fact]
        public async Task CuentaCorriente_LlenaTodasLasPropiedadesIncluidosLosMovimientos()
        {
            var handler = Stub("""
            {
              "saldoActual": 1500.25,
              "saldoVencido": 300.10,
              "saldoAVencer": 1200.15,
              "movimientos": [
                {
                  "fecha": "2026-05-01T00:00:00",
                  "concepto": "Cuota 1",
                  "debe": 1000.00,
                  "haber": 250.00,
                  "saldo": 750.00
                }
              ]
            }
            """);

            var result = await CrearClient(handler).GetCurrentAccountAsync();

            Assert.True(result.Success);
            AssertTodasLasPropiedadesLlenas(result.Data!);
            AssertTodasLasPropiedadesLlenas(Assert.Single(result.Data!.Movimientos));

            Assert.Equal(1500.25m, result.Data.SaldoActual);
            Assert.Equal(300.10m, result.Data.SaldoVencido);
            Assert.Equal(1200.15m, result.Data.SaldoAVencer);
        }

        [Fact]
        public async Task PagosDeCursos_LlenaTodasLasPropiedades()
        {
            var handler = Stub("""
            {
              "montoTotal": 5000.00,
              "cursos": [
                {
                  "idCurso": 88,
                  "nombreCurso": "Programación 1",
                  "monto": 2500.00,
                  "estado": "PENDIENTE",
                  "fechaVencimiento": "2026-07-15T00:00:00"
                }
              ]
            }
            """);

            var result = await CrearClient(handler).GetCoursePaymentsAsync();

            Assert.True(result.Success);
            AssertTodasLasPropiedadesLlenas(result.Data!);
            AssertTodasLasPropiedadesLlenas(Assert.Single(result.Data!.Cursos));
        }

        [Fact]
        public async Task Carritos_LlenanTodasLasPropiedades()
        {
            var handler = Stub("""
            {
              "carritos": [ { "idCarrito": "C-1", "senia": 250.00 } ],
              "estadoCuenta": { "saldoActual": 900.00 }
            }
            """);

            var result = await CrearClient(handler).GetCartsByEnrollmentAsync([555]);

            Assert.True(result.Success);
            AssertTodasLasPropiedadesLlenas(result.Data!);
            AssertTodasLasPropiedadesLlenas(Assert.Single(result.Data!.Carritos));
            AssertTodasLasPropiedadesLlenas(result.Data.EstadoCuenta!);

            // LogicaORT manda el importe con la clave "senia"; el DTO lo expone como PagoReserva
            // via [JsonPropertyName]. Si se quita el atributo, esto queda en 0.
            Assert.Equal(250.00m, Assert.Single(result.Data.Carritos).PagoReserva);
        }

        [Fact]
        public async Task SeniaMinima_LlenaLaPropiedad()
        {
            var handler = Stub("""{ "seniaMinima": 1234.50 }""");

            var result = await CrearClient(handler).GetMinimumDepositAsync(10, 20);

            Assert.True(result.Success);
            AssertTodasLasPropiedadesLlenas(result.Data!);
            Assert.Equal(1234.50m, result.Data!.SeniaMinima);
        }

        [Fact]
        public async Task MensajesDePago_ConservanElFormatoClaveValorEnEspanol()
        {
            // CartPaymentMessage tiene [JsonPropertyName("clave")/("valor")]: si se quitan,
            // deserializa en null sin error.
            var handler = Stub("""
            [ { "clave": "123|10|1|7|555", "valor": "Tu pago se realizó exitosamente." } ]
            """);

            var result = await CrearClient(handler).PayCartsByEnrollmentAsync(
                [555], "CUENTA_PERSONAL");

            Assert.True(result.Success);
            var mensaje = Assert.Single(result.Data!);
            Assert.Equal("123|10|1|7|555", mensaje.Key);
            Assert.Equal("Tu pago se realizó exitosamente.", mensaje.Value);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Claves de query string salientes: también son contrato del sistema remoto
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task SeniaMinima_UsaLasClavesDeQueryEnEspanol()
        {
            var handler = Stub("""{ "seniaMinima": 0 }""");

            await CrearClient(handler).GetMinimumDepositAsync(10, 20);

            var url = Assert.Single(handler.Requests).RequestUri;
            Assert.Contains("idInscripto=10", url);
            Assert.Contains("idProducto=20", url);
            Assert.DoesNotContain("productId", url);
        }

        [Fact]
        public async Task Pagar_UsaLasClavesDeQueryEnEspanol()
        {
            var handler = Stub("[]");

            await CrearClient(handler).PayCartsByEnrollmentAsync([1, 2], "ABITAB");

            var url = Assert.Single(handler.Requests).RequestUri;
            Assert.Contains("tipoPago=ABITAB", url);
            Assert.Contains("idsInscripcion=1", url);
            Assert.Contains("idsInscripcion=2", url);
        }

        [Fact]
        public async Task UrlDeFactura_UsaLasClavesDeQueryEnEspanol()
        {
            var handler = Stub("\"https://pago.test/factura/1\"");

            await CrearClient(handler).GetCreateInvoiceUrlByEnrollmentAsync([1], "BANRED", "77");

            var url = Assert.Single(handler.Requests).RequestUri;
            Assert.Contains("tipoPago=BANRED", url);
            Assert.Contains("banco=77", url);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Falla si alguna propiedad pública quedó en su valor por defecto: es la señal de que la
        /// clave del JSON no matcheó. Al recorrer por reflexión, una propiedad nueva en el DTO que
        /// no esté en el fixture rompe el test en vez de pasar desapercibida.
        /// </summary>
        private static void AssertTodasLasPropiedadesLlenas(object dto)
        {
            var tipo = dto.GetType();
            var sinLlenar = new List<string>();

            foreach (var prop in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0) continue;

                var valor = prop.GetValue(dto);

                var vacia = valor is null
                    || (valor is string s && string.IsNullOrEmpty(s))
                    || (valor is IEnumerable e and not string && !e.Cast<object>().Any())
                    || EsDefaultDeValor(valor);

                if (vacia) sinLlenar.Add(prop.Name);
            }

            Assert.True(
                sinLlenar.Count == 0,
                $"{tipo.Name}: estas propiedades no se llenaron desde el JSON, así que la clave del " +
                $"cable no matchea o falta en el fixture: {string.Join(", ", sinLlenar)}");
        }

        private static bool EsDefaultDeValor(object valor)
        {
            var tipo = valor.GetType();
            if (!tipo.IsValueType) return false;
            return valor.Equals(Activator.CreateInstance(tipo));
        }

        private static EnrollmentsAndPaymentsApiClient CrearClient(StubHandler handler) =>
            new(new HttpClient(handler) { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<EnrollmentsAndPaymentsApiClient>.Instance);

        private static StubHandler Stub(string body) => new(body);

        private sealed class StubHandler(string body) : HttpMessageHandler
        {
            public List<CapturedRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new CapturedRequest(
                    request.RequestUri?.ToString() ?? string.Empty,
                    request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            }
        }

        private sealed record CapturedRequest(string RequestUri, string Body);
    }
}
