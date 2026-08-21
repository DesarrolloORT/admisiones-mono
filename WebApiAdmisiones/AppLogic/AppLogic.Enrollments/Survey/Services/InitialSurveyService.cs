using AppLogic.Enrollments.Constants;
using AppLogic.Identity;
using AppLogic.Contracts.Text;
using AppLogic.People.Constants;
using AppLogic.Enrollments.Survey.Dtos;
using AppLogic.Enrollments.Survey.Mapping;
using AppLogic.Enrollments.Survey.Rules;
using AppLogic.Enrollments.Survey.Validators;
using AppLogic.Enrollments.Interfaces;
using AppLogic.Integrations.Tivenos.Dtos;
using AppLogic.Integrations.Tivenos.Interfaces;
using AppLogic.Contracts;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;
using AppLogic.Contracts.Constants;

namespace AppLogic.Enrollments.Survey.Services;

public sealed class InitialSurveyService(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext,
    IAdmissionDueDateCalculator generalService,
    ITivenosQueueService tivenosEnvioService)
    : IInitialSurveyService
{
    private const long CodigoOrientacionQuintoLegacy = 1304;

    public OperationResult<GetInitialSurveyResponse> GetInitialSurvey(long personId)
    {
        using var uow = uowFactory.Create();

        var person = uow.Personas.GetByKey(personId);
        if (person == null)
            return OperationResult<GetInitialSurveyResponse>.IsFailed(
                "GEN_OEI_01",
                nameof(GetInitialSurvey),
                "Persona no encontrada.",
                404);

        var documentValidation = IdentityDocumentRules.ValidateBaseDocument(person.TipoDocumento, person.Documento);
        if (!documentValidation.IsValid)
            return OperationResult<GetInitialSurveyResponse>.IsFailed(
                "GEN_OEI_02",
                nameof(GetInitialSurvey),
                documentValidation.Message,
                400);

        var documentType = TextNormalization.Trim(person.TipoDocumento);
        var document = TextNormalization.Trim(person.Documento);
        if (!CanAnswerInitialSurvey(documentType, document, uow))
            return OperationResult<GetInitialSurveyResponse>.IsSuccess(
                new GetInitialSurveyResponse { CanAnswerSurvey = false },
                nameof(GetInitialSurvey),
                "La persona no tiene derecho a encuesta inicial.",
                200);

        var survey = uow.EncuestaIniAdmisions.GetByPersona(personId);
        if (survey == null)
            return OperationResult<GetInitialSurveyResponse>.IsSuccess(
                new GetInitialSurveyResponse { CanAnswerSurvey = true },
                nameof(GetInitialSurvey),
                "La persona no tiene encuesta inicial.",
                200);

        // Procesada por LogicaORT: la encuesta ya se migró a T_ENCUESTA_INI y no se muestra más.
        if (!InitialSurveyState.IsEditable(survey))
            return OperationResult<GetInitialSurveyResponse>.IsSuccess(
                new GetInitialSurveyResponse { CanAnswerSurvey = false },
                nameof(GetInitialSurvey),
                "La encuesta inicial ya fue procesada con la preinscripcion.",
                200);

        var pendientes = InitialSurveyValidation.ValidateCompletenessFromDb(uow, survey, personId);
        return OperationResult<GetInitialSurveyResponse>.Ok(
            new GetInitialSurveyResponse
            {
                CanAnswerSurvey = true,
                Survey = InitialSurveyMapper.MapForRead(uow, survey, personId, pendientes)
            },
            nameof(GetInitialSurvey));
    }

    public OperationResult<SaveInitialSurveyResponse> SaveInitialSurvey(
        long personId,
        SaveInitialSurveyRequest request)
    {
        using var uow = uowFactory.Create();

        var person = uow.Personas.GetByKey(personId);
        if (person == null)
        {
            return OperationResult<SaveInitialSurveyResponse>.IsFailed(
                "INS_EI_01",
                nameof(SaveInitialSurvey),
                PersonConstants.PersonaNoEncontradaMessage,
                404);
        }

        var documentValidation = IdentityDocumentRules.ValidateBaseDocument(person.TipoDocumento, person.Documento);
        if (!documentValidation.IsValid)
        {
            return OperationResult<SaveInitialSurveyResponse>.IsFailed(
                "INS_EI_55",
                nameof(SaveInitialSurvey),
                documentValidation.Message,
                400);
        }

        var documentType = TextNormalization.Trim(person.TipoDocumento);
        var document = TextNormalization.Trim(person.Documento);
        if (!CanAnswerInitialSurvey(documentType, document, uow))
        {
            return OperationResult<SaveInitialSurveyResponse>.IsFailed(
                "INS_EI_56",
                nameof(SaveInitialSurvey),
                "La persona no tiene derecho a guardar encuesta inicial.",
                403);
        }

        var rechazo = InitialSurveyCatalogValidation.ValidatePartialRequest(uow, request);
        if (rechazo != InitialSurveyRejection.None)
        {
            return rechazo.ToFailure<SaveInitialSurveyResponse>(nameof(SaveInitialSurvey));
        }

        var contexto = ResolveAdmissionContext(uow, personId, request);
        if (!contexto.Success)
        {
            return contexto.Failure().As<SaveInitialSurveyResponse>(nameof(SaveInitialSurvey));
        }

        var survey = GetSurveyToSave(uow, personId, request, contexto.Data);
        if (!InitialSurveyState.IsEditable(survey))
        {
            return OperationResult<SaveInitialSurveyResponse>.IsFailed(
                "INS_EI_57",
                nameof(SaveInitialSurvey),
                "La encuesta inicial ya fue procesada con la preinscripcion y no admite cambios.",
                409);
        }

        var isNew = survey == null;
        survey ??= CreateInitialSurvey(person, personId);

        uow.BeginTransaction();
        try
        {
            InitialSurveyMapper.ApplyTechnicalData(
                survey,
                person,
                personId,
                request.DegreeProgramId ?? survey.IdProducto,
                request.AdmissionProcessId ?? survey.IdProceso,
                contexto.Data?.IdComienzo ?? survey.IdComienzo,
                contexto.Data?.IdTurno ?? survey.IdTurno);
            InitialSurveyMapper.ApplyEducation(uow, survey, request);
            InitialSurveyMapper.ApplyAcademicDecision(survey, request);
            InitialSurveyMapper.ApplyOrtExperience(survey, request);

            if (isNew)
                uow.EncuestaIniAdmisions.Add(survey);
            else
                uow.EncuestaIniAdmisions.Update(survey);

            InitialSurveyChildRecords.ApplyChildLists(uow, dbConnectionContext, personId, request);

            uow.Save();

            var persistedSurvey = uow.EncuestaIniAdmisions.GetByKey(survey.IdEncuestaIni) ?? survey;
            var response = InitialSurveyValidation.ValidateCompletenessFromDb(uow, persistedSurvey, personId);

            survey.EstadoEncuestaIniAdmision = response.Status;
            if (response.Status == InitialSurveyState.EstadoDefinitivo)
            {
                var finalization = FinalizeDefinitiveSurvey(uow, survey, personId);
                if (!finalization.Success)
                {
                    uow.Rollback();
                    return finalization.Failure().As<SaveInitialSurveyResponse>(nameof(SaveInitialSurvey));
                }
            }

            uow.EncuestaIniAdmisions.Update(survey);
            uow.Save();
            uow.Commit();

            return OperationResult<SaveInitialSurveyResponse>.Ok(response, nameof(SaveInitialSurvey));
        }
        catch
        {
            uow.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Derecho a encuesta: no ser fresco del proceso legacy y no tener encuesta histórica.
    /// El estado DEFINITIVO no se consulta acá a propósito: significa "completa", no "cerrada".
    /// El cierre lo marca FECHA_PROCESADO_ENCUESTA_INI, que sella LogicaORT cuando la confirmación
    /// de preinscripción copia la encuesta a T_ENCUESTA_INI.
    /// </summary>
    private static bool CanAnswerInitialSurvey(string documentType, string document, IUnitOfWork uow)
    {
        if (uow.VdEsFrescoAdmisions?.ExistePorDocumento(documentType, document) == true)
            return false;

        return uow.EncuestaInis?.ExistePorDocumento(documentType, document) != true;
    }

    private static OperationResult<AdmissionContext?> ResolveAdmissionContext(
        IUnitOfWork uow,
        long personId,
        SaveInitialSurveyRequest request)
    {
        if (!request.DegreeProgramId.HasValue && !request.AdmissionProcessId.HasValue)
            return OperationResult<AdmissionContext?>.Ok(null, nameof(SaveInitialSurvey));

        if (!request.DegreeProgramId.HasValue || !request.AdmissionProcessId.HasValue)
            return OperationResult<AdmissionContext?>.Ok(null, nameof(SaveInitialSurvey));

        var offerings = uow.InteresProductoOfertas.GetOfertasSeleccionadas(
            personId,
            request.DegreeProgramId.Value,
            request.AdmissionProcessId.Value);

        if (offerings.Count == 0)
        {
            return OperationResult<AdmissionContext?>.IsFailed(
                "INS_EI_53",
                nameof(SaveInitialSurvey),
                "No existe oferta/interes activo para la persona, producto y proceso indicados.",
                404);
        }

        if (offerings.Count > 1)
        {
            return OperationResult<AdmissionContext?>.IsFailed(
                "INS_EI_54",
                nameof(SaveInitialSurvey),
                "Existe mas de una oferta/interes activo para la persona, producto y proceso indicados.",
                409);
        }

        var offering = offerings.Single();
        return OperationResult<AdmissionContext?>.Ok(
            new AdmissionContext(
                offering.Supraoferta?.IdComienzo,
                offering.IdTurno),
            nameof(SaveInitialSurvey));
    }

    private static EncuestaIniAdmision? GetSurveyToSave(
        IUnitOfWork uow,
        long personId,
        SaveInitialSurveyRequest request,
        AdmissionContext? contexto)
    {
        if (request.DegreeProgramId.HasValue && contexto?.IdComienzo.HasValue == true)
        {
            var surveyByProductAndIntake = uow.EncuestaIniAdmisions.GetByPersonaProductoComienzo(
                personId,
                request.DegreeProgramId.Value,
                contexto.IdComienzo.Value);
            if (surveyByProductAndIntake != null)
                return surveyByProductAndIntake;
        }

        return uow.EncuestaIniAdmisions.GetByPersona(personId);
    }

    private EncuestaIniAdmision CreateInitialSurvey(Persona person, long personId)
    {
        return new EncuestaIniAdmision
        {
            IdEncuestaIni = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION),
            FechaEncuestaIni = dbConnectionContext.CurrentDateTime(),
            TipoDocumento = person.TipoDocumento,
            Documento = person.Documento,
            CodigoPersona = personId,
            TipoInscripcion = InitialSurveyState.TipoInscripcionSoloEncuesta,
            NuevaversionEncuestaIni = SchemaConstants.BooleanFlag.Yes,
            EstadoEncuestaIniAdmision = InitialSurveyState.EstadoTemporal
        };
    }

    private OperationResult<bool> FinalizeDefinitiveSurvey(
        IUnitOfWork uow,
        EncuestaIniAdmision survey,
        long personId)
    {
        if (survey.IdProceso.HasValue && survey.IdProducto.HasValue)
        {
            var dueDateResult = generalService.CalculateAdmissionDueDate(uow, personId, survey.IdProceso.Value);
            if (!dueDateResult.Success)
            {
                return dueDateResult.Failure().As<bool>(nameof(SaveInitialSurvey));
            }

            survey.FechaVtoAdmision = dueDateResult.Data;
        }

        return InitialSurveyState.YesNoToBool(survey.CursaSecundariaActualmenteEncuestaIni) == false
            ? OperationResult<bool>.Ok(true, nameof(SaveInitialSurvey))
            : SyncPersonHighSchool(uow, survey, personId, nameof(SaveInitialSurvey));
    }

    private OperationResult<bool> SyncPersonHighSchool(
        IUnitOfWork uow,
        EncuestaIniAdmision survey,
        long personId,
        string methodName)
    {
        var data = GetFinalHighSchoolData(survey, methodName);
        if (!data.Success)
        {
            return data.Failure().As<bool>(methodName);
        }

        var highSchoolData = data.Data!;
        var currentDate = dbConnectionContext.CurrentDateTime();
        var existing = uow.BachilleratoPersonas.GetByKey(personId);
        if (existing == null)
        {
            uow.BachilleratoPersonas.Add(new BachilleratoPersona
            {
                CodigoPersona = personId,
                CodigoInstitucion = highSchoolData.CodigoInstitucion,
                AnioBachillerPer = highSchoolData.AnioBachiller,
                CodigoOrientacion = highSchoolData.CodigoOrientacion,
                ActualizacionBachillerPer = currentDate
            });

            tivenosEnvioService.EnqueueHighSchoolDataCreation(
                uow,
                new DtoTivenosBachilleratoRequest
                {
                    CodigoPersona = personId,
                    CodigoOrientacion = highSchoolData.CodigoOrientacion
                },
                dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS));
            return OperationResult<bool>.Ok(true, methodName);
        }

        if (!HighSchoolChanged(existing, highSchoolData))
            return OperationResult<bool>.Ok(false, methodName);

        existing.CodigoInstitucion = highSchoolData.CodigoInstitucion;
        existing.AnioBachillerPer = highSchoolData.AnioBachiller;
        existing.CodigoOrientacion = highSchoolData.CodigoOrientacion;
        existing.ActualizacionBachillerPer = currentDate;
        uow.BachilleratoPersonas.Update(existing);

        tivenosEnvioService.EnqueueHighSchoolDataUpdate(
            uow,
            new DtoTivenosBachilleratoRequest
            {
                CodigoPersona = personId,
                CodigoOrientacion = highSchoolData.CodigoOrientacion
            },
            dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS));
        return OperationResult<bool>.Ok(true, methodName);
    }

    private static OperationResult<PersonHighSchoolData> GetFinalHighSchoolData(
        EncuestaIniAdmision survey,
        string methodName)
    {
        if (!long.TryParse(survey.UltimoAnioSextoEncuestaIni, out var ultimoAnio))
        {
            return OperationResult<PersonHighSchoolData>.IsFailed(
                "INS_EI_36",
                methodName,
                "No se pudo resolver el anio de bachillerato para la persona.",
                400);
        }

        ultimoAnio = ResolveLegacyLastHighSchoolYear(ultimoAnio);
        if (ultimoAnio is < 4 or > 6)
        {
            return OperationResult<PersonHighSchoolData>.IsFailed(
                "INS_EI_37",
                methodName,
                "Anio de bachillerato invalido para la persona.",
                400);
        }

        var trackCode = ultimoAnio switch
        {
            4 => null,
            5 => CodigoOrientacionQuintoLegacy,
            6 => survey.CodigoTitulo,
            _ => null
        };

        if (ultimoAnio == 6 && (!trackCode.HasValue || trackCode.Value <= 0))
        {
            return OperationResult<PersonHighSchoolData>.IsFailed(
                "INS_EI_38",
                methodName,
                "No se pudo resolver la orientacion de bachillerato para la persona.",
                400);
        }

        return OperationResult<PersonHighSchoolData>.Ok(
            new PersonHighSchoolData(survey.CodigoInstitucionBac, ultimoAnio.ToString(), trackCode),
            methodName);
    }

    private static long ResolveLegacyLastHighSchoolYear(long value)
        => value is >= 10 and <= 12 ? value - 6 : value;

    private static bool HighSchoolChanged(BachilleratoPersona existing, PersonHighSchoolData data)
    {
        return existing.CodigoInstitucion != data.CodigoInstitucion
            || !string.Equals(existing.AnioBachillerPer, data.AnioBachiller, StringComparison.Ordinal)
            || existing.CodigoOrientacion != data.CodigoOrientacion;
    }

    private sealed record AdmissionContext(long? IdComienzo, long? IdTurno);

    private sealed record PersonHighSchoolData(
        long? CodigoInstitucion,
        string AnioBachiller,
        long? CodigoOrientacion);
}
