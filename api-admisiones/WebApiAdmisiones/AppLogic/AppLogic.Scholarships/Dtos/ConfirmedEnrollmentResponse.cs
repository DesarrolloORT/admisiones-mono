using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Scholarships.Dtos;

/// <summary>
/// Inscripción confirmada de la persona, tal como la necesita el circuito de becas. Sale de la vista
/// VD_INSCRIPCIONES_FRESCO_1Y2; antes se serializaba la fila cruda de Devart con nombres en español.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConfirmedEnrollmentResponse
{
    /// <summary>Identificador del inscripto.</summary>
    public decimal? EnrollmentId { get; set; }

    /// <summary>Fecha en que se registró la inscripción.</summary>
    public DateTime? EnrollmentDate { get; set; }

    /// <summary>Usuario que registró la inscripción.</summary>
    public string? EnrollmentUser { get; set; }

    /// <summary>Estado de la inscripción (siempre confirmada en este endpoint).</summary>
    public string? EnrollmentStatus { get; set; }

    /// <summary>Producto (carrera) al que se inscribió.</summary>
    public decimal? ProductId { get; set; }

    /// <summary>Nombre extenso del producto.</summary>
    public string? ProductFullName { get; set; }

    /// <summary>Nivel del producto.</summary>
    public long? ProductLevelId { get; set; }

    /// <summary>Proceso de admisión al que pertenece la inscripción.</summary>
    public decimal? AdmissionProcessId { get; set; }

    /// <summary>Comienzo (cohorte) elegido.</summary>
    public decimal? IntakeId { get; set; }

    /// <summary>Nombre del comienzo.</summary>
    public string? IntakeName { get; set; }

    /// <summary>Fecha de inicio del comienzo.</summary>
    public DateTime? IntakeStartDate { get; set; }

    /// <summary>Turno elegido.</summary>
    public decimal? ShiftId { get; set; }

    /// <summary>Nombre del turno.</summary>
    public string? ShiftName { get; set; }

    /// <summary>Oferta concreta a la que se inscribió.</summary>
    public decimal? OfferingId { get; set; }

    /// <summary>Fecha de referencia de inicio de la oferta.</summary>
    public DateTime? ReferenceDate { get; set; }

    /// <summary>Origen desde el que llegó la inscripción (columna VENGO_DE de la vista).</summary>
    public string? Origin { get; set; }
}
