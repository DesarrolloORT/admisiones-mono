using AppLogic.ApiClients;
using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.Helpers.ValidationHelpers;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Inscripciones;
using AppLogic.Services.Personas;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using System.Security.Cryptography;
using System.Text;
using Utilities;

namespace AppLogic.Services.Inscripciones
{
    public class InscripcionesService : IInscripcionesService
    {
        private const string EstadoTemporal = "TEMPORAL";
        private const string EstadoDefinitivo = "DEFINITIVO";

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly IGeneralService _generalService;
        private readonly InscripcionesyPagosApiClient _inscripcionesyPagosApiClient;

        public InscripcionesService(
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            IGeneralService generalService,
            InscripcionesyPagosApiClient inscripcionesyPagosApiClient)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _generalService = generalService;
            _inscripcionesyPagosApiClient = inscripcionesyPagosApiClient;
        }

        public OperationResult<DtoUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscripto = uow.Inscriptos.GetUltimaInscripcionActiva(codigoPersona);
            if (inscripto == null)
            {
                return OperationResult<DtoUltimaInscripcion>.IsFailed(
                    "GEN_UI_01",
                    nameof(ObtenerUltimaInscripcionActiva),
                    "No se encontró inscripción para la persona.",
                    204);
            }

            var dto = new DtoUltimaInscripcion
            {
                IdInscripto = inscripto.IdInscripto,
                IdProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                NombreProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreProducto,
                NombreExtensoProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                IdComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.IdComienzo ?? 0,
                NombreComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
            };
            return OperationResult<DtoUltimaInscripcion>.Ok(dto, nameof(ObtenerUltimaInscripcionActiva));
        }
        public OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.VdEsFrescoAdmisions.TieneInscripcionActivaParaProceso(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionActivaParaProceso));
        }

        public OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.Inscriptos.TieneInscripcionAdmisiones(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionAdmisiones));
        }

        #region PASO 1 - REGISTRAR INTERES POR PRODUCTO
        public OperationResult<bool> RegistrarInteresProducto(long codigoPersona, InteresProductoRequest request)
        {
            using var uow = _uowFactory.Create();

            var validacion = InteresProductoValidationHelper.ValidarRegistroInteresProducto(
                uow,
                codigoPersona,
                request.IdProducto,
                request.IdProcesoSeleccionado,
                nameof(RegistrarInteresProducto));

            if (!validacion.Success)
            {
                return validacion;
            }

            var fechaActual = DateTime.Now;
            var intereses = uow.Interes.GetInteresesPersonaProcesosHabilitados(codigoPersona).ToList();

            uow.BeginTransaction();

            try
            {
                ResetearInteresesProductos(uow, intereses, fechaActual);

                var interesExistente = intereses.FirstOrDefault(i => i.IdProceso == request.IdProcesoSeleccionado);
                if (interesExistente == null)
                {
                    interesExistente = CrearInteres(uow, codigoPersona, request.IdProcesoSeleccionado);
                }

                ActivarInteresProducto(uow, interesExistente, request.IdProducto, fechaActual);
                // TODO Tivenos: encolar AltaInteresXSeleccionEnSitio para el interes producto registrado.
                AsegurarPersonaAdmite(uow, codigoPersona, fechaActual);
                uow.InteresProductoOfertas.Add(
                    InteresProductoEntityFactoryHelper.CrearInteresProductoOferta((long)interesExistente.IdInteres, request.IdProducto, request.IdOferta));
                var resultadoEncuesta = ActualizarEncuestaInicial(uow, codigoPersona, request.IdProducto, request.IdProcesoSeleccionado);
                if (!resultadoEncuesta.Success)
                {
                    uow.Rollback();
                    return resultadoEncuesta;
                }

                uow.Commit();

                return OperationResult<bool>.Ok(true, nameof(RegistrarInteresProducto));
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        private static void ResetearInteresesProductos(IUnitOfWork uow, IEnumerable<Intere> intereses, DateTime fechaActual)
        {
            foreach (var interes in intereses)
            {
                foreach (var interesProducto in interes.InteresProductos)
                {
                    var interesProductoActual = uow.InteresProductos.GetByKey(interesProducto.IdInteres, interesProducto.IdProducto);
                    if (interesProductoActual == null)
                    {
                        continue;
                    }

                    if (interesProductoActual.IdGradoInteres == Constantes.kGRADO_INTERES_INSCRIPTO)
                    {
                        continue;
                    }

                    interesProductoActual.IdGradoInteresAnt = interesProductoActual.IdGradoInteres;
                    interesProductoActual.IdGradoInteres = Constantes.kGRADO_INTERES_DESINTERESADO;
                    interesProductoActual.UsuarioModifInteresProd = Constantes.kUSERNAME_USUARIO_ADMISIONES;
                    interesProductoActual.FechaModifInteresProd = fechaActual;
                    interesProductoActual.IdgradoantModifInteresProd = interesProductoActual.IdGradoInteresAnt;
                    uow.InteresProductos.Update(interesProductoActual);
                }
            }
        }

        private Intere CrearInteres(IUnitOfWork uow, long codigoPersona, long idProceso)
        {
            var interes = InteresProductoEntityFactoryHelper.CrearInteres(
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES),
                codigoPersona,
                idProceso);
            uow.Interes.Add(interes);
            return interes;
        }

        private static void ActivarInteresProducto(IUnitOfWork uow, Intere interes, long idProducto, DateTime fechaActual)
        {
            var interesProductoExistente = interes.InteresProductos.FirstOrDefault(ip => ip.IdProducto == idProducto);
            if (interesProductoExistente == null)
            {
                uow.InteresProductos.Add(
                    InteresProductoEntityFactoryHelper.CrearInteresProducto(interes.IdInteres, idProducto, fechaActual));
                return;
            }

            var interesProductoActual = uow.InteresProductos.GetByKey(interes.IdInteres, idProducto);
            if (interesProductoActual == null || interesProductoActual.IdGradoInteres == Constantes.kGRADO_INTERES_INSCRIPTO)
            {
                return;
            }

            interesProductoActual.IdGradoInteresAnt = interesProductoActual.IdGradoInteres;
            interesProductoActual.IdGradoInteres = Constantes.kGRADO_INTERES_ALTO;
            interesProductoActual.FechaInteresProd = fechaActual;
            interesProductoActual.UsuarioModifInteresProd = Constantes.kUSERNAME_USUARIO_ADMISIONES;
            interesProductoActual.FechaModifInteresProd = fechaActual;
            interesProductoActual.IdgradoantModifInteresProd = interesProductoActual.IdGradoInteresAnt;
            uow.InteresProductos.Update(interesProductoActual);
        }

        private static void AsegurarPersonaAdmite(IUnitOfWork uow, long codigoPersona, DateTime fechaActual)
        {
            var personaAdmite = uow.PersonaAdmites.GetByKey(codigoPersona);
            if (personaAdmite == null)
            {
                uow.PersonaAdmites.Add(
                    InteresProductoEntityFactoryHelper.CrearPersonaAdmite(codigoPersona, fechaActual));
                return;
            }

            if (!personaAdmite.FechaFrescoPersonaAdmite.HasValue)
            {
                personaAdmite.FechaFrescoPersonaAdmite = fechaActual;
                uow.PersonaAdmites.Update(personaAdmite);
            }
        }
        private static OperationResult<bool> ActualizarEncuestaInicial(IUnitOfWork uow, long codigoPersona, long idProducto, long idProceso)
        {
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
            {
                return OperationResult<bool>.Ok(true, nameof(RegistrarInteresProducto));
            }

            var idComienzo = uow.ProcesoComienzos.GetComienzoActivoPorProcesoOProducto(idProducto, idProceso);
            if (!idComienzo.HasValue || idComienzo.Value == 0)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_IP_06",
                    nameof(RegistrarInteresProducto),
                    $"No existe comienzo activo, producto:{idProducto} proceso:{idProceso}.",
                    400);
            }

            encuesta.IdProceso = idProceso;
            encuesta.IdComienzo = idComienzo.Value;
            return OperationResult<bool>.Ok(true, nameof(RegistrarInteresProducto));
        }

        #endregion PASO 1 - REGISTRAR INTERES POR PRODUCTO

        #region PASO 2 - ENCUESTA INICIAL y PREINSCRIPCIÓN

        public OperationResult<DtoEncuestaInicialAdmisionResponse> ObtenerEncuestaInicial(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
                return OperationResult<DtoEncuestaInicialAdmisionResponse>.IsFailed(
                    "GEN_OEI_01",
                    nameof(ObtenerEncuestaInicial),
                    "Persona no encontrada.",
                    404);

            var validacionDocumento = DocumentUtils.ValidarDocumentoBase(persona.TipoDocumento, persona.Documento);
            if (!validacionDocumento.IsValid)
                return OperationResult<DtoEncuestaInicialAdmisionResponse>.IsFailed(
                    "GEN_OEI_02",
                    nameof(ObtenerEncuestaInicial),
                    validacionDocumento.Message,
                    400);

            var tipoDocumento = DocumentUtils.Normalizar(persona.TipoDocumento);
            var documento = DocumentUtils.Normalizar(persona.Documento);

            if (!TieneDerechoAEncuestaInicial(tipoDocumento, documento, uow))
                return OperationResult<DtoEncuestaInicialAdmisionResponse>.IsSuccess(
                    new DtoEncuestaInicialAdmisionResponse { TieneDerechoEncuesta = false },
                    nameof(ObtenerEncuestaInicial),
                    "La persona no tiene derecho a encuesta inicial.",
                    200);

            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
                return OperationResult<DtoEncuestaInicialAdmisionResponse>.IsSuccess(
                    new DtoEncuestaInicialAdmisionResponse { TieneDerechoEncuesta = true },
                    nameof(ObtenerEncuestaInicial),
                    "La Persona ya completó la encuesta inicial.",
                    200);

            return OperationResult<DtoEncuestaInicialAdmisionResponse>.Ok(
                new DtoEncuestaInicialAdmisionResponse
                {
                    TieneDerechoEncuesta = true,
                    Encuesta = encuesta.ToDtoWithRelated(1),
                    UniversidadesConsideradas = uow.EmpresaConsideradaAdmisions.GetByPersona(codigoPersona).ToDtos(),
                    UniversidadesEducacionSuperior = uow.EducacionSuperiorAdmisions.GetByPersona(codigoPersona).ToDtos(),
                    OpcionesMotivosSeleccionados = uow.MotivoEleccionAdmisions.GetByPersona(codigoPersona).ToDtos(),
                    OpcionesPublicidadSeleccionadas = uow.PublicidadEleccionAdmisions.GetByPersona(codigoPersona).ToDtos()
                },
                nameof(ObtenerEncuestaInicial));
        }

        public OperationResult<bool> GuardarEncuestaInicial(long codigoPersona, GuardarEncuestaInicialRequest request)
        {
            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
            {
                return OperationResult<bool>.IsFailed(
                    "INS_EI_01",
                    nameof(GuardarEncuestaInicial),
                    PersonaConstants.PersonaNoEncontradaMessage,
                    404);
            }

            var validacionConsistencia = EncuestaInicialValidationHelper.ValidarConsistenciaParcial(uow, request, nameof(GuardarEncuestaInicial));
            if (!validacionConsistencia.Success)
            {
                return validacionConsistencia;
            }

            var encuesta = ObtenerEncuestaParaGuardar(uow, codigoPersona, request);
            var esNueva = encuesta == null;
            encuesta ??= CrearEncuestaInicial(persona, codigoPersona);

            var idProducto = request.IdProducto ?? encuesta.IdProducto;
            var idProceso = request.IdProceso ?? encuesta.IdProceso;
            long? idComienzo = encuesta.IdComienzo;
            if (idProducto.HasValue && idProceso.HasValue)
            {
                var idComienzoResult = EncuestaInicialValidationHelper.ObtenerIdComienzoValido(uow, idProducto.Value, idProceso.Value, nameof(GuardarEncuestaInicial));
                if (!idComienzoResult.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        idComienzoResult.ErrorCode,
                        nameof(GuardarEncuestaInicial),
                        idComienzoResult.Message,
                        idComienzoResult.HttpCode);
                }

                idComienzo = idComienzoResult.Data;
            }

            AplicarRequestAEncuesta(encuesta, request, persona, idComienzo);

            uow.BeginTransaction();
            try
            {
                if (esNueva)
                {
                    uow.EncuestaIniAdmisions.Add(encuesta);
                }

                AplicarListasHijas(uow, codigoPersona, request);
                uow.Save();

                var completitud = EncuestaInicialValidationHelper.ResolverCompletitud(uow, encuesta, codigoPersona, nameof(GuardarEncuestaInicial));
                if (!completitud.Success)
                {
                    uow.Rollback();
                    return OperationResult<bool>.IsFailed(
                        completitud.ErrorCode,
                        nameof(GuardarEncuestaInicial),
                        completitud.Message,
                        completitud.HttpCode);
                }

                encuesta.EstadoEncuestaIniAdmision = completitud.Data ? EstadoDefinitivo : EstadoTemporal;
                if (encuesta.IdProceso.HasValue && encuesta.IdProducto.HasValue)
                {
                    var fechaVencimientoResult = _generalService.CalcularFechaVencimientoAdmisiones(codigoPersona, encuesta.IdProceso.Value);
                    if (!fechaVencimientoResult.Success)
                    {
                        uow.Rollback();
                        return OperationResult<bool>.IsFailed(
                            fechaVencimientoResult.ErrorCode,
                            nameof(GuardarEncuestaInicial),
                            fechaVencimientoResult.Message,
                            fechaVencimientoResult.HttpCode);
                    }

                    encuesta.FechaVtoAdmision = fechaVencimientoResult.Data;
                }

                if (!esNueva)
                {
                    uow.EncuestaIniAdmisions.Update(encuesta);
                }

                uow.Commit();
                return OperationResult<bool>.Ok(true, nameof(GuardarEncuestaInicial));
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        public async Task<OperationResult<ConfirmarPreInscripcionResponse>> ConfirmarPreInscripcion(long codigoPersona, ConfirmarPreInscripcionRequest request)
        {
            const string methodName = nameof(ConfirmarPreInscripcion);

            if (request == null)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_01", methodName, "Request invalido.", 400);
            }

            if (!request.AceptoReglamento)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_02", methodName, "Debe aceptar el reglamento estudiantil para confirmar la preinscripcion.", 400);
            }

            if (request.IdOfertaSeleccionada <= 0)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_03", methodName, "La oferta seleccionada es invalida.", 400);
            }

            if (request.IdTurno <= 0)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_04", methodName, "El turno seleccionado es invalido.", 400);
            }

            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_05", methodName, PersonaConstants.PersonaNoEncontradaMessage, 404);
            }

            var contextoResult = ObtenerContextoConfirmacion(uow, codigoPersona, request.IdOfertaSeleccionada, methodName);
            if (!contextoResult.Success)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed(
                    contextoResult.ErrorCode,
                    methodName,
                    contextoResult.Message,
                    contextoResult.HttpCode);
            }

            var contexto = contextoResult.Data!;
            var validacionDocumentos = DocumentoIdentidadPersonaService.ValidarDocumentosIdentidadParaConfirmacion(
                uow,
                persona,
                methodName);
            if (!validacionDocumentos.Success)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed(validacionDocumentos.ErrorCode, methodName, validacionDocumentos.Message, validacionDocumentos.HttpCode);
            }

            var aceptacion = AsegurarAceptacionReglamentoEstudiantil(
                uow,
                codigoPersona,
                contexto.IdProducto,
                contexto.IdComienzo,
                methodName);
            if (!aceptacion.Success)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed(aceptacion.ErrorCode, methodName, aceptacion.Message, aceptacion.HttpCode);
            }

            var apiRequest = new ConfirmarPreInscripcionApiRequest
            {
                IdProducto = contexto.IdProducto,
                IdProceso = contexto.IdProceso,
                IdOfertaSeleccionada = request.IdOfertaSeleccionada,
                TipoInscripcion = string.IsNullOrWhiteSpace(request.TipoInscripcion) ? "ONLINE" : request.TipoInscripcion.Trim(),
                Turno = new DtoTurno { IdTurno = request.IdTurno }
            };

            var apiResult = await _inscripcionesyPagosApiClient.ConfirmarPreInscripcionAsync(apiRequest);
            if (!apiResult.Success)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed(apiResult.ErrorCode, methodName, apiResult.Message, apiResult.HttpCode);
            }

            if (apiResult.Data == null)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_13", methodName, "La API interna no devolvio datos de confirmacion.", 502);
            }

            return OperationResult<ConfirmarPreInscripcionResponse>.Ok(
                MapearConfirmacionPreInscripcion(apiResult.Data, contexto, request.IdTurno),
                methodName);
        }

        private static OperationResult<ContextoConfirmacionPreInscripcion> ObtenerContextoConfirmacion(
            IUnitOfWork uow,
            long codigoPersona,
            long idOfertaSeleccionada,
            string methodName)
        {
            var encuestaAdmision = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuestaAdmision != null
                && string.Equals(encuestaAdmision.EstadoEncuestaIniAdmision, EstadoDefinitivo, StringComparison.OrdinalIgnoreCase))
            {
                if (encuestaAdmision.FechaVtoAdmision.HasValue && encuestaAdmision.FechaVtoAdmision.Value.Date < DateTime.Today)
                {
                    return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                        "INS_CPI_12",
                        methodName,
                        "La encuesta inicial de admision se encuentra vencida.",
                        409);
                }

                if (!TieneDatosConfirmacionValidos(encuestaAdmision.IdProducto, encuestaAdmision.IdProceso, encuestaAdmision.IdComienzo))
                {
                    return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                        "INS_CPI_08",
                        methodName,
                        "La encuesta inicial de admision no contiene producto, proceso o comienzo validos.",
                        400);
                }

                return OperationResult<ContextoConfirmacionPreInscripcion>.Ok(
                    new ContextoConfirmacionPreInscripcion(
                        encuestaAdmision.IdProducto!.Value,
                        encuestaAdmision.IdProceso!.Value,
                        encuestaAdmision.IdComienzo!.Value,
                        encuestaAdmision.Producto,
                        encuestaAdmision.Comienzo),
                    methodName);
            }

            var encuestaHistorica = uow.EncuestaInis.GetByPersona(codigoPersona);
            if (encuestaHistorica == null)
            {
                var errorCode = encuestaAdmision == null ? "INS_CPI_06" : "INS_CPI_07";
                var message = encuestaAdmision == null
                    ? "No se encontro una encuesta inicial de admision vigente para la persona."
                    : "La encuesta inicial debe estar en estado DEFINITIVO para confirmar la preinscripcion.";
                var httpCode = encuestaAdmision == null ? 404 : 409;

                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    errorCode,
                    methodName,
                    message,
                    httpCode);
            }

            var oferta = uow.Ofertas.GetByKeyWithRelated(idOfertaSeleccionada);
            var idProducto = oferta?.Supraoferta?.Paquete?.IdProducto;
            var idComienzo = oferta?.Supraoferta?.IdComienzo;
            if (!TieneValorPositivo(idProducto) || !TieneValorPositivo(idComienzo))
            {
                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    "INS_CPI_08",
                    methodName,
                    "La oferta seleccionada no contiene producto o comienzo validos.",
                    400);
            }

            var proceso = uow.Interes.GetProcesoPorInteresActivo(codigoPersona, idProducto!.Value);
            if (proceso == null || proceso.IdProceso <= 0)
            {
                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    "INS_CPI_08",
                    methodName,
                    "No se pudo determinar un proceso valido para confirmar la preinscripcion.",
                    400);
            }

            return OperationResult<ContextoConfirmacionPreInscripcion>.Ok(
                new ContextoConfirmacionPreInscripcion(
                    idProducto.Value,
                    proceso.IdProceso,
                    idComienzo!.Value,
                    oferta!.Supraoferta.Paquete.Producto,
                    oferta.Supraoferta.Comienzo),
                methodName);
        }

        private static bool TieneDatosConfirmacionValidos(long? idProducto, long? idProceso, long? idComienzo)
        {
            return TieneValorPositivo(idProducto)
                && TieneValorPositivo(idProceso)
                && TieneValorPositivo(idComienzo);
        }

        private static bool TieneValorPositivo(long? valor)
        {
            return valor.HasValue && valor.Value > 0;
        }

        private OperationResult<DtoAceptacionReglamentoEstDevart> AsegurarAceptacionReglamentoEstudiantil(
            IUnitOfWork uow,
            long codigoPersona,
            long idProducto,
            long idComienzo,
            string methodName)
        {
            var existente = uow.AceptacionReglamentoEsts.GetByPersonaProductoComienzo(codigoPersona, idProducto, idComienzo);
            if (existente != null)
            {
                return OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(existente.ToDto(), methodName);
            }

            var entidad = new AceptacionReglamentoEst
            {
                IdAceptacionReglamentoEst = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST),
                CodigoPersona = codigoPersona,
                IdProducto = idProducto,
                IdComienzo = idComienzo,
                IdSistema = CommonConstants.IdSistemaAdmisiones
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

        private static ConfirmarPreInscripcionResponse MapearConfirmacionPreInscripcion(
            ConfirmarPreInscripcionApiResponse source,
            ContextoConfirmacionPreInscripcion contexto,
            long idTurno)
        {
            return new ConfirmarPreInscripcionResponse
            {
                Confirmada = source.Confirmada || source.Success,
                IdInscripcion = source.IdInscripcion,
                SeniaInscripcion = source.SeniaInscripcion,
                FechaVencimientoPago = source.FechaVencimientoPago,
                Resumen = new ResumenInscripcionDto
                {
                    IdProducto = source.Resumen?.IdProducto ?? contexto.IdProducto,
                    Carrera = source.Resumen?.Carrera ?? contexto.Producto?.NombreExtensoProducto ?? contexto.Producto?.NombreProducto,
                    IdComienzo = source.Resumen?.IdComienzo ?? contexto.IdComienzo,
                    Comienzo = source.Resumen?.Comienzo ?? contexto.Comienzo?.NombreComienzo,
                    IdTurno = source.Resumen?.IdTurno ?? idTurno,
                    Turno = source.Resumen?.Turno
                }
            };
        }

        private sealed record ContextoConfirmacionPreInscripcion(
            long IdProducto,
            long IdProceso,
            long IdComienzo,
            Producto? Producto,
            Comienzo? Comienzo);

        private static EncuestaIniAdmision? ObtenerEncuestaParaGuardar(
            IUnitOfWork uow,
            long codigoPersona,
            GuardarEncuestaInicialRequest request)
        {
            if (request.IdProducto.HasValue && request.IdProceso.HasValue)
            {
                var idComienzo = uow.ProcesoComienzos.GetComienzoActivoPorProcesoOProducto(
                    request.IdProducto.Value,
                    request.IdProceso.Value);
                if (idComienzo.HasValue && idComienzo.Value > 0)
                {
                    var encuestaPorProductoComienzo = uow.EncuestaIniAdmisions.GetByPersonaProductoComienzo(
                        codigoPersona,
                        request.IdProducto.Value,
                        idComienzo.Value);
                    if (encuestaPorProductoComienzo != null)
                    {
                        return encuestaPorProductoComienzo;
                    }
                }
            }

            return uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
        }

        private EncuestaIniAdmision CrearEncuestaInicial(Persona persona, long codigoPersona)
        {
            var ahora = DateTime.Now;
            return new EncuestaIniAdmision
            {
                IdEncuestaIni = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION),
                FechaEncuestaIni = ahora,
                TipoDocumento = persona.TipoDocumento,
                Documento = persona.Documento,
                CodigoPersona = codigoPersona,
                TipoInscripcion = "SOLO_ENCUESTA_INI",
                NuevaversionEncuestaIni = CommonConstants.Booleanos.Si,
                EstadoEncuestaIniAdmision = EstadoTemporal,
                UsuarioIngreso = string.Empty,
                FechaIngreso = ahora,
                HoraIngreso = ahora.ToString("HH:mm:ss")
            };
        }

        private static void AplicarRequestAEncuesta(
            EncuestaIniAdmision encuesta,
            GuardarEncuestaInicialRequest request,
            Persona persona,
            long? idComienzo)
        {
            if (request.IdProducto.HasValue)
            {
                encuesta.IdProducto = request.IdProducto.Value;
            }

            if (idComienzo.HasValue)
            {
                encuesta.IdComienzo = idComienzo.Value;
            }

            if (request.IdProceso.HasValue)
            {
                encuesta.IdProceso = request.IdProceso.Value;
            }

            if (encuesta.IdProducto.HasValue)
            {
                encuesta.ClaveEncuestaIni = GenerarClaveEncuesta(encuesta.IdProducto.Value, persona.Documento);
            }

            if (request.NombreInstitucion != null)
            {
                encuesta.NombreInstSecEncuestaIni = NormalizarTextoOpcional(request.NombreInstitucion);
            }

            if (request.CodigoTitulo.HasValue)
            {
                encuesta.CodigoTitulo = request.CodigoTitulo.Value;
            }

            if (request.UltimoAnioSexto.HasValue)
            {
                encuesta.UltimoAnioSextoEncuestaIni = request.UltimoAnioSexto.Value.ToString();
            }

            if (request.VecesSextoBool.HasValue)
            {
                encuesta.VecesSextoEncuestaIni = request.VecesSextoBool.Value
                    ? request.VecesSexto?.ToString()
                    : null;
            }
            else if (request.VecesSexto.HasValue)
            {
                encuesta.VecesSextoEncuestaIni = request.VecesSexto.Value.ToString();
            }

            if (request.InstruccionPadre.HasValue)
            {
                encuesta.InstruccionPadreEncuestaIni = request.InstruccionPadre.Value.ToString();
            }

            if (request.InstruccionMadre.HasValue)
            {
                encuesta.InstruccionMadreEncuestaIni = request.InstruccionMadre.Value.ToString();
            }

            if (request.DecisionCarrera.HasValue)
            {
                encuesta.DecisionCarreraEncuestaIni = request.DecisionCarrera.Value.ToString();
            }

            if (request.DecisionUniversidad.HasValue)
            {
                encuesta.DecisionUniverEncuestaIni = request.DecisionUniversidad.Value.ToString();
            }

            if (request.InfoOtrasUniversidadesAntes != null)
            {
                encuesta.InforOtrasAntesEncuestaIni = NormalizarSiNo(request.InfoOtrasUniversidadesAntes);
            }

            if (request.InfoOtrasLinea1 != null)
            {
                encuesta.InforOtrasLinea1Ini = NormalizarTextoOpcional(request.InfoOtrasLinea1);
            }

            if (request.InfoOtrasLinea2 != null)
            {
                encuesta.InforOtrasLinea2Ini = NormalizarTextoOpcional(request.InfoOtrasLinea2);
            }

            if (request.CompartidoCon.HasValue)
            {
                CargarConQuienCompartioDecision(encuesta, request.CompartidoCon.Value);
            }

            if (request.CodigoInstitucionBac.HasValue)
            {
                encuesta.CodigoInstitucionBac = request.CodigoInstitucionBac.Value;
            }

            if (request.InformarEncuesta != null)
            {
                encuesta.InformarEncuestaIni = NormalizarSiNo(request.InformarEncuesta);
            }

            if (request.UltimoAnioSecundaria.HasValue)
            {
                encuesta.UltimoanioSecundariaEncuestaIni = request.UltimoAnioSecundaria.Value == PersonaConstants.Parametros.UruguayCodigoPais;
            }

            if (request.TieneEducacionSuperior.HasValue)
            {
                encuesta.TieneEducacionSuperiorEncuestaIni = ConvertirBoolASiNo(request.TieneEducacionSuperior.Value);
            }

            if (request.NivelDecision.HasValue)
            {
                encuesta.NivelDecisionEncuestaIni = request.NivelDecision.Value == 1;
            }

            if (request.AsesoramientoOrt.HasValue)
            {
                encuesta.AsesoramientoOrtEncuestaIni = ConvertirBoolASiNo(request.AsesoramientoOrt.Value);
            }

            if (request.ValoracionAsesoramientoOrt.HasValue && request.AsesoramientoOrt == true)
            {
                encuesta.ValoracionAsesoramientoOrtEncuestaIni = request.ValoracionAsesoramientoOrt.Value > 3;
            }
            else if (request.AsesoramientoOrt == false)
            {
                encuesta.ValoracionAsesoramientoOrtEncuestaIni = null;
            }

            if (request.VistaSitioWebOrt.HasValue)
            {
                encuesta.VistaSitioWebOrtEncuestaIni = ConvertirBoolASiNo(request.VistaSitioWebOrt.Value);
            }

            if (request.ValoracionSitioWeb.HasValue && request.VistaSitioWebOrt == true)
            {
                encuesta.ValoracionSitioWebOrtEncuestaIni = request.ValoracionSitioWeb.Value > 3;
            }
            else if (request.VistaSitioWebOrt == false)
            {
                encuesta.ValoracionSitioWebOrtEncuestaIni = null;
            }

            if (request.VistaInstalacionesOrt.HasValue)
            {
                encuesta.VistaInstalacionesOrtEncuestaIni = ConvertirBoolASiNo(request.VistaInstalacionesOrt.Value);
            }

            if (request.ValoracionInstalacionesOrt.HasValue && request.VistaInstalacionesOrt == true)
            {
                encuesta.ValoracionInstalacionesOrtEncuestaIni = request.ValoracionInstalacionesOrt.Value > 3;
            }
            else if (request.VistaInstalacionesOrt == false)
            {
                encuesta.ValoracionInstalacionesOrtEncuestaIni = null;
            }

            if (request.PublicidadOrt.HasValue)
            {
                encuesta.PublicidadOrtEncuestaIni = ConvertirBoolASiNo(request.PublicidadOrt.Value);
            }

            if (request.InstruccionMadreOrt.HasValue)
            {
                encuesta.InstruccionMadreOrtEncuestaIni = ConvertirBoolASiNo(request.InstruccionMadreOrt.Value);
            }

            if (request.InstruccionPadreOrt.HasValue)
            {
                encuesta.InstruccionPadreOrtEncuestaIni = ConvertirBoolASiNo(request.InstruccionPadreOrt.Value);
            }
        }

        private void AplicarListasHijas(IUnitOfWork uow, long codigoPersona, GuardarEncuestaInicialRequest request)
        {
            if (string.Equals(NormalizarSiNo(request.InfoOtrasUniversidadesAntes), CommonConstants.Booleanos.No, StringComparison.OrdinalIgnoreCase))
            {
                uow.EmpresaConsideradaAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.UniversidadesConsideradas != null)
            {
                ReemplazarUniversidadesConsideradas(uow, codigoPersona, request.UniversidadesConsideradas);
            }

            if (request.TieneEducacionSuperior == false)
            {
                uow.EducacionSuperiorAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.UniversidadesEducacionSuperior != null)
            {
                ReemplazarEducacionSuperior(uow, codigoPersona, request.UniversidadesEducacionSuperior);
            }

            if (request.OpcionesMotivosSeleccionados != null)
            {
                uow.MotivoEleccionAdmisions.RemoveByPersona(codigoPersona);
                foreach (var motivo in request.OpcionesMotivosSeleccionados.DistinctBy(m => m.IdMotivo))
                {
                    uow.MotivoEleccionAdmisions.Add(new MotivoEleccionAdmision
                    {
                        IdMotivo = motivo.IdMotivo,
                        CodigoPersona = codigoPersona
                    });
                }
            }

            if (request.PublicidadOrt == false)
            {
                uow.PublicidadEleccionAdmisions.RemoveByPersona(codigoPersona);
            }
            else if (request.OpcionesPublicidadSeleccionadas != null)
            {
                uow.PublicidadEleccionAdmisions.RemoveByPersona(codigoPersona);
                foreach (var publicidad in request.OpcionesPublicidadSeleccionadas.DistinctBy(p => p.IdPublicidad))
                {
                    uow.PublicidadEleccionAdmisions.Add(new PublicidadEleccionAdmision
                    {
                        IdPublicidad = publicidad.IdPublicidad,
                        CodigoPersona = codigoPersona
                    });
                }
            }
        }

        private void ReemplazarUniversidadesConsideradas(
            IUnitOfWork uow,
            long codigoPersona,
            IEnumerable<EncuestaEmpresaRequest> universidades)
        {
            uow.EmpresaConsideradaAdmisions.RemoveByPersona(codigoPersona);
            foreach (var universidad in universidades)
            {
                uow.EmpresaConsideradaAdmisions.Add(new EmpresaConsideradaAdmision
                {
                    IdEmpresaConsiderada = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EMPRESA_CONSIDERADA_ADMISION),
                    CodigoPersona = codigoPersona,
                    CodigoEmpresa = universidad.CodigoEmpresa == 0 ? null : universidad.CodigoEmpresa,
                    NombreOtraEmpresa = universidad.CodigoEmpresa == 0 ? universidad.Nombre?.Trim() : null
                });
            }
        }

        private void ReemplazarEducacionSuperior(
            IUnitOfWork uow,
            long codigoPersona,
            IEnumerable<EncuestaEmpresaRequest> universidades)
        {
            uow.EducacionSuperiorAdmisions.RemoveByPersona(codigoPersona);
            foreach (var universidad in universidades)
            {
                uow.EducacionSuperiorAdmisions.Add(new EducacionSuperiorAdmision
                {
                    IdEducacionSuperior = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_EDUCACION_SUPERIOR_ADMISION),
                    CodigoPersona = codigoPersona,
                    CodigoEmpresa = universidad.CodigoEmpresa == 0 ? null : universidad.CodigoEmpresa,
                    NombreOtraEmpresa = universidad.CodigoEmpresa == 0 ? universidad.Nombre?.Trim() : null
                });
            }
        }

        private static string? NormalizarSiNo(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();
        }

        private static string? NormalizarTextoOpcional(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static string ConvertirBoolASiNo(bool valor)
        {
            return valor ? CommonConstants.Booleanos.Si : CommonConstants.Booleanos.No;
        }

        private static string GenerarClaveEncuesta(long idProducto, string? documento)
        {
            var input = $"{idProducto}/{documento?.Trim().ToUpperInvariant()}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash)[..30];
        }

        private static void CargarConQuienCompartioDecision(EncuestaIniAdmision encuesta, int valor)
        {
            encuesta.ComparPadresEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.Padres);
            encuesta.ComparOtrosEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.Otros);
            encuesta.ComparAmigoFamEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.AmigoFamilia);
            encuesta.ComparNadieEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.Nadie);
            encuesta.ComparAmigoPropEncuestaIni = ConvertirBoolASiNo(valor == PersonaConstants.CompartidoCon.AmigoPropio);
        }

        private static bool TieneDerechoAEncuestaInicial(string tipoDocumento, string documento, IUnitOfWork uow)
        {
            if (uow.VdEsFrescoAdmisions.ExistePorDocumento(tipoDocumento, documento))
                return false;

            if (uow.EncuestaInis.ExistePorDocumento(tipoDocumento, documento))
                return false;

            if (uow.EncuestaIniAdmisions.ExisteCompletaPorDocumento(tipoDocumento, documento))
                return false;

            return true;
        }

        #endregion PASO 2 - ENCUESTA INICIAL y PREINSCRIPCIÓN

        #region PASO 3 - PAGOS

        #endregion PASO 3 - PAGOS
    }
}
