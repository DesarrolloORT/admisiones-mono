using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    /// <summary>
    /// Datos de la última inscripción activa del alumno, equivalente al
    /// DTOInscripcionAdmisiones de la API anterior.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoUltimaInscripcion
    {
        public long IdInscripto { get; set; }
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public string? NombreExtensoProducto { get; set; }
        public long IdComienzo { get; set; }
        public string? NombreComienzo { get; set; }
    }
}
