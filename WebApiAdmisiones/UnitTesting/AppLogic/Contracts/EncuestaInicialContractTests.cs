using AppLogic.Dtos.Catalogos;
using AppLogic.Dtos.EncuestaInicial;
using System.Reflection;
using System.Text.Json.Nodes;

namespace UnitTesting.AppLogic.Contracts
{
    public class EncuestaInicialContractTests
    {
        private static JsonObject Contract => JsonNode.Parse(File.ReadAllText(ContractPath()))!.AsObject();

        [Fact]
        public void EncuestaInicialContract_IsValidJson()
        {
            Assert.Equal(1, Contract["version"]!.GetValue<int>());
            Assert.Equal("DtoGuardarEncuestaInicialRequest", Contract["request"]!.GetValue<string>());
            Assert.NotNull(Contract["fields"]);
        }

        [Fact]
        public void EncuestaInicialContract_CoversRequestProperties()
        {
            var fields = Contract["fields"]!.AsObject();
            var requestProperties = typeof(DtoGuardarEncuestaInicialRequest)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .Order()
                .ToList();

            Assert.Equal(requestProperties, fields.Select(f => f.Key).Order().ToList());
        }

        [Fact]
        public void EncuestaInicialContract_CatalogPathsReferenceCatalogResponse()
        {
            var catalogProperties = typeof(DtoEncuestaInicialCatalogosResponse)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet();

            foreach (var field in Contract["fields"]!.AsObject())
            {
                var catalogPath = field.Value?["catalogPath"]?.GetValue<string>();
                if (catalogPath == null)
                {
                    continue;
                }

                var root = catalogPath.Split('.')[0].Replace("[]", string.Empty);
                Assert.Contains(root, catalogProperties);
            }
        }

        [Fact]
        public void EncuestaInicialContract_PrivateAllowedValuesMatchBackendValidation()
        {
            Assert.Equal([1, 2], AllowedNumbers("UltimoAnioSecundaria"));
            Assert.Equal([1, 2], AllowedNumbers("NivelDecision"));
            Assert.Equal([1, 2, 3, 4, 5], AllowedNumbers("ValoracionAsesoramientoOrt"));
            Assert.Equal([1, 2, 3, 4, 5], AllowedNumbers("ValoracionSitioWeb"));
            Assert.Equal([1, 2, 3, 4, 5], AllowedNumbers("ValoracionInstalacionesOrt"));
            Assert.Equal(["SI", "NO"], AllowedStrings("InfoOtrasUniversidadesAntes"));
            Assert.Equal(["SI", "NO"], AllowedStrings("InformarEncuesta"));
        }

        [Fact]
        public void EncuestaInicialContract_BooleanFieldsAllowTrueFalse()
        {
            var booleanFields = Contract["fields"]!.AsObject()
                .Where(f => f.Value?["type"]?.GetValue<string>() == "boolean")
                .Select(f => f.Key)
                .ToList();

            Assert.NotEmpty(booleanFields);
            foreach (var field in booleanFields)
            {
                Assert.Equal([true, false], AllowedBooleans(field));
            }
        }

        private static List<int> AllowedNumbers(string field)
        {
            return Contract["fields"]![field]!["allowedValues"]!
                .AsArray()
                .Select(v => v!.GetValue<int>())
                .ToList();
        }

        private static List<string> AllowedStrings(string field)
        {
            return Contract["fields"]![field]!["allowedValues"]!
                .AsArray()
                .Select(v => v!.GetValue<string>())
                .ToList();
        }

        private static List<bool> AllowedBooleans(string field)
        {
            return Contract["fields"]![field]!["allowedValues"]!
                .AsArray()
                .Select(v => v!.GetValue<bool>())
                .ToList();
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
                    "encuesta-inicial.contract.json");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new FileNotFoundException("No se encontro encuesta-inicial.contract.json.");
        }
    }
}
