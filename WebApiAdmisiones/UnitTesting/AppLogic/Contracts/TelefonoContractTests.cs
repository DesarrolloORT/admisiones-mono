using AppLogic.Contracts.Dtos;
using AppLogic.Contracts.Text;
using AppLogic.People.Dtos;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebApiAdmisiones.Controllers;
using Xunit;

namespace UnitTesting.AppLogic.Contracts
{
    /// <summary>
    /// Verifica que <c>telefono.contract.json</c> siga describiendo lo que hace el código: los campos
    /// del DTO, las rutas de los endpoints, los códigos de error y —lo más importante— que cada
    /// ejemplo del contrato dé el resultado que promete.
    /// </summary>
    public class TelefonoContractTests
    {
        private static readonly JsonSerializerOptions CamelCase = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static JsonObject Contract => JsonNode.Parse(File.ReadAllText(ContractPath()))!.AsObject();

        private static JsonObject Endpoint(string name) => Contract["endpoints"]![name]!.AsObject();

        [Fact]
        public void TelefonoContract_IsValidJson()
        {
            Assert.Equal(1, Contract["version"]!.GetValue<int>());
            Assert.NotEmpty(Contract["description"]!.GetValue<string>());
            Assert.NotEmpty(Contract["validation"]!["rules"]!.AsArray());
        }

        [Fact]
        public void TelefonoContract_PhoneNumberFieldsMatchDto()
        {
            var contractFields = Contract["types"]!["PhoneNumber"]!["fields"]!.AsObject()
                .Select(f => f.Key)
                .Order()
                .ToList();

            var dtoProperties = typeof(PhoneNumber)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
                .Order()
                .ToList();

            Assert.Equal(dtoProperties, contractFields);
        }

        [Fact]
        public void TelefonoContract_ElTelefonoDeLaRespuestaEsString()
        {
            var declarado = Endpoint("obtenerDatosPersona")["data"]!["primaryPhone"]!["type"]!.GetValue<string>();
            var real = typeof(PersonDetailsResponse).GetProperty(nameof(PersonDetailsResponse.PrimaryPhone))!.PropertyType;

            Assert.Equal("string", declarado);
            Assert.Equal(typeof(string), real);
        }

        [Fact]
        public void TelefonoContract_PhoneNumberDeserializaElPayloadDelFront()
        {
            var phone = JsonSerializer.Deserialize<PhoneNumber>(
                """{"nationalNumber":"99333222","iso2":"UY"}""",
                CamelCase);

            Assert.Equal("99333222", phone!.NationalNumber);
            Assert.Equal("UY", phone.Iso2);
            // Lo que el front no manda queda vacío: el servidor no lo necesita.
            Assert.Null(phone.E164);
            Assert.Equal(0, phone.CountryCode);
            Assert.False(phone.IsValid);
        }

        [Fact]
        public void TelefonoContract_ElEjemploDelRequestDeserializa()
        {
            var example = Endpoint("actualizarDatosPersona")["example"]!.ToJsonString();

            var request = JsonSerializer.Deserialize<UpdatePersonDetailsRequest>(example, CamelCase);

            Assert.Equal("99333222", request!.PrimaryPhone.NationalNumber);
            Assert.Equal("UY", request.PrimaryPhone.Iso2);
        }

        [Fact]
        public void TelefonoContract_ElTelefonoDelRegistroTambienEsUnObjeto()
        {
            var request = JsonSerializer.Deserialize<RegisterPersonRequest>(
                """{"documentType":"CI","primaryPhone":{"nationalNumber":"99333222","iso2":"UY"}}""",
                CamelCase);

            Assert.Equal("99333222", request!.PrimaryPhone.NationalNumber);
            Assert.Equal("UY", request.PrimaryPhone.Iso2);
        }

        [Fact]
        public void TelefonoContract_BodySinTelefono_NoRompeElBinder()
        {
            // El default `new()` evita un 500: el caso de uso lo rechaza con PER_ADP_03.
            var request = JsonSerializer.Deserialize<UpdatePersonDetailsRequest>(
                """{"address":"18 de julio 1234"}""",
                CamelCase);

            Assert.NotNull(request!.PrimaryPhone);
            Assert.Null(request.PrimaryPhone.NationalNumber);
        }

