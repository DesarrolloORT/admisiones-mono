namespace AppLogic.DTOs
{
    public class ConfirmarPreInscripcionRequest
    {
        public bool AceptoReglamento { get; set; }
        public long IdOfertaSeleccionada { get; set; }
        public long IdTurno { get; set; }
        public string? TipoInscripcion { get; set; }
    }

    public class ConfirmarPreInscripcionResponse
    {
        public bool Confirmada { get; set; }
        public long? IdInscripcion { get; set; }
        public decimal SeniaInscripcion { get; set; }
        public DateTime? FechaVencimientoPago { get; set; }
        public ResumenInscripcionDto Resumen { get; set; } = new();
    }

    public class ResumenInscripcionDto
    {
        public long IdProducto { get; set; }
        public string? Carrera { get; set; }
        public long IdComienzo { get; set; }
        public string? Comienzo { get; set; }
        public long IdTurno { get; set; }
        public string? Turno { get; set; }
    }
}
