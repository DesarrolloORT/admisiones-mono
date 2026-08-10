namespace AppLogic.Integrations.Tivenos.Dtos;

public sealed class TivenosAltaInteresOperacion
{
    public string TipoProcesoLlamador { get; init; } = string.Empty;
    public string Disparador { get; init; } = string.Empty;
    public string? OrigenLlamador { get; init; }

    public static TivenosAltaInteresOperacion CreateProductInterest() => new()
    {
        TipoProcesoLlamador = "Alta",
        Disparador = "CreateProductInterest",
    };

    public static TivenosAltaInteresOperacion UpdateInterest() => new()
    {
        TipoProcesoLlamador = "Modificar",
        Disparador = "ActualizarInteres",
    };

    public static TivenosAltaInteresOperacion CreateOrUpdateInterest() => new()
    {
        OrigenLlamador = "SIS",
        TipoProcesoLlamador = "Alta",
        Disparador = "ActualizarInteres",
    };

    public static TivenosAltaInteresOperacion SiteRegistration() => new()
    {
        TipoProcesoLlamador = "Alta",
        Disparador = "Registro",
    };
}
