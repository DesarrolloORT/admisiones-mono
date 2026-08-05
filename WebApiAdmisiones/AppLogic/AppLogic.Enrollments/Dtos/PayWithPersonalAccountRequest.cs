namespace AppLogic.Enrollments.Dtos;

/// <summary>Pago de inscripciones contra la cuenta personal del alumno.</summary>
public class PayWithPersonalAccountRequest
{
    /// <summary>Inscripciones a pagar.</summary>
    public List<long> EnrollmentIds { get; set; } = new();
}