        [Theory]
        [InlineData("actualizarDatosPersona", typeof(PersonController), nameof(PersonController.UpdatePersonDetails))]
        [InlineData("validarTelefono", typeof(PersonController), nameof(PersonController.ValidatePhoneNumber))]
        [InlineData("obtenerDatosPersona", typeof(PersonController), nameof(PersonController.GetPersonDetails))]
        [InlineData("confirmarNuevaPersona", typeof(RegistrationController), nameof(RegistrationController.ConfirmNewPerson))]
        [InlineData("confirmarSolicitudAlta", typeof(RegistrationController), nameof(RegistrationController.ConfirmRegistrationRequest))]
        public void TelefonoContract_RutasYVerbosCoincidenConElController(string endpointKey, Type controller, string action)
        {
            var endpoint = Endpoint(endpointKey);
            var method = controller.GetMethod(action)!;
            var verb = method.GetCustomAttributes<HttpMethodAttribute>().Single();
            var prefix = controller.GetCustomAttribute<RouteAttribute>()!.Template;

            Assert.Equal(endpoint["method"]!.GetValue<string>(), verb.HttpMethods.Single());
            Assert.Equal(endpoint["route"]!.GetValue<string>(), $"{prefix}/{verb.Template}");
        }

        [Theory]
        [InlineData("REG_TEL_01", RegistrationErrorCodes.InvalidPrimaryPhone)]
        [InlineData("REG_TEL_02", RegistrationErrorCodes.MissingPrimaryPhone)]
        [InlineData("REG_TEL_03", RegistrationErrorCodes.UnknownPhoneCountryCode)]
        public void TelefonoContract_CodigosDelRegistroCoincidenConLasConstantes(string enElContrato, string enElCodigo)
        {
            Assert.Equal(enElContrato, enElCodigo);
            Assert.NotNull(Contract["errors"]![enElContrato]);
            Assert.Equal(400, Contract["errors"]![enElContrato]!["httpCode"]!.GetValue<int>());
        }

        [Fact]
        public void TelefonoContract_LosCodigosDeclaradosEnLosEndpointsExistenEnErrors()
        {
            var declarados = Contract["errors"]!.AsObject().Select(e => e.Key).ToHashSet();

            var usados = Contract["endpoints"]!.AsObject()
                .SelectMany(e => e.Value!["mainErrors"]?.AsArray() ?? [])
                .Select(c => c!.GetValue<string>())
                .Where(c => c.StartsWith("PER_") || c.StartsWith("REG_"));

            Assert.All(usados, code => Assert.Contains(code, declarados));
        }

        /// <summary>
        /// El corazón del contrato: cada ejemplo se ejecuta contra la normalización real. Si el
        /// comportamiento cambia, el ejemplo que quedó desactualizado hace fallar el build.
        /// </summary>
        [Fact]
        public void TelefonoContract_LosEjemplosDanElResultadoQuePrometen()
        {
            var examples = Contract["validation"]!["examples"]!.AsArray();
            Assert.NotEmpty(examples);

            foreach (var example in examples)
            {
                var nationalNumber = example!["nationalNumber"]!.GetValue<string>();
                var iso2 = example["iso2"]?.GetValue<string>();
                var esperadoValido = example["valid"]!.GetValue<bool>();
                var contexto = $"nationalNumber='{nationalNumber}', iso2='{iso2 ?? "null"}'";

                var result = PhoneNormalization.Validate(nationalNumber, isPrimaryPhone: true, iso2);

                Assert.NotNull(result);
                Assert.Equal(esperadoValido, result!.TelefonoValido);

                if (esperadoValido)
                {
                    Assert.Equal(example["stored"]!.GetValue<string>(), result.TelefonoE164);
                }
                else
                {
                    // Un ejemplo inválido tiene que explicar por qué, o el front no sabe qué mostrar.
                    Assert.False(string.IsNullOrWhiteSpace(example["reason"]?.GetValue<string>()), contexto);
                }
            }
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
                    "telefono.contract.json");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new FileNotFoundException("No se encontro telefono.contract.json.");
        }
    }
}
