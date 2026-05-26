using System;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public class DtoDatosPersona
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string PrimerNombre { get; set; } = string.Empty;
        public string SegundoNombre { get; set; } = string.Empty;
        public string PrimerApellido { get; set; } = string.Empty;
        public string SegundoApellido { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public long CodigoPais { get; set; }
        public long CodigoEstado { get; set; }
        public long CodigoCiudad { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string Telefono1 { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string VerificacionMail { get; set; } = string.Empty;
        public bool IdentidadRestringida { get; set; }
    }
}
