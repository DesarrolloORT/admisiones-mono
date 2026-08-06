using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;

namespace AppLogic.Integrations.EnrollmentsAndPayments.Mapping;

/// <summary>
/// Traduce la respuesta plana de ofertas de la API de Inscripciones y Pagos al DTO anidado que
/// consume el resto del backend.
/// </summary>
public static class OfferingApiMapper
{
    /// <summary>Forma exacta que devuelve la API interna: plana, sin anidar el turno.</summary>
    public sealed class OfferingApiResponse
    {
        public long IdOferta { get; set; }
        public long IdTurno { get; set; }
        public string? NombreTurno { get; set; }
        public string? HorarioReferencia { get; set; }
    }

    public static List<OfertaInscripcionDto> ToDtos(List<OfferingApiResponse>? ofertasApi) =>
        ofertasApi?.Select(ToDto).ToList() ?? [];

    public static OfertaInscripcionDto ToDto(OfferingApiResponse source) => new()
    {
        IdOferta = source.IdOferta,
        Turno = new DtoTurno
        {
            IdTurno = source.IdTurno,
            NombreTurno = source.NombreTurno
        },
        HorarioReferencia = source.HorarioReferencia
    };
}
