using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    /// <summary>
    /// Producto elegible para postulación a beca: combina inscripciones realizadas,
    /// inscripciones pendientes en workflow e intereses activos de la persona.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoProductoBeca
    {
        public DateTime FechaInscripcion { get; set; }
        public long IdProducto { get; set; }
        public long IdNivelProducto { get; set; }
        public string? NombreProducto { get; set; }
        public string? NombreComienzo { get; set; }
        public string? NombreTurno { get; set; }
        public long IdProceso { get; set; }
    }
}
