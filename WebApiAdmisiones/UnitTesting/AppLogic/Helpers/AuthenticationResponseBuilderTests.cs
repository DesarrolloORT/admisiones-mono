using System.Text.Json;
using AppLogic.Authentication.Rules;
using AppLogic.Authentication.Dtos;
using BusinessLogic.Entities;

namespace UnitTesting.AppLogic.Helpers
{
    public class AuthenticationResponseBuilderTests
    {
        private static Persona CreatePerson()
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
            var person = CreatePerson();

            var result = AuthenticationResponseBuilder.BuildAuthenticatedPerson(person);

            Assert.Equal(person.CodigoPersona, result.PersonId);
            Assert.Equal(person.PrimerNombre, result.FirstName);
            Assert.Equal(person.SegundoNombre, result.MiddleName);
            Assert.Equal(person.PrimerApellido, result.FirstSurname);
            Assert.Equal(person.SegundoApellido, result.SecondSurname);
            Assert.Equal(person.TipoPersona, result.PersonType);
            Assert.Equal(person.Documento, result.DocumentNumber);
            Assert.Equal(person.Email, result.Email);
        }

        [Fact]
        public void Build_WithoutMessage_UsesDefaultMessage()
        {
            var personaAuth = AuthenticationResponseBuilder.BuildAuthenticatedPerson(CreatePerson());

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
            var personaAuth = AuthenticationResponseBuilder.BuildAuthenticatedPerson(CreatePerson());

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
            var personaAuth = AuthenticationResponseBuilder.BuildAuthenticatedPerson(CreatePerson());

            var result = AuthenticationResponseBuilder.Build(
                personaAuth,
                "access-token",
                "refresh-token",
                "refresh-token-hash");

            Assert.Same(personaAuth, result.Person);
            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("refresh-token", result.RefreshToken);
            Assert.Equal("refresh-token-hash", result.RefreshTokenHash);
        }

        [Fact]
        public void Build_SerializedJson_DoesNotIncludeIgnoredTokenFields()
        {
            var personaAuth = AuthenticationResponseBuilder.BuildAuthenticatedPerson(CreatePerson());
            var result = AuthenticationResponseBuilder.Build(
                personaAuth,
                "access-token",
                "refresh-token",
                "refresh-token-hash");

            var json = JsonSerializer.Serialize(result);

            Assert.DoesNotContain("access-token", json, StringComparison.Ordinal);
            Assert.DoesNotContain("refresh-token", json, StringComparison.Ordinal);
            Assert.DoesNotContain("ana@example.com", json, StringComparison.Ordinal);
            Assert.Contains("\"personId\"", json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
