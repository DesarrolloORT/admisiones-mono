using System;
using System.Collections.Generic;

namespace AppLogic.ApiClients.Dtos
{
    /// <summary>
    /// Response de confirmar preinscripción.
    /// </summary>
    public class ConfirmarPreInscripcionApiResponse
    {
        public bool Confirmada { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long? IdInscripcion { get; set; }
        public bool InscripcionPendiente { get; set; }
        public DateTime? FechaVencimientoPago { get; set; }
        public List<CarritoSeniaApiDto> Carritos { get; set; } = new();
        public ResumenInscripcionApiDto? Resumen { get; set; }
        public EstadoCuentaApiDto? EstadoCuenta { get; set; }
    }

    public class CarritoSeniaApiDto
    {
        public string IdCarrito { get; set; } = string.Empty;
        public decimal Senia { get; set; }
    }

    public class ResumenInscripcionApiDto
    {
        public long IdOferta { get; set; }
        public long IdProducto { get; set; }
        public string? Carrera { get; set; }
        public long IdComienzo { get; set; }
        public string? Comienzo { get; set; }
        public long IdTurno { get; set; }
        public string? Turno { get; set; }
    }

    public class EstadoCuentaApiDto
    {
        public decimal SaldoActual { get; set; }
    }
}
