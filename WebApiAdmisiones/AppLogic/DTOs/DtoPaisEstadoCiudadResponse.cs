using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public class DtoPaisEstadoCiudadResponse
    {
        public long CodigoPais { get; set; }
        public string? Nombre { get; set; }
        public List<DtoEstadoCiudadResponse> Estado { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class DtoEstadoCiudadResponse
    {
        public long CodigoPais { get; set; }
        public long CodigoEstado { get; set; }
        public string? Nombre { get; set; }
        public List<DtoCiudadResponse> Ciudad { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class DtoCiudadResponse
    {
        public long CodigoPais { get; set; }
        public long CodigoEstado { get; set; }
        public long CodigoCiudad { get; set; }
        public string? Nombre { get; set; }
    }

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
