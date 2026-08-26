using AppLogic.Catalogs.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Catalogs.Mapping;

public static class DegreeProgramMapper
{
    // El listado entero se ordena por nombre
    private static readonly StringComparer NombreComparer = StringComparer.InvariantCultureIgnoreCase;

    /// <summary>
    /// Fila plana de carrera, común a las dos vistas de origen (niveles 1-2 y 3-4), sobre la que se
    /// arma el agrupado por nivel y escuela.
    /// </summary>
    public sealed record DegreeProgramRow(
        long ProductId,
        string? ProductName,
        long ProductLevelId,
        string? ProductLevelName,
        long SchoolId,
        string? SchoolName,
        long? AdmissionProcessId,
        bool? HasSeminar);

    public static DegreeProgramRow ToRow(this VdProductosDisponibles1y2 product) => new(
        product.IdProducto,
        product.NombreWebProducto,
        product.IdNivelProducto,
        product.NombreNivelProducto,
        product.IdEscuela,
        product.NombreExtensoEscuela,
        null,
        null);

    public static DegreeProgramRow ToRow(this VdOfertasDisponibles3y4 offering) => new(
        offering.IdProducto!.Value,
        offering.NombreWebProducto,
        offering.IdNivelProducto,
        offering.NombreNivelProducto,
        offering.IdEscuela,
        offering.NombreExtensoEscuela,
        (long)offering.IdProceso,
        offering.ConSeminarios == "SI");

    /// <summary>
    /// Agrupa las carreras por nivel de producto y escuela. Actualización profesional agrega un
    /// nivel más de agrupación por "con seminarios" / "sin seminarios".
    /// </summary>
    public static List<DegreeProgramsByLevelResponse> ToGroupedResponse(
        IEnumerable<DegreeProgramRow> rows,
        bool groupBySeminar) => rows
            .GroupBy(x => new { x.ProductLevelId, x.ProductLevelName })
            .OrderBy(g => g.Key.ProductLevelName, NombreComparer)
            .Select(nivel => new DegreeProgramsByLevelResponse
            {
                ProductLevelId = nivel.Key.ProductLevelId,
                ProductLevelName = nivel.Key.ProductLevelName,
                Schools = nivel
                    .GroupBy(x => new { x.SchoolId, x.SchoolName })
                    .OrderBy(g => g.Key.SchoolName, NombreComparer)
                    .Select(escuela => groupBySeminar
                        ? new DegreeProgramsBySchoolResponse
                        {
                            SchoolId = escuela.Key.SchoolId,
                            SchoolName = escuela.Key.SchoolName,
                            Seminars = escuela
                                // HasSeminar siempre tiene valor para nivel 3/4 (ver ToRow(VdOfertasDisponibles3y4)).
                                .GroupBy(x => x.HasSeminar!.Value)
                                .OrderBy(g => g.Key)
                                .Select(seminario => new DegreeProgramsBySeminarResponse
                                {
                                    HasSeminar = seminario.Key,
                                    Products = ToProducts(seminario)
                                })
                                .ToList()
                        }
                        : new DegreeProgramsBySchoolResponse
                        {
                            SchoolId = escuela.Key.SchoolId,
                            SchoolName = escuela.Key.SchoolName,
                            Products = ToProducts(escuela)
                        })
                    .ToList()
            })
            .ToList();

    private static List<DegreeProgramResponse> ToProducts(IEnumerable<DegreeProgramRow> rows) => rows
        .GroupBy(x => x.ProductId)
        .Select(g => g.First())
        .OrderBy(x => x.ProductName, NombreComparer)
        .Select(x => new DegreeProgramResponse
        {
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            AdmissionProcessId = x.AdmissionProcessId
        })
        .ToList();
}
