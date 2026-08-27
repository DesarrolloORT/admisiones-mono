using AppLogic.Enrollments.Constants;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Survey.Rules;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;
using AppLogic.Contracts.Constants;
using Microsoft.Extensions.Configuration;
using ModBandejaAppLogic.DevartDTOs;

namespace AppLogic.Enrollments.Rules;

internal static class PreEnrollmentConfirmationRules
{
    /// <summary>Niveles de producto terciario/universitario (ver vistas *3y4* en Devart); únicos habilitados para inscripción corporativa.</summary>
    private const long NivelProducto3 = 3;
    private const long NivelProducto4 = 4;

    /// <summary>
    /// Sin fallback a propósito: un id de estado de proceso equivocado no falla, deja filas de bandeja
    /// apuntando a un estado de otro ambiente.
    /// </summary>
    private static long RequiredId(IConfiguration configuration, string key) =>
        configuration.GetValue<long?>(key)
        ?? throw new InvalidOperationException($"Falta la clave de configuracion '{key}'.");

    /// <summary>Validación hoja: solo mira el request, sin tocar la base.</summary>
    public static PreEnrollmentRejection ValidateRequest(ConfirmPreEnrollmentRequest request)
    {
        if (request == null)
        {
            return PreEnrollmentRejection.InvalidRequest;
        }

        if (request.SelectedOfferingIds == null
            || request.SelectedOfferingIds.Count == 0
            || request.SelectedOfferingIds.Any(id => id <= 0))
        {
            return PreEnrollmentRejection.InvalidSelectedOfferings;
        }

        return PreEnrollmentRejection.None;
    }

