namespace AppLogic.Enrollments.Dtos;

/// <summary>Reactivación de una inscripción dada de baja.</summary>
public class ReactivateEnrollmentRequest
{
    /// <summary>Inscripción dada de baja que se quiere reactivar.</summary>
    public long EnrollmentId { get; set; }
}
