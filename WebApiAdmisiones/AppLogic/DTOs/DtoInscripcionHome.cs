using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    /// <summary>
    /// Inscripcion resumida para mostrar en el home de admisiones.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoInscripcionHome
    {
        public string Estado { get; set; } = string.Empty;
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public long IdComienzo { get; set; }
        public string? NombreComienzo { get; set; }
        public long IdTurno { get; set; }
        public string? NombreTurno { get; set; }
    }
}
