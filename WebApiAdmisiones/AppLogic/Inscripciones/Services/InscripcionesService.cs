using AppLogic.Inscripciones.Encuesta.Dtos;
using AppLogic.Inscripciones.Dtos;
using AppLogic.ApiClients.Interfaces;
using AppLogic.ApiClients.Dtos;
using AppLogic.Inscripciones.Constants;
using AppLogic.Inscripciones.Mappers;
using AppLogic.Personas.Constants;
using AppLogic.DevartDTOs;
using AppLogic.Inscripciones.Rules;
using AppLogic.Inscripciones.Interfaces;
using AppLogic.Personas.Services;
using AppLogic.Tivenos.Dtos;
using AppLogic.Tivenos.Interfaces;
using AppLogic.Common.Validation;
using AppLogic.Helpers;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.DependencyInjection;
using ModBandejaAppLogic.Interfaces;
using Utilities;

namespace AppLogic.Inscripciones.Services;

public class InscripcionesService(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext,
    ITivenosEnvioService tivenosEnvioService,
    IInscripcionesyPagosApiClient inscripcionesyPagosApiClient,
    IEncuestaInicialService encuestaInicialService,
    IServiceScopeFactory serviceScopeFactory) : IInscripcionesService
{
    private static readonly HashSet<string> TiposPagoFactura =
        [InscripcionesConstants.TipoPago.Banred, InscripcionesConstants.TipoPago.Sistarbanc, InscripcionesConstants.TipoPago.Geopay];
    private static readonly HashSet<string> MetodosPagoExternos =
        [InscripcionesConstants.TipoPago.Abitab, InscripcionesConstants.TipoPago.Paganza];

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;
    private readonly ITivenosEnvioService _tivenosEnvioService = tivenosEnvioService;
    private readonly IInscripcionesyPagosApiClient _inscripcionesyPagosApiClient = inscripcionesyPagosApiClient;
    private readonly IEncuestaInicialService _encuestaInicialService = encuestaInicialService;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    public async Task<OperationResult<DtoDetalleInscripcionResponse>> ObtenerDetalleInscripcion(long codigoPersona, long idProducto, long idProceso)
    {
        using var uow = _uowFactory.Create();

        var inscripcionNivel1y2 = uow.VdInscripcionesFresco1y2s.GetInscripcionFrescoHabilitada(codigoPersona, idProducto, idProceso);
        var estado = inscripcionNivel1y2?.EstadoInscripcion;
        var idInscripto = (long?)inscripcionNivel1y2?.IdInscripto;

        if (estado == null)
        {
            var inscripcionNivel3y4 = uow.VdInscripcionesFresco3y4s.GetInscripcionFrescoHabilitada(codigoPersona, idProducto, idProceso);
            estado = inscripcionNivel3y4?.EstadoInscripcion;
            idInscripto = (long?)inscripcionNivel3y4?.IdInscripto;
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
                var ofertas = uow.InteresProductoOfertas.GetOfertasSeleccionadas(codigoPersona, idProducto, idProceso);
                response.Detalle = ofertas.Count > 0 ? InscripcionesMapper.MapearDetalleEnProceso(ofertas) : null;
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
    /// (existe reserva mínima) devuelve el bloque compacto <see cref="DtoReservaMinima"/>; si no, el payload
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
            return carritos.Failure().As<DtoDetalleInscripcionResponse>(nameof(ObtenerDetalleInscripcion));

        var reservaMinima = uow.InscriptoSeniaMinima.GetByKey(inscriptoPago.IdInscripto);
        if (reservaMinima != null)
        {
            var persona = uow.Personas.GetByKey(codigoPersona);
            response.ReservaMinima = new DtoReservaMinima
            {
                TipoPago = reservaMinima.MetodoPagoSeniaMinima,
                Cedula = persona?.Documento?.Trim(),
                CodigoPersona = codigoPersona,
                PagoReserva = ConfirmarPreInscripcionRules.SumarPagoReserva(carritos.Data?.Carritos)
            };
        }
        else
        {
            response.PagoPendiente = InscripcionesMapper.MapearPagoPendiente(inscriptoPago, carritos.Data);
        }
        return null;
    }

    private static DtoConfirmadaDetalle ConstruirDetalleConfirmada(IUnitOfWork uow, long codigoPersona, Inscripto inscripto)
    {
        var coordinadores = uow.VdInscriptoCoordinadores.GetByInscripto(inscripto.IdInscripto);
        var materias = uow.VdInscriptoCreditoAlumnos.GetByInscripto(inscripto.IdInscripto);
        return InscripcionesMapper.MapearConfirmada(codigoPersona, inscripto, coordinadores, materias);
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

        var validacionOfertasSolicitadas = InteresProductoValidation.ValidarOfertasSolicitadas(
            request.IdsOferta,
            nameof(RegistrarInteresProducto));
        if (!validacionOfertasSolicitadas.Success)
        {
            return validacionOfertasSolicitadas;
        }

        var validacion = InteresProductoValidation.ValidarRegistroInteresProducto(
            uow,
            codigoPersona,
            request.IdProducto,
            request.IdProcesoSeleccionado,
            nameof(RegistrarInteresProducto));

        if (!validacion.Success)
        {
            return validacion;
        }

        var ofertasValidas = ValidarYObtenerOfertas(uow, request);
        if (!ofertasValidas.Success)
            return ofertasValidas.Failure().As<bool>(nameof(RegistrarInteresProducto));

        return RegistrarInteresYEncolarTivenos(uow, codigoPersona, request, ofertasValidas.Data!);
    }

    /// <summary>Valida cada oferta solicitada y devuelve las entidades cargadas, o el primer error.</summary>
    private static OperationResult<List<Oferta>> ValidarYObtenerOfertas(IUnitOfWork uow, DtoInteresProductoRequest request)
    {
        var ofertas = new List<Oferta>();
        foreach (var idOferta in request.IdsOferta)
        {
            var validacionOferta = InteresProductoValidation.ObtenerOfertaValidaParaInteres(
                uow,
                idOferta,
                request.IdProducto,
                request.IdProcesoSeleccionado,
                nameof(RegistrarInteresProducto));
            if (!validacionOferta.Success)
                return validacionOferta.Failure().As<List<Oferta>>(nameof(RegistrarInteresProducto));

            ofertas.Add(validacionOferta.Data!);
        }

        return OperationResult<List<Oferta>>.Ok(ofertas, nameof(RegistrarInteresProducto));
    }

    /// <summary>
    /// Registra el interés por producto y encola el alta en Tivenos dentro de una única transacción.
    /// Hace rollback ante cualquier error o excepción.
    /// </summary>
    private OperationResult<bool> RegistrarInteresYEncolarTivenos(
        IUnitOfWork uow, long codigoPersona, DtoInteresProductoRequest request, List<Oferta> ofertas)
    {
        var fechaActual = DateTime.Now;

        uow.BeginTransaction();
        try
        {
            var resultado = InteresProductoRegistroRules.RegistrarInteresProducto(
                uow,
                _dbConnectionContext,
                codigoPersona,
                request,
                ofertas,
                fechaActual,
                nameof(RegistrarInteresProducto));
            if (!resultado.Success)
            {
                uow.Rollback();
                return resultado.Failure().As<bool>(nameof(RegistrarInteresProducto));
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
                    return resultadoTivenos.Failure().As<bool>(nameof(RegistrarInteresProducto));
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

    /// <summary>
    /// Confirma la preinscripcion de una o varias ofertas (nivel 1 y 2 manda una unica oferta en la
    /// lista, nivel 3 y 4 puede mandar varias). Siempre llama a la variante multiple de LogicaORT,
    /// que soporta ambos casos en una unica transaccion.
    /// </summary>
    public async Task<OperationResult<DtoConfirmarPreInscripcionResponse>> ConfirmarPreInscripcion(long codigoPersona, DtoConfirmarPreInscripcionRequest request)
    {
        const string methodName = nameof(ConfirmarPreInscripcion);

        var validacionRequest = ConfirmarPreInscripcionRules.ValidarRequest(request, methodName);
        if (!validacionRequest.Success)
            return validacionRequest.Failure().As<DtoConfirmarPreInscripcionResponse>(methodName);

        using var uow = _uowFactory.Create();

        var persona = uow.Personas.GetByKey(codigoPersona);
        if (persona == null)
        {
            return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_05", methodName, PersonaConstants.PersonaNoEncontradaMessage, 404);
        }

        var ofertasCompatibles = ValidarOfertasCompatibles(uow, codigoPersona, request, methodName);
        if (!ofertasCompatibles.Success)
            return ofertasCompatibles.Failure().As<DtoConfirmarPreInscripcionResponse>(methodName);
        var ofertasSeleccionadas = ofertasCompatibles.Data!;
        var contexto = ofertasSeleccionadas[0];

        var validacionDocumentos = DocumentoIdentidadPersonaService.ValidarDocumentosIdentidadParaConfirmacion(
            uow,
            persona,
            methodName);
        if (!validacionDocumentos.Success)
        {
            return validacionDocumentos.Failure().As<DtoConfirmarPreInscripcionResponse>(methodName);
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
            return aceptacion.Failure().As<DtoConfirmarPreInscripcionResponse>(methodName);
        }

        if (request.EsInscripcionCorporativa)
            return ConfirmarInscripcionCorporativa(uow, codigoPersona, persona, ofertasSeleccionadas, methodName);

        return await ConfirmarInscripcionOnlineAsync(uow, contexto, ofertasSeleccionadas, request, methodName);
    }

    /// <summary>Un tramite + una instancia + una fila en T_INST_WORKFLOW_INSCRIPCION por oferta.</summary>
    private OperationResult<DtoConfirmarPreInscripcionResponse> ConfirmarInscripcionCorporativa(
        IUnitOfWork uow,
        long codigoPersona,
        Persona persona,
        List<DatosConfirmacionOferta> ofertasSeleccionadas,
        string methodName)
    {
        var contexto = ofertasSeleccionadas[0];
        var validacionNivel = ConfirmarPreInscripcionRules.ValidarNivelCorporativo(contexto, methodName);
        if (!validacionNivel.Success)
            return validacionNivel.Failure().As<DtoConfirmarPreInscripcionResponse>(methodName);

        foreach (var oferta in ofertasSeleccionadas)
        {
            var xml = ConfirmarPreInscripcionRules.CrearXmlInstanciaCorporativa(oferta, persona);
            var dtoTramite = ConfirmarPreInscripcionRules.CrearDtoTramiteCorporativo(codigoPersona);
            var dtoInstancia = ConfirmarPreInscripcionRules.CrearDtoInstanciaCorporativa(oferta, codigoPersona, xml);
            var dtosBandeja = ConfirmarPreInscripcionRules.CrearBandejasCorporativas(InscripcionesConstants.BandejaCorporativa.UsuarioSistema);

            // El IBandejaService del modulo Bandeja (Core) dispone su DbContext al terminar cada
            // llamada, aunque ese contexto es compartido (scoped) por DI. Con varias ofertas este
            // metodo se llama mas de una vez por request, y la segunda llamada reventaria con
            // ObjectDisposedException si reusara el service del scope principal. Se resuelve en un
            // scope de DI propio por oferta para que cada llamada tenga su propio contexto.
            using var bandejaScope = _serviceScopeFactory.CreateScope();
            var bandejaService = bandejaScope.ServiceProvider.GetRequiredService<IBandejaService>();
            var altaResult = bandejaService.AltaTramiteWorkflow(dtoTramite, dtoInstancia, dtosBandeja);

            if (!altaResult.Success)
                return altaResult.Failure().As<DtoConfirmarPreInscripcionResponse>(methodName);

            uow.InstWorkflowInscripcions.Add(
                ConfirmarPreInscripcionRules.CrearInstWorkflowInscripcionCorporativa(oferta, altaResult.Data));
            uow.Save();
        }

        return OperationResult<DtoConfirmarPreInscripcionResponse>.Ok(
            ConfirmarPreInscripcionRules.MapearResultadoCorporativo(), methodName);
    }

    private async Task<OperationResult<DtoConfirmarPreInscripcionResponse>> ConfirmarInscripcionOnlineAsync(
        IUnitOfWork uow,
        DatosConfirmacionOferta contexto,
        List<DatosConfirmacionOferta> ofertasSeleccionadas,
        DtoConfirmarPreInscripcionRequest request,
        string methodName)
    {
        var apiRequest = ConfirmarPreInscripcionRules.CrearApiRequestMultiple(contexto, request.IdsOfertasSeleccionadas);
        var apiResult = await _inscripcionesyPagosApiClient.ConfirmarPreInscripcionMultipleAsync(apiRequest);

        // La descripción de oferta solo existe para nivel 3 y 4 (vista de ofertas disponibles); en nivel 1 y 2 el diccionario queda vacío.
        var descripcionesOferta = uow.VdOfertasDisponibles3y4s.GetOfertasDisponibles(contexto.IdProducto)
            .GroupBy(o => o.IdOferta)
            .ToDictionary(g => g.Key, g => g.First().DescripcionOferta);
        return InscripcionesMapper.MapearResultadoApiMultiple(apiResult, ofertasSeleccionadas, descripcionesOferta, methodName);
    }

    /// <summary>
    /// Recorre las ofertas seleccionadas, valida cada una y verifica que sean compatibles entre sí
    /// (mismo producto y turno, y mismo comienzo para nivel 1 y 2). Devuelve los datos de confirmación
    /// consolidados de la selección, o el primer error encontrado.
    /// </summary>
    private static OperationResult<List<DatosConfirmacionOferta>> ValidarOfertasCompatibles(
        IUnitOfWork uow, long codigoPersona, DtoConfirmarPreInscripcionRequest request, string methodName)
    {
        var seleccionadas = new List<DatosConfirmacionOferta>();
        foreach (var idOfertaSeleccionada in request.IdsOfertasSeleccionadas)
        {
            var oferta = uow.Ofertas.GetByKeyWithRelated(idOfertaSeleccionada);
            if (oferta == null)
            {
                return OperationResult<List<DatosConfirmacionOferta>>.IsFailed("INS_CPI_15", methodName, $"No se encontro la oferta seleccionada: {idOfertaSeleccionada}.", 404);
            }

            var datosOferta = ConfirmarPreInscripcionRules.ObtenerDatosConfirmacionOferta(uow, codigoPersona, oferta, methodName);
            if (!datosOferta.Success)
                return datosOferta.Failure().As<List<DatosConfirmacionOferta>>(methodName);

            if (seleccionadas.Count > 0 && !EsOfertaCompatibleConSeleccion(seleccionadas[0], datosOferta.Data!))
            {
                return OperationResult<List<DatosConfirmacionOferta>>.IsFailed(
                    "INS_CPI_17",
                    methodName,
                    "Todas las ofertas seleccionadas deben pertenecer al mismo producto y turno (y al mismo comienzo para nivel 1 y 2).",
                    400);
            }

            seleccionadas.Add(datosOferta.Data!);
        }

        return OperationResult<List<DatosConfirmacionOferta>>.Ok(seleccionadas, methodName);
    }

    private static bool EsOfertaCompatibleConSeleccion(DatosConfirmacionOferta seleccion, DatosConfirmacionOferta oferta)
    {
        if (seleccion.IdProducto != oferta.IdProducto || seleccion.IdTurno != oferta.IdTurno)
        {
            return false;
        }

        return !RequiereMismoComienzoEntreOfertas(seleccion) || seleccion.IdComienzo == oferta.IdComienzo;
    }

    /// <summary>Nivel 1 y 2 exigen que todas las ofertas compartan comienzo; nivel 3 y 4 no.</summary>
    private static bool RequiereMismoComienzoEntreOfertas(DatosConfirmacionOferta datos)
    {
        return datos.Producto?.IdNivelProducto is not (3 or 4);
    }

    public async Task<OperationResult<DtoConfirmarPreInscripcionResponse>> ReactivarInscripcion(long codigoPersona, DtoReactivarInscripcionRequest request)
    {
        const string methodName = nameof(ReactivarInscripcion);

        if (request == null || request.IdInscripcion <= 0)
        {
            return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_REA_00", methodName, "Request invalido.", 400);
        }

        using var uow = _uowFactory.Create();

        var inscriptoBaja = uow.Inscriptos.GetDetalleByKey(request.IdInscripcion, codigoPersona);
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
            IdsOfertasSeleccionadas = [inscriptoBaja.IdOferta.Value],
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
            case InscripcionesConstants.TipoPago.CuentaPersonal:
            {
                var result = await PagarCuentaPersonal(codigoPersona, new DtoPagarCuentaPersonalRequest { IdInscripcion = request.IdInscripcion });
                if (!result.Success)
                    return result.Failure().As<DtoPagarResponse>(methodName);

                using var uow = _uowFactory.Create();
                var inscripto = uow.Inscriptos.GetDetalleByKey(request.IdInscripcion, codigoPersona);
                var detalle = inscripto != null ? ConstruirDetalleConfirmada(uow, codigoPersona, inscripto) : null;
                return OperationResult<DtoPagarResponse>.Ok(
                    new DtoPagarResponse { Resultado = InscripcionesConstants.ResultadoPago.PagoConfirmado, Mensajes = result.Data ?? new(), Confirmada = detalle },
                    methodName);
            }

            case InscripcionesConstants.TipoPago.Abitab:
            case InscripcionesConstants.TipoPago.Paganza:
            {
                var result = GuardarMetodoPago(codigoPersona, new DtoGuardarMetodoPagoRequest { IdInscripcion = request.IdInscripcion, TipoPago = tipoPago });
                return result.Success
                    ? OperationResult<DtoPagarResponse>.Ok(new DtoPagarResponse { Resultado = InscripcionesConstants.ResultadoPago.MetodoGuardado }, methodName)
                    : result.Failure().As<DtoPagarResponse>(methodName);
            }

            case InscripcionesConstants.TipoPago.Banred:
            case InscripcionesConstants.TipoPago.Geopay:
            case InscripcionesConstants.TipoPago.Sistarbanc:
            {
                var result = await ObtenerUrlFactura(codigoPersona, new DtoObtenerUrlFacturaRequest
                {
                    IdInscripcion = request.IdInscripcion,
                    TipoPago = tipoPago,
                    IdBancoSistarbanc = request.IdBancoSistarbanc
                });
                return result.Success
                    ? OperationResult<DtoPagarResponse>.Ok(new DtoPagarResponse
                    {
                        Resultado = InscripcionesConstants.ResultadoPago.UrlGenerada,
                        UrlPago = result.Data!.Url,
                        ParametrosEncriptados = result.Data.ParametrosEncriptados
                    }, methodName)
                    : result.Failure().As<DtoPagarResponse>(methodName);
            }

            default:
                return OperationResult<DtoPagarResponse>.IsFailed("INS_PAG_01", methodName, "TipoPago invalido.", 400);
        }
    }

    private async Task<OperationResult<DtoObtenerUrlFacturaResponse>> ObtenerUrlFactura(long codigoPersona, DtoObtenerUrlFacturaRequest request)
    {
        const string methodName = nameof(ObtenerUrlFactura);

        var validacion = ValidarRequestInscripcion(request, x => x.IdInscripcion, "INS_UF_00", "INS_UF_01", methodName);
        if (!validacion.Success)
            return validacion.Failure().As<DtoObtenerUrlFacturaResponse>(methodName);

        var tipoPago = request.TipoPago?.Trim().ToUpperInvariant();
        var tipoPagoNormalizado = tipoPago ?? string.Empty;
        if (!TiposPagoFactura.Contains(tipoPagoNormalizado))
        {
            return OperationResult<DtoObtenerUrlFacturaResponse>.IsFailed("INS_UF_02", methodName, "TipoPago invalido.", 400);
        }

        var idBancoSistarbanc = request.IdBancoSistarbanc?.Trim();
        if (tipoPagoNormalizado == InscripcionesConstants.TipoPago.Sistarbanc && string.IsNullOrWhiteSpace(idBancoSistarbanc))
        {
            return OperationResult<DtoObtenerUrlFacturaResponse>.IsFailed("INS_UF_03", methodName, "IdBancoSistarbanc requerido para SISTARBANC.", 400);
        }

        using (var uow = _uowFactory.Create())
        {
            var pertenencia = ValidarPertenenciaInscripto(uow, request.IdInscripcion, codigoPersona, "INS_UF_04", methodName);
            if (!pertenencia.Success)
                return pertenencia.Failure().As<DtoObtenerUrlFacturaResponse>(methodName);
        }

        var banco = tipoPagoNormalizado == InscripcionesConstants.TipoPago.Sistarbanc ? idBancoSistarbanc! : string.Empty;
        var urlResult = await _inscripcionesyPagosApiClient.ObtenerUrlCrearFacturaPorInscripcionAsync(request.IdInscripcion, tipoPagoNormalizado, banco);
        if (!urlResult.Success)
            return urlResult.Failure().As<DtoObtenerUrlFacturaResponse>(methodName);

        var (url, parametrosEncriptados) = SepararUrlYParametrosEncriptados(urlResult.Data);
        return OperationResult<DtoObtenerUrlFacturaResponse>.Ok(
            new DtoObtenerUrlFacturaResponse { Url = url, ParametrosEncriptados = parametrosEncriptados },
            methodName);
    }

    private const string MarcadorParametrosEncriptados = "parametrosEncriptados=";

    private static (string Url, string? ParametrosEncriptados) SepararUrlYParametrosEncriptados(string urlCompleta)
    {
        var indiceMarcador = urlCompleta.IndexOf(MarcadorParametrosEncriptados, StringComparison.Ordinal);
        if (indiceMarcador <= 0)
        {
            return (urlCompleta, null);
        }

        var url = urlCompleta[..(indiceMarcador - 1)];
        var parametrosEncriptados = Uri.UnescapeDataString(urlCompleta[(indiceMarcador + MarcadorParametrosEncriptados.Length)..]);
        return (url, parametrosEncriptados);
    }

    private async Task<OperationResult<List<DtoMensajePagoCarrito>>> PagarCuentaPersonal(long codigoPersona, DtoPagarCuentaPersonalRequest request)
    {
        const string methodName = nameof(PagarCuentaPersonal);

        var validacion = ValidarRequestInscripcion(request, x => x.IdInscripcion, "INS_PC_00", "INS_PC_01", methodName);
        if (!validacion.Success)
            return validacion.Failure().As<List<DtoMensajePagoCarrito>>(methodName);

        using (var uow = _uowFactory.Create())
        {
            var pertenencia = ValidarPertenenciaInscripto(uow, request.IdInscripcion, codigoPersona, "INS_PC_02", methodName);
            if (!pertenencia.Success)
                return pertenencia.Failure().As<List<DtoMensajePagoCarrito>>(methodName);
        }

        var pagoResult = await _inscripcionesyPagosApiClient.PagarCarritosPorInscripcionAsync(request.IdInscripcion);
        return pagoResult.Success
            ? OperationResult<List<DtoMensajePagoCarrito>>.Ok(pagoResult.Data, methodName)
            : pagoResult.Failure().As<List<DtoMensajePagoCarrito>>(methodName);
    }

    private OperationResult<bool> GuardarMetodoPago(long codigoPersona, DtoGuardarMetodoPagoRequest request)
    {
        const string methodName = nameof(GuardarMetodoPago);

        var validacion = ValidarRequestInscripcion(request, x => x.IdInscripcion, "INS_MP_00", "INS_MP_01", methodName);
        if (!validacion.Success)
            return validacion.Failure().As<bool>(methodName);

        var tipoPagoNormalizado = request.TipoPago?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!MetodosPagoExternos.Contains(tipoPagoNormalizado))
        {
            return OperationResult<bool>.IsFailed("INS_MP_02", methodName, "TipoPago invalido.", 400);
        }

        using var uow = _uowFactory.Create();

        if (uow.Inscriptos.GetDetalleByKey(request.IdInscripcion, codigoPersona) == null)
        {
            return OperationResult<bool>.IsFailed("INS_MP_03", methodName, "No se encontro la inscripcion para la persona.", 404);
        }

        if (uow.InscriptoSeniaMinima.GetByKey(request.IdInscripcion) != null)
        {
            return OperationResult<bool>.IsFailed("INS_MP_04", methodName, "La reserva minima ya fue registrada para la inscripcion.", 409);
        }

        uow.InscriptoSeniaMinima.Add(new InscriptoSeniaMinimum
        {
            IdInscripto = request.IdInscripcion,
            MetodoPagoSeniaMinima = tipoPagoNormalizado
        });
        uow.Save();

        return OperationResult<bool>.Ok(true, methodName);
    }

    private static OperationResult<long> ValidarRequestInscripcion<TRequest>(
        TRequest request,
        Func<TRequest, long> obtenerIdInscripcion,
        string codigoRequestInvalido,
        string codigoIdInvalido,
        string methodName)
        where TRequest : class
    {
        if (request == null)
            return OperationResult<long>.IsFailed(codigoRequestInvalido, methodName, "Request invalido.", 400);

        var idInscripcion = obtenerIdInscripcion(request);
        return idInscripcion > 0
            ? OperationResult<long>.Ok(idInscripcion, methodName)
            : OperationResult<long>.IsFailed(codigoIdInvalido, methodName, "IdInscripcion invalido.", 400);
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

