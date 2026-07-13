using System.Text.Json;
using AppLogic.Autenticacion.Helpers;
using AppLogic.Autenticacion.Responses;
using BusinessLogic.Entities;

namespace UnitTesting.AppLogic.Helpers
{
    public class AuthenticationResponseBuilderTests
    {
        private static Persona CrearPersona()
        {
            return new Persona
            {
                CodigoPersona = 12345,
                PrimerNombre = "Ana",
                SegundoNombre = "Maria",
                PrimerApellido = "Perez",
                SegundoApellido = "Gomez",
                TipoPersona = "CONTACTO",
                Documento = "1234567-2",
                Email = "ana@example.com"
            };
        }

        [Fact]
        public void BuildPersonaAuth_MapsAllFieldsFromPersona()
        {
            var persona = CrearPersona();

            var result = AuthenticationResponseBuilder.BuildPersonaAuth(persona);

            Assert.Equal(persona.CodigoPersona, result.CodigoPersona);
            Assert.Equal(persona.PrimerNombre, result.PrimerNombre);
            Assert.Equal(persona.SegundoNombre, result.SegundoNombre);
            Assert.Equal(persona.PrimerApellido, result.PrimerApellido);
            Assert.Equal(persona.SegundoApellido, result.SegundoApellido);
            Assert.Equal(persona.TipoPersona, result.TipoPersona);
            Assert.Equal(persona.Documento, result.Documento);
            Assert.Equal(persona.Email, result.Email);
        }

        [Fact]
        public void Build_WithoutMessage_UsesDefaultMessage()
        {
            var personaAuth = AuthenticationResponseBuilder.BuildPersonaAuth(CrearPersona());

            var result = AuthenticationResponseBuilder.Build(
                personaAuth,
                "access-token",
                "refresh-token",
                "refresh-token-hash");

            Assert.Equal(
                "Autenticación exitosa. Los tokens han sido establecidos como cookies seguras.",
                result.Message);
        }

        [Fact]
        public void Build_WithMessage_UsesProvidedMessage()
        {
            var personaAuth = AuthenticationResponseBuilder.BuildPersonaAuth(CrearPersona());

            var result = AuthenticationResponseBuilder.Build(
                personaAuth,
                "access-token",
                "refresh-token",
                "refresh-token-hash",
                "Tokens renovados correctamente.");

            Assert.Equal("Tokens renovados correctamente.", result.Message);
        }

        [Fact]
        public void Build_AssignsPersonaAndTokensExactly()
        {
            var personaAuth = AuthenticationResponseBuilder.BuildPersonaAuth(CrearPersona());

            var result = AuthenticationResponseBuilder.Build(
                personaAuth,
                "access-token",
                "refresh-token",
                "refresh-token-hash");

            Assert.Same(personaAuth, result.Persona);
            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("refresh-token", result.RefreshToken);
            Assert.Equal("refresh-token-hash", result.RefreshTokenHash);
        }

        [Fact]
        public void Build_SerializedJson_DoesNotIncludeIgnoredTokenFields()
        {
            var personaAuth = AuthenticationResponseBuilder.BuildPersonaAuth(CrearPersona());
            var result = AuthenticationResponseBuilder.Build(
                personaAuth,
                "access-token",
                "refresh-token",
                "refresh-token-hash");

            var json = JsonSerializer.Serialize(result);

            Assert.DoesNotContain("access-token", json, StringComparison.Ordinal);
            Assert.DoesNotContain("refresh-token", json, StringComparison.Ordinal);
            Assert.DoesNotContain("ana@example.com", json, StringComparison.Ordinal);
            Assert.Contains("\"codigoPersona\"", json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
