using System.Linq;
using System.Text.Json;
using AppLogic.Enrollments.Dtos;
using AppLogic.Identity.Dtos;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using AppLogic.Scholarships.Dtos;
using Xunit;

namespace UnitTesting.AppLogic.Contracts
{
    /// <summary>
    /// Los DTOs propios serializan en inglés: es el contrato nuevo, y este cambio se coordinó
    /// con el front.
    /// Los DTOs que deserializan respuestas de la API interna son la excepción: modelan un formato
    /// ajeno y conservan sus nombres originales.
    /// </summary>
    public class RenamedDtoJsonContractTests
    {
        private static readonly JsonSerializerOptions CamelCase = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static string[] PropertyNames<T>(T value) =>
            JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(value, CamelCase))
                .EnumerateObject()
                .Select(p => p.Name)
                .ToArray();

        [Fact]
        public void ScholarshipSummary_SerializaEnIngles()
        {
            var names = PropertyNames(new ScholarshipSummary());

            Assert.Contains("scholarshipId", names);
            Assert.Contains("applicationId", names);
            Assert.Contains("name", names);
            Assert.Contains("degreeProgram", names);
            Assert.Contains("status", names);
            Assert.Contains("applicationCloseDate", names);
            Assert.Contains("testDate", names);
            Assert.Contains("resultsDate", names);
            Assert.Contains("primaryAction", names);
            Assert.Contains("canContinueApplication", names);
            Assert.Contains("canDownloadStudyMaterial", names);
            Assert.Contains("studyMaterialUrl", names);
            Assert.Equal(12, names.Length);
        }

        [Fact]
        public void IdentityDocumentFile_SerializaEnIngles()
        {
            var names = PropertyNames(new IdentityDocumentFile());

            Assert.Equal(["content", "fileName"], names.Order().ToArray());
        }

        [Fact]
        public void PaymentMessage_SerializaEnIngles()
        {
            var names = PropertyNames(new PaymentMessage());

            Assert.Equal(["key", "value"], names.Order().ToArray());
        }

        [Fact]
        public void CartPaymentMessage_ConservaElFormatoDeLaApiInterna()
        {
            // No es preferencia de nomenclatura: es el formato que emite un sistema ajeno.
            // Si esto cambia, la deserialización deja de mapear.
            var names = PropertyNames(new CartPaymentMessage());

            Assert.Equal(["clave", "valor"], names.Order().ToArray());
        }

        [Fact]
        public void CartPaymentMessage_DeserializaElFormatoDeLaApiInterna()
        {
            var message = JsonSerializer.Deserialize<CartPaymentMessage>(
                """{"clave":"123|10","valor":"Pago realizado"}""");

            Assert.Equal("123|10", message!.Key);
            Assert.Equal("Pago realizado", message.Value);
        }
    }
}
