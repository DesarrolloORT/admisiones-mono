using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AppLogic.DTOs
{
    /// <summary>
    /// DTO de entrada para actualizar un ProcesoComienzo.
    /// Solo expone los campos escalares editables, evitando
    /// las navigation properties del DTO generado.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ProcesoComienzoRequest
    {
        public long IdProceso { get; set; }
        public long IdComienzo { get; set; }
        public string? HoraIngreso { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public string? UsuarioIngreso { get; set; }
    }
}
