using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Registration.Dtos;

/// <summary>
/// Confirma que el registro se completó. <see cref="MailSent"/> distingue éxito parcial
/// (registro OK, mail de activación no enviado) de éxito completo (SRV-05).
/// </summary>
[ExcludeFromCodeCoverage]
public class RegistrationConfirmationResponse
{
    /// <summary>
    /// <c>false</c> = éxito parcial: el registro quedó hecho pero no se pudo enviar el mail de
    /// activación, así que el front debe ofrecer el recupero de contraseña.
    /// </summary>
    public bool MailSent { get; set; } = true;
}
