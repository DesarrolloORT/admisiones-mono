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

            var nivel12 = uow.Productos.GetProductosVigentes()
                .Select(CarrerasMapper.ToAdmisionesDto);

            var nivel34 = uow.VdOfertasDisponibles3y4s.GetProductosDisponibles().Select(CarrerasMapper.ToAdmisionesDto);

            return OperationResult<IEnumerable<DtoCarreraResponse>>.Ok(nivel12.Concat(nivel34), nameof(ObtenerCarreras));
        }

        public OperationResult<IEnumerable<DtoComienzoResponse>> ObtenerComienzos(long idCarrera)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.VdProcesosDisponibles1y2s.GetProcesosDisponibles(idCarrera);
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

        public OperationResult<IEnumerable<DtoBancoDevart>> ObtenerBancos()
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Bancos.GetAllHabilitados();
            return OperationResult<IEnumerable<DtoBancoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerBancos));
        }

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Empresas.GetInstituciones(codigoPais, codigoEstado);
            return OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerInstituciones));
        }

        public OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoDescuentos.GetFondosDeBecaVigentesPorProducto(idProducto);
            return OperationResult<IEnumerable<DtoTipoDescuentoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerFondosDeBecaPorProducto));
        }
    }
}
