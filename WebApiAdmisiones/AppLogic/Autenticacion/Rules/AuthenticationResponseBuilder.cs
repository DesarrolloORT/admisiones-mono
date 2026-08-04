using AppLogic.Autenticacion.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Autenticacion.Rules;

public static class AuthenticationResponseBuilder
{
    public static DtoPersonaAuth BuildPersonaAuth(Persona persona)
    {
        return new DtoPersonaAuth
        {
            CodigoPersona = persona.CodigoPersona,
            PrimerNombre = persona.PrimerNombre,
            SegundoNombre = persona.SegundoNombre,
            PrimerApellido = persona.PrimerApellido,
            SegundoApellido = persona.SegundoApellido,
            TipoPersona = persona.TipoPersona,
            Documento = persona.Documento,
            Email = persona.Email
        };
    }

    public static DtoAuthenticationResponse Build(
        DtoPersonaAuth persona,
        string accessToken,
        string refreshToken,
        string refreshTokenHash,
        string? message = null)
    {
        var response = new DtoAuthenticationResponse
        {
            Persona = persona,
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
