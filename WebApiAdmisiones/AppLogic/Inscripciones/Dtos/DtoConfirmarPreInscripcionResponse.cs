using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.Inscripciones.Dtos;

public class DtoConfirmarPreInscripcionResponse
{
    public bool Confirmada { get; set; }

    /// <summary>True cuando la preinscripción quedó pero la inscripción fue a bandeja ("A la espera"). El front muestra "Inscripción en proceso".</summary>
    public bool EnEspera { get; set; }

    /// <summary>Cabecera común a todas las ofertas confirmadas (producto, carrera y vencimiento, que es igual para todas).</summary>
    public DtoCabeceraInscripcion Resumen { get; set; } = new();
    public DtoEstadoCuenta? EstadoCuenta { get; set; }

    /// <summary>Una inscripción por cada oferta confirmada (1 elemento para nivel 1 y 2, 1 o mas para nivel 3 y 4).</summary>
    public List<DtoInscripcionOferta> Inscripciones { get; set; } = new();

    /// <summary>Total a pagar de reserva, suma de todas las ofertas confirmadas (el alumno paga todo junto, no elige).</summary>
    public decimal PagoReserva { get; set; }
}

/// <summary>Datos compartidos por todas las ofertas de la selección. Turno/comienzo son por oferta (van en <see cref="DtoInscripcionOferta"/>) porque en nivel 3 y 4 pueden diferir.</summary>
public class DtoCabeceraInscripcion
{
    public long IdProducto { get; set; }
    public string? Carrera { get; set; }

    /// <summary>Fecha límite de pago de la reserva; es la misma para todas las ofertas. Nula mientras no hay confirmación (estado "En proceso").</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? FechaVencimientoPago { get; set; }
}

public class DtoInscripcionOferta
{
    /// <summary>Solo cuando la oferta ya generó inscripción (confirmada / pago pendiente). Nula en estado "En proceso".</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? IdInscripcion { get; set; }

    public long IdOferta { get; set; }
    public string? Comienzo { get; set; }
    public string? Turno { get; set; }

    /// <summary>Descripción de la oferta (solo nivel 3 y 4, sale de la vista de ofertas disponibles). Se omite si no aplica.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DescripcionOferta { get; set; }
}

public class DtoEstadoCuenta
{
    public decimal SaldoActual { get; set; }
}
