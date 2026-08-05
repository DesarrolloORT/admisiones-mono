using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.Enrollments.Dtos;

/// <summary>Resultado de confirmar la preinscripción de una o varias ofertas.</summary>
public class ConfirmPreEnrollmentResponse
{
    /// <summary>La preinscripción quedó confirmada.</summary>
    public bool Confirmed { get; set; }

    /// <summary>True cuando la preinscripción quedó pero la inscripción fue a bandeja ("A la espera"). El front muestra "Inscripción en proceso".</summary>
    public bool Waiting { get; set; }

    /// <summary>Cabecera común a todas las ofertas confirmadas (producto, carrera y vencimiento, que es igual para todas).</summary>
    public EnrollmentHeader Summary { get; set; } = new();

    /// <summary>Saldo de la cuenta corriente del alumno.</summary>
    public CurrentAccountBalance? CurrentAccount { get; set; }

    /// <summary>Una inscripción por cada oferta confirmada (1 elemento para nivel 1 y 2, 1 o mas para nivel 3 y 4).</summary>
    public List<EnrollmentOffering> Enrollments { get; set; } = new();

    /// <summary>Total a pagar de reserva, suma de todas las ofertas confirmadas (el alumno paga todo junto, no elige).</summary>
    public decimal DepositAmount { get; set; }
}

/// <summary>Datos compartidos por todas las ofertas de la selección. Turno/comienzo son por oferta (van en <see cref="EnrollmentOffering"/>) porque en nivel 3 y 4 pueden diferir.</summary>
public class EnrollmentHeader
{
    /// <summary>Código del producto.</summary>
    public long ProductId { get; set; }

    /// <summary>Nombre de la carrera.</summary>
    public string? DegreeProgram { get; set; }

    /// <summary>Fecha límite de pago de la reserva; es la misma para todas las ofertas. Nula mientras no hay confirmación (estado "En proceso").</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? PaymentDueDate { get; set; }
}

/// <summary>Oferta de la selección, con su comienzo y turno propios.</summary>
public class EnrollmentOffering
{
    /// <summary>Solo cuando la oferta ya generó inscripción (confirmada / pago pendiente). Nula en estado "En proceso".</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? EnrollmentId { get; set; }

    /// <summary>Código de la oferta.</summary>
    public long OfferingId { get; set; }

    /// <summary>Nombre del comienzo (cohorte).</summary>
    public string? Intake { get; set; }

    /// <summary>Nombre del turno.</summary>
    public string? Shift { get; set; }

    /// <summary>Descripción de la oferta (solo nivel 3 y 4, sale de la vista de ofertas disponibles). Se omite si no aplica.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OfferingDescription { get; set; }
}

/// <summary>Saldo de la cuenta corriente del alumno.</summary>
public class CurrentAccountBalance
{
    /// <summary>Saldo actual.</summary>
    public decimal CurrentBalance { get; set; }
}
