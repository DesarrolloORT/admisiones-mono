using AppLogic.Registration.Dtos;
using AppLogic.Registration.Interfaces;
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
    /// Verifica que <c>registro.contract.json</c> siga describiendo el flujo real: los flags de
    /// evaluate-document, los finales de los endpoints de confirmación y las rutas del controller.
    /// </summary>
    public class RegistroContractTests
    {
        private static JsonObject Contract => JsonNode.Parse(File.ReadAllText(ContractPath()))!.AsObject();

        private static JsonObject Endpoint(string name) => Contract["endpoints"]![name]!.AsObject();

        private static JsonObject Ending(string key) => Contract["cases"]!["endings"]!.AsArray()
            .Single(e => e!["key"]!.GetValue<string>() == key)!.AsObject();

        [Fact]
        public void RegistroContract_IsValidJson()
        {
            Assert.Equal(1, Contract["version"]!.GetValue<int>());
            Assert.NotEmpty(Contract["description"]!.GetValue<string>());
            Assert.NotEmpty(Contract["cases"]!["flow"]!.AsArray());
        }

        /// <summary>
        /// Los flags documentados tienen que ser exactamente las propiedades del DTO: si se agrega un
        /// camino nuevo al registro y no se documenta, este test lo señala.
        /// </summary>
        [Fact]
        public void RegistroContract_LosFlagsDeEvaluateDocumentSonLosDelDto()
        {
            var contractFields = Contract["types"]!["DocumentEvaluationResponse"]!["fields"]!.AsObject()
                .Select(f => f.Key)
                .Order()
                .ToList();

            var dtoProperties = typeof(DocumentEvaluationResponse)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
                .Order()
                .ToList();

            Assert.Equal(dtoProperties, contractFields);
        }

        [Fact]
        public void RegistroContract_CadaCaminoDeclaraUnFlagQueExisteEnElDto()
        {
            var flags = Contract["types"]!["DocumentEvaluationResponse"]!["fields"]!.AsObject()
                .Select(f => f.Key)
                .ToHashSet();

            var caminos = Contract["cases"]!["flow"]!.AsArray();

            Assert.All(caminos, camino => Assert.Contains(camino!["flag"]!.GetValue<string>(), flags));
            // Cinco caminos, uno por flag; 'flowId' no es un camino.
            Assert.Equal(flags.Count - 1, caminos.Count);
        }

        [Fact]
        public void RegistroContract_LosCamposDeRegistrationFlowResultSonLosDelRecord()
        {
            var contractFields = Contract["types"]!["RegistrationFlowResult"]!["fields"]!.AsObject()
                .Select(f => f.Key)
                .Order()
                .ToList();

            var recordProperties = typeof(RegistrationFlowResult)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
                .Order()
                .ToList();

            Assert.Equal(recordProperties, contractFields);
        }

        /// <summary>
        /// El corazón del contrato: los dos finales prometen valores concretos de mailSent y
        /// pendingReview. Acá se comparan contra los defaults reales del record.
        /// </summary>
        [Fact]
        public void RegistroContract_ElAltaDePersonaNoEsPendingReview()
        {
            var declarado = Ending("personaNuevaPendienteDeMail")["data"]!.AsObject();
            var real = new RegistrationFlowResult("cualquiera");

            Assert.Equal(declarado["mailSent"]!.GetValue<bool>(), real.MailSent);
            Assert.Equal(declarado["pendingReview"]!.GetValue<bool>(), real.PendingReview);
        }

        [Fact]
        public void RegistroContract_LaSolicitudDeAltaEsPendingReviewSinMail()
        {
            var declarado = Ending("solicitudPendienteDeRevision")["data"]!.AsObject();

            Assert.True(declarado["pendingReview"]!.GetValue<bool>());
            Assert.False(declarado["mailSent"]!.GetValue<bool>());
            // Los valores que promete el contrato salen de ConfirmRegistrationRequest: los verifica
            // RegistrationServiceTests.ConfirmarSolicitudAlta_DoesNotRequireProductOrProcess.
        }

        [Theory]
        [InlineData("evaluarDocumento", nameof(RegistrationController.EvaluateDocument))]
        [InlineData("verificarIdentidad", nameof(RegistrationController.VerifyIdentity))]
        [InlineData("confirmarNuevaPersona", nameof(RegistrationController.ConfirmNewPerson))]
        [InlineData("confirmarSolicitudAlta", nameof(RegistrationController.ConfirmRegistrationRequest))]
        public void RegistroContract_RutasYVerbosCoincidenConElController(string endpointKey, string action)
        {
            var endpoint = Endpoint(endpointKey);
            var method = typeof(RegistrationController).GetMethod(action)!;
            var verb = method.GetCustomAttributes<HttpMethodAttribute>().Single();
            var prefix = typeof(RegistrationController).GetCustomAttribute<RouteAttribute>()!.Template;

            Assert.Equal(endpoint["method"]!.GetValue<string>(), verb.HttpMethods.Single());
            Assert.Equal(endpoint["route"]!.GetValue<string>(), $"{prefix}/{verb.Template}");
        }

        /// <summary>
        /// El 'dataDto' del contrato tiene que ser el T que el controller declara en su
        /// ProducesResponseType de 200: si cambia el tipo de respuesta, el contrato queda en evidencia.
        /// </summary>
        [Theory]
        [InlineData("evaluarDocumento", nameof(RegistrationController.EvaluateDocument))]
        [InlineData("verificarIdentidad", nameof(RegistrationController.VerifyIdentity))]
        [InlineData("confirmarNuevaPersona", nameof(RegistrationController.ConfirmNewPerson))]
        [InlineData("confirmarSolicitudAlta", nameof(RegistrationController.ConfirmRegistrationRequest))]
        public void RegistroContract_ElDataDtoCoincideConElProducesResponseType(string endpointKey, string action)
        {
            var declarado = Endpoint(endpointKey)["dataDto"]!.GetValue<string>();
            var real = typeof(RegistrationController).GetMethod(action)!
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Single(a => a.StatusCode == 200)
                .Type
                .GetGenericArguments()
                .Single();

            // Nullable-annotated (RegistrationConfirmationResponse?) llega como el tipo desnudo.
            Assert.Equal(declarado, (Nullable.GetUnderlyingType(real) ?? real).Name);
        }

        [Fact]
        public void RegistroContract_LosCodigosDeclaradosEnLosEndpointsExistenEnErrors()
        {
            var declarados = Contract["errors"]!.AsObject().Select(e => e.Key).ToHashSet();

            var usados = Contract["endpoints"]!.AsObject()
                .SelectMany(e => e.Value!["mainErrors"]?.AsArray() ?? [])
                .Select(c => c!.GetValue<string>());

            Assert.All(usados, code => Assert.Contains(code, declarados));
        }

        /// <summary>Cada error declarado tiene que apuntar a endpoints que existan en el contrato.</summary>
        [Fact]
        public void RegistroContract_LosEndpointsDeCadaErrorExisten()
        {
            var endpoints = Contract["endpoints"]!.AsObject().Select(e => e.Key).ToHashSet();

            var referenciados = Contract["errors"]!.AsObject()
                .SelectMany(e => e.Value!["endpoints"]!.AsArray())
                .Select(e => e!.GetValue<string>());

            Assert.All(referenciados, name => Assert.Contains(name, endpoints));
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
                    "registro.contract.json");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new FileNotFoundException("No se encontro registro.contract.json.");
        }
    }
}
