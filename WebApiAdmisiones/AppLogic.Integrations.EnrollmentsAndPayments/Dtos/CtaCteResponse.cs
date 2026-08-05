using System;
using System.Collections.Generic;

namespace AppLogic.Integrations.EnrollmentsAndPayments.Dtos;

/// <summary>
/// Response de cuenta corriente.
/// Corresponde a: GET /api/Pagos/CtaCte
/// </summary>
public class CtaCteResponse
{
    public decimal SaldoActual { get; set; }
    public decimal SaldoVencido { get; set; }
    public decimal SaldoAVencer { get; set; }
    public List<MovimientoCtaCte> Movimientos { get; set; } = new();
}

/// <summary>
/// Movimiento de cuenta corriente.
/// </summary>
public class MovimientoCtaCte
{
    public DateTime Fecha { get; set; }
    public string? Concepto { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public decimal Saldo { get; set; }
}
