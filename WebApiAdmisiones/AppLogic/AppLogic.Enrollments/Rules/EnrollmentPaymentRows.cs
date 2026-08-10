using AppLogic.Enrollments.Mapping;
using AppLogic.Enrollments.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Enrollments.Rules;

/// <summary>
/// Proyecta las filas de las vistas fresco de nivel 1-2 y 3-4 a una forma común.
/// Solo 3 y 4 traen descripción de oferta (seminarios).
/// </summary>
internal static class EnrollmentPaymentRows
{
    // IdInscripto es la clave de la vista: nunca viene null, el decimal? es ruido del mapeo Devart.
    internal static EnrollmentPaymentRow From(VdInscripcionesFresco1y2 fila) => new(
        (long)fila.IdInscripto!.Value, fila.IdOferta, fila.NombreComienzo, fila.NombreTurno, null);

    internal static EnrollmentPaymentRow From(VdInscripcionesFresco3y4 fila) => new(
        (long)fila.IdInscripto!.Value, fila.IdOferta, fila.NombreComienzo, fila.NombreTurno, fila.DescripcionOferta);
}
