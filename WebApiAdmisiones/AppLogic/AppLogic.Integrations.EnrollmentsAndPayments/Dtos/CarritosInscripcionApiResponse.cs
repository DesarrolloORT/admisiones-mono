using System.Collections.Generic;

namespace AppLogic.Integrations.EnrollmentsAndPayments.Dtos;

public class CarritosInscripcionApiResponse
{
    public List<CarritoPagoReservaApiDto> Carritos { get; set; } = new();
    public EstadoCuentaApiDto? EstadoCuenta { get; set; }
}
