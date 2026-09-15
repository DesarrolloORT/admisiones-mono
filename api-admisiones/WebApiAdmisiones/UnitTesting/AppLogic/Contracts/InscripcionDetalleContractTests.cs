using AppLogic.Enrollments.Dtos;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace UnitTesting.AppLogic.Contracts
{
    /// <summary>
    /// Mantiene inscripcion-detalle.contract.json en sync con los DTOs de respuesta: si alguien agrega,
    /// quita o renombra una propiedad, el contrato documentado debe reflejarlo (o el test falla).
    /// </summary>
    public class InscripcionDetalleContractTests
    {
        private static JsonObject Contract => JsonNode.Parse(File.ReadAllText(ContractPath()))!.AsObject();

        [Fact]
        public void Contract_IsValidJson()
        {
            Assert.Equal(2, Contract["version"]!.GetValue<int>());
            Assert.NotNull(Contract["types"]);
            Assert.NotNull(Contract["endpoints"]);
        }

        [Theory]
        [InlineData("EnrollmentHeader", typeof(EnrollmentHeader))]
        [InlineData("EnrollmentOffering", typeof(EnrollmentOffering))]
        [InlineData("CurrentAccountBalance", typeof(CurrentAccountBalance))]
        [InlineData("MinimumDepositDetails", typeof(MinimumDepositDetails))]
        [InlineData("ConfirmedEnrollment", typeof(ConfirmedEnrollment))]
        [InlineData("Coordinator", typeof(Coordinator))]
        [InlineData("Subject", typeof(Subject))]
        public void Contract_TypeFieldsMatchDto(string typeName, Type dtoType)
        {
            var documented = Contract["types"]![typeName]!["fields"]!.AsObject()
                .Select(f => f.Key).Order().ToList();

            Assert.Equal(JsonProperties(dtoType), documented);
        }

        [Fact]
        public void Contract_ConfirmarPreInscripcionDataMatchesDto()
        {
            var documented = Contract["endpoints"]!["confirmarPreInscripcion"]!["data"]!.AsObject()
                .Select(f => f.Key).Order().ToList();

            Assert.Equal(JsonProperties(typeof(ConfirmPreEnrollmentResponse)), documented);
        }

        [Fact]
        public void Contract_DetalleDataMatchesDto()
        {
            var documented = Contract["endpoints"]!["obtenerDetalleInscripcion"]!["data"]!.AsObject()
                .Select(f => f.Key).Order().ToList();

            Assert.Equal(JsonProperties(typeof(EnrollmentDetailsResponse)), documented);
        }

        [Fact]
        public void Contract_ConfirmadaEstadoMatchesDto()
        {
            var documented = Contract["endpoints"]!["obtenerDetalleInscripcion"]!["estados"]!["Confirmada"]!["confirmed"]!["fields"]!.AsObject()
                .Select(f => f.Key).Order().ToList();

            Assert.Equal(JsonProperties(typeof(ConfirmedEnrollmentDetailsResponse)), documented);
        }

        [Fact]
        public void Contract_DocumentedEstadosMatchBackendConstants()
        {
            var documented = Contract["endpoints"]!["obtenerDetalleInscripcion"]!["estados"]!.AsObject()
                .Select(e => e.Key).Order().ToList();

            Assert.Equal(
                new[] { "A la espera", "Confirmada", "En proceso", "Pago pendiente" },
                documented);
        }

        private static List<string> JsonProperties(Type dtoType)
        {
            return dtoType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(JsonName)
                .Order()
                .ToList();
        }

        private static string JsonName(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        }

        private static string ContractPath()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(
                    current.FullName,
                    "WebApiAdmisiones",
                    "WebApiAdmisiones",
                    "Docs",
                    "contracts",
                    "inscripcion-detalle.contract.json");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new FileNotFoundException("No se encontro inscripcion-detalle.contract.json.");
        }
    }
}
