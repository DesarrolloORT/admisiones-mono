using AppLogic.Catalogs.Dtos;
using AppLogic.Enrollments.Survey.Dtos;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace UnitTesting.AppLogic.Contracts
{
    public class EncuestaInicialContractTests
    {
        private static JsonObject Contract => JsonNode.Parse(File.ReadAllText(ContractPath()))!.AsObject();

        [Fact]
        public void EncuestaInicialContract_IsValidJson()
        {
            Assert.Equal(11, Contract["version"]!.GetValue<int>());
            Assert.Equal("SaveInitialSurveyRequest", Contract["request"]!.GetValue<string>());
            Assert.Equal("InitialSurveyDetails", Contract["response"]!.GetValue<string>());
            Assert.NotNull(Contract["fields"]);
            Assert.NotNull(Contract["sections"]);
        }

        /// <summary>
        /// Lo que la encuesta devuelve pero no recibe (identidad, estado y el departamento derivado de
        /// la institución) vive en readOnlyFields, no en fields: fields refleja el request.
        /// </summary>
        [Fact]
        public void EncuestaInicialContract_CoversReadOnlyProperties()
        {
            var requestProperties = typeof(SaveInitialSurveyRequest)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(JsonName)
                .ToHashSet();

            var readOnlyProperties = typeof(InitialSurveyDetails)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .Select(JsonName)
                .Where(name => !requestProperties.Contains(name))
                .Order()
                .ToList();

            var contractReadOnly = Contract["readOnlyFields"]!.AsObject().Select(f => f.Key).Order().ToList();

            Assert.Equal(readOnlyProperties, contractReadOnly);
        }

        [Fact]
        public void EncuestaInicialContract_CoversRequestProperties()
        {
            var fields = Contract["fields"]!.AsObject();
            var requestProperties = typeof(SaveInitialSurveyRequest)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .Select(JsonName)
                .Order()
                .ToList();

            Assert.Equal(requestProperties, fields.Select(f => f.Key).Order().ToList());
        }

        [Fact]
        public void EncuestaInicialContract_CatalogSectionsReferenceCatalogResponse()
        {
            var catalogProperties = typeof(InitialSurveyCatalogsResponse)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(JsonName)
                .ToHashSet();

            var sections = CatalogSectionKeys()
                .ToHashSet();

            Assert.Subset(catalogProperties, sections);
        }

        [Fact]
        public void EncuestaInicialContract_CatalogPathsReferenceCatalogSections()
        {
            var catalogSections = CatalogSectionKeys()
                .ToHashSet();

            foreach (var field in Contract["fields"]!.AsObject())
            {
                var catalogPath = field.Value?["catalogPath"]?.GetValue<string>();
                if (catalogPath == null)
                {
                    continue;
                }

                var root = catalogPath.Split('.')[0].Replace("[]", string.Empty);
                Assert.Contains(root, catalogSections);
            }
        }

        [Fact]
        public void EncuestaInicialContract_AllowedOptionsExposeValueAndLabel()
        {
            foreach (var field in Contract["fields"]!.AsObject())
            {
                var allowedOptions = field.Value?["allowedOptions"]?.AsArray();
                if (allowedOptions == null)
                {
                    continue;
                }

                Assert.NotEmpty(allowedOptions);
                foreach (var option in allowedOptions)
                {
                    Assert.NotNull(option?["value"]);
                    Assert.False(string.IsNullOrWhiteSpace(option?["label"]?.GetValue<string>()));
                }
            }
        }

        [Fact]
        public void EncuestaInicialContract_PrivateAllowedOptionsMatchBackendValidation()
        {
            Assert.Equal([1, 2], AllowedOptionNumbers("lastSecondaryYearLocationId"));
            Assert.Equal([1, 2, 3], AllowedOptionNumbers("previousHigherEducationId"));
            Assert.Equal([1, 2], AllowedOptionNumbers("decisionLevelId"));
            Assert.Equal(["Uruguay", "En el exterior"], AllowedOptionLabels("lastSecondaryYearLocationId"));
            Assert.Equal(["S\u00ed, en Uruguay", "S\u00ed, en el exterior", "No"], AllowedOptionLabels("previousHigherEducationId"));
            Assert.Equal(["Decidido/a", "Con dudas"], AllowedOptionLabels("decisionLevelId"));
        }

        [Fact]
        public void EncuestaInicialContract_AnioBachilleratoDisallowsCuartoForNivelUniversitario()
        {
            var rules = Contract["fields"]!["highSchoolYear"]!["disallowedWhen"]!.AsArray();
            var rule = rules.Single(r => r!["code"]!.GetValue<string>() == "INS_EI_64");

            Assert.Equal("selectedCarrera.nivel == 1", rule!["condition"]!.GetValue<string>());
            Assert.Equal([4, 10], rule["values"]!.AsArray().Select(v => v!.GetValue<int>()).ToList());
        }

        [Fact]
        public void EncuestaInicialContract_ConditionalArraysRequireAtLeastOneItem()
        {
            string[] arrays =
            [
                "higherEducationUniversityIds",
                "consideredUniversityIds",
                "ortAdvertisingIds",
                "ortChoiceReasonIds"
            ];

            foreach (var field in arrays)
                Assert.Equal(1, Contract["fields"]![field]!["minItems"]!.GetValue<int>());
        }

        [Fact]
        public void EncuestaInicialContract_VecesRecursaHasMinimumOne()
        {
            Assert.Equal(1, Contract["fields"]!["highSchoolYearRepeatCount"]!["min"]!.GetValue<int>());
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
                Assert.Equal([true, false], AllowedOptionBooleans(field));
            }
        }

        private static string JsonName(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        }

        private static List<int> AllowedOptionNumbers(string field)
        {
            return Contract["fields"]![field]!["allowedOptions"]!
                .AsArray()
                .Select(v => v!["value"]!.GetValue<int>())
                .ToList();
        }

        private static List<string> AllowedOptionLabels(string field)
        {
            return Contract["fields"]![field]!["allowedOptions"]!
                .AsArray()
                .Select(v => v!["label"]!.GetValue<string>())
                .ToList();
        }

        private static List<bool> AllowedOptionBooleans(string field)
        {
            return Contract["fields"]![field]!["allowedOptions"]!
                .AsArray()
                .Select(v => v!["value"]!.GetValue<bool>())
                .ToList();
        }

        private static IEnumerable<string> CatalogSectionKeys()
        {
            return Contract["sections"]!.AsArray()
                .Select(s => s!["key"]!.GetValue<string>());
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
