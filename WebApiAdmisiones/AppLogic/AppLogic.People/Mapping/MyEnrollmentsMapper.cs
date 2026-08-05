using AppLogic.People.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.People.Mapping;

/// <summary>
/// Une las filas de las vistas fresco de nivel 1-2 y 3-4 y las agrupa por producto y proceso,
/// que es la forma que consume la pantalla "Mis carreras".
/// Traducción pura: recibe las filas ya leídas, no consulta la base.
/// </summary>
internal static class MyEnrollmentsMapper
{
    internal static List<MyEnrollmentsResponse> ToGroupedResponse(
        IEnumerable<VdInscripcionesFresco1y2> nivel1y2,
        IEnumerable<VdInscripcionesFresco3y4> nivel3y4)
    {
        return nivel1y2.Select(ToItem)
            .Concat(nivel3y4.Select(ToItem))
            .GroupBy(x => new { x.IdProducto, x.IdProceso })
            .OrderBy(g => g.Key.IdProducto)
            .ThenBy(g => g.Key.IdProceso)
            .Select(ToResponse)
            .ToList();
    }

    private static MyEnrollmentsResponse ToResponse(IGrouping<object, EnrollmentRow> grupo)
    {
        var filas = grupo.OrderBy(x => x.FechaInicioComienzo).ToList();
        var cabecera = filas[0];
        return new MyEnrollmentsResponse
        {
            ProductId = cabecera.IdProducto,
            ProductFullName = cabecera.NombreExtensoProducto,
            AdmissionProcessId = cabecera.IdProceso,
            ProductLevelId = cabecera.IdNivelProducto,
            EnrollmentStatus = cabecera.EstadoInscripcion,
            HasSeminars = cabecera.ProgConSeminariosProducto,
            Enrollments = filas.Select(x => new MyEnrollmentItem
            {
                EnrollmentId = x.IdInscripto,
                OfferingId = x.IdOferta,
                OfferingDescription = x.DescripcionOferta,
                ShiftId = x.IdTurno,
                IntakeId = x.IdComienzo,
                IntakeStartDate = x.FechaInicioComienzo,
                IntakeName = x.NombreComienzo,
                ShiftName = x.NombreTurno,
                ReferenceDate = x.FechaReferencia,
            }).ToList()
        };
    }

    private static EnrollmentRow ToItem(VdInscripcionesFresco1y2 source) => new(
        source.IdProducto,
        source.NombreExtensoProducto,
        source.IdProceso,
        source.IdNivelProducto,
        source.EstadoInscripcion,
        null,
        source.IdInscripto,
        source.IdTurno,
        source.IdComienzo,
        source.FechaInicioComienzo,
        source.NombreComienzo,
        source.NombreTurno,
        source.FechaReferencia,
        (long?)source.IdOferta,
        null);

    private static EnrollmentRow ToItem(VdInscripcionesFresco3y4 source) => new(
        source.IdProducto,
        source.NombreExtensoProducto,
        source.IdProceso,
        source.IdNivelProducto,
        source.EstadoInscripcion,
        source.ProgConSeminariosProducto,
        source.IdInscripto,
        source.IdTurno,
        source.IdComienzo,
        source.FechaInicioComienzo,
        source.NombreComienzo,
        source.NombreTurno,
        source.FechaReferencia,
        source.IdOferta,
        source.DescripcionOferta);

    /// <summary>Forma común de las dos vistas fresco, para poder unirlas.</summary>
    private sealed record EnrollmentRow(
        decimal? IdProducto,
        string? NombreExtensoProducto,
        decimal? IdProceso,
        long? IdNivelProducto,
        string? EstadoInscripcion,
        string? ProgConSeminariosProducto,
        decimal? IdInscripto,
        decimal? IdTurno,
        decimal? IdComienzo,
        DateTime? FechaInicioComienzo,
        string? NombreComienzo,
        string? NombreTurno,
        DateTime? FechaReferencia,
        long? IdOferta,
        string? DescripcionOferta);
}
