using System;
using System.Text.Json.Serialization;

namespace WebApiAdmisiones.Models
{
    public class ArchivoPayload
    {
        public string? NombreArchivo { get; set; }
        public byte[]? Archivo { get; set; }
    }

    public class SubirFotoAlumnoRequest
    {
        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    public class UploadDocumentoAlumnoRequest
    {
        [JsonRequired]
        public int Tipo { get; set; }

        [JsonRequired]
        public DateTime Fecha { get; set; }

        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    public class ReconocimientoDocumentoApiRequest
    {
        public string? TipoMime { get; set; }
        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    public class UploadArchivoIngresoRequest
    {
        [JsonRequired]
        public long IdIngresoMensualNF { get; set; }

        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    public class UploadArchivoEgresoRequest
    {
        [JsonRequired]
        public long IdEgresoMensualNF { get; set; }

        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    public class UploadArchivoRevalidaDjRequest
    {
        [JsonRequired]
        public long IdDeclaracionJuradaWeb { get; set; }

        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }
}