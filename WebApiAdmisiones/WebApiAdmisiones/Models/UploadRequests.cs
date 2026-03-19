using System;

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
        public int Tipo { get; set; }
        public DateTime Fecha { get; set; }
        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    public class UploadArchivoIngresoRequest
    {
        public long IdIngresoMensualNF { get; set; }
        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    public class UploadArchivoEgresoRequest
    {
        public long IdEgresoMensualNF { get; set; }
        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }

    public class UploadArchivoRevalidaDjRequest
    {
        public long IdDeclaracionJuradaWeb { get; set; }
        public ArchivoPayload ArchivoAdjunto { get; set; } = new();
    }
}