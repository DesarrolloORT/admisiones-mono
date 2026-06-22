using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public class DtoCarreraResponse
    {
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public long IdNivelProducto { get; set; }
        public string? NombreNivelProducto { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class DtoComienzoResponse
    {
        public long IdProceso { get; set; }
        public string? NombreProceso { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public static class CarrerasMapper
    {
        public static DtoCarreraResponse ToAdmisionesDto(
            this Producto carrera)
        {
            return new DtoCarreraResponse
            {
                IdProducto = carrera.IdProducto,
                NombreProducto = carrera.NombreWebProducto,
                IdNivelProducto = carrera.IdNivelProducto,
                NombreNivelProducto = carrera.NivelProducto?.NombreNivelProducto
            };
        }
    }

    [ExcludeFromCodeCoverage]
    public static class ComienzosMapper
    {
        public static DtoComienzoResponse ToAdmisionesDto(
            this Proceso comienzo)
        {
            return new DtoComienzoResponse
            {
                IdProceso = comienzo.IdProceso,
                NombreProceso = comienzo.NombreProceso
            };
        }
    }
}
