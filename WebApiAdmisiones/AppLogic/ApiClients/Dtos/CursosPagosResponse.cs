using System;
using System.Collections.Generic;

namespace AppLogic.ApiClients.Dtos
{
    /// <summary>
    /// Response de cursos a pagar.
    /// Corresponde a: GET /api/Pagos
    /// </summary>
    public class CursosPagosResponse
    {
        public List<CursoPago> Cursos { get; set; } = new();
        public decimal MontoTotal { get; set; }
    }

    /// <summary>
    /// DTO de curso para pago.
    /// </summary>
    public class CursoPago
    {
        public long IdCurso { get; set; }
        public string? NombreCurso { get; set; }
        public decimal Monto { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }
}
