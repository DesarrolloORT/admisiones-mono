namespace AppLogic.Inscripciones.Dtos
{
    public class DtoGuardarMetodoPagoRequest
    {
        public long IdInscripcion { get; set; }
        public string TipoPago { get; set; } = string.Empty;
    }
}
