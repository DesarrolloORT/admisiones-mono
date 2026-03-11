namespace AppLogic.DTOs
{
    /// <summary>
    /// Datos de persona devueltos tras una autenticación exitosa.
    /// </summary>
    public class DTOPersonaAuth
    {
        public long CodigoPersona { get; set; }
        public string? PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? TipoPersona { get; set; }
        public string? Documento { get; set; }
    }
}
