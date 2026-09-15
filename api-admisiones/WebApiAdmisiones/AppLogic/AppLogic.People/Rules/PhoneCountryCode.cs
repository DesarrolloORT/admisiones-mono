using BusinessLogic.IDevartRepositories;

namespace AppLogic.People.Rules;

/// <summary>
/// Resuelve la fila de T_CARACTERISTICA_PAIS que corresponde al país del teléfono.
/// FDP guarda ID_CARACTERISTICA_PAIS_TELx junto al E.164 y la usa para desarmar el número al
/// mostrarlo (<c>PhoneVerification.SacarSimboloYcaracteristica</c>): si queda en el default 1 y ese
/// no es el país del número, FDP lo muestra mal.
/// </summary>
public static class PhoneCountryCode
{
    /// <summary>ISO2 centinela para "sin país informado", igual que <c>GetByISO2</c> en FDP.</summary>
    private const string UnknownIso2 = "--";

    /// <summary>
    /// Id de la característica para el ISO2 indicado, o <c>null</c> si no existe la fila. El
    /// llamador corta con 400 en ese caso, igual que FDP (<c>DP_PC_00</c>).
    /// </summary>
    public static long? ResolveId(IUnitOfWork uow, string? iso2)
    {
        var buscado = string.IsNullOrWhiteSpace(iso2) ? UnknownIso2 : iso2.Trim();

        // ponytail: GetAll() sobre T_CARACTERISTICA_PAIS, que tiene una fila por país. Si el volumen
        // llega a molestar, agregar GetByISO2 al repositorio como en FDP (eso sí toca DataAccess).
        return uow.CaracteristicaPais.GetAll()
            .FirstOrDefault(c => string.Equals(c.Iso2, buscado, StringComparison.OrdinalIgnoreCase))
            ?.IdCaracteristicaPais;
    }
}
