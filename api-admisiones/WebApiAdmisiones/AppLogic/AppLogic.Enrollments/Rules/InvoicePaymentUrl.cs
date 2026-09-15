namespace AppLogic.Enrollments.Rules;

/// <summary>
/// La API interna devuelve la URL de pago y los parámetros encriptados en un solo string.
/// El front los necesita separados.
/// </summary>
public static class InvoicePaymentUrl
{
    private const string EncryptedParametersMarker = "parametrosEncriptados=";

    public static (string Url, string? EncryptedParameters) Split(string urlCompleta)
    {
        var indiceMarcador = urlCompleta.IndexOf(EncryptedParametersMarker, StringComparison.Ordinal);
        if (indiceMarcador <= 0)
        {
            return (urlCompleta, null);
        }

        var url = urlCompleta[..(indiceMarcador - 1)];
        var parametrosEncriptados = Uri.UnescapeDataString(urlCompleta[(indiceMarcador + EncryptedParametersMarker.Length)..]);
        return (url, parametrosEncriptados);
    }
}
