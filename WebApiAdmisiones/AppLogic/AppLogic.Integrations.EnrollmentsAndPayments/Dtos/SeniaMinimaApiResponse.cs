namespace AppLogic.Integrations.EnrollmentsAndPayments.Dtos;

/// <summary>
/// Response de la seña mínima a pagar de una inscripción (read-only).
/// Corresponde a: GET /Inscripciones/SeniaMinima
/// </summary>
public class SeniaMinimaApiResponse
{
    public decimal SeniaMinima { get; set; }
}
