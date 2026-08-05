using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Catalogs.Dtos;

/// <summary>País con sus estados/provincias y ciudades, para los combos de domicilio.</summary>
[ExcludeFromCodeCoverage]
public class CountryStateCityResponse
{
    /// <summary>Código de país.</summary>
    public long CountryId { get; set; }

    /// <summary>Nombre del país.</summary>
    public string? Name { get; set; }

    /// <summary>Estados o provincias del país.</summary>
    public List<StateWithCitiesResponse> States { get; set; } = [];
}

/// <summary>Estado o provincia con sus ciudades.</summary>
[ExcludeFromCodeCoverage]
public class StateWithCitiesResponse
{
    /// <summary>Código del país al que pertenece.</summary>
    public long CountryId { get; set; }

    /// <summary>Código del estado o provincia.</summary>
    public long StateId { get; set; }

    /// <summary>Nombre del estado o provincia.</summary>
    public string? Name { get; set; }

    /// <summary>Ciudades del estado o provincia.</summary>
    public List<CityResponse> Cities { get; set; } = [];
}

/// <summary>Ciudad dentro de un estado o provincia.</summary>
[ExcludeFromCodeCoverage]
public class CityResponse
{
    /// <summary>Código del país al que pertenece.</summary>
    public long CountryId { get; set; }

    /// <summary>Código del estado o provincia al que pertenece.</summary>
    public long StateId { get; set; }

    /// <summary>Código de la ciudad.</summary>
    public long CityId { get; set; }

    /// <summary>Nombre de la ciudad.</summary>
    public string? Name { get; set; }
}
