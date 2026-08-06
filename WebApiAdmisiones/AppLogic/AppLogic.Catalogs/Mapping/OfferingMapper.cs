using AppLogic.Catalogs.Dtos;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Catalogs.Mapping;

/// <summary>
/// Unifica las dos fuentes de ofertas en <see cref="OfferingResponse"/>: la vista Devart de niveles
/// 3 y 4, y el DTO que devuelve la API de Inscripciones y Pagos para niveles 1 y 2.
/// </summary>
public static class OfferingMapper
{
    public static OfferingResponse ToResponse(this VdOfertasDisponibles3y4 offering, Turno? turno) => new()
    {
        OfferingId = offering.IdOferta,
        Shift = new ShiftResponse
        {
            ShiftId = offering.IdTurno,
            ShiftName = turno?.NombreTurno
        },
        ReferenceSchedule = null,
        ReferenceDate = offering.FechaReferencia,
        OfferingDescription = offering.DescripcionOferta
    };

    public static OfferingResponse ToResponse(this OfertaInscripcionDto offering) => new()
    {
        OfferingId = offering.IdOferta,
        Shift = new ShiftResponse
        {
            ShiftId = offering.Turno.IdTurno,
            ShiftName = offering.Turno.NombreTurno
        },
        ReferenceSchedule = offering.HorarioReferencia,
        ReferenceDate = offering.FechaReferencia,
        OfferingDescription = offering.DescripcionOferta
    };
}
