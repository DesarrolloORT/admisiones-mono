using System;

namespace WebApiAdmisiones.Models
{
    public class UploadArchivoRequest
    {
        public string? NombreArchivo { get; set; }
        public byte[]? Archivo { get; set; }
    }

    public class UploadDocumentoAlumnoRequest : UploadArchivoRequest
    {
        public int Tipo { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class UploadArchivoIngresoRequest : UploadArchivoRequest
    {
        public long IdIngresoMensualNF { get; set; }
    }

    public class UploadArchivoEgresoRequest : UploadArchivoRequest
    {
        public long IdEgresoMensualNF { get; set; }
    }

    public class UploadArchivoRevalidaDjRequest : UploadArchivoRequest
    {
        public long IdDeclaracionJuradaWeb { get; set; }
    }
}