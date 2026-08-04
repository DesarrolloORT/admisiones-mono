using System.Diagnostics.CodeAnalysis;
using AppLogic.ApiClients.Dtos;
using AppLogic.Inscripciones.Dtos;
using AppLogic.Inscripciones.Rules;
using BusinessLogic.Entities;
using AppLogic.Helpers;
using Utilities;

namespace AppLogic.Inscripciones.Mappers;

/// <summary>
/// Fila resuelta de la vista fresco (1y2 o 3y4) con los datos que necesita el detalle de "Pago pendiente" —
/// evita depender de la navegación completa de <see cref="Inscripto"/> (Oferta→Supraoferta→Comienzo/Turno/Paquete/Producto),
/// que la vista fresco ya trae resuelta en la misma fila.
/// </summary>
internal sealed record FilaInscripcionPago(
    long IdInscripto,
    long? IdOferta,
    string? NombreComienzo,
    string? NombreTurno,
    string? DescripcionOferta);

/// <summary>
/// Traduce entidades y respuestas de la API interna a los DTOs de respuesta del módulo Inscripciones.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class InscripcionesMapper
{
    private sealed record CoordinadorMapeado(DtoCoordinador Coordinador, long? Codigo);

    /// <summary>Detalle del estado "Pago pendiente": cabecera compartida + una entrada por cada inscripción (nivel 3 y 4 con seminarios puede traer más de una).</summary>
    public static DtoConfirmarPreInscripcionResponse MapearPagoPendiente(
        ICollection<FilaInscripcionPago> filas, long idProducto, string? nombreExtensoProducto, DateTime? fechaVencimientoPago, CarritosInscripcionApiResponse? carritos)
    {
        var pagoReserva = ConfirmarPreInscripcionRules.SumarPagoReserva(carritos?.Carritos);
        return new DtoConfirmarPreInscripcionResponse
        {
            Confirmada = true,
            EstadoCuenta = MapearEstadoCuenta(carritos?.EstadoCuenta),
            Resumen = new DtoCabeceraInscripcion
            {
                IdProducto = idProducto,
                Carrera = nombreExtensoProducto,
                FechaVencimientoPago = fechaVencimientoPago
            },
            Inscripciones = filas
                .Select(fila => new DtoInscripcionOferta
                {
                    IdInscripcion = fila.IdInscripto,
                    IdOferta = fila.IdOferta ?? 0,
                    Comienzo = fila.NombreComienzo,
                    Turno = fila.NombreTurno,
                    DescripcionOferta = fila.DescripcionOferta
                })
                .ToList(),
            PagoReserva = pagoReserva
        };
    }

    public static DtoConfirmadaDetalle MapearConfirmada(
        long codigoPersona,
        IReadOnlyList<(Inscripto Inscripto, ICollection<VdInscriptoCreditoAlumno> Materias)> ofertas,
        ICollection<VdInscriptoCoordinadore> coordinadores)
    {
        var coordinadorAcademico = MapearCoordinadorAcademico(coordinadores);
        var coordinadorCursos = MapearCoordinadorCursos(coordinadores);
        if (EsMismoCoordinador(coordinadorAcademico, coordinadorCursos))
        {
            coordinadorCursos = null;
        }

        var producto = ofertas[0].Inscripto.Oferta?.Supraoferta?.Paquete?.Producto;

        return new DtoConfirmadaDetalle
        {
            CodigoPersona = codigoPersona,
            IdProducto = producto?.IdProducto ?? 0,
            Carrera = NombreCarrera(producto),
            CoordinadorAcademico = coordinadorAcademico?.Coordinador,
            CoordinadorCursos = coordinadorCursos?.Coordinador,
            Inscripciones = ofertas
                .Select(o => MapearInscripcionConfirmada(o.Inscripto, o.Materias))
                .ToList()
        };
    }

    private static DtoInscripcionConfirmada MapearInscripcionConfirmada(Inscripto inscripto, ICollection<VdInscriptoCreditoAlumno> materias)
    {
        var comienzo = inscripto.Oferta?.Supraoferta?.Comienzo;
        return new DtoInscripcionConfirmada
        {
            IdInscripcion = inscripto.IdInscripto,
            IdOferta = inscripto.IdOferta ?? 0,
            IdComienzo = comienzo?.IdComienzo ?? 0,
            Comienzo = comienzo?.NombreComienzo,
            IdTurno = inscripto.Oferta?.IdTurno ?? 0,
            Turno = inscripto.Oferta?.Turno?.NombreTurno,
            MateriasPrimerSemestre = materias
                .Where(m => m.IdMateria.HasValue)
                .GroupBy(m => m.IdMateria!.Value)
                .Select(g => new DtoMateria { IdMateria = g.Key, Nombre = g.First().DescripcionMateria?.Trim() })
                .ToList()
        };
    }

    public static OperationResult<DtoConfirmarPreInscripcionResponse> MapearResultadoApiMultiple(
        OperationResult<ConfirmarPreInscripcionMultipleApiResponse> apiResult,
        List<DatosConfirmacionOferta> ofertasContexto,
        Dictionary<long, string?> descripciones,
        string methodName)
    {
        if (!apiResult.Success)
            return apiResult.Failure().As<DtoConfirmarPreInscripcionResponse>(methodName);

        if (apiResult.Data == null)
        {
            return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_13", methodName, "La API interna no devolvio datos de confirmacion.", 502);
        }

        return OperationResult<DtoConfirmarPreInscripcionResponse>.Ok(
            MapearConfirmacionPreInscripcionMultiple(apiResult.Data, ofertasContexto, descripciones),
            methodName);
    }

    /// <summary>Detalle del estado "En proceso": cabecera compartida + una entrada por cada oferta con interés (nivel 3 y 4 puede traer varias).</summary>
    public static DtoDetalleEnProceso MapearDetalleEnProceso(ICollection<Oferta> ofertas)
    {
        var producto = ofertas.FirstOrDefault()?.Supraoferta?.Paquete?.Producto;
        return new DtoDetalleEnProceso
        {
            Resumen = new DtoCabeceraInscripcion
            {
                IdProducto = producto?.IdProducto ?? 0,
                Carrera = NombreCarrera(producto)
            },
            Intereses = ofertas
                .Select(o => new DtoInscripcionOferta
                {
                    IdOferta = o.IdOferta,
                    Comienzo = o.Supraoferta?.Comienzo?.NombreComienzo,
                    Turno = o.Turno?.NombreTurno
                })
                .ToList()
        };
    }

    private static DtoConfirmarPreInscripcionResponse MapearConfirmacionPreInscripcionMultiple(
        ConfirmarPreInscripcionMultipleApiResponse source,
        List<DatosConfirmacionOferta> ofertasContexto,
        Dictionary<long, string?> descripciones)
    {
        // comienzo y turno son por oferta (en nivel 3 y 4 el comienzo puede diferir): se toman de los
        // datos cargados de cada oferta, no del resumen consolidado de la API.
        var datosPorOferta = ofertasContexto
            .GroupBy(o => o.IdOferta)
            .ToDictionary(g => g.Key, g => g.First());
        var cabecera = ofertasContexto.First();

        var inscripciones = source.Ofertas
            .Select(o =>
            {
                datosPorOferta.TryGetValue(o.IdOferta, out var datos);
                descripciones.TryGetValue(o.IdOferta, out var descripcion);
                return new DtoInscripcionOferta
                {
                    IdInscripcion = o.IdInscripcion,
                    IdOferta = o.IdOferta,
                    Comienzo = datos?.Comienzo?.NombreComienzo,
                    Turno = datos?.Turno?.NombreTurno,
                    DescripcionOferta = descripcion
                };
            })
            .ToList();

        return new DtoConfirmarPreInscripcionResponse
        {
            Confirmada = source.Confirmada || source.Respuesta,
            EnEspera = source.InscripcionPendiente,
            EstadoCuenta = MapearEstadoCuenta(source.EstadoCuenta),
            Resumen = new DtoCabeceraInscripcion
            {
                IdProducto = source.Resumen != null && source.Resumen.IdProducto > 0 ? source.Resumen.IdProducto : cabecera.IdProducto,
                Carrera = source.Resumen?.Carrera ?? NombreCarrera(cabecera.Producto),
                // El vencimiento es el mismo para todas las ofertas: se toma el de la primera.
                FechaVencimientoPago = source.Ofertas.FirstOrDefault()?.FechaVencimientoPago
            },
            Inscripciones = inscripciones,
            // El alumno paga todas las ofertas confirmadas de una sola vez, no elige: el front recibe
            // directamente el total (para nivel 1 y 2, con una sola oferta, coincide con esa unica seña).
            PagoReserva = source.Ofertas.Sum(o => (decimal)o.ValorSeniaMinima)
        };
    }

    private static DtoEstadoCuenta? MapearEstadoCuenta(EstadoCuentaApiDto? source)
    {
        if (source == null)
        {
            return null;
        }

        return new DtoEstadoCuenta
        {
            SaldoActual = source.SaldoActual
        };
    }

    private static CoordinadorMapeado? MapearCoordinadorAcademico(IEnumerable<VdInscriptoCoordinadore> coordinadores)
    {
        var coordinador = coordinadores.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c.CooacadPrimerNombre)
            || !string.IsNullOrWhiteSpace(c.CooacadPrimerApellido)
            || !string.IsNullOrWhiteSpace(c.MailAcad));

        return coordinador == null
            ? null
            : new CoordinadorMapeado(
                new DtoCoordinador
                {
                    Nombre = NombreCompleto(coordinador.CooacadPrimerNombre, coordinador.CooacadPrimerApellido),
                    Email = coordinador.MailAcad?.Trim()
                },
                coordinador.CooacadCodigo);
    }

    private static CoordinadorMapeado? MapearCoordinadorCursos(IEnumerable<VdInscriptoCoordinadore> coordinadores)
    {
        var coordinador = coordinadores.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c.CoorespPrimerNombre)
            || !string.IsNullOrWhiteSpace(c.CoorespPrimerApellido)
            || !string.IsNullOrWhiteSpace(c.MailResp));

        return coordinador == null
            ? null
            : new CoordinadorMapeado(
                new DtoCoordinador
                {
                    Nombre = NombreCompleto(coordinador.CoorespPrimerNombre, coordinador.CoorespPrimerApellido),
                    Email = coordinador.MailResp?.Trim()
                },
                coordinador.CoorespCodigo);
    }

    private static bool EsMismoCoordinador(CoordinadorMapeado? academico, CoordinadorMapeado? cursos)
    {
        if (academico == null || cursos == null)
        {
            return false;
        }

        if (academico.Codigo.HasValue && cursos.Codigo.HasValue)
        {
            return academico.Codigo.Value == cursos.Codigo.Value;
        }

        return SonTextosEquivalentes(academico.Coordinador.Email, cursos.Coordinador.Email)
            || SonTextosEquivalentes(academico.Coordinador.Nombre, cursos.Coordinador.Nombre);
    }

    private static bool SonTextosEquivalentes(string? textoA, string? textoB)
    {
        return !string.IsNullOrWhiteSpace(textoA)
            && !string.IsNullOrWhiteSpace(textoB)
            && string.Equals(textoA.Trim(), textoB.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? NombreCompleto(string? nombre, string? apellido)
    {
        var partes = new[] { nombre?.Trim(), apellido?.Trim() }
            .Where(p => !string.IsNullOrWhiteSpace(p));

        var completo = string.Join(" ", partes);
        return string.IsNullOrWhiteSpace(completo) ? null : completo;
    }

    /// <summary>Nombre de carrera a mostrar: web, si no el extenso, si no el corto.</summary>
    private static string? NombreCarrera(Producto? producto)
    {
        return producto?.NombreWebProducto ?? producto?.NombreExtensoProducto ?? producto?.NombreProducto;
    }
}
