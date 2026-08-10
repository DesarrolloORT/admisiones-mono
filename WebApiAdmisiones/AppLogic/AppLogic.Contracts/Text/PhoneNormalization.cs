using Utilities;

namespace AppLogic.Contracts.Text;

/// <summary>
/// Normaliza teléfonos a E.164 (+59899333222) con el validador de Core, igual que FDP
/// (DatosPersonalesService.ProcesarContacto), para que las dos APIs escriban el mismo formato en
/// CREADOR.T_PERSONA. El ISO2 lo informa el cliente: no hay país por defecto, así que un número
/// local sin ISO2 no valida.
/// </summary>
public static class PhoneNormalization
{
    /// <summary>
    /// Valida y normaliza un teléfono. Devuelve <c>null</c> si no vino teléfono; si vino, hay que
    /// chequear <see cref="PhoneVerification.TelefonoValido"/> antes de usar
    /// <see cref="PhoneVerification.TelefonoE164"/>.
    /// </summary>
    /// <param name="phone">Número nacional, o en internacional si ya trae '+'.</param>
    /// <param name="isPrimaryPhone">
    /// true = teléfono principal, tiene que ser móvil. false = acepta fijo o móvil.
    /// </param>
    /// <param name="iso2">País del número. Sin él solo validan los números que ya traen '+'.</param>
    public static PhoneVerification? Validate(string? phone, bool isPrimaryPhone, string? iso2)
    {
        var raw = TextNormalization.Trim(phone);
        if (string.IsNullOrWhiteSpace(raw))
        {
            // El validador de Core tira NullReferenceException con null, y "vacío" no es lo mismo
            // que "inválido": el llamador decide si el teléfono era obligatorio.
            return null;
        }

        return PhoneVerification.Validar(raw, TextNormalization.TrimOrNull(iso2), isPrimaryPhone);
    }
}
