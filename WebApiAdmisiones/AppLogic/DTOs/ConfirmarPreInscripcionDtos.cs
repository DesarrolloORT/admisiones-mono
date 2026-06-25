using System.Collections.Generic;

namespace AppLogic.DTOs
{
    public class ConfirmarPreInscripcionRequest
    {
        public bool AceptoReglamento { get; set; }
        public long IdOfertaSeleccionada { get; set; }
    }

    public class ConfirmarPreInscripcionResponse
    {
        public bool Confirmada { get; set; }
        public long? IdInscripcion { get; set; }
        public decimal SeniaInscripcion { get; set; }
        public DateTime? FechaVencimientoPago { get; set; }
        public List<CarritoSeniaDto> CarritosSenia { get; set; } = new();
        public ResumenInscripcionDto Resumen { get; set; } = new();
        public EstadoCuentaDto? EstadoCuenta { get; set; }
    }

    public class CarritoSeniaDto
    {
        public string IdCarrito { get; set; } = string.Empty;
        public decimal Senia { get; set; }
    }

    public class ResumenInscripcionDto
    {
        public long IdOferta { get; set; }
        public long IdProducto { get; set; }
        public string? Carrera { get; set; }
        public long IdComienzo { get; set; }
        public string? Comienzo { get; set; }
        public long IdTurno { get; set; }
        public string? Turno { get; set; }
    }

    public class EstadoCuentaDto
    {
        public decimal SaldoActual { get; set; }
    }
}
