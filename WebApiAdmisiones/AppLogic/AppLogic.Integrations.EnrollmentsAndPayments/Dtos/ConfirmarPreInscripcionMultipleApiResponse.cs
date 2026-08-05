using System;
using System.Collections.Generic;

namespace AppLogic.Integrations.EnrollmentsAndPayments.Dtos;

/// <summary>
/// Response de confirmar preinscripción con varias ofertas a la vez (nivel 3 y 4).
/// </summary>
public class ConfirmarPreInscripcionMultipleApiResponse
{
    public bool Respuesta { get; set; }
    public bool Confirmada { get; set; }
    public bool InscripcionPendiente { get; set; }
    public ResumenInscripcionApiDto? Resumen { get; set; }
    public List<ResultadoInscripcionOfertaApiDto> Ofertas { get; set; } = new();
    public EstadoCuentaApiDto? EstadoCuenta { get; set; }
}

public class ResultadoInscripcionOfertaApiDto
{
    public long IdOferta { get; set; }
    public long? IdInscripcion { get; set; }
    public DateTime? FechaVencimientoPago { get; set; }
    public double ValorCuota { get; set; }
    public double ValorSeniaMinima { get; set; }
}
