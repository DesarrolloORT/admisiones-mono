using AppLogic.Dtos.Catalogos;
using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
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

            var opcionesSiNo = new[] { Combo(1, "Sí"), Combo(2, "No") };
            var universidades = uow.Empresas.GetUniversidades()
                .Select(e => new DtoUniversidadCatalogo
                {
                    Value = e.CodigoEmpresa,
                    Label = e.Nombre ?? string.Empty
                })
                .ToList();

            var response = new DtoEncuestaInicialCatalogosResponse
            {
                Educacion = new DtoEncuestaEducacionCatalogos
                {
                    OpcionesSiNo = opcionesSiNo,
                    UbicacionesUltimoAnioSecundaria =
                    [
                        Combo(1, "Uruguay"),
                        Combo(2, "En el exterior")
                    ],
                    EstadosEducacionSuperiorPrevia =
                    [
                        Combo(1, "Sí, en Uruguay"),
                        Combo(2, "Sí, en el exterior"),
                        Combo(3, "No")
                    ],
                    AniosBachillerato = uow.AnioBachillers.GetAllWithRelated()
                        .Select(a => new DtoAnioBachilleratoCatalogo
                        {
                            Value = (long)(a.CantAniosAnioBachiller ?? a.IdAnioBachiller),
                            Label = a.NombreAnioBachiller ?? $"{a.CantAniosAnioBachiller}",
                            Orientaciones = a.Titulos
                                .Select(t => new DtoBachilleratoCatalogo
                                {
                                    Value = t.CodigoTitulo,
                                    Label = t.Nombre ?? string.Empty,
                                    Orientacion = t.OrientacionTitulo,
                                    OrientacionNueva = t.OrientacionNewTitulo
                                })
                                .ToList()
                        })
                        .ToList(),
                    Universidades = universidades,
                    NivelesFormacionTutores =
                    [
                        Combo(1, "Primaria"),
                        Combo(2, "Secundaria"),
                        Combo(3, "Formación técnica"),
                        Combo(4, "Formación universitaria incompleta"),
                        Combo(5, "Formación universitaria completa"),
                        Combo(6, "Estudios de postgrado"),
                        Combo(7, "Otros estudios")
                    ]
                },
                DecisionAcademica = new DtoEncuestaDecisionAcademicaCatalogos
                {
                    AniosEducacionMediaSuperior =
                    [
                        Combo(2, "1° EMS (4° año)"),
                        Combo(3, "2° EMS (5° año)"),
                        Combo(4, "3° EMS (6° año)"),
                        Combo(0, "Otro")
                    ],
                    ApoyosDecision =
                    [
                        Combo(1, "Padres u otros familiares"),
                        Combo(2, "Amigos de la familia"),
                        Combo(3, "Amigos propios, compañeros"),
                        Combo(4, "Otros"),
                        Combo(5, "Nadie")
                    ],
                    NivelesDecision =
                    [
                        Combo(1, "Decidido/a"),
                        Combo(2, "Con dudas")
                    ],
                    Universidades = universidades,
                    MotivosEleccionOrt = uow.MotivoOpcionesAdmisions.GetAll()
                        .Select(m => Combo(m.IdMotivo, m.NombreMotivo))
                        .ToList()
                },
                ExperienciaOrt = new DtoEncuestaExperienciaOrtCatalogos
                {
                    OpcionesSiNo = opcionesSiNo,
                    Valoraciones =
                    [
                        Combo(1, "1"),
                        Combo(2, "2"),
                        Combo(3, "3"),
                        Combo(4, "4"),
                        Combo(5, "5")
                    ],
                    PublicidadesOrt = uow.PublicidadOpcionesAdmisions.GetAll()
                        .Select(p => Combo(p.IdPublicidad, p.NombrePublicidad))
                        .ToList()
                },
                SituacionLaboral = new DtoEncuestaSituacionLaboralCatalogos
                {
                    OpcionesSiNo = opcionesSiNo,
                    TiposJornada =
                    [
                        Combo(1, "Tiempo completo"),
                        Combo(2, "Tiempo parcial")
                    ]
                }
            };

            return OperationResult<DtoEncuestaInicialCatalogosResponse>.Ok(response, nameof(ObtenerEncuestaInicial));
        }

        private static DtoComboOption Combo(long value, string label) => new()
        {
            Value = value,
            Label = label
        };
        // Versión async para compatibilidad con controllers async
        public Task<OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>> ObtenerPaisesEstadosCiudadesAsync()
        {
            return Task.FromResult(ObtenerPaisesEstadosCiudades());
        }

        public OperationResult<IEnumerable<DtoCarreraResponse>> ObtenerCarreras(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var nivel12 = uow.VdProductosDisponibles1y2s.GetProductosDisponibles(codigoPersona)
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
