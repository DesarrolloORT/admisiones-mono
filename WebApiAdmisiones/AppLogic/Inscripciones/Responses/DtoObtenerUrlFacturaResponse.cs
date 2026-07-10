namespace AppLogic.Inscripciones.Responses
{
    public class DtoObtenerUrlFacturaResponse
    {
        public string Url { get; set; } = string.Empty;
        public string? ParametrosEncriptados { get; set; }
    }
}
