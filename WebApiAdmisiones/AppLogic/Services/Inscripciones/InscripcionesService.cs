using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Dtos.Inscripciones;
using AppLogic.Dtos.Tivenos;
using AppLogic.ApiClients;
using AppLogic.Constants;
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
        private static readonly HashSet<string> TiposPagoFactura = ["BANRED", "SISTARBANC", "GEOPAY"];
        private static readonly HashSet<string> MetodosPagoExternos = ["ABITAB", "PAGANZA"];

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

        public async Task<OperationResult<DtoDetalleInscripcionResponse>> ObtenerDetalleInscripcion(long codigoPersona, long idProducto, long idProceso)
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
                return OperationResult<DtoDetalleInscripcionResponse>.IsFailed(
                    "INS_DET_01",
                    nameof(ObtenerDetalleInscripcion),
                    "No se encontró la inscripción para la persona.",
                    404);
            }

            var response = new DtoDetalleInscripcionResponse { Estado = estado };

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
                        return OperationResult<DtoDetalleInscripcionResponse>.IsFailed(
                            "INS_DET_02",
                            nameof(ObtenerDetalleInscripcion),
                            "No se encontró la inscripción para la persona.",
                            404);
                    }
                    var carritos = await _inscripcionesyPagosApiClient.ObtenerCarritosPorInscripcionAsync(inscriptoPago.IdInscripto);
                    if (!carritos.Success)
                    {
                        return OperationResult<DtoDetalleInscripcionResponse>.IsFailed(
                            carritos.ErrorCode,
                            nameof(ObtenerDetalleInscripcion),
                            carritos.Message,
                            carritos.HttpCode);
                    }
                    response.PagoPendiente = MapearPagoPendiente(inscriptoPago, carritos.Data);
                    break;

                case InscripcionesConstants.EstadoInscripcion.Confirmada:
                    var inscripto = idInscripto.HasValue
                        ? uow.Inscriptos.GetDetalleByKey(idInscripto.Value, codigoPersona)
                        : null;
                    if (inscripto == null)
                    {
                        return OperationResult<DtoDetalleInscripcionResponse>.IsFailed(
                            "INS_DET_02",
                            nameof(ObtenerDetalleInscripcion),
                            "No se encontró la inscripción confirmada para la persona.",
                            404);
                    }
                    var coordinadores = uow.VdInscriptoCoordinadores.GetByInscripto(inscripto.IdInscripto);
                    var materias = uow.VdInscriptoCreditoAlumnos.GetByInscripto(inscripto.IdInscripto);
                    response.Confirmada = MapearConfirmada(codigoPersona, inscripto, coordinadores, materias);
                    break;

                // "A la espera" y estados desconocidos: se devuelve solo el estado, sin detalle.
            }

            return OperationResult<DtoDetalleInscripcionResponse>.Ok(response, nameof(ObtenerDetalleInscripcion));
        }

        private static DtoConfirmarPreInscripcionResponse MapearPagoPendiente(Inscripto inscripto, CarritosInscripcionApiResponse? carritos)
        {
            return new DtoConfirmarPreInscripcionResponse
            {
                Confirmada = true,
                IdInscripcion = inscripto.IdInscripto,
                FechaVencimientoPago = inscripto.FechaVtoInscr,
                Senia = ConfirmarPreInscripcionHelper.SumarSenias(carritos?.Carritos),
                EstadoCuenta = ConfirmarPreInscripcionHelper.MapearEstadoCuenta(carritos?.EstadoCuenta),
                Resumen = MapearResumenDesdeInscripto(inscripto)
            };
        }

        private static DtoConfirmadaDetalle MapearConfirmada(
            long codigoPersona,
            Inscripto inscripto,
            ICollection<VdInscriptoCoordinadore> coordinadores,
            ICollection<VdInscriptoCreditoAlumno> materias)
        {
            var coordinadorAcademico = MapearCoordinadorAcademico(coordinadores);
            var coordinadorCursos = MapearCoordinadorCursos(coordinadores);
            if (EsMismoCoordinador(coordinadorAcademico, coordinadorCursos))
            {
                coordinadorCursos = null;
            }

            return new DtoConfirmadaDetalle
            {
                NumeroEstudiante = codigoPersona,
                Resumen = MapearResumenDesdeInscripto(inscripto),
                CoordinadorAcademico = coordinadorAcademico?.Dto,
                CoordinadorCursos = coordinadorCursos?.Dto,
                MateriasPrimerSemestre = materias
                    .Where(m => m.IdMateria.HasValue)
                    .GroupBy(m => m.IdMateria!.Value)
                    .Select(g => new DtoMateria { IdMateria = g.Key, Nombre = g.First().DescripcionMateria?.Trim() })
                    .ToList()
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

            return TextoIgual(academico.Dto.Email, cursos.Dto.Email)
                || TextoIgual(academico.Dto.Nombre, cursos.Dto.Nombre);
        }

        private static bool TextoIgual(string? izquierda, string? derecha)
        {
            return !string.IsNullOrWhiteSpace(izquierda)
                && !string.IsNullOrWhiteSpace(derecha)
                && string.Equals(izquierda.Trim(), derecha.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string? NombreCompleto(string? nombre, string? apellido)
        {
            var partes = new[] { nombre?.Trim(), apellido?.Trim() }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var completo = string.Join(" ", partes);
            return string.IsNullOrWhiteSpace(completo) ? null : completo;
        }

        private sealed record CoordinadorMapeado(DtoCoordinador Dto, long? Codigo);

        private static DtoResumenInscripcion MapearResumenDesdeInscripto(Inscripto inscripto)
        {
            var producto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto;
            var comienzo = inscripto.Oferta?.Supraoferta?.Comienzo;
            return new DtoResumenInscripcion
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

        private static DtoResumenInscripcion? MapearOfertaResumen(Oferta? oferta)
        {
            if (oferta == null)
            {
                return null;
            }

            var producto = oferta.Supraoferta?.Paquete?.Producto;
            var comienzo = oferta.Supraoferta?.Comienzo;
            return new DtoResumenInscripcion
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

        public OperationResult<DtoAceptacionReglamentoEstudiantilResponse> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var aceptacion = uow.AceptacionReglamentoEsts.GetPrimeraByPersona(codigoPersona);
            var response = new DtoAceptacionReglamentoEstudiantilResponse
            {
                AceptoReglamentoEstudiantil = aceptacion != null,
                FechaAceptacion = aceptacion?.FechaIngreso
            };

            return OperationResult<DtoAceptacionReglamentoEstudiantilResponse>.Ok(
                response,
                nameof(ObtenerAceptacionReglamentoEstudiantil));
        }

        #region PASO 1 - REGISTRAR INTERES POR PRODUCTO
        public OperationResult<bool> RegistrarInteresProducto(long codigoPersona, DtoInteresProductoRequest request)
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

        public OperationResult<bool> GuardarEncuestaInicial(long codigoPersona, DtoGuardarEncuestaInicialRequest request)
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

            var idComienzoResult = ResolverIdComienzoEncuesta(uow, request, encuesta);
            if (!idComienzoResult.Success)
            {
                return OperationResult<bool>.IsFailed(
                    idComienzoResult.ErrorCode,
                    nameof(GuardarEncuestaInicial),
                    idComienzoResult.Message,
                    idComienzoResult.HttpCode);
            }

            EncuestaInicialAdmisionHelper.AplicarRequestAEncuesta(encuesta, request, persona, idComienzoResult.Data);
            var actualizaPersona = AplicarDatosLaborales(persona, request);

            return PersistirEncuestaInicial(uow, persona, encuesta, request, codigoPersona, esNueva, actualizaPersona);
        }

        private static OperationResult<long?> ResolverIdComienzoEncuesta(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            BusinessLogic.Entities.EncuestaIniAdmision encuesta)
        {
            var idProducto = request.CarreraId ?? encuesta.IdProducto;
            var idProceso = request.ComienzoId ?? encuesta.IdProceso;
            long? idComienzo = encuesta.IdComienzo;

            if (idProducto.HasValue && idProceso.HasValue)
            {
                var idComienzoResult = EncuestaInicialValidationHelper.ObtenerIdComienzoValido(uow, idProducto.Value, idProceso.Value, nameof(GuardarEncuestaInicial));
                if (!idComienzoResult.Success)
                {
                    return OperationResult<long?>.IsFailed(
                        idComienzoResult.ErrorCode,
                        nameof(GuardarEncuestaInicial),
                        idComienzoResult.Message,
                        idComienzoResult.HttpCode);
                }

                idComienzo = idComienzoResult.Data;
            }

            return OperationResult<long?>.Ok(idComienzo, nameof(GuardarEncuestaInicial));
        }

        private OperationResult<bool> PersistirEncuestaInicial(
            IUnitOfWork uow,
            BusinessLogic.Entities.Persona persona,
            BusinessLogic.Entities.EncuestaIniAdmision encuesta,
            DtoGuardarEncuestaInicialRequest request,
            long codigoPersona,
            bool esNueva,
            bool actualizaPersona)
        {
            uow.BeginTransaction();
            try
            {
                if (esNueva)
                {
                    uow.EncuestaIniAdmisions.Add(encuesta);
                }

                EncuestaInicialAdmisionHelper.AplicarListasHijas(uow, _dbConnectionContext, codigoPersona, request);
                if (actualizaPersona)
                {
                    uow.Personas.Update(persona);
                }

                uow.Save();

                var finalizacion = FinalizarEncuesta(uow, persona, encuesta, codigoPersona);
                if (!finalizacion.Success)
                {
                    uow.Rollback();
                    return finalizacion;
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

        private OperationResult<bool> FinalizarEncuesta(
            IUnitOfWork uow,
            BusinessLogic.Entities.Persona persona,
            BusinessLogic.Entities.EncuestaIniAdmision encuesta,
            long codigoPersona)
        {
            var completitud = EncuestaInicialValidationHelper.ResolverCompletitud(uow, encuesta, persona, codigoPersona, nameof(GuardarEncuestaInicial));
            if (!completitud.Success)
            {
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
                var fechaVencimientoResult = _generalService.CalcularFechaVencimientoAdmisiones(uow, codigoPersona, encuesta.IdProceso.Value);
                if (!fechaVencimientoResult.Success)
                {
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
                    return OperationResult<bool>.IsFailed(
                        sincronizacionBachillerato.ErrorCode,
                        nameof(GuardarEncuestaInicial),
                        sincronizacionBachillerato.Message,
                        sincronizacionBachillerato.HttpCode);
                }
            }

            return OperationResult<bool>.Ok(completitud.Data, nameof(GuardarEncuestaInicial));
        }

        private static bool AplicarDatosLaborales(BusinessLogic.Entities.Persona persona, DtoGuardarEncuestaInicialRequest request)
        {
            if (!string.Equals(persona.TipoPersona, PersonaConstants.TipoPersonaSgi, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var actualizaPersona = false;

            if (request.TrabajaActualmente.HasValue)
            {
                persona.TrabajaActualmente = request.TrabajaActualmente.Value ? "S" : "N";
                actualizaPersona = true;
            }

            if (request.TipoJornadaId.HasValue)
            {
                persona.TipoJornada = (byte)request.TipoJornadaId.Value;
                actualizaPersona = true;
            }

            return actualizaPersona;
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
                new DtoTivenosBachilleratoRequest
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
                new DtoTivenosBachilleratoRequest
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

            ultimoAnio = ResolverUltimoAnioBachilleratoLegacy(ultimoAnio);

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

        private static long ResolverUltimoAnioBachilleratoLegacy(long valor)
        {
            return valor is >= 10 and <= 12 ? valor - 6 : valor;
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

        public async Task<OperationResult<DtoConfirmarPreInscripcionResponse>> ConfirmarPreInscripcion(long codigoPersona, DtoConfirmarPreInscripcionRequest request)
        {
            const string methodName = nameof(ConfirmarPreInscripcion);

            var validacionRequest = ConfirmarPreInscripcionHelper.ValidarRequest(request, methodName);
            if (!validacionRequest.Success)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed(
                    validacionRequest.ErrorCode,
                    methodName,
                    validacionRequest.Message,
                    validacionRequest.HttpCode);
            }

            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_05", methodName, PersonaConstants.PersonaNoEncontradaMessage, 404);
            }

            var oferta = uow.Ofertas.GetByKeyWithRelated(request.IdOfertaSeleccionada);
            if (oferta == null)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_15", methodName, "No se encontro la oferta seleccionada.", 404);
            }

            var contextoResult = ConfirmarPreInscripcionHelper.ObtenerContextoConfirmacion(uow, codigoPersona, oferta, methodName);
            if (!contextoResult.Success)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed(
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
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed(validacionDocumentos.ErrorCode, methodName, validacionDocumentos.Message, validacionDocumentos.HttpCode);
            }

            var aceptacion = ConfirmarPreInscripcionHelper.AsegurarAceptacionReglamentoEstudiantil(
                uow,
                _dbConnectionContext,
                codigoPersona,
                contexto.IdProducto,
                contexto.IdComienzo,
                request.AceptoReglamento,
                methodName);
            if (!aceptacion.Success)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed(aceptacion.ErrorCode, methodName, aceptacion.Message, aceptacion.HttpCode);
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

            return confirmacionResult;
        }

        #endregion PASO 2 - ENCUESTA INICIAL y PREINSCRIPCIÓN

        #region PASO 3 - PAGOS

        public async Task<OperationResult<DtoPagarResponse>> Pagar(long codigoPersona, DtoPagarRequest request)
        {
            const string methodName = nameof(Pagar);

            if (request == null)
            {
                return OperationResult<DtoPagarResponse>.IsFailed("INS_PAG_00", methodName, "Request invalido.", 400);
            }

            var tipoPago = request.TipoPago?.Trim().ToUpperInvariant();
            switch (tipoPago)
            {
                case "CUENTA_PERSONAL":
                {
                    var result = await PagarCuentaPersonal(codigoPersona, new DtoPagarCuentaPersonalRequest { IdInscripto = request.IdInscripto });
                    return result.Success
                        ? OperationResult<DtoPagarResponse>.Ok(new DtoPagarResponse { Resultado = "PAGO_CONFIRMADO", Mensajes = result.Data ?? new() }, methodName)
                        : OperationResult<DtoPagarResponse>.IsFailed(result.ErrorCode, methodName, result.Message, result.HttpCode);
                }

                case "ABITAB":
                case "PAGANZA":
                {
                    var result = GuardarMetodoPago(codigoPersona, new DtoGuardarMetodoPagoRequest { IdInscripto = request.IdInscripto, MetodoPago = tipoPago });
                    return result.Success
                        ? OperationResult<DtoPagarResponse>.Ok(new DtoPagarResponse { Resultado = "METODO_GUARDADO" }, methodName)
                        : OperationResult<DtoPagarResponse>.IsFailed(result.ErrorCode, methodName, result.Message, result.HttpCode);
                }

                case "BANRED":
                case "GEOPAY":
                case "SISTARBANC":
                {
                    var result = await ObtenerUrlFactura(codigoPersona, new DtoObtenerUrlFacturaRequest
                    {
                        IdInscripto = request.IdInscripto,
                        TipoPago = tipoPago,
                        IdBancoSistarbanc = request.IdBancoSistarbanc
                    });
                    return result.Success
                        ? OperationResult<DtoPagarResponse>.Ok(new DtoPagarResponse { Resultado = "URL_GENERADA", UrlPago = result.Data }, methodName)
                        : OperationResult<DtoPagarResponse>.IsFailed(result.ErrorCode, methodName, result.Message, result.HttpCode);
                }

                default:
                    return OperationResult<DtoPagarResponse>.IsFailed("INS_PAG_01", methodName, "TipoPago invalido.", 400);
            }
        }

        public async Task<OperationResult<string>> ObtenerUrlFactura(long codigoPersona, DtoObtenerUrlFacturaRequest request)
        {
            const string methodName = nameof(ObtenerUrlFactura);

            var validacion = ValidarRequestInscripcion(request, x => x.IdInscripto, "INS_UF_00", "INS_UF_01", methodName);
            if (!validacion.Success)
                return OperationResult<string>.IsFailed(validacion.ErrorCode, methodName, validacion.Message, validacion.HttpCode);

            var tipoPago = request.TipoPago?.Trim().ToUpperInvariant();
            var tipoPagoNormalizado = tipoPago ?? string.Empty;
            if (!TiposPagoFactura.Contains(tipoPagoNormalizado))
            {
                return OperationResult<string>.IsFailed("INS_UF_02", methodName, "TipoPago invalido.", 400);
            }

            var idBancoSistarbanc = request.IdBancoSistarbanc?.Trim();
            if (tipoPagoNormalizado == "SISTARBANC" && string.IsNullOrWhiteSpace(idBancoSistarbanc))
            {
                return OperationResult<string>.IsFailed("INS_UF_03", methodName, "IdBancoSistarbanc requerido para SISTARBANC.", 400);
            }

            using (var uow = _uowFactory.Create())
            {
                var pertenencia = ValidarPertenenciaInscripto(uow, request.IdInscripto, codigoPersona, "INS_UF_04", methodName);
                if (!pertenencia.Success)
                    return OperationResult<string>.IsFailed(pertenencia.ErrorCode, methodName, pertenencia.Message, pertenencia.HttpCode);
            }

            var banco = tipoPagoNormalizado == "SISTARBANC" ? idBancoSistarbanc! : string.Empty;
            var urlResult = await _inscripcionesyPagosApiClient.ObtenerUrlCrearFacturaPorInscripcionAsync(request.IdInscripto, tipoPagoNormalizado, banco);
            return urlResult.Success
                ? OperationResult<string>.Ok(urlResult.Data, methodName)
                : OperationResult<string>.IsFailed(urlResult.ErrorCode, methodName, urlResult.Message, urlResult.HttpCode);
        }

        public async Task<OperationResult<List<DtoMensajePagoCarrito>>> PagarCuentaPersonal(long codigoPersona, DtoPagarCuentaPersonalRequest request)
        {
            const string methodName = nameof(PagarCuentaPersonal);

            var validacion = ValidarRequestInscripcion(request, x => x.IdInscripto, "INS_PC_00", "INS_PC_01", methodName);
            if (!validacion.Success)
                return OperationResult<List<DtoMensajePagoCarrito>>.IsFailed(validacion.ErrorCode, methodName, validacion.Message, validacion.HttpCode);

            using (var uow = _uowFactory.Create())
            {
                var pertenencia = ValidarPertenenciaInscripto(uow, request.IdInscripto, codigoPersona, "INS_PC_02", methodName);
                if (!pertenencia.Success)
                    return OperationResult<List<DtoMensajePagoCarrito>>.IsFailed(pertenencia.ErrorCode, methodName, pertenencia.Message, pertenencia.HttpCode);
            }

            var pagoResult = await _inscripcionesyPagosApiClient.PagarCarritosPorInscripcionAsync(request.IdInscripto);
            return pagoResult.Success
                ? OperationResult<List<DtoMensajePagoCarrito>>.Ok(pagoResult.Data, methodName)
                : OperationResult<List<DtoMensajePagoCarrito>>.IsFailed(pagoResult.ErrorCode, methodName, pagoResult.Message, pagoResult.HttpCode);
        }

        public OperationResult<bool> GuardarMetodoPago(long codigoPersona, DtoGuardarMetodoPagoRequest request)
        {
            const string methodName = nameof(GuardarMetodoPago);

            var validacion = ValidarRequestInscripcion(request, x => x.IdInscripto, "INS_MP_00", "INS_MP_01", methodName);
            if (!validacion.Success)
                return OperationResult<bool>.IsFailed(validacion.ErrorCode, methodName, validacion.Message, validacion.HttpCode);

            var metodoPago = request.MetodoPago?.Trim().ToUpperInvariant();
            var metodoPagoNormalizado = metodoPago ?? string.Empty;
            if (!MetodosPagoExternos.Contains(metodoPagoNormalizado))
            {
                return OperationResult<bool>.IsFailed("INS_MP_02", methodName, "MetodoPago invalido.", 400);
            }

            using var uow = _uowFactory.Create();

            if (uow.Inscriptos.GetDetalleByKey(request.IdInscripto, codigoPersona) == null)
            {
                return OperationResult<bool>.IsFailed("INS_MP_03", methodName, "No se encontro la inscripcion para la persona.", 404);
            }

            if (uow.InscriptoSeniaMinima.GetByKey(request.IdInscripto) != null)
            {
                return OperationResult<bool>.IsFailed("INS_MP_04", methodName, "La senia minima ya fue registrada para la inscripcion.", 409);
            }

            uow.InscriptoSeniaMinima.Add(new InscriptoSeniaMinimum
            {
                IdInscripto = request.IdInscripto,
                MetodoPagoSeniaMinima = metodoPagoNormalizado
            });
            uow.Save();

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<long> ValidarRequestInscripcion<TRequest>(
            TRequest request,
            Func<TRequest, long> obtenerIdInscripto,
            string codigoRequestInvalido,
            string codigoIdInvalido,
            string methodName)
            where TRequest : class
        {
            if (request == null)
                return OperationResult<long>.IsFailed(codigoRequestInvalido, methodName, "Request invalido.", 400);

            var idInscripto = obtenerIdInscripto(request);
            return idInscripto > 0
                ? OperationResult<long>.Ok(idInscripto, methodName)
                : OperationResult<long>.IsFailed(codigoIdInvalido, methodName, "IdInscripto invalido.", 400);
        }

        private static OperationResult<bool> ValidarPertenenciaInscripto(
            IUnitOfWork uow,
            long idInscripto,
            long codigoPersona,
            string codigoError,
            string methodName)
        {
            return uow.Inscriptos.GetDetalleByKey(idInscripto, codigoPersona) != null
                ? OperationResult<bool>.Ok(true, methodName)
                : OperationResult<bool>.IsFailed(codigoError, methodName, "No se encontro la inscripcion para la persona.", 404);
        }

        #endregion PASO 3 - PAGOS
    }
}

