using AppLogic.ApiClients;
using AppLogic.Constants;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.Helpers.ValidationHelpers;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Inscripciones;
using AppLogic.Services.Personas;
using AppLogic.Utilities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Services.Inscripciones
{
    public class InscripcionesService : IInscripcionesService
    {
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

            if (request == null)
            {
                return OperationResult<bool>.IsFailed("GEN_IP_00", nameof(RegistrarInteresProducto), "Request invalido.", 400);
            }

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

            var validacionOferta = InteresProductoValidationHelper.ObtenerOfertaValidaParaInteres(
                uow,
                request.IdOferta,
                request.IdProducto,
                request.IdProcesoSeleccionado,
                nameof(RegistrarInteresProducto));
            if (!validacionOferta.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validacionOferta.ErrorCode,
                    nameof(RegistrarInteresProducto),
                    validacionOferta.Message,
                    validacionOferta.HttpCode);
            }

            var oferta = validacionOferta.Data!;
            var fechaActual = DateTime.Now;

            uow.BeginTransaction();

            try
            {
                var resultado = InteresProductoRegistroHelper.RegistrarInteresProducto(
                    uow,
                    _dbConnectionContext,
                    codigoPersona,
                    request,
                    oferta,
                    fechaActual,
                    nameof(RegistrarInteresProducto));
                if (!resultado.Success)
                {
                    uow.Rollback();
                    return resultado;
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

            if (!EncuestaInicialAdmisionHelper.TieneDerechoAEncuestaInicial(tipoDocumento, documento, uow))
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
                EncuestaInicialAdmisionHelper.CrearRespuestaEncuestaInicial(uow, codigoPersona, encuesta),
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

            var encuesta = EncuestaInicialAdmisionHelper.ObtenerEncuestaParaGuardar(uow, codigoPersona, request);
            var esNueva = encuesta == null;
            encuesta ??= EncuestaInicialAdmisionHelper.CrearEncuestaInicial(_dbConnectionContext, persona, codigoPersona);

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

            EncuestaInicialAdmisionHelper.AplicarRequestAEncuesta(encuesta, request, persona, idComienzo);

            uow.BeginTransaction();
            try
            {
                if (esNueva)
                {
                    uow.EncuestaIniAdmisions.Add(encuesta);
                }

                EncuestaInicialAdmisionHelper.AplicarListasHijas(uow, _dbConnectionContext, codigoPersona, request);
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

                encuesta.EstadoEncuestaIniAdmision = completitud.Data
                    ? EncuestaInicialAdmisionHelper.EstadoDefinitivo
                    : EncuestaInicialAdmisionHelper.EstadoTemporal;
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

            var validacionRequest = ConfirmarPreInscripcionHelper.ValidarRequest(request, methodName);
            if (!validacionRequest.Success)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed(
                    validacionRequest.ErrorCode,
                    methodName,
                    validacionRequest.Message,
                    validacionRequest.HttpCode);
            }

            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_05", methodName, PersonaConstants.PersonaNoEncontradaMessage, 404);
            }

            var oferta = uow.Ofertas.GetByKeyWithRelated(request.IdOfertaSeleccionada);
            if (oferta == null)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_15", methodName, "No se encontro la oferta seleccionada.", 404);
            }

            var contextoResult = ConfirmarPreInscripcionHelper.ObtenerContextoConfirmacion(uow, codigoPersona, oferta, methodName);
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

            var aceptacion = ConfirmarPreInscripcionHelper.AsegurarAceptacionReglamentoEstudiantil(
                uow,
                _dbConnectionContext,
                codigoPersona,
                contexto.IdProducto,
                contexto.IdComienzo,
                methodName);
            if (!aceptacion.Success)
            {
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed(aceptacion.ErrorCode, methodName, aceptacion.Message, aceptacion.HttpCode);
            }

            var apiRequest = ConfirmarPreInscripcionHelper.CrearApiRequest(contexto, request.IdOfertaSeleccionada);
            var apiResult = await _inscripcionesyPagosApiClient.ConfirmarPreInscripcionAsync(apiRequest);
            return ConfirmarPreInscripcionHelper.MapearResultadoApi(apiResult, contexto, methodName);
        }

        #endregion PASO 2 - ENCUESTA INICIAL y PREINSCRIPCIÓN

        #region PASO 3 - PAGOS

        #endregion PASO 3 - PAGOS
    }
}
