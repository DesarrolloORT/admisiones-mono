using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.Inscripciones.Dtos;

/// <summary>
/// Detalle de una inscripción de "Mis carreras". El contenido depende del estado:
/// "En proceso" trae la oferta seleccionada; "A la espera" no trae detalle;
/// "Pago pendiente" trae seña/vencimiento/resumen; "Confirmada" trae número de estudiante,
/// coordinador académico, resumen de carrera y materias del primer semestre.
/// </summary>
public class DtoDetalleInscripcionResponse
{
    public string Estado { get; set; } = string.Empty;

    /// <summary>Ofertas seleccionadas (una o varias para nivel 3 y 4). Solo se completa cuando el estado es "En proceso".</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DtoDetalleEnProceso? Detalle { get; set; }

    /// <summary>Solo se completa cuando el estado es "Pago pendiente" y aún no eligió método de pago.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DtoConfirmarPreInscripcionResponse? PagoPendiente { get; set; }

    /// <summary>Solo cuando el estado es "Pago pendiente" Y ya eligió método de pago (existe reserva mínima).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DtoReservaMinima? ReservaMinima { get; set; }

    /// <summary>Solo se completa cuando el estado es "Confirmada".</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DtoConfirmadaDetalle? Confirmada { get; set; }
}

public class DtoReservaMinima
{
    public string TipoPago { get; set; } = string.Empty; // ABITAB / PAGANZA
    public string? Cedula { get; set; }
    public long CodigoPersona { get; set; }
    public decimal PagoReserva { get; set; }
}

/// <summary>Detalle del estado "En proceso": cabecera compartida + las ofertas en las que se registró interés.</summary>
public class DtoDetalleEnProceso
{
    public DtoCabeceraInscripcion Resumen { get; set; } = new();
    public List<DtoInscripcionOferta> Intereses { get; set; } = new();
}

public class DtoConfirmadaDetalle
{
    public long CodigoPersona { get; set; }
    public DtoResumenInscripcion Resumen { get; set; } = new();
    public DtoCoordinador? CoordinadorAcademico { get; set; }
    public DtoCoordinador? CoordinadorCursos { get; set; }
    public List<DtoMateria> MateriasPrimerSemestre { get; set; } = new();
}

/// <summary>Resumen de una única inscripción confirmada (pantalla "Mis carreras" en estado Confirmada).</summary>
public class DtoResumenInscripcion
{
    public long IdOferta { get; set; }
    public long IdProducto { get; set; }
    public string? Carrera { get; set; }
    public long IdComienzo { get; set; }
    public string? Comienzo { get; set; }
    public long IdTurno { get; set; }
    public string? Turno { get; set; }
}

public class DtoCoordinador
{
    public string? Nombre { get; set; }
    public string? Email { get; set; }
}

public class DtoMateria
{
    public long IdMateria { get; set; }
    public string? Nombre { get; set; }
}
