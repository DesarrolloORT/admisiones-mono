namespace AppLogic.DTOs
{
    public class ArchivoDescargaDto
    {
        public byte[] Archivo { get; set; } = [];
        public string NombreArchivo { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
