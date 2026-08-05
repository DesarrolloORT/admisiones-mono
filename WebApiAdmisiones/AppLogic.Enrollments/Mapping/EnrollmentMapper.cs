using System.Diagnostics.CodeAnalysis;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Rules;
using BusinessLogic.Entities;
using AppLogic.Contracts;
using Utilities;

namespace AppLogic.Enrollments.Mapping;

/// <summary>
/// Fila resuelta de la vista fresco (1y2 o 3y4) con los datos que necesita el detalle de "Pago pendiente" —
/// evita depender de la navegación completa de <see cref="Inscripto"/> (Oferta→Supraoferta→Comienzo/Turno/Paquete/Producto),
/// que la vista fresco ya trae resuelta en la misma fila.
/// </summary>
internal sealed record EnrollmentPaymentRow(
    long IdInscripto,
    long? IdOferta,
    string? NombreComienzo,
    string? NombreTurno,
    string? DescripcionOferta);

/// <summary>
/// Traduce entidades y respuestas de la API interna a los DTOs de respuesta del módulo Inscripciones.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class EnrollmentMapper
{
    private sealed record MappedCoordinator(Coordinator Coordinador, long? Codigo);

    /// <summary>Detalle del estado "Pago pendiente": cabecera compartida + una entrada por cada inscripción (nivel 3 y 4 con seminarios puede traer más de una).</summary>
    public static ConfirmPreEnrollmentResponse MapPendingPayment(
        ICollection<EnrollmentPaymentRow> filas, long productId, string? productFullName, DateTime? fechaVencimientoPago, CarritosInscripcionApiResponse? carritos)
    {
        var depositPayment = PreEnrollmentConfirmationRules.AddDepositPayment(carritos?.Carritos);
        return new ConfirmPreEnrollmentResponse
        {
            Confirmed = true,
            CurrentAccount = MapCurrentAccount(carritos?.EstadoCuenta),
            Summary = new EnrollmentHeader
            {
                ProductId = productId,
                DegreeProgram = productFullName,
                PaymentDueDate = fechaVencimientoPago
            },
            Enrollments = filas
                .Select(fila => new EnrollmentOffering
                {
                    EnrollmentId = fila.IdInscripto,
                    OfferingId = fila.IdOferta ?? 0,
                    Intake = fila.NombreComienzo,
                    Shift = fila.NombreTurno,
                    OfferingDescription = fila.DescripcionOferta
                })
                .ToList(),
            DepositAmount = depositPayment
        };
    }

    public static ConfirmedEnrollmentDetailsResponse MapConfirmed(
        long personId,
        IReadOnlyList<(Inscripto Inscripto, ICollection<VdInscriptoCreditoAlumno> Materias)> offerings,
        ICollection<VdInscriptoCoordinadore> coordinadores)
    {
        var coordinadorAcademico = MapAcademicCoordinator(coordinadores);
        var coordinadorCursos = MapCourseCoordinator(coordinadores);
        if (IsSameCoordinator(coordinadorAcademico, coordinadorCursos))
        {
            coordinadorCursos = null;
        }

        var product = offerings[0].Inscripto.Oferta?.Supraoferta?.Paquete?.Producto;

        return new ConfirmedEnrollmentDetailsResponse
        {
            PersonId = personId,
            ProductId = product?.IdProducto ?? 0,
            DegreeProgram = DegreeProgramName(product),
            AcademicCoordinator = coordinadorAcademico?.Coordinador,
            CourseCoordinator = coordinadorCursos?.Coordinador,
            Enrollments = offerings
                .Select(o => MapConfirmedEnrollment(o.Inscripto, o.Materias))
                .ToList()
        };
    }

    private static ConfirmedEnrollment MapConfirmedEnrollment(Inscripto enrollment, ICollection<VdInscriptoCreditoAlumno> materias)
    {
        var intake = enrollment.Oferta?.Supraoferta?.Comienzo;
        return new ConfirmedEnrollment
        {
            EnrollmentId = enrollment.IdInscripto,
            OfferingId = enrollment.IdOferta ?? 0,
            IntakeId = intake?.IdComienzo ?? 0,
            Intake = intake?.NombreComienzo,
            ShiftId = enrollment.Oferta?.IdTurno ?? 0,
            Shift = enrollment.Oferta?.Turno?.NombreTurno,
            FirstSemesterSubjects = materias
                .Where(m => m.IdMateria.HasValue)
                .GroupBy(m => m.IdMateria!.Value)
                .Select(g => new Subject { SubjectId = g.Key, Name = g.First().DescripcionMateria?.Trim() })
                .ToList()
        };
    }

    public static OperationResult<ConfirmPreEnrollmentResponse> MapMultipleApiResult(
        OperationResult<ConfirmarPreInscripcionMultipleApiResponse> apiResult,
        List<OfferingConfirmationData> ofertasContexto,
        Dictionary<long, string?> descripciones,
        string methodName)
    {
        if (!apiResult.Success)
            return apiResult.Failure().As<ConfirmPreEnrollmentResponse>(methodName);

        if (apiResult.Data == null)
        {
            return OperationResult<ConfirmPreEnrollmentResponse>.IsFailed("INS_CPI_13", methodName, "La API interna no devolvio datos de confirmacion.", 502);
        }

        return OperationResult<ConfirmPreEnrollmentResponse>.Ok(
            MapMultiplePreEnrollmentConfirmation(apiResult.Data, ofertasContexto, descripciones),
            methodName);
    }

    /// <summary>Detalle del estado "En proceso": cabecera compartida + una entrada por cada oferta con interés (nivel 3 y 4 puede traer varias).</summary>
    public static InProgressDetails MapInProgressDetails(ICollection<Oferta> offerings)
    {
        var product = offerings.FirstOrDefault()?.Supraoferta?.Paquete?.Producto;
        return new InProgressDetails
        {
            Summary = new EnrollmentHeader
            {
                ProductId = product?.IdProducto ?? 0,
                DegreeProgram = DegreeProgramName(product)
            },
            Interests = offerings
                .Select(o => new EnrollmentOffering
                {
                    OfferingId = o.IdOferta,
                    Intake = o.Supraoferta?.Comienzo?.NombreComienzo,
                    Shift = o.Turno?.NombreTurno
                })
                .ToList()
        };
    }

    private static ConfirmPreEnrollmentResponse MapMultiplePreEnrollmentConfirmation(
        ConfirmarPreInscripcionMultipleApiResponse source,
        List<OfferingConfirmationData> ofertasContexto,
        Dictionary<long, string?> descripciones)
    {
        // comienzo y turno son por oferta (en nivel 3 y 4 el comienzo puede diferir): se toman de los
        // datos cargados de cada oferta, no del resumen consolidado de la API.
        var dataByOffering = ofertasContexto
            .GroupBy(o => o.IdOferta)
            .ToDictionary(g => g.Key, g => g.First());
        var cabecera = ofertasContexto.First();

        var enrollments = source.Ofertas
            .Select(o =>
            {
                dataByOffering.TryGetValue(o.IdOferta, out var data);
                descripciones.TryGetValue(o.IdOferta, out var descripcion);
                return new EnrollmentOffering
                {
                    EnrollmentId = o.IdInscripcion,
                    OfferingId = o.IdOferta,
                    Intake = data?.Comienzo?.NombreComienzo,
                    Shift = data?.Turno?.NombreTurno,
                    OfferingDescription = descripcion
                };
            })
            .ToList();

        return new ConfirmPreEnrollmentResponse
        {
            Confirmed = source.Confirmada || source.Respuesta,
            Waiting = source.InscripcionPendiente,
            CurrentAccount = MapCurrentAccount(source.EstadoCuenta),
            Summary = new EnrollmentHeader
            {
                ProductId = source.Resumen != null && source.Resumen.IdProducto > 0 ? source.Resumen.IdProducto : cabecera.IdProducto,
                DegreeProgram = source.Resumen?.Carrera ?? DegreeProgramName(cabecera.Producto),
                // El vencimiento es el mismo para todas las ofertas: se toma el de la primera.
                PaymentDueDate = source.Ofertas.FirstOrDefault()?.FechaVencimientoPago
            },
            Enrollments = enrollments,
            // El alumno paga todas las ofertas confirmadas de una sola vez, no elige: el front recibe
            // directamente el total (para nivel 1 y 2, con una sola oferta, coincide con esa unica seña).
            DepositAmount = source.Ofertas.Sum(o => (decimal)o.ValorSeniaMinima)
        };
    }

    private static CurrentAccountBalance? MapCurrentAccount(EstadoCuentaApiDto? source)
    {
        if (source == null)
        {
            return null;
        }

        return new CurrentAccountBalance
        {
            CurrentBalance = source.SaldoActual
        };
    }

    private static MappedCoordinator? MapAcademicCoordinator(IEnumerable<VdInscriptoCoordinadore> coordinadores)
    {
        var coordinador = coordinadores.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c.CooacadPrimerNombre)
            || !string.IsNullOrWhiteSpace(c.CooacadPrimerApellido)
            || !string.IsNullOrWhiteSpace(c.MailAcad));

        return coordinador == null
            ? null
            : new MappedCoordinator(
                new Coordinator
                {
                    Name = NombreCompleto(coordinador.CooacadPrimerNombre, coordinador.CooacadPrimerApellido),
                    Email = coordinador.MailAcad?.Trim()
                },
                coordinador.CooacadCodigo);
    }

    private static MappedCoordinator? MapCourseCoordinator(IEnumerable<VdInscriptoCoordinadore> coordinadores)
    {
        var coordinador = coordinadores.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c.CoorespPrimerNombre)
            || !string.IsNullOrWhiteSpace(c.CoorespPrimerApellido)
            || !string.IsNullOrWhiteSpace(c.MailResp));

        return coordinador == null
            ? null
            : new MappedCoordinator(
                new Coordinator
                {
                    Name = NombreCompleto(coordinador.CoorespPrimerNombre, coordinador.CoorespPrimerApellido),
                    Email = coordinador.MailResp?.Trim()
                },
                coordinador.CoorespCodigo);
    }

    private static bool IsSameCoordinator(MappedCoordinator? academico, MappedCoordinator? cursos)
    {
        if (academico == null || cursos == null)
        {
            return false;
        }

        if (academico.Codigo.HasValue && cursos.Codigo.HasValue)
        {
            return academico.Codigo.Value == cursos.Codigo.Value;
        }

        return AreEquivalentTexts(academico.Coordinador.Email, cursos.Coordinador.Email)
            || AreEquivalentTexts(academico.Coordinador.Name, cursos.Coordinador.Name);
    }

    private static bool AreEquivalentTexts(string? textoA, string? textoB)
    {
        return !string.IsNullOrWhiteSpace(textoA)
            && !string.IsNullOrWhiteSpace(textoB)
            && string.Equals(textoA.Trim(), textoB.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? NombreCompleto(string? name, string? apellido)
    {
        var partes = new[] { name?.Trim(), apellido?.Trim() }
            .Where(p => !string.IsNullOrWhiteSpace(p));

        var completo = string.Join(" ", partes);
        return string.IsNullOrWhiteSpace(completo) ? null : completo;
    }

    /// <summary>Nombre de carrera a mostrar: web, si no el extenso, si no el corto.</summary>
    private static string? DegreeProgramName(Producto? product)
    {
        return product?.NombreWebProducto ?? product?.NombreExtensoProducto ?? product?.NombreProducto;
    }

    /// <summary>
    /// Límite anticorrupción de los mensajes de pago: <see cref="CartPaymentMessage"/> es el formato
    /// que emite la API de Inscripciones y Pagos (clave/valor en español); <see cref="PaymentMessage"/>
    /// es el que viaja al front.
    /// </summary>
    public static List<PaymentMessage> ToPaymentMessages(IEnumerable<CartPaymentMessage>? mensajes) =>
        (mensajes ?? []).Select(m => new PaymentMessage { Key = m.Key, Value = m.Value }).ToList();
}