    public static OperationResult<OfferingConfirmationData> GetOfferingConfirmationData(
        IUnitOfWork uow,
        long personId,
        Oferta offering,
        string methodName)
    {
        var selectedOfferingId = offering.IdOferta;
        var offeringProductId = offering.Supraoferta?.Paquete?.IdProducto ?? 0;
        var offeringIntakeId = offering.Supraoferta?.IdComienzo ?? 0;
        if (offeringProductId <= 0
            || offeringIntakeId <= 0
            || offering.IdTurno <= 0)
        {
            return OperationResult<OfferingConfirmationData>.IsFailed(
                "INS_CPI_08",
                methodName,
                "La oferta seleccionada no contiene producto, comienzo o turno validos.",
                400);
        }

        if (!string.Equals(offering.InscripcionesAbiertasOferta, SchemaConstants.BooleanFlag.Yes, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(offering.Supraoferta?.EstadoSupraoferta, SchemaConstants.ParentOfferingStatus.Final, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<OfferingConfirmationData>.IsFailed(
                "INS_CPI_16",
                methodName,
                "La oferta seleccionada no se encuentra abierta para inscripcion.",
                409);
        }

        // Nivel 3 y 4 no requieren encuesta inicial de admision: es un
        // requisito propio de nivel 1 y 2.
        var productLevelId = offering.Supraoferta?.Paquete?.Producto?.IdNivelProducto;
        if (productLevelId == NivelProducto3 || productLevelId == NivelProducto4)
        {
            return GetConfirmationDataFromInterest(uow, personId, offering, offeringProductId, selectedOfferingId, offeringIntakeId, methodName);
        }

        var admissionSurvey = uow.EncuestaIniAdmisions.GetByPersona(personId);
        if (admissionSurvey is not null && InitialSurveyState.IsComplete(admissionSurvey))
        {
            return BuildConfirmationDataFromFinalSurvey(
                uow,
                personId,
                offering,
                admissionSurvey,
                offeringProductId,
                offeringIntakeId,
                methodName);
        }

        var historicSurvey = uow.EncuestaInis.GetByPersona(personId);
        if (historicSurvey == null)
        {
            var errorCode = admissionSurvey == null ? "INS_CPI_06" : "INS_CPI_07";
            var message = admissionSurvey == null
                ? "No se encontro una encuesta inicial de admision vigente para la persona."
                : "La encuesta inicial debe estar completa para confirmar la preinscripcion.";
            var httpCode = admissionSurvey == null ? 404 : 409;

            return OperationResult<OfferingConfirmationData>.IsFailed(
                errorCode,
                methodName,
                message,
                httpCode);
        }

        return GetConfirmationDataFromInterest(uow, personId, offering, offeringProductId, selectedOfferingId, offeringIntakeId, methodName);
    }

    private static OperationResult<OfferingConfirmationData> GetConfirmationDataFromInterest(
        IUnitOfWork uow,
        long personId,
        Oferta offering,
        long offeringProductId,
        long selectedOfferingId,
        long offeringIntakeId,
        string methodName)
    {
        var admissionProcess = uow.InteresProductoOfertas.GetProcesoPorInteresActivoOferta(
            personId,
            offeringProductId,
            selectedOfferingId);
        if (admissionProcess == null || admissionProcess.IdProceso <= 0)
        {
            return OfferingInterestNotFoundError(methodName);
        }

        return OperationResult<OfferingConfirmationData>.Ok(
            new OfferingConfirmationData(
                selectedOfferingId,
                offeringProductId,
                admissionProcess.IdProceso,
                offeringIntakeId,
                offering.IdTurno,
                offering.Supraoferta!.Paquete!.Producto,
                offering.Supraoferta.Comienzo,
                offering.Turno),
            methodName);
    }

    public static ConfirmarPreInscripcionMultipleApiRequest CreateMultipleApiRequest(
        OfferingConfirmationData contexto,
        List<long> idsOfertasSeleccionadas)
    {
        return new ConfirmarPreInscripcionMultipleApiRequest
        {
            IdProducto = contexto.IdProducto,
            IdProceso = contexto.IdProceso,
            IdsOfertasSeleccionadas = idsOfertasSeleccionadas,
            TipoInscripcion = EnrollmentConstants.EnrollmentType.Online,
            Turno = new DtoTurno { IdTurno = contexto.IdTurno }
        };
    }

    /// <summary>La inscripción corporativa solo aplica a productos de nivel 3 o 4.</summary>
    public static bool IsLevelEnabledForCorporate(OfferingConfirmationData contexto)
    {
        var levelId = contexto.Producto?.IdNivelProducto;
        return levelId == NivelProducto3 || levelId == NivelProducto4;
    }

    // Un trámite (y un XML) por oferta, cada instancia describe una sola oferta.
    public static string BuildCorporateInstanceXml(OfferingConfirmationData contexto, Persona person)
    {
        var studentName = $"({person.CodigoPersona}) {person.PrimerNombre} {person.PrimerApellido}".Trim();
        return "<TAREA>" +
            $"<FIELD propertyName=\"Alumno\" name=\"Alumno\" data=\" {studentName}\"></FIELD>" +
            $"<FIELD propertyName=\"Comienzo\" name=\"Comienzo\" data=\" {contexto.Comienzo?.NombreComienzo}\"></FIELD>" +
            $"<FIELD propertyName=\"Producto\" name=\"Producto\" data=\" {contexto.Producto?.NombreProducto}\"></FIELD>" +
            $"<FIELD propertyName=\"Oferta\" name=\"Oferta Turno\" data=\" ({contexto.IdOferta}) {contexto.Turno?.NombreTurno}\"></FIELD>" +
            "<FIELD propertyName=\"Motivo\" name=\"Motivo\" data=\" Inscripcion corporativa\"></FIELD>" +
            "</TAREA>";
    }

    public static DtoTramiteBandejaDevartModBandeja CreateCorporateCase(long personId)
    {
        return new DtoTramiteBandejaDevartModBandeja
        {
            CodigoPersona = personId,
            IdGrupoResponsable = EnrollmentConstants.CorporateInbox.IdGrupoResponsable,
            IdProceso = EnrollmentConstants.CorporateInbox.IdProceso,
            ObservacionesTramiteBandeja = string.Empty,
            TitularTramiteBandeja = "Inscripcion corporativa",
        };
    }

    public static DtoInstanciaWorkflowDevartModBandeja CreateCorporateInstance(
        OfferingConfirmationData contexto,
        long personId,
        string xml)
    {
        var ahora = DateTime.Now;
        return new DtoInstanciaWorkflowDevartModBandeja
        {
            IdProceso = EnrollmentConstants.CorporateInbox.IdProceso,
            DescripcionInstanciaWorkflow = "Inscripcion corporativa",
            SolicitanteInstanciaWorkflow = personId,
            // IdObjetoInstanciaWorkflow es un solo long: referencia la primera oferta; el detalle completo va en el XML.
            IdObjetoInstanciaWorkflow = contexto.IdOferta,
            FechaVtoInstanciaWorkflow = ahora.AddDays(5),
            XmlInstanciaWorkflow = xml,
            IdDepartamento = contexto.Producto?.IdDepartamento,
        };
    }

    public static IEnumerable<DtoBandejaDevartModBandeja> CreateCorporateInboxes(
        IConfiguration configuration,
        string usuarioIngreso)
    {
        var ahora = DateTime.Now;

        return new[]
        {
            // Paso 1 (inicio de tramite): se auto-completa al instante para avanzar al paso real, igual que OrdenWF==1 en legacy (proceso 75).
            new DtoBandejaDevartModBandeja
            {
                IdEstadoProceso = RequiredId(configuration, EnrollmentConstants.CorporateInbox.ConfigKeys.IdEstadoProcesoInicio),
                IdGrupoResponsable = EnrollmentConstants.CorporateInbox.IdGrupoResponsable,
                FechaIngresoBandeja = ahora,
                FechaTomadoBandeja = ahora,
                UsuarioTomadoBandeja = usuarioIngreso,
                FechaRealizadoBandeja = ahora,
                FechaVencimientoBandeja = ahora,
                AccionMenu = "SIGUIENTE",
                UsuarioAccionMenu = usuarioIngreso,
                FechaAccionMenu = ahora,
                FechaReasignadoBandeja = ahora,
                ReasignadoPorBandeja = usuarioIngreso,
            },
            // Paso 2 (solicitud): queda pendiente de verdad para el grupo responsable.
            new DtoBandejaDevartModBandeja
            {
                IdEstadoProceso = RequiredId(configuration, EnrollmentConstants.CorporateInbox.ConfigKeys.IdEstadoProcesoSolicitud),
                IdGrupoResponsable = EnrollmentConstants.CorporateInbox.IdGrupoResponsable,
                FechaIngresoBandeja = ahora,
                FechaTomadoBandeja = DateTime.MinValue,
                UsuarioTomadoBandeja = string.Empty,
                FechaVencimientoBandeja = ahora.AddDays(5),
                FechaReasignadoBandeja = ahora,
                ReasignadoPorBandeja = usuarioIngreso,
            }
        };
    }

    // La inscripcion corporativa siempre va "a la espera": el front muestra la pantalla generica de ese
    // estado, que no trae detalle. El response solo necesita los flags; el detalle va en el XML de la instancia.
    public static ConfirmPreEnrollmentResponse MapCorporateResult()
    {
        return new ConfirmPreEnrollmentResponse
        {
            Confirmed = false,
            Waiting = true
        };
    }

    // Fila estructurada de la inscripcion corporativa.
    public static InstWorkflowInscripcion CreateCorporateWorkflowEnrollment(OfferingConfirmationData offering, long idInstancia)
    {
        return new InstWorkflowInscripcion
        {
            IdInstanciaWorkflow = idInstancia,
            IdOferta = offering.IdOferta,
            IdTurno = offering.IdTurno,
            IdComienzo = offering.IdComienzo,
            IdProducto = offering.IdProducto
        };
    }

    public static OperationResult<DtoAceptacionReglamentoEstDevart> EnsureStudentRegulationsAcceptance(
        IUnitOfWork uow,
        IDbConnectionContext dbConnectionContext,
        long personId,
        long productId,
        long idComienzo,
        bool aceptoReglamento,
        string methodName)
    {
        var existing = uow.AceptacionReglamentoEsts.GetByPersonaProductoComienzo(personId, productId, idComienzo);
        if (existing != null)
        {
            return OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(existing.ToDto(), methodName);
        }

        // El reglamento se acepta una sola vez por persona; si no lo acepta ahora pero ya lo
        // aceptó antes, se carga la fila para esta oferta sin volver a pedir aceptación.
        if (!aceptoReglamento
            && uow.AceptacionReglamentoEsts.GetByPersona(personId) == null)
        {
            return OperationResult<DtoAceptacionReglamentoEstDevart>.IsFailed(
                "INS_CPI_02",
                methodName,
                "Debe aceptar el reglamento estudiantil para confirmar la preinscripcion.",
                400);
        }

        var entidad = new AceptacionReglamentoEst
        {
            IdAceptacionReglamentoEst = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST),
            CodigoPersona = personId,
            IdProducto = productId,
            IdComienzo = idComienzo,
            IdSistema = SchemaConstants.AdmissionsSystemId
        };

        uow.BeginTransaction();
        try
        {
            uow.AceptacionReglamentoEsts.Add(entidad);
            uow.Save();
            uow.Commit();
        }
        catch
        {
            uow.Rollback();
            throw;
        }

        return OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(entidad.ToDto(), methodName);
    }

    private static OperationResult<OfferingConfirmationData> BuildConfirmationDataFromFinalSurvey(
        IUnitOfWork uow,
        long personId,
        Oferta offering,
        EncuestaIniAdmision admissionSurvey,
        long offeringProductId,
        long offeringIntakeId,
        string methodName)
    {
        // Una encuesta ya cerrada (DEFINITIVO) no vence: la confirmacion que la sello puede estar
        // esperando en bandeja, y ahi el vencimiento no es del postulante.
        var alreadySealed = string.Equals(
            admissionSurvey.EstadoEncuestaIniAdmision,
            InitialSurveyState.EstadoDefinitivo,
            StringComparison.OrdinalIgnoreCase);

        if (admissionSurvey.FechaVtoAdmision.HasValue
            && admissionSurvey.FechaVtoAdmision.Value.Date < DateTime.Today
            && !alreadySealed
            && uow.EncuestaInis.GetByPersona(personId) == null)
        {
            return OperationResult<OfferingConfirmationData>.IsFailed(
                "INS_CPI_12",
                methodName,
                "La encuesta inicial de admision se encuentra vencida.",
                409);
        }

        if (!HasValidConfirmationData(admissionSurvey.IdProducto, admissionSurvey.IdProceso, admissionSurvey.IdComienzo))
        {
            return OperationResult<OfferingConfirmationData>.IsFailed(
                "INS_CPI_08",
                methodName,
                "La encuesta inicial de admision no contiene producto, proceso o comienzo validos.",
                400);
        }

        var interestProcess = uow.InteresProductoOfertas.GetProcesoPorInteresActivoOferta(
            personId,
            offeringProductId,
            offering.IdOferta);
        if (interestProcess == null || interestProcess.IdProceso <= 0)
        {
            return OfferingInterestNotFoundError(methodName);
        }

        return OperationResult<OfferingConfirmationData>.Ok(
            new OfferingConfirmationData(
                offering.IdOferta,
                offeringProductId,
                interestProcess.IdProceso,
                offeringIntakeId,
                offering.IdTurno,
                offering.Supraoferta!.Paquete!.Producto ?? admissionSurvey.Producto,
                offering.Supraoferta.Comienzo ?? admissionSurvey.Comienzo,
                offering.Turno),
            methodName);
    }

    internal static decimal AddDepositPayment(IEnumerable<CarritoPagoReservaApiDto>? carritos)
    {
        return carritos?.Sum(c => c.PagoReserva) ?? 0;
    }

    private static OperationResult<OfferingConfirmationData> OfferingInterestNotFoundError(string methodName)
    {
        return OperationResult<OfferingConfirmationData>.IsFailed(
            "INS_CPI_14",
            methodName,
            "No existe interes activo para la oferta seleccionada.",
            409);
    }

    private static bool HasValidConfirmationData(long? productId, long? admissionProcessId, long? idComienzo)
    {
        return HasPositiveValue(productId)
            && HasPositiveValue(admissionProcessId)
            && HasPositiveValue(idComienzo);
    }

    private static bool HasPositiveValue(long? valor)
    {
        return valor.HasValue && valor.Value > 0;
    }
}

internal sealed record OfferingConfirmationData(
    long IdOferta,
    long IdProducto,
    long IdProceso,
    long IdComienzo,
    long IdTurno,
    Producto? Producto,
    Comienzo? Comienzo,
    Turno? Turno);
