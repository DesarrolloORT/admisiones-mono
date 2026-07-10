using AppLogic.Catalogos.Responses;
using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Catalogos.Mappers
{
    [ExcludeFromCodeCoverage]
    public static class PaisesEstadosCiudadesMapper
    {
        public static DtoPaisEstadoCiudadResponse ToPaisesEstadosCiudadesDto(this Pais pais)
        {
            return new DtoPaisEstadoCiudadResponse
            {
                CodigoPais = pais.CodigoPais,
                Nombre = pais.Nombre,
                Estado = pais.Estado?
                    .Select(estado => estado.ToPaisesEstadosCiudadesDto())
                    .ToList() ?? []
            };
        }

        private static DtoEstadoCiudadResponse ToPaisesEstadosCiudadesDto(this Estado estado)
        {
            return new DtoEstadoCiudadResponse
            {
                CodigoPais = estado.CodigoPais,
                CodigoEstado = estado.CodigoEstado,
                Nombre = estado.Nombre,
                Ciudad = estado.Ciudad?
                    .Select(ciudad => ciudad.ToPaisesEstadosCiudadesDto())
                    .ToList() ?? []
            };
        }

        private static DtoCiudadResponse ToPaisesEstadosCiudadesDto(this Ciudad ciudad)
        {
            return new DtoCiudadResponse
            {
                CodigoPais = ciudad.CodigoPais,
                CodigoEstado = ciudad.CodigoEstado,
                CodigoCiudad = ciudad.CodigoCiudad,
                Nombre = ciudad.Nombre
            };
        }
    }
}
