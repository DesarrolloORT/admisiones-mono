namespace AppLogic.Becas.Responses
{
    public class DtoBecaPersona
    {
        public long IdBeca { get; set; }
        public long? IdPostulacion { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public string? Estado { get; set; }
        public DateTime? FechaCierrePostulacion { get; set; }
        public DateTime? FechaPrueba { get; set; }
        public DateTime? FechaResultados { get; set; }
        public string AccionPrincipal { get; set; } = string.Empty;
        public bool PuedeContinuarPostulacion { get; set; }
        public bool PuedeDescargarMaterialEstudio { get; set; }
        public string? UrlMaterialEstudio { get; set; }
    }
}
