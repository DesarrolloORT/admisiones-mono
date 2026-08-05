namespace AppLogic.Catalogs.Dtos;

/// <summary>Turno de una oferta.</summary>
public sealed class ShiftResponse
{
    /// <summary>Identificador del turno.</summary>
    public long ShiftId { get; init; }

    /// <summary>Nombre del turno (Matutino, Nocturno…).</summary>
    public string? ShiftName { get; init; }
}

/// <summary>
/// Oferta disponible para una carrera y proceso. Según el nivel del producto sale de las vistas
/// Devart (niveles 3 y 4) o de la API de Inscripciones y Pagos (niveles 1 y 2); este DTO unifica
/// las dos fuentes en la respuesta que ve el front.
/// </summary>
public sealed class OfferingResponse
{
    /// <summary>Identificador de la oferta.</summary>
    public long OfferingId { get; init; }

    /// <summary>Turno de la oferta.</summary>
    public ShiftResponse Shift { get; init; } = new();

    /// <summary>Horario de referencia, cuando la oferta lo declara.</summary>
    public string? ReferenceSchedule { get; init; }

    /// <summary>Fecha de referencia de inicio, cuando la oferta la declara.</summary>
    public DateTime? ReferenceDate { get; init; }

    /// <summary>Texto libre con el detalle de la oferta.</summary>
    public string? OfferingDescription { get; init; }
}
