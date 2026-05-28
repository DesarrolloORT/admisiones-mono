using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
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
            var response = new DtoEncuestaInicialCatalogosResponse
            {
                NivelConocimiento =
                [
                    Combo(1, "Ninguno"),
                    Combo(2, "Básico"),
                    Combo(3, "Medio"),
                    Combo(4, "Superior")
                ],
                DecisionCarrera = DecisionSecundariaOptions(),
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
                DecisionUniversidad = DecisionSecundariaOptions(),
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
                ]
            };

            return OperationResult<DtoEncuestaInicialCatalogosResponse>.Ok(response, nameof(ObtenerEncuestaInicial));
        }

        private static IReadOnlyList<DtoComboOption> DecisionSecundariaOptions() =>
        [
            Combo(2, "1° EMS (4° año)"),
            Combo(3, "2° EMS (5° año)"),
            Combo(4, "3° EMS (6° año)"),
            Combo(0, "Otro")
        ];

        private static DtoComboOption Combo(int value, string label) => new()
        {
            Value = value,
            Label = label
        };

        public OperationResult<DtoPaisDevart> ObtenerPais(long idPais)
        {
            using var uow = _uowFactory.Create();
            var pais = uow.Paises.GetPaisConEstadosYCiudades(idPais);
            if (pais == null)
            {
                return OperationResult<DtoPaisDevart>.IsFailed("FDP_GPAC_01", nameof(ObtenerPais), "País no encontrado.", 204);
            }

            if (pais.Estado != null)
            {
                pais.Estado = pais.Estado.OrderBy(e => e.Nombre).ToList();
                foreach (var estado in pais.Estado)
                {
                    if (estado.Ciudad != null)
                    {
                        estado.Ciudad = estado.Ciudad.OrderBy(c => c.Nombre).ToList();
                    }
                }
            }

            return OperationResult<DtoPaisDevart>.Ok(pais.ToDtoWithRelated(2), nameof(ObtenerPais));
        }

        public OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.AcaTipoDocumentos.GetAll().ToList();
            return OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>.Ok(AcaTipoDocumentoConverter.ToDtos(entidades), nameof(ObtenerTipoDocumentos));
        }

        public OperationResult<IEnumerable<DtoCarreraResponse>> ObtenerCarreras()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosVigentesParaRegistro();
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

        public OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>> ObtenerMotivosEleccion()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.MotivoOpcionesAdmisions.GetAll().ToList();
            return OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerMotivosEleccion));
        }

        public OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>> ObtenerPublicidadesEleccion()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.PublicidadOpcionesAdmisions.GetAll().ToList();
            return OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerPublicidadesEleccion));
        }

        public OperationResult<IEnumerable<DtoTituloDevart>> ObtenerBachilleratos(long idAnioBachillerato)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Titulos.GetBachilleratosPorAnio(idAnioBachillerato);
            return OperationResult<IEnumerable<DtoTituloDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerBachilleratos));
        }

        public OperationResult<DtoAnioBachillerDevart> ObtenerAnioBachiller(long idAnioBachillerato)
        {
            using var uow = _uowFactory.Create();
            var anio = uow.AnioBachillers.GetWithRelated(idAnioBachillerato);
            if (anio == null)
            {
                return OperationResult<DtoAnioBachillerDevart>.IsFailed("GEN_ANB_01", nameof(ObtenerAnioBachiller), "Año de bachillerato no encontrado.", 204);
            }

            return OperationResult<DtoAnioBachillerDevart>.Ok(anio.ToDto(), nameof(ObtenerAnioBachiller));
        }

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Empresas.GetInstituciones(codigoPais, codigoEstado);
            return OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerInstituciones));
        }

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerUniversidades()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Empresas.GetUniversidades();
            return OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerUniversidades));
        }

        public OperationResult<IEnumerable<DtoProductoBeca>> ObtenerProductosBeca(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            // 1. Inscripciones realizadas (T_INSCRIPTO)
            var realizadas = uow.Inscriptos.GetInscripcionesRealizadas(codigoPersona)
                .Select(i => new DtoProductoBeca
                {
                    FechaInscripcion = i.FechaInscr ?? DateTime.MinValue,
                    IdProducto = i.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                    IdNivelProducto = i.Oferta?.Supraoferta?.Paquete?.Producto?.IdNivelProducto ?? 0,
                    NombreProducto = i.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                    NombreComienzo = i.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
                    NombreTurno = i.Oferta?.Turno?.NombreTurno,
                    IdProceso = i.Oferta?.Supraoferta?.Comienzo?.ProcesoComienzos?
                                           .FirstOrDefault()?.IdProceso ?? 0,
                })
                .ToList();

            var pendientes = ConstruirPendientesProductosBeca(uow, codigoPersona);

            // 3. Productos con interés activo (sin inscripción pendiente en workflow)
            var intereses = uow.Productos.GetProductosConInteresActivo(codigoPersona)
                .Select(p => new DtoProductoBeca
                {
                    FechaInscripcion = DateTime.MinValue,
                    IdProducto = p.IdProducto,
                    IdNivelProducto = p.IdNivelProducto,
                    NombreProducto = p.NombreExtensoProducto,
                    NombreComienzo = p.ProcesoProductos?.FirstOrDefault()?.Proceso?.NombreProceso,
                    NombreTurno = null,
                    IdProceso = p.ProcesoProductos?.FirstOrDefault()?.IdProceso ?? 0,
                })
                .ToList();

            var todos = realizadas.Concat(pendientes).Concat(intereses)
                .GroupBy(b => b.IdProducto)
                .Select(g => g.OrderBy(b => b.FechaInscripcion).First())
                .ToList();

            return OperationResult<IEnumerable<DtoProductoBeca>>.Ok(todos, nameof(ObtenerProductosBeca));
        }



        private static List<DtoProductoBeca> ConstruirPendientesProductosBeca(IUnitOfWork uow, long codigoPersona)
        {
            var instancias = uow.InstanciaWorkflows.GetInscripcionesPendientes(codigoPersona);
            var instanciaIds = instancias.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesPorInstanciaId = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(instanciaIds)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            var pendientesConProducto = instancias
                .Where(iw => inscripcionesPorInstanciaId.ContainsKey(iw.IdInstanciaWorkflow)
                    && inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow].IdProducto.HasValue)
                .ToList();

            var productoIds = pendientesConProducto
                .Select(iw => (long)inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow].IdProducto!.Value)
                .Distinct()
                .ToList();

            var productosPorId = uow.Productos
                .GetByKeys(productoIds)
                .ToDictionary(p => p.IdProducto);

            var comienzoIds = pendientesConProducto
                .Select(iw => inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow].IdComienzo)
                .Where(id => id.HasValue)
                .Select(id => (long)id!.Value)
                .Distinct()
                .ToList();

            var comienzosPorId = uow.Comienzos
                .GetByKeys(comienzoIds)
                .ToDictionary(c => c.IdComienzo);

            var turnoIds = pendientesConProducto
                .Select(iw => inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow].IdTurno)
                .Where(id => id.HasValue)
                .Select(id => (long)id!.Value)
                .Distinct()
                .ToList();

            var turnosPorId = uow.Turnos
                .GetByKeys(turnoIds)
                .ToDictionary(t => t.IdTurno);

            return pendientesConProducto
                .Select(iw =>
                {
                    var inscripcion = inscripcionesPorInstanciaId[iw.IdInstanciaWorkflow];
                    var idProducto = (long)inscripcion.IdProducto!.Value;

                    productosPorId.TryGetValue(idProducto, out var producto);

                    BusinessLogic.Entities.Comienzo? comienzo = null;
                    if (inscripcion.IdComienzo.HasValue)
                    {
                        comienzosPorId.TryGetValue((long)inscripcion.IdComienzo.Value, out comienzo);
                    }

                    BusinessLogic.Entities.Turno? turno = null;
                    if (inscripcion.IdTurno.HasValue)
                    {
                        turnosPorId.TryGetValue((long)inscripcion.IdTurno.Value, out turno);
                    }

                    return new DtoProductoBeca
                    {
                        FechaInscripcion = iw.FechaInicialInstanciaWf ?? DateTime.MinValue,
                        IdProducto = idProducto,
                        IdNivelProducto = producto?.IdNivelProducto ?? 0,
                        NombreProducto = producto?.NombreExtensoProducto,
                        NombreComienzo = comienzo?.NombreComienzo,
                        NombreTurno = turno?.NombreTurno,
                        IdProceso = (long)iw.IdProceso,
                    };
                })
                .ToList();
        }

        public OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoDescuentos.GetFondosDeBecaVigentesPorProducto(idProducto);
            return OperationResult<IEnumerable<DtoTipoDescuentoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerFondosDeBecaPorProducto));
        }
    }
}
