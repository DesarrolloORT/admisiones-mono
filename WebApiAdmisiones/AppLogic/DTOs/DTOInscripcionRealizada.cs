namespace AppLogic.DTOs
{
    /// <summary>
    /// Inscripción realizada por la persona, proyección para el historial de admisiones.
    /// </summary>
    public class DTOInscripcionRealizada
    {
        public DateTime FechaInscripcion { get; set; }
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public string? NombreComienzo { get; set; }
        public string? NombreTurno { get; set; }
    }
}
