namespace AppLogic.Enrollments.Dtos;

/// <summary>Reactivación de una o varias inscripciones dadas de baja.</summary>
public class ReactivateEnrollmentRequest
{
    /// <summary>
    /// Inscripciones dadas de baja a reactivar. Para productos de nivel 1 y 2 debe traer exactamente
    /// una; para nivel 3 y 4 puede traer varias (una por seminario/oferta).
    /// </summary>
    public List<long> EnrollmentIds { get; set; } = new();
}
