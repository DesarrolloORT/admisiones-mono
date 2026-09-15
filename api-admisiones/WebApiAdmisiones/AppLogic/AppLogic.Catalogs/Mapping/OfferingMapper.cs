using AppLogic.Catalogs.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Catalogs.Mapping;

/// <summary>
/// Unifica las dos vistas Devart de ofertas en <see cref="OfferingResponse"/>: niveles 3 y 4, y
/// niveles 1 y 2.
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

    public static OfferingResponse ToResponse(this VdOfertasDisponibles1y2 offering) => new()
    {
        OfferingId = offering.IdOferta,
        Shift = new ShiftResponse
        {
            ShiftId = offering.IdTurno,
            ShiftName = offering.NombreTurno
        },
        ReferenceSchedule = offering.HorarioReferenciaOferta
    };
}
