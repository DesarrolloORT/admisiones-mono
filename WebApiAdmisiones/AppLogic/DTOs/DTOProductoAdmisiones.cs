using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    /// <summary>
    /// Datos de producto devueltos en listas de interés/vigentes para admisiones.
    /// Proyección reducida de T_PRODUCTO con los campos relevantes para el front.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoProductoAdmisiones
    {
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public string? NombreExtensoProducto { get; set; }
        public long IdNivelProducto { get; set; }
        public string? NombreNivelProducto { get; set; }
        public string? AliasProducto { get; set; }
        public string? InscribibleProducto { get; set; }
        public string? IntermedioProducto { get; set; }
        public string? VisibleAdmisionesProducto { get; set; }
        public long IdProceso { get; set; }
        public string? NombreProceso { get; set; }
    }
}
