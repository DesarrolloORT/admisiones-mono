using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


namespace WebApiAdmisiones.Models
{
    [ExcludeFromCodeCoverage]
    public class ArchivoPayload
    {
        public string? NombreArchivo { get; set; }
        public byte[]? Archivo { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SubirFotoPersonaRequest
    {
        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class UploadDocumentoPersonaRequest
    {
        [JsonRequired]
        public DateTime Fecha { get; set; }

        public ArchivoPayload Frente { get; set; } = new();

        public ArchivoPayload Dorso { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class ReconocimientoDocumentoApiRequest
    {
        public string? TipoMime { get; set; }
        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class UploadArchivoIngresoRequest
    {
        [JsonRequired]
        public long IdIngresoMensualNF { get; set; }

        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class UploadArchivoEgresoRequest
    {
        [JsonRequired]
        public long IdEgresoMensualNF { get; set; }

        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class UploadArchivoRevalidaDjRequest
    {
        [JsonRequired]
        public long IdDeclaracionJuradaWeb { get; set; }

        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }
}
