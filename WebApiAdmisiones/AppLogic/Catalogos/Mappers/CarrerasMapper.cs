using AppLogic.Catalogos.Dtos;
using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Catalogos.Mappers
{
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
}
