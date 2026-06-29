using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Dtos.Catalogos
{
    [ExcludeFromCodeCoverage]
    public class DtoCarrerasPorNivelResponse
    {
        public long IdNivelProducto { get; set; }
        public string? NombreNivelProducto { get; set; }
        public List<DtoCarrerasPorEscuelaResponse> Escuelas { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class DtoCarrerasPorEscuelaResponse
    {
        public long IdEscuela { get; set; }
        public string? NombreEscuela { get; set; }
        public List<DtoCarreraResponse> Productos { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class DtoCarreraResponse
    {
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
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
            this VdProductosDisponibles1y2 producto)
        {
            return new DtoCarreraResponse
            {
                IdProducto = producto.IdProducto,
                NombreProducto = producto.NombreWebProducto
            };
        }

        public static DtoCarreraResponse ToAdmisionesDto(
            this VdOfertasDisponibles3y4 oferta)
        {
            return new DtoCarreraResponse
            {
                IdProducto = oferta.IdProducto!.Value,
                NombreProducto = oferta.NombreWebProducto
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

        public static DtoComienzoResponse ToAdmisionesDto(
            this VdProcesosDisponibles1y2 comienzo)
        {
            return new DtoComienzoResponse
            {
                IdProceso = (long)comienzo.IdProceso,
                NombreProceso = comienzo.NombreProceso
            };
        }
    }
}
