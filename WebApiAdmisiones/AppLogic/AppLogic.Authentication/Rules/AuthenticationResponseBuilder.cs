using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Authentication.Rules;

public static class AuthenticationResponseBuilder
{
    public static AuthenticatedPerson BuildAuthenticatedPerson(Persona person)
    {
        return new AuthenticatedPerson
        {
            PersonId = person.CodigoPersona,
            FirstName = person.PrimerNombre,
            MiddleName = person.SegundoNombre,
            FirstSurname = person.PrimerApellido,
            SecondSurname = person.SegundoApellido,
            PersonType = person.TipoPersona,
            DocumentNumber = person.Documento,
            Email = person.Email
        };
    }

    public static AuthenticationResponse Build(
        AuthenticatedPerson person,
        string accessToken,
        string refreshToken,
        string refreshTokenHash,
        string? message = null)
    {
        var response = new AuthenticationResponse
        {
            Person = person,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenHash = refreshTokenHash
        };

        if (message is not null)
        {
            response.Message = message;
        }

        return response;
    }
}
