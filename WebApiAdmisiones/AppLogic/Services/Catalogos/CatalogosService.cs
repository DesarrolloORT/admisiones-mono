using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices.Catalogos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services.Catalogos
{
    public class CatalogosService : ICatalogosService
    {
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly InscripcionesyPagosApiClient? _inscripcionesyPagosApiClient;

        public CatalogosService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public CatalogosService(IUnitOfWorkFactory uowFactory, InscripcionesyPagosApiClient inscripcionesyPagosApiClient)
        {
            _uowFactory = uowFactory;
            _inscripcionesyPagosApiClient = inscripcionesyPagosApiClient;
        }

        public OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>> ObtenerPaisesEstadosCiudades()
        {
            using var uow = _uowFactory.Create();

            var paises = uow.Paises.GetPaisesConEstadosYCiudades().ToList();
            var response = paises.Select(pais => pais.ToPaisesEstadosCiudadesDto()).ToList();

            return OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>.Ok(response, nameof(ObtenerPaisesEstadosCiudades));
        }

        public OperationResult<DtoEncuestaInicialCatalogosResponse> ObtenerEncuestaInicial()
        {
            using var uow = _uowFactory.Create();

            var response = new DtoEncuestaInicialCatalogosResponse
            {
                NivelConocimiento =
                [
                    Combo(1, "Ninguno"),
                    Combo(2, "Básico"),
                    Combo(3, "Medio"),
                    Combo(4, "Superior")
                ],
                OpcionesEMS =
                [
                    Combo(2, "1° EMS (4° año)"),
                    Combo(3, "2° EMS (5° año)"),
                    Combo(4, "3° EMS (6° año)"),
                    Combo(0, "Otro")
                ],
                CompartidoCon =
                [
                    Combo(1, "Padres u otros familiares"),
                    Combo(2, "Amigos de la familia"),
                    Combo(3, "Amigos propios, compañeros"),
                    Combo(4, "Otros"),
                    Combo(5, "Nadie")
                ],
                FormacionTutores =
                [
                    Combo(1, "Primaria"),
                    Combo(2, "Secundaria"),
                    Combo(3, "Formación técnica"),
                    Combo(4, "Formación universitaria incompleta"),
                    Combo(5, "Formación universitaria completa"),
                    Combo(6, "Estudios de postgrado"),
                    Combo(7, "Otros estudios")
                ],
                EstadoEducacionSuperior =
                [
                    Combo(3, "Egresado"),
                    Combo(1, "En curso"),
                    Combo(2, "Abandonado")
                ],
                AniosAprobadosEducacionSuperior =
                [
                    Combo(13, "1 año"),
                    Combo(14, "2 años"),
                    Combo(15, "3 años"),
                    Combo(16, "4 años"),
                    Combo(17, "5 años"),
                    Combo(18, "6 años"),
                    Combo(19, "7 años"),
                    Combo(20, "8 años o más")
                ],
                MotivosEleccion = uow.MotivoOpcionesAdmisions.GetAll().ToDtos(),
                PublicidadesEleccion = uow.PublicidadOpcionesAdmisions.GetAll().ToDtos(),
                AniosBachiller = uow.AnioBachillers.GetAllWithRelated()
                    .Select(a => new DtoAnioBachilleratoCatalogo
                    {
                        IdAnioBachiller = a.IdAnioBachiller,
                        NombreAnioBachiller = a.NombreAnioBachiller,
                        CantAniosAnioBachiller = a.CantAniosAnioBachiller,
                        Bachilleratos = a.Titulos
                            .Select(t => new DtoBachilleratoCatalogo
                            {
                                CodigoTitulo = t.CodigoTitulo,
                                Nombre = t.Nombre,
                                OrientacionTitulo = t.OrientacionTitulo,
                                OrientacionNewTitulo = t.OrientacionNewTitulo
                            })
                            .ToList()
                    })
                    .ToList(),
                Universidades = uow.Empresas.GetUniversidades()
                    .Select(e => new DtoUniversidadCatalogo
                    {
                        CodigoEmpresa = e.CodigoEmpresa,
                        Nombre = e.Nombre
                    })
                    .ToList()
            };

            return OperationResult<DtoEncuestaInicialCatalogosResponse>.Ok(response, nameof(ObtenerEncuestaInicial));
        }

        private static DtoComboOption Combo(int value, string label) => new()
        {
            Value = value,
            Label = label
        };

        // Versión async para compatibilidad con controllers async
        public Task<OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>> ObtenerPaisesEstadosCiudadesAsync()
        {
            return Task.FromResult(ObtenerPaisesEstadosCiudades());
        }

        public OperationResult<IEnumerable<DtoCarreraResponse>> ObtenerCarreras()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosVigentes();
            return OperationResult<IEnumerable<DtoCarreraResponse>>.Ok(entidades.Select(CarrerasMapper.ToAdmisionesDto), nameof(ObtenerCarreras));
        }

        public OperationResult<IEnumerable<DtoComienzoResponse>> ObtenerComienzos(long idCarrera)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Procesos.GetProcesosHabilitadosPorProducto(idCarrera);
            return OperationResult<IEnumerable<DtoComienzoResponse>>.Ok(entidades.Select(ComienzosMapper.ToAdmisionesDto), nameof(ObtenerComienzos));
        }

        public async Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerTurnos(long idCarrera, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var producto = uow.Productos.GetByKey(idCarrera);

            if (producto == null)
            {
                return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                    "CAT_TURNOS_02",
                    nameof(ObtenerTurnos),
                    "Producto no encontrado.",
                    404,
                    default
                );
            }

            if (producto.IdNivelProducto == 3 || producto.IdNivelProducto == 4)
            {
                var ofertas = uow.VdOfertasDisponibles3y4s
                    .GetOfertasDisponibles(idCarrera)
                    .GroupBy(o => new { o.IdOferta, o.IdTurno })
                    .Select(g => g.First())
                    .OrderBy(o => o.IdTurno)
                    .ThenBy(o => o.IdOferta)
                    .ToList();

                var turnosPorId = uow.Turnos
                    .GetByKeys(ofertas.Select(o => o.IdTurno))
                    .ToDictionary(t => t.IdTurno);

                var response = ofertas
                    .Select(oferta =>
                    {
                        turnosPorId.TryGetValue(oferta.IdTurno, out var turno);

                        return new OfertaInscripcionDto
                        {
                            IdOferta = oferta.IdOferta,
                            Turno = new DtoTurno
                            {
                                IdTurno = oferta.IdTurno,
                                NombreTurno = turno?.NombreTurno
                            },
                            HorarioReferencia = null
                        };
                    })
                    .ToList();

                return OperationResult<List<OfertaInscripcionDto>>.Ok(response, nameof(ObtenerTurnos));
            }

            if (producto.IdNivelProducto != 1 && producto.IdNivelProducto != 2)
            {
                return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                    "CAT_TURNOS_04",
                    nameof(ObtenerTurnos),
                    "Nivel de producto no soportado para obtener turnos.",
                    400,
                    default
                );
            }

            if (_inscripcionesyPagosApiClient == null)
            {
                return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                    "CAT_TURNOS_01",
                    nameof(ObtenerTurnos),
                    "Cliente de Inscripciones y Pagos no configurado.",
                    500,
                    default
                );
            }

            var resultadoOfertas = await _inscripcionesyPagosApiClient
                .ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync(idCarrera, idProceso);

            if (!resultadoOfertas.Success)
            {
                return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                    resultadoOfertas.ErrorCode,
                    nameof(ObtenerTurnos),
                    resultadoOfertas.Message,
                    resultadoOfertas.HttpCode,
                    resultadoOfertas.Data
                );
            }

            return OperationResult<List<OfertaInscripcionDto>>.Ok(resultadoOfertas.Data, nameof(ObtenerTurnos));
        }

        public async Task<OperationResult<BancosResponseDto>> ObtenerBancos()
        {
            const string methodName = nameof(ObtenerBancos);

            if (_inscripcionesyPagosApiClient == null)
            {
                return OperationResult<BancosResponseDto>.IsFailed(
                    "CAT_BAN_01",
                    methodName,
                    "Cliente de Inscripciones y Pagos no configurado.",
                    500);
            }

            var bancosResult = await _inscripcionesyPagosApiClient.ObtenerBancosAsync();
            if (!bancosResult.Success)
            {
                return OperationResult<BancosResponseDto>.IsFailed(
                    bancosResult.ErrorCode,
                    methodName,
                    bancosResult.Message,
                    bancosResult.HttpCode);
            }

            if (bancosResult.Data == null)
            {
                return OperationResult<BancosResponseDto>.IsFailed(
                    "CAT_BAN_02",
                    methodName,
                    "La API interna no devolvio datos de bancos.",
                    502);
            }

            return OperationResult<BancosResponseDto>.Ok(MapearBancos(bancosResult.Data), methodName);
        }

        private static BancosResponseDto MapearBancos(BancosResponse source)
        {
            return new BancosResponseDto
            {
                TotalCount = source.TotalCount,
                Bancos = source.Bancos
                    .Select(b => new AppLogic.DTOs.BancoDto
                    {
                        IdBanco = b.IdBanco,
                        NombreBanco = b.NombreBanco,
                        Codigo = b.Codigo,
                        Activo = b.Activo
                    })
                    .ToList()
            };
        }

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Empresas.GetInstituciones(codigoPais, codigoEstado);
            return OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerInstituciones));
        }

        //public OperationResult<IEnumerable<DtoProductoBeca>> ObtenerProductosBeca(long codigoPersona)
        //{
        //    using var uow = _uowFactory.Create();

        //    // 1. Inscripciones realizadas (T_INSCRIPTO)
        //    var realizadas = uow.Inscriptos.GetInscripcionesRealizadas(codigoPersona)
        //        .Select(i => new DtoProductoBeca
        //        {
        //            FechaInscripcion = i.FechaInscr ?? DateTime.MinValue,
        //            IdProducto = i.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
        //            IdNivelProducto = i.Oferta?.Supraoferta?.Paquete?.Producto?.IdNivelProducto ?? 0,
        //            NombreProducto = i.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
        //            NombreComienzo = i.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
        //            NombreTurno = i.Oferta?.Turno?.NombreTurno,
        //            IdProceso = i.Oferta?.Supraoferta?.Comienzo?.ProcesoComienzos?
        //                                   .FirstOrDefault()?.IdProceso ?? 0,
        //        })
        //        .ToList();

        //    var pendientes = ConstruirPendientesProductosBeca(uow, codigoPersona);

        //    // 3. Productos con interés activo (sin inscripción pendiente en workflow)
        //    var intereses = uow.Productos.GetProductosConInteresActivo(codigoPersona)
        //        .Select(p => new DtoProductoBeca
        //        {
        //            FechaInscripcion = DateTime.MinValue,
        //            IdProducto = p.IdProducto,
        //            IdNivelProducto = p.IdNivelProducto,
        //            NombreProducto = p.NombreExtensoProducto,
        //            NombreComienzo = p.ProcesoProductos?.FirstOrDefault()?.Proceso?.NombreProceso,
        //            NombreTurno = null,
        //            IdProceso = p.ProcesoProductos?.FirstOrDefault()?.IdProceso ?? 0,
        //        })
        //        .ToList();

        //    var todos = realizadas.Concat(pendientes).Concat(intereses)
        //        .GroupBy(b => b.IdProducto)
        //        .Select(g => g.OrderBy(b => b.FechaInscripcion).First())
        //        .ToList();

        //    return OperationResult<IEnumerable<DtoProductoBeca>>.Ok(todos, nameof(ObtenerProductosBeca));
        //}



        //private static List<DtoProductoBeca> ConstruirPendientesProductosBeca(IUnitOfWork uow, long codigoPersona)
        //{
        //    var instancias = uow.InstanciaWorkflows.GetInscripcionesPendientes(codigoPersona);
        //    var instanciaIds = instancias.Select(iw => iw.IdInstanciaWorkflow).ToList();
        //    var inscripcionesPorInstanciaId = uow.InstWorkflowInscripcions
        //        .GetByInstanciaIds(instanciaIds)
        //        .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

        //    var pendientesConProducto = instanciass
        //        .Where(iw => inscripcionesPorInstanciaId.ContainsKey(iw.IdInstanciaWorkflow)
        //            && inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow].IdProducto.HasValue)
        //        .ToList();

        //    var productoIds = pendientesConProducto
        //        .Select(iw => (long)inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow].IdProducto!.Value)
        //        .Distinct()
        //        .ToList();

        //    var productosPorId = uow.Productos
        //        .GetByKeys(productoIds)
        //        .ToDictionary(p => p.IdProducto);

        //    var comienzoIds = pendientesConProducto
        //        .Select(iw => inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow].IdComienzo)
        //        .Where(id => id.HasValue)
        //        .Select(id => (long)id!.Value)
        //        .Distinct()
        //        .ToList();

        //    var comienzosPorId = uow.Comienzos
        //        .GetByKeys(comienzoIds)
        //        .ToDictionary(c => c.IdComienzo);

        //    var turnoIds = pendientesConProducto
        //        .Select(iw => inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow].IdTurno)
        //        .Where(id => id.HasValue)
        //        .Select(id => (long)id!.Value)
        //        .Distinct()
        //        .ToList();

        //    var turnosPorId = uow.Turnos
        //        .GetByKeys(turnoIds)
        //        .ToDictionary(t => t.IdTurno);

        //    return pendientesConProducto
        //        .Select(iw =>
        //        {
        //            var inscripcion = inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow];
        //            var idProducto = (long)inscripcion.IdProducto!.Value;

        //            productosPorId.TryGetValue(idProducto, out var producto);

        //            BusinessLogic.Entities.Comienzo? comienzo = null;
        //            if (inscripcion.IdComienzo.HasValue)
        //            {
        //                comienzosPorId.TryGetValue((long)inscripcion.IdComienzo.Value, out comienzo);
        //            }

        //            BusinessLogic.Entities.Turno? turno = null;
        //            if (inscripcion.IdTurno.HasValue)
        //            {
        //                turnosPorId.TryGetValue((long)inscripcion.IdTurno.Value, out turno);
        //            }

        //            return new DtoProductoBeca
        //            {
        //                FechaInscripcion = iw.FechaInicialInstanciaWf ?? DateTime.MinValue,
        //                IdProducto = idProducto,
        //                IdNivelProducto = producto?.IdNivelProducto ?? 0,
        //                NombreProducto = producto?.NombreExtensoProducto,
        //                NombreComienzo = comienzo?.NombreComienzo,
        //                NombreTurno = turno?.NombreTurno,
        //                IdProceso = (long)iw.IdProceso,
        //            };
        //        })
        //        .ToList();
        //}

        public OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoDescuentos.GetFondosDeBecaVigentesPorProducto(idProducto);
            return OperationResult<IEnumerable<DtoTipoDescuentoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerFondosDeBecaPorProducto));
        }
    }
}
