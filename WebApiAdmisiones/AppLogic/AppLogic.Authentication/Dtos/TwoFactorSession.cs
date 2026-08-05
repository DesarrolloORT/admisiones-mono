namespace AppLogic.Authentication.Dtos;

/// <summary>
/// Modelo de sesión 2FA almacenada en Redis durante el flujo de verificación.
/// Guarda solo la identidad ya verificada contra LDAP y el hash del código de verificación.
/// </summary>
public sealed class TwoFactorSession
{
    /// <summary>Persona ya autenticada contra LDAP, a la espera del segundo factor.</summary>
    public long PersonId { get; set; }

    /// <summary>Primer nombre.</summary>
    public string? FirstName { get; set; }

    /// <summary>Segundo nombre.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Primer apellido.</summary>
    public string? FirstSurname { get; set; }

    /// <summary>Segundo apellido.</summary>
    public string? SecondSurname { get; set; }

    /// <summary>Tipo de persona en SGI.</summary>
    public string? PersonType { get; set; }

    /// <summary>Número de documento con el que se autenticó.</summary>
    public string? DocumentNumber { get; set; }

    // Datos de verificación

    /// <summary>Hash del código enviado por mail: nunca se guarda el código en claro.</summary>
    public string? CodeHash { get; set; }

    /// <summary>Vencimiento del código, en UTC.</summary>
    public DateTime CodeExpiresAtUtc { get; set; }

    /// <summary>Casilla a la que se envió el código.</summary>
    public string? Email { get; set; }

    /// <summary>Intentos fallidos acumulados: al llegar al máximo se invalida la sesión.</summary>
    public int Attempts { get; set; }
}
