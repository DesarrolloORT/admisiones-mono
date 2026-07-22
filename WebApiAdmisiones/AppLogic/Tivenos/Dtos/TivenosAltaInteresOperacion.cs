namespace AppLogic.Tivenos.Dtos;

public sealed class TivenosAltaInteresOperacion
{
    public string TipoProcesoLlamador { get; init; } = string.Empty;
    public string Disparador { get; init; } = string.Empty;
    public string? OrigenLlamador { get; init; }

    public static TivenosAltaInteresOperacion AltaInteresProducto() => new()
    {
        TipoProcesoLlamador = "Alta",
        Disparador = "AltaInteresProducto",
    };

    public static TivenosAltaInteresOperacion ModificarActualizarInteres() => new()
    {
        TipoProcesoLlamador = "Modificar",
        Disparador = "ActualizarInteres",
    };

    public static TivenosAltaInteresOperacion AltaActualizarInteres() => new()
    {
        OrigenLlamador = "SIS",
        TipoProcesoLlamador = "Alta",
        Disparador = "ActualizarInteres",
    };
}
