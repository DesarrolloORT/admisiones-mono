namespace AppLogic.Contracts.Constants;

/// <summary>
/// Valores literales del esquema Oracle compartidos por más de un módulo.
/// Lo que use un solo módulo va en las constantes de ese módulo, no acá.
/// </summary>
public static class SchemaConstants
{
    public const int AdmissionsSystemId = 25;

    /// <summary>Formato de HORA_ULTIMA_ACTUALIZACION en las tablas legacy SGI.</summary>
    public const string LegacyTimeFormat = "HH:mm:ss";

    public static class BooleanFlag
    {
        public const string Yes = "SI";
        public const string No = "NO";
    }

    /// <summary>Valores de EstadoSupraoferta (Oferta.Supraoferta).</summary>
    public static class ParentOfferingStatus
    {
        public const string Final = "D";
    }
}
