using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public class DtoCarreraResponse
    {
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public long IdNivelProducto { get; set; }
        public string? NombreNivelProducto { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? IdEscuela { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NombreEscuela { get; set; }
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

        public static DtoCarreraResponse ToAdmisionesDto(
            this VdOfertasDisponibles3y4 oferta, Producto? producto)
        {
            return new DtoCarreraResponse
            {
                IdProducto = oferta.IdProducto!.Value,
                NombreProducto = oferta.NombreWebProducto,
                IdNivelProducto = producto?.IdNivelProducto ?? 0,
                NombreNivelProducto = producto?.NivelProducto?.NombreNivelProducto,
                IdEscuela = oferta.IdEscuela,
                NombreEscuela = oferta.NombreExtensoEscuela
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
