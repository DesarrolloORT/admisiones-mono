using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.Enrollments.Dtos;

/// <summary>
/// Detalle de una inscripción de "Mis carreras". El contenido depende del estado:
/// "En proceso" trae la oferta seleccionada; "A la espera" no trae detalle;
/// "Pago pendiente" trae seña/vencimiento/resumen; "Confirmada" trae número de estudiante,
/// coordinador académico, resumen de carrera y materias del primer semestre.
/// </summary>
public class EnrollmentDetailsResponse
{
    /// <summary>Estado de la inscripción tal como lo devuelven las vistas fresco.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Ofertas seleccionadas (una o varias para nivel 3 y 4). Solo se completa cuando el estado es "En proceso".</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InProgressDetails? InProgress { get; set; }

    /// <summary>Solo se completa cuando el estado es "Pago pendiente" y aún no eligió método de pago.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConfirmPreEnrollmentResponse? PendingPayment { get; set; }

    /// <summary>Solo cuando el estado es "Pago pendiente" Y ya eligió método de pago (existe reserva mínima).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MinimumDepositDetails? MinimumDeposit { get; set; }

    /// <summary>Solo se completa cuando el estado es "Confirmada".</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConfirmedEnrollmentDetailsResponse? Confirmed { get; set; }
}

/// <summary>Reserva mínima ya registrada, con el método de pago elegido.</summary>
public class MinimumDepositDetails
{
    /// <summary>Método de pago externo elegido: ABITAB o PAGANZA.</summary>
    public string PaymentType { get; set; } = string.Empty;

    /// <summary>Documento de la persona, que el sitio de pago externo necesita.</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>Código de la persona.</summary>
    public long PersonId { get; set; }

    /// <summary>Importe de la reserva a pagar.</summary>
    public decimal DepositAmount { get; set; }
}

/// <summary>Detalle del estado "En proceso": cabecera compartida + las ofertas en las que se registró interés.</summary>
public class InProgressDetails
{
    /// <summary>Cabecera con producto, carrera y datos comunes a todas las ofertas.</summary>
    public EnrollmentHeader Summary { get; set; } = new();

    /// <summary>Ofertas en las que la persona registró interés.</summary>
    public List<EnrollmentOffering> Interests { get; set; } = new();
}

/// <summary>Cabecera compartida (producto/carrera/coordinadores, iguales para todas las ofertas del mismo pago) + una entrada por cada oferta confirmada (nivel 3 y 4 con seminarios puede traer más de una).</summary>
public class ConfirmedEnrollmentDetailsResponse
{
    /// <summary>Código de la persona.</summary>
    public long PersonId { get; set; }

    /// <summary>Código del producto.</summary>
    public long ProductId { get; set; }

    /// <summary>Nombre de la carrera.</summary>
    public string? DegreeProgram { get; set; }

    /// <summary>Coordinador académico de la carrera.</summary>
    public Coordinator? AcademicCoordinator { get; set; }

    /// <summary>Coordinador de cursos de la carrera.</summary>
    public Coordinator? CourseCoordinator { get; set; }

    /// <summary>Una entrada por cada oferta confirmada.</summary>
    public List<ConfirmedEnrollment> Enrollments { get; set; } = new();
}

/// <summary>Oferta confirmada, con sus materias del primer semestre.</summary>
public class ConfirmedEnrollment
{
    /// <summary>Código de la inscripción.</summary>
    public long EnrollmentId { get; set; }

    /// <summary>Código de la oferta.</summary>
    public long OfferingId { get; set; }

    /// <summary>Código del comienzo (cohorte).</summary>
    public long IntakeId { get; set; }

    /// <summary>Nombre del comienzo.</summary>
    public string? Intake { get; set; }

    /// <summary>Código del turno.</summary>
    public long ShiftId { get; set; }

    /// <summary>Nombre del turno.</summary>
    public string? Shift { get; set; }

    /// <summary>Materias del primer semestre.</summary>
    public List<Subject> FirstSemesterSubjects { get; set; } = new();
}

/// <summary>Coordinador de contacto de la carrera.</summary>
public class Coordinator
{
    /// <summary>Nombre del coordinador.</summary>
    public string? Name { get; set; }

    /// <summary>Email de contacto.</summary>
    public string? Email { get; set; }
}

/// <summary>Materia del plan de estudios.</summary>
public class Subject
{
    /// <summary>Código de la materia.</summary>
    public long SubjectId { get; set; }

    /// <summary>Nombre de la materia.</summary>
    public string? Name { get; set; }
}
