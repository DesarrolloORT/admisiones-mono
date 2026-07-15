namespace AppLogic.Inscripciones.Dtos
{
    public class DtoConfirmarPreInscripcionResponse
    {
        public bool Confirmada { get; set; }

        /// <summary>True cuando la preinscripción quedó pero la inscripción fue a bandeja ("A la espera"). El front muestra "Inscripción en proceso".</summary>
        public bool EnEspera { get; set; }

        public long? IdInscripcion { get; set; }
        public DateTime? FechaVencimientoPago { get; set; }
        public decimal Senia { get; set; }
        public DtoResumenInscripcion Resumen { get; set; } = new();
        public DtoEstadoCuenta? EstadoCuenta { get; set; }
    }

    public class DtoResumenInscripcion
    {
        public long IdOferta { get; set; }
        public long IdProducto { get; set; }
        public string? Carrera { get; set; }
        public long IdComienzo { get; set; }
        public string? Comienzo { get; set; }
        public long IdTurno { get; set; }
        public string? Turno { get; set; }
    }

    public class DtoEstadoCuenta
    {
        public decimal SaldoActual { get; set; }
    }
}
