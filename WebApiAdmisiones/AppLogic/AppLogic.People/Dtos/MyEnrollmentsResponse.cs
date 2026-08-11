using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.People.Dtos;

/// <summary>
/// Inscripciones de la persona agrupadas por producto, proceso de admisión y estado: una entrada
/// por combinación, con las ofertas concretas adentro. Un mismo producto y proceso puede aparecer
/// más de una vez si sus ofertas están en estados distintos.
/// </summary>
[ExcludeFromCodeCoverage]
public class MyEnrollmentsResponse
{
    /// <summary>Producto (carrera) al que se inscribió.</summary>
    public decimal? ProductId { get; set; }

    /// <summary>Nombre extenso del producto.</summary>
    public string? ProductFullName { get; set; }

    /// <summary>Proceso de admisión.</summary>
    public decimal? AdmissionProcessId { get; set; }

    /// <summary>Nivel del producto: 1 y 2 son grado y tecnicatura; 3 y 4, actualización profesional.</summary>
    public long? ProductLevelId { get; set; }

    /// <summary>
    /// Estado de la inscripción.
    /// </summary>
    public string? EnrollmentStatus { get; set; }

    /// <summary>"SI" / "NO": solo viene para niveles 3 y 4, donde la oferta puede tener seminarios.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HasSeminars { get; set; }

    /// <summary>
    /// Vencimiento del pago de la inscripción (FECHA_VTO_INSCR). Se toma de la fila cabecera del
    /// grupo; null si todavía no hay vencimiento calculado.
    /// </summary>
    public DateTime? PaymentDueDate { get; set; }

    /// <summary>Ofertas concretas de esta combinación de producto y proceso.</summary>
    public List<MyEnrollmentItem> Enrollments { get; set; } = [];
}

/// <summary>Una oferta concreta dentro de una inscripción.</summary>
[ExcludeFromCodeCoverage]
public class MyEnrollmentItem
{
    /// <summary>Identificador del inscripto.</summary>
    public decimal? EnrollmentId { get; set; }

    /// <summary>Oferta elegida. Solo viene para niveles 3 y 4.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? OfferingId { get; set; }

    /// <summary>Texto libre con el detalle de la oferta. Solo viene para niveles 3 y 4.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OfferingDescription { get; set; }

    /// <summary>Turno elegido.</summary>
    public decimal? ShiftId { get; set; }

    /// <summary>Comienzo (cohorte) elegido.</summary>
    public decimal? IntakeId { get; set; }

    /// <summary>Fecha de inicio del comienzo.</summary>
    public DateTime? IntakeStartDate { get; set; }

    /// <summary>Nombre del comienzo.</summary>
    public string? IntakeName { get; set; }

    /// <summary>Nombre del turno.</summary>
    public string? ShiftName { get; set; }

    /// <summary>Fecha de referencia de inicio de la oferta.</summary>
    public DateTime? ReferenceDate { get; set; }
}
