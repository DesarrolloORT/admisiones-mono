using AppLogic.Catalogs.Dtos;
using BusinessLogic.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Catalogs.Mapping;

[ExcludeFromCodeCoverage]
public static class LocationMapper
{
    public static CountryStateCityResponse ToResponse(this Pais country)
    {
        return new CountryStateCityResponse
        {
            CountryId = country.CodigoPais,
            Name = country.Nombre,
            States = country.Estado?
                .Select(estado => estado.ToResponse())
                .ToList() ?? []
        };
    }

    private static StateWithCitiesResponse ToResponse(this Estado estado)
    {
        return new StateWithCitiesResponse
        {
            CountryId = estado.CodigoPais,
            StateId = estado.CodigoEstado,
            Name = estado.Nombre,
            Cities = estado.Ciudad?
                .Select(city => city.ToResponse())
                .ToList() ?? []
        };
    }

    private static CityResponse ToResponse(this Ciudad city)
    {
        return new CityResponse
        {
            CountryId = city.CodigoPais,
            StateId = city.CodigoEstado,
            CityId = city.CodigoCiudad,
            Name = city.Nombre
        };
    }
}
