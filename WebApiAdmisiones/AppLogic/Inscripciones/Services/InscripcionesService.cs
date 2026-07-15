using AppLogic.Inscripciones.Encuesta.Dtos;
using AppLogic.Inscripciones.Dtos;
using AppLogic.ApiClients.Interfaces;
using AppLogic.ApiClients.Dtos;
using AppLogic.Inscripciones.Constants;
using AppLogic.Personas.Constants;
using AppLogic.DevartDTOs;
using AppLogic.Inscripciones.Rules;
using AppLogic.Inscripciones.Interfaces;
using AppLogic.Personas.Services;
using AppLogic.Tivenos.Dtos;
using AppLogic.Tivenos.Interfaces;
using AppLogic.Common.Validation;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Inscripciones.Services
{
    public class InscripcionesService : IInscripcionesService
    {
        private static readonly HashSet<string> TiposPagoFactura = ["BANRED", "SISTARBANC", "GEOPAY"];
        private static readonly HashSet<string> MetodosPagoExternos = ["ABITAB", "PAGANZA"];

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly ITivenosEnvioService _tivenosEnvioService;
        private readonly IInscripcionesyPagosApiClient _inscripcionesyPagosApiClient;
        private readonly IEncuestaInicialService _encuestaInicialService;

        public InscripcionesService(
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            ITivenosEnvioService tivenosEnvioService,
            IInscripcionesyPagosApiClient inscripcionesyPagosApiClient,
            IEncuestaInicialService encuestaInicialService)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _tivenosEnvioService = tivenosEnvioService;
            _inscripcionesyPagosApiClient = inscripcionesyPagosApiClient;
            _encuestaInicialService = encuestaInicialService;
        }

        public async Task<OperationResult<DtoDetalleInscripcionResponse>> ObtenerDetalleInscripcion(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();

            var fresco1y2 = uow.VdInscripcionesFresco1y2s.GetInscripcionFrescoHabilitada(codigoPersona, idProducto, idProceso);
            var estado = fresco1y2?.EstadoInscripcion;
            var idInscripto = (long?)fresco1y2?.IdInscripto;

            if (estado == null)
            {
                var fresco3y4 = uow.VdInscripcionesFresco3y4s.GetInscripcionFrescoHabilitada(codigoPersona, idProducto, idProceso);
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
                    response.Detalle = MapearOfertaResumen(oferta, idProceso);
                    break;

                case InscripcionesConstants.EstadoInscripcion.PagoPendiente:
                    var errorPagoPendiente = await ArmarPagoPendiente(uow, response, codigoPersona, idInscripto);
                    if (errorPagoPendiente != null)
                    {
                        return errorPagoPendiente;
                    }
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
                    response.Confirmada = ConstruirDetalleConfirmada(uow, codigoPersona, inscripto);
                    break;

                // "A la espera" y estados desconocidos: se devuelve solo el estado, sin detalle.
            }

            return OperationResult<DtoDetalleInscripcionResponse>.Ok(response, nameof(ObtenerDetalleInscripcion));
        }

        /// <summary>
        /// Arma el detalle de una inscripción en estado "Pago pendiente". Si ya eligió método de pago
        /// (existe seña mínima) devuelve el bloque compacto <see cref="DtoSeniaMinima"/>; si no, el payload
        /// completo. Devuelve un resultado de error para cortar, o null si completó el response correctamente.
        /// </summary>
        private async Task<OperationResult<DtoDetalleInscripcionResponse>?> ArmarPagoPendiente(
            IUnitOfWork uow, DtoDetalleInscripcionResponse response, long codigoPersona, long? idInscripto)
        {
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

            var seniaMinima = uow.InscriptoSeniaMinima.GetByKey(inscriptoPago.IdInscripto);
            if (seniaMinima != null)
            {
                var persona = uow.Personas.GetByKey(codigoPersona);
                response.SeniaMinima = new DtoSeniaMinima
                {
                    MetodoPago = seniaMinima.MetodoPagoSeniaMinima,
                    Cedula = persona?.Documento?.Trim(),
                    CodigoPersona = codigoPersona,
                    Senia = ConfirmarPreInscripcionRules.SumarSenias(carritos.Data?.Carritos)
                };
            }
            else
            {
                response.PagoPendiente = MapearPagoPendiente(inscriptoPago, carritos.Data);
            }
            return null;
        }

        private static DtoConfirmarPreInscripcionResponse MapearPagoPendiente(Inscripto inscripto, CarritosInscripcionApiResponse? carritos)
        {
            return new DtoConfirmarPreInscripcionResponse
            {
                Confirmada = true,
                IdInscripcion = inscripto.IdInscripto,
                FechaVencimientoPago = inscripto.FechaVtoInscr,
                Senia = ConfirmarPreInscripcionRules.SumarSenias(carritos?.Carritos),
                EstadoCuenta = ConfirmarPreInscripcionRules.MapearEstadoCuenta(carritos?.EstadoCuenta),
                Resumen = MapearResumenDesdeInscripto(inscripto)
            };
        }

        private static DtoConfirmadaDetalle ConstruirDetalleConfirmada(IUnitOfWork uow, long codigoPersona, Inscripto inscripto)
        {
            var coordinadores = uow.VdInscriptoCoordinadores.GetByInscripto(inscripto.IdInscripto);
            var materias = uow.VdInscriptoCreditoAlumnos.GetByInscripto(inscripto.IdInscripto);
            return MapearConfirmada(codigoPersona, inscripto, coordinadores, materias);
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

        private static DtoResumenInscripcion? MapearOfertaResumen(Oferta? oferta, long idProceso)
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
                IdComienzo = idProceso,
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

            var validacion = InteresProductoValidationRules.ValidarRegistroInteresProducto(
                uow,
                codigoPersona,
                request.IdProducto,
                request.IdProcesoSeleccionado,
                nameof(RegistrarInteresProducto));

            if (!validacion.Success)
            {
                return validacion;
            }

            var validacionOferta = InteresProductoValidationRules.ObtenerOfertaValidaParaInteres(
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
                var resultado = InteresProductoRegistroRules.RegistrarInteresProducto(
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

        public OperationResult<DtoObtenerEncuestaInicialResponse> ObtenerEncuestaInicial(long codigoPersona)
        {
            return _encuestaInicialService.ObtenerEncuestaInicial(codigoPersona);
        }

        public OperationResult<DtoGuardarEncuestaInicialResponse> GuardarEncuestaInicial(long codigoPersona, DtoGuardarEncuestaInicialRequest request)
        {
            return _encuestaInicialService.GuardarEncuestaInicial(codigoPersona, request);
        }

        public async Task<OperationResult<DtoConfirmarPreInscripcionResponse>> ConfirmarPreInscripcion(long codigoPersona, DtoConfirmarPreInscripcionRequest request)
        {
            const string methodName = nameof(ConfirmarPreInscripcion);

            var validacionRequest = ConfirmarPreInscripcionRules.ValidarRequest(request, methodName);
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

            var contextoResult = ConfirmarPreInscripcionRules.ObtenerContextoConfirmacion(uow, codigoPersona, oferta, methodName);
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

            var aceptacion = ConfirmarPreInscripcionRules.AsegurarAceptacionReglamentoEstudiantil(
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

            var apiRequest = ConfirmarPreInscripcionRules.CrearApiRequest(contexto, request.IdOfertaSeleccionada);
            var apiResult = await _inscripcionesyPagosApiClient.ConfirmarPreInscripcionAsync(apiRequest);
            var confirmacionResult = ConfirmarPreInscripcionRules.MapearResultadoApi(apiResult, contexto, methodName);
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

        public async Task<OperationResult<DtoConfirmarPreInscripcionResponse>> ReactivarInscripcion(long codigoPersona, DtoReactivarInscripcionRequest request)
        {
            const string methodName = nameof(ReactivarInscripcion);

            if (request == null || request.IdInscripto <= 0)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_REA_00", methodName, "Request invalido.", 400);
            }

            using var uow = _uowFactory.Create();

            var inscriptoBaja = uow.Inscriptos.GetDetalleByKey(request.IdInscripto, codigoPersona);
            if (inscriptoBaja == null)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_REA_01", methodName, "No se encontro la inscripcion para la persona.", 404);
            }

            if (inscriptoBaja.BajaInscr == null || inscriptoBaja.IdOferta == null)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_REA_02", methodName, "La inscripcion indicada no esta dada de baja.", 409);
            }

            return await ConfirmarPreInscripcion(codigoPersona, new DtoConfirmarPreInscripcionRequest
            {
                IdOfertaSeleccionada = inscriptoBaja.IdOferta.Value,
                AceptoReglamento = true
            });
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
                    if (!result.Success)
                        return OperationResult<DtoPagarResponse>.IsFailed(result.ErrorCode, methodName, result.Message, result.HttpCode);

                    using var uow = _uowFactory.Create();
                    var inscripto = uow.Inscriptos.GetDetalleByKey(request.IdInscripto, codigoPersona);
                    var detalle = inscripto != null ? ConstruirDetalleConfirmada(uow, codigoPersona, inscripto) : null;
                    return OperationResult<DtoPagarResponse>.Ok(
                        new DtoPagarResponse { Resultado = "PAGO_CONFIRMADO", Mensajes = result.Data ?? new(), Confirmada = detalle },
                        methodName);
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
                        ? OperationResult<DtoPagarResponse>.Ok(new DtoPagarResponse
                        {
                            Resultado = "URL_GENERADA",
                            UrlPago = result.Data!.Url,
                            ParametrosEncriptados = result.Data.ParametrosEncriptados
                        }, methodName)
                        : OperationResult<DtoPagarResponse>.IsFailed(result.ErrorCode, methodName, result.Message, result.HttpCode);
                }

                default:
                    return OperationResult<DtoPagarResponse>.IsFailed("INS_PAG_01", methodName, "TipoPago invalido.", 400);
            }
        }

        private async Task<OperationResult<DtoObtenerUrlFacturaResponse>> ObtenerUrlFactura(long codigoPersona, DtoObtenerUrlFacturaRequest request)
        {
            const string methodName = nameof(ObtenerUrlFactura);

            var validacion = ValidarRequestInscripcion(request, x => x.IdInscripto, "INS_UF_00", "INS_UF_01", methodName);
            if (!validacion.Success)
                return OperationResult<DtoObtenerUrlFacturaResponse>.IsFailed(validacion.ErrorCode, methodName, validacion.Message, validacion.HttpCode);

            var tipoPago = request.TipoPago?.Trim().ToUpperInvariant();
            var tipoPagoNormalizado = tipoPago ?? string.Empty;
            if (!TiposPagoFactura.Contains(tipoPagoNormalizado))
            {
                return OperationResult<DtoObtenerUrlFacturaResponse>.IsFailed("INS_UF_02", methodName, "TipoPago invalido.", 400);
            }

            var idBancoSistarbanc = request.IdBancoSistarbanc?.Trim();
            if (tipoPagoNormalizado == "SISTARBANC" && string.IsNullOrWhiteSpace(idBancoSistarbanc))
            {
                return OperationResult<DtoObtenerUrlFacturaResponse>.IsFailed("INS_UF_03", methodName, "IdBancoSistarbanc requerido para SISTARBANC.", 400);
            }

            using (var uow = _uowFactory.Create())
            {
                var pertenencia = ValidarPertenenciaInscripto(uow, request.IdInscripto, codigoPersona, "INS_UF_04", methodName);
                if (!pertenencia.Success)
                    return OperationResult<DtoObtenerUrlFacturaResponse>.IsFailed(pertenencia.ErrorCode, methodName, pertenencia.Message, pertenencia.HttpCode);
            }

            var banco = tipoPagoNormalizado == "SISTARBANC" ? idBancoSistarbanc! : string.Empty;
            var urlResult = await _inscripcionesyPagosApiClient.ObtenerUrlCrearFacturaPorInscripcionAsync(request.IdInscripto, tipoPagoNormalizado, banco);
            if (!urlResult.Success)
            {
                return OperationResult<DtoObtenerUrlFacturaResponse>.IsFailed(urlResult.ErrorCode, methodName, urlResult.Message, urlResult.HttpCode);
            }

            var (url, parametrosEncriptados) = SepararUrlYParametrosEncriptados(urlResult.Data);
            return OperationResult<DtoObtenerUrlFacturaResponse>.Ok(
                new DtoObtenerUrlFacturaResponse { Url = url, ParametrosEncriptados = parametrosEncriptados },
                methodName);
        }

        private const string MarcadorParametrosEncriptados = "parametrosEncriptados=";

        private static (string Url, string? ParametrosEncriptados) SepararUrlYParametrosEncriptados(string urlCompleta)
        {
            var idx = urlCompleta.IndexOf(MarcadorParametrosEncriptados, StringComparison.Ordinal);
            if (idx <= 0)
            {
                return (urlCompleta, null);
            }

            var url = urlCompleta[..(idx - 1)];
            var parametrosEncriptados = Uri.UnescapeDataString(urlCompleta[(idx + MarcadorParametrosEncriptados.Length)..]);
            return (url, parametrosEncriptados);
        }

        private async Task<OperationResult<List<DtoMensajePagoCarrito>>> PagarCuentaPersonal(long codigoPersona, DtoPagarCuentaPersonalRequest request)
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

        private OperationResult<bool> GuardarMetodoPago(long codigoPersona, DtoGuardarMetodoPagoRequest request)
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

