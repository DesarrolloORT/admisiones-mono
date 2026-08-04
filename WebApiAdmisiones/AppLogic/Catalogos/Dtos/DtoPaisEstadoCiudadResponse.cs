using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Catalogos.Dtos;

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
