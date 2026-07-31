using AppLogic.Catalogos.Dtos;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace UnitTesting.AppLogic.Contracts
{
    public class CarrerasContractTests
    {
        private static JsonObject Contract => JsonNode.Parse(File.ReadAllText(ContractPath()))!.AsObject();
        private static JsonObject Endpoint => Contract["endpoints"]!["obtenerCarreras"]!.AsObject();

        [Fact]
        public void CarrerasContract_IsValidJson()
        {
            Assert.Equal(1, Contract["version"]!.GetValue<int>());
            Assert.Equal("GET", Endpoint["method"]!.GetValue<string>());
            Assert.Equal("Catalogos/Carreras", Endpoint["route"]!.GetValue<string>());
        }

        [Fact]
        public void CarrerasContract_PropuestaAcademicaMatchesEnum()
        {
            var options = Endpoint["query"]!["propuestaAcademica"]!["allowedOptions"]!.AsArray();

            var fromContract = options
                .Select(o => (Value: o!["value"]!.GetValue<int>(), Name: o["name"]!.GetValue<string>()))
                .OrderBy(o => o.Value)
                .ToList();

            var fromEnum = Enum.GetValues<PropuestaAcademica>()
                .Select(v => (Value: (int)v, Name: v.ToString()))
                .OrderBy(v => v.Value)
                .ToList();

            Assert.Equal(fromEnum, fromContract);
        }

        [Fact]
        public void CarrerasContract_DataFieldsMatchResponseDto()
        {
            var dataFields = Endpoint["data"]!.AsObject().Select(f => f.Key).Order().ToList();
            var dtoProperties = typeof(DtoCarrerasPorNivelResponse)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
                .Order()
                .ToList();

            Assert.Equal(dtoProperties, dataFields);
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
                    "carreras.contract.json");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new FileNotFoundException("No se encontro carreras.contract.json.");
        }
    }
}
