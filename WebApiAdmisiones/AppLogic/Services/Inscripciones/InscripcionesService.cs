using AppLogic.ApiClients;
using AppLogic.Constants;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.Helpers.ValidationHelpers;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Inscripciones;
using AppLogic.IServices.Tivenos;
using AppLogic.Services.Personas;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Services.Inscripciones
{
    public class InscripcionesService : IInscripcionesService
    {
        private const long CodigoOrientacionQuintoLegacy = 1304;

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly IGeneralService _generalService;
        private readonly ITivenosEnvioService _tivenosEnvioService;
        private readonly InscripcionesyPagosApiClient _inscripcionesyPagosApiClient;

        public InscripcionesService(
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            IGeneralService generalService,
            ITivenosEnvioService tivenosEnvioService,
            InscripcionesyPagosApiClient inscripcionesyPagosApiClient)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _generalService = generalService;
            _tivenosEnvioService = tivenosEnvioService;
            _inscripcionesyPagosApiClient = inscripcionesyPagosApiClient;
        }

        public async Task<OperationResult<DetalleInscripcionResponse>> ObtenerDetalleInscripcion(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();

            var fresco1y2 = uow.VdInscripcionesFresco1y2s.GetInscripcionesFrescoHabilitadas(codigoPersona)
                .FirstOrDefault(x => x.IdProducto == idProducto && x.IdProceso == idProceso);
            var estado = fresco1y2?.EstadoInscripcion;
            var idInscripto = (long?)fresco1y2?.IdInscripto;

            if (estado == null)
            {
                var fresco3y4 = uow.VdInscripcionesFresco3y4s.GetInscripcionesFrescoHabilitadas(codigoPersona)
                    .FirstOrDefault(x => x.IdProducto == idProducto && x.IdProceso == idProceso);
                estado = fresco3y4?.EstadoInscripcion;
                idInscripto = (long?)fresco3y4?.IdInscripto;
            }

            if (estado == null)
            {
                return OperationResult<DetalleInscripcionResponse>.IsFailed(
                    "INS_DET_01",
                    nameof(ObtenerDetalleInscripcion),
                    "No se encontró la inscripción para la persona.",
                    404);
            }

            var response = new DetalleInscripcionResponse { Estado = estado };

            switch (estado)
            {
                case InscripcionesConstants.EstadoInscripcion.EnProceso:
                    var oferta = uow.InteresProductoOfertas.GetOfertaSeleccionada(codigoPersona, idProducto, idProceso);
                    response.Detalle = MapearOfertaResumen(oferta);
                    break;

                case InscripcionesConstants.EstadoInscripcion.PagoPendiente:
                    var inscriptoPago = idInscripto.HasValue
                        ? uow.Inscriptos.GetDetalleByKey(idInscripto.Value, codigoPersona)
                        : null;
                    if (inscriptoPago == null)
                    {
                        return OperationResult<DetalleInscripcionResponse>.IsFailed(
                            "INS_DET_02",
                            nameof(ObtenerDetalleInscripcion),
                            "No se encontró la inscripción para la persona.",
                            404);
                    }
                    // La seña a pagar la calcula LogicaORT (cálculo canónico ValorSeniaMinimaConCanje).
                    var senia = await _inscripcionesyPagosApiClient.ObtenerSeniaMinimaAsync(inscriptoPago.IdInscripto, idProducto);
                    if (!senia.Success)
                    {
                        return OperationResult<DetalleInscripcionResponse>.IsFailed(
                            senia.ErrorCode,
                            nameof(ObtenerDetalleInscripcion),
                            senia.Message,
                            senia.HttpCode);
                    }
                    response.PagoPendiente = MapearPagoPendiente(inscriptoPago, senia.Data!.SeniaMinima);
                    break;

                case InscripcionesConstants.EstadoInscripcion.Confirmada:
                    var inscripto = idInscripto.HasValue
                        ? uow.Inscriptos.GetDetalleByKey(idInscripto.Value, codigoPersona)
                        : null;
                    if (inscripto == null)
                    {
                        return OperationResult<DetalleInscripcionResponse>.IsFailed(
                            "INS_DET_02",
                            nameof(ObtenerDetalleInscripcion),
                            "No se encontró la inscripción confirmada para la persona.",
                            404);
                    }
                    var materias = uow.Ofertas.GetMateriasPorOferta(inscripto.IdOferta ?? 0);
                    response.Confirmada = MapearConfirmada(codigoPersona, inscripto, materias);
                    break;

                // "A la espera" y estados desconocidos: se devuelve solo el estado, sin detalle.
            }

            return OperationResult<DetalleInscripcionResponse>.Ok(response, nameof(ObtenerDetalleInscripcion));
        }

        private static PagoPendienteDetalleDto MapearPagoPendiente(Inscripto inscripto, decimal senia)
        {
            return new PagoPendienteDetalleDto
            {
                IdInscripcion = inscripto.IdInscripto,
                Senia = senia,
                FechaVencimientoPago = inscripto.FechaVtoInscr,
                Resumen = MapearResumenDesdeInscripto(inscripto)
            };
        }

        private static ConfirmadaDetalleDto MapearConfirmada(long codigoPersona, Inscripto inscripto, ICollection<Materia> materias)
        {
            var producto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto;
            return new ConfirmadaDetalleDto
            {
                NumeroEstudiante = codigoPersona,
                Resumen = MapearResumenDesdeInscripto(inscripto),
                CoordinadorAcademico = new CoordinadorDto
                {
                    Nombre = producto?.NombreCoordAcadProducto,
                    Email = producto?.EmailCoordAcadProducto
                },
                MateriasPrimerSemestre = materias
                    .Select(m => new MateriaDto { IdMateria = m.IdMateria, Nombre = m.NombreMateria })
                    .ToList()
            };
        }

        private static ResumenInscripcionDto MapearResumenDesdeInscripto(Inscripto inscripto)
        {
            var producto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto;
            var comienzo = inscripto.Oferta?.Supraoferta?.Comienzo;
            return new ResumenInscripcionDto
            {
                IdOferta = inscripto.IdOferta ?? 0,
                IdProducto = producto?.IdProducto ?? 0,
                Carrera = producto?.NombreWebProducto ?? producto?.NombreExtensoProducto ?? producto?.NombreProducto,
                IdComienzo = comienzo?.IdComienzo ?? 0,
                Comienzo = comienzo?.NombreComienzo,
                IdTurno = inscripto.Oferta?.IdTurno ?? 0,
                Turno = inscripto.Oferta?.Turno?.NombreTurno
            };
        }

        private static ResumenInscripcionDto? MapearOfertaResumen(Oferta? oferta)
        {
            if (oferta == null)
            {
                return null;
            }

            var producto = oferta.Supraoferta?.Paquete?.Producto;
            var comienzo = oferta.Supraoferta?.Comienzo;
            return new ResumenInscripcionDto
            {
                IdOferta = oferta.IdOferta,
                IdProducto = producto?.IdProducto ?? 0,
                Carrera = producto?.NombreWebProducto ?? producto?.NombreExtensoProducto ?? producto?.NombreProducto,
                IdComienzo = comienzo?.IdComienzo ?? 0,
                Comienzo = comienzo?.NombreComienzo,
                IdTurno = oferta.IdTurno,
                Turno = oferta.Turno?.NombreTurno
            };
        }

        public OperationResult<AceptacionReglamentoEstudiantilResponse> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var aceptacion = uow.AceptacionReglamentoEsts.GetPrimeraByPersona(codigoPersona);
            var response = new AceptacionReglamentoEstudiantilResponse
            {
                AceptoReglamentoEstudiantil = aceptacion != null,
                FechaAceptacion = aceptacion?.FechaIngreso
            };

            return OperationResult<AceptacionReglamentoEstudiantilResponse>.Ok(
                response,
                nameof(ObtenerAceptacionReglamentoEstudiantil));
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
                    return OperationResult<bool>.IsFailed(
                        resultado.ErrorCode,
                        nameof(RegistrarInteresProducto),
                        resultado.Message,
                        resultado.HttpCode);
                }

                if (resultado.Data != null)
                {
                    var resultadoTivenos = _tivenosEnvioService.EncolarAltaInteresXSeleccionEnSitio(
                        uow,
                        resultado.Data,
                        _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS),
                        nameof(RegistrarInteresProducto));
                    if (!resultadoTivenos.Success)
                    {
                        uow.Rollback();
                        return OperationResult<bool>.IsFailed(
                            resultadoTivenos.ErrorCode,
                            nameof(RegistrarInteresProducto),
                            resultadoTivenos.Message,
                            resultadoTivenos.HttpCode);
                    }
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
            var actualizaTrabajaActualmente = AplicarTrabajaActualmente(persona, request);

            uow.BeginTransaction();
            try
            {
                if (esNueva)
                {
                    uow.EncuestaIniAdmisions.Add(encuesta);
                }

                EncuestaInicialAdmisionHelper.AplicarListasHijas(uow, _dbConnectionContext, codigoPersona, request);
                if (actualizaTrabajaActualmente)
                {
                    uow.Personas.Update(persona);
                }

                uow.Save();

                var completitud = EncuestaInicialValidationHelper.ResolverCompletitud(uow, encuesta, persona, codigoPersona, nameof(GuardarEncuestaInicial));
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

                if (completitud.Data)
                {
                    var sincronizacionBachillerato = SincronizarBachilleratoPersona(
                        uow,
                        encuesta,
                        codigoPersona,
                        nameof(GuardarEncuestaInicial));
                    if (!sincronizacionBachillerato.Success)
                    {
                        uow.Rollback();
                        return OperationResult<bool>.IsFailed(
                            sincronizacionBachillerato.ErrorCode,
                            nameof(GuardarEncuestaInicial),
                            sincronizacionBachillerato.Message,
                            sincronizacionBachillerato.HttpCode);
                    }
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

        private static bool AplicarTrabajaActualmente(BusinessLogic.Entities.Persona persona, GuardarEncuestaInicialRequest request)
        {
            if (!string.Equals(persona.TipoPersona, PersonaConstants.TipoPersonaSgi, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (request.TrabajaActualmente == null)
            {
                return false;
            }

            persona.TrabajaActualmente = request.TrabajaActualmente.Value ? "S" : "N";
            return true;
        }

        private OperationResult<bool> SincronizarBachilleratoPersona(
            IUnitOfWork uow,
            BusinessLogic.Entities.EncuestaIniAdmision encuesta,
            long codigoPersona,
            string methodName)
        {
            var datos = ObtenerDatosBachilleratoDefinitivo(encuesta, methodName);
            if (!datos.Success)
            {
                return OperationResult<bool>.IsFailed(
                    datos.ErrorCode,
                    methodName,
                    datos.Message,
                    datos.HttpCode);
            }

            var datosBachillerato = datos.Data!;
            var fechaActual = _dbConnectionContext.CurrentDateTime();
            var existente = uow.BachilleratoPersonas.GetByKey(codigoPersona);
            if (existente == null)
            {
                uow.BachilleratoPersonas.Add(new BusinessLogic.Entities.BachilleratoPersona
                {
                    CodigoPersona = codigoPersona,
                    CodigoInstitucion = datosBachillerato.CodigoInstitucion,
                    AnioBachillerPer = datosBachillerato.AnioBachiller,
                    CodigoOrientacion = datosBachillerato.CodigoOrientacion,
                    ActualizacionBachillerPer = fechaActual,
                    UsuarioIngreso = string.Empty,
                    FechaIngreso = fechaActual,
                    HoraIngreso = fechaActual.ToString("HH:mm:ss")
                });

                return _tivenosEnvioService.EncolarAltaDatosBachillerato(
                uow,
                new TivenosBachilleratoRequest
                {
                    CodigoPersona = codigoPersona,
                    CodigoOrientacion = datosBachillerato.CodigoOrientacion
                },
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS),
                methodName);
            }

            if (!CambioBachillerato(existente, datosBachillerato))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            existente.CodigoInstitucion = datosBachillerato.CodigoInstitucion;
            existente.AnioBachillerPer = datosBachillerato.AnioBachiller;
            existente.CodigoOrientacion = datosBachillerato.CodigoOrientacion;
            existente.ActualizacionBachillerPer = fechaActual;
            uow.BachilleratoPersonas.Update(existente);

            return _tivenosEnvioService.EncolarModificacionDatosBachillerato(
                uow,
                new TivenosBachilleratoRequest
                {
                    CodigoPersona = codigoPersona,
                    CodigoOrientacion = datosBachillerato.CodigoOrientacion
                },
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS),
                methodName);
        }

        private static OperationResult<DatosBachilleratoPersona> ObtenerDatosBachilleratoDefinitivo(
            BusinessLogic.Entities.EncuestaIniAdmision encuesta,
            string methodName)
        {
            if (!long.TryParse(encuesta.UltimoAnioSextoEncuestaIni, out var ultimoAnio))
            {
                return OperationResult<DatosBachilleratoPersona>.IsFailed(
                    "INS_EI_36",
                    methodName,
                    "No se pudo resolver el anio de bachillerato para la persona.",
                    400);
            }

            if (ultimoAnio is < 4 or > 6)
            {
                return OperationResult<DatosBachilleratoPersona>.IsFailed(
                    "INS_EI_37",
                    methodName,
                    "Anio de bachillerato invalido para la persona.",
                    400);
            }

            var codigoOrientacion = ultimoAnio switch
            {
                4 => null,
                5 => CodigoOrientacionQuintoLegacy,
                6 => encuesta.CodigoTitulo,
                _ => null
            };

            if (ultimoAnio == 6 && (!codigoOrientacion.HasValue || codigoOrientacion.Value <= 0))
            {
                return OperationResult<DatosBachilleratoPersona>.IsFailed(
                    "INS_EI_38",
                    methodName,
                    "No se pudo resolver la orientacion de bachillerato para la persona.",
                    400);
            }

            return OperationResult<DatosBachilleratoPersona>.Ok(
                new DatosBachilleratoPersona(
                    encuesta.CodigoInstitucionBac,
                    ultimoAnio.ToString(),
                    codigoOrientacion),
                methodName);
        }

        private static bool CambioBachillerato(
            BusinessLogic.Entities.BachilleratoPersona existente,
            DatosBachilleratoPersona datos)
        {
            return existente.CodigoInstitucion != datos.CodigoInstitucion
                || !string.Equals(existente.AnioBachillerPer, datos.AnioBachiller, StringComparison.Ordinal)
                || existente.CodigoOrientacion != datos.CodigoOrientacion;
        }

        private sealed record DatosBachilleratoPersona(
            long? CodigoInstitucion,
            string AnioBachiller,
            long? CodigoOrientacion);

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
            var confirmacionResult = ConfirmarPreInscripcionHelper.MapearResultadoApi(apiResult, contexto, methodName);
            if (!confirmacionResult.Success)
            {
                return confirmacionResult;
            }

            // La confirmación de LogicaORT puede no traer la fecha de vencimiento; se lee de T_INSCRIPTO.
            if (confirmacionResult.Data!.FechaVencimientoPago == null && confirmacionResult.Data.IdInscripcion.HasValue)
            {
                var inscriptoConfirmado = uow.Inscriptos.GetByKey(confirmacionResult.Data.IdInscripcion.Value);
                confirmacionResult.Data.FechaVencimientoPago = inscriptoConfirmado?.FechaVtoInscr;
            }

            var estadoCuentaResult = await _inscripcionesyPagosApiClient.ObtenerCtaCteAsync();
            if (estadoCuentaResult.Success)
            {
                confirmacionResult.Data!.EstadoCuenta = ConfirmarPreInscripcionHelper.MapearEstadoCuenta(estadoCuentaResult.Data);
            }

            return confirmacionResult;
        }

        #endregion PASO 2 - ENCUESTA INICIAL y PREINSCRIPCIÓN

        #region PASO 3 - PAGOS

        #endregion PASO 3 - PAGOS
    }
}
