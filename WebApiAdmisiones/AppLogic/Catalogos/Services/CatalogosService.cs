using AppLogic.Catalogos.Interfaces;
using AppLogic.Catalogos.Mappers;
using AppLogic.Catalogos.Dtos;
using AppLogic.ApiClients.Dtos;
using AppLogic.ApiClients.Interfaces;
using AppLogic.DevartDTOs;
using AppLogic.Inscripciones.Encuesta.Rules;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Catalogos.Services;

public class CatalogosService(IUnitOfWorkFactory uowFactory, IInscripcionesyPagosApiClient inscripcionesyPagosApiClient) : ICatalogosService
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IInscripcionesyPagosApiClient _inscripcionesyPagosApiClient = inscripcionesyPagosApiClient;

    private OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>> ObtenerPaisesEstadosCiudades()
    {
        using var uow = _uowFactory.Create();

        var paises = uow.Paises.GetPaisesConEstadosYCiudades().ToList();
        var response = paises.Select(pais => pais.ToPaisesEstadosCiudadesDto()).ToList();

        return OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>.Ok(response, nameof(ObtenerPaisesEstadosCiudades));
    }

    private OperationResult<DtoEncuestaInicialCatalogosResponse> ObtenerEncuestaInicial()
    {
        using var uow = _uowFactory.Create();

        var universidades = uow.Empresas.GetUniversidades()
            .Select(e => new DtoUniversidadCatalogo
            {
                Value = e.CodigoEmpresa,
                Label = e.Nombre ?? string.Empty
            })
            .Append(new DtoUniversidadCatalogo { Value = 0, Label = "Otro" })
            .ToList();

        var response = new DtoEncuestaInicialCatalogosResponse
        {
            Educacion = new DtoEncuestaEducacionCatalogos
            {
                OpcionesSiNo = EncuestaInicialOpciones.OpcionesSiNo,
                UbicacionesUltimoAnioSecundaria = EncuestaInicialOpciones.UbicacionesUltimoAnioSecundaria,
                EstadosEducacionSuperiorPrevia = EncuestaInicialOpciones.EstadosEducacionSuperiorPrevia,
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
                NivelesFormacionTutores = EncuestaInicialOpciones.NivelesFormacionTutores
            },
            DecisionAcademica = new DtoEncuestaDecisionAcademicaCatalogos
            {
                AniosEducacionMediaSuperior = EncuestaInicialOpciones.AniosEducacionMediaSuperior,
                ApoyosDecision = EncuestaInicialOpciones.ApoyosDecision,
                NivelesDecision = EncuestaInicialOpciones.NivelesDecision,
                Universidades = universidades,
                MotivosEleccionOrt = uow.MotivoOpcionesAdmisions.GetAll()
                    .Select(m => Combo(m.IdMotivo, m.NombreMotivo))
                    .ToList()
            },
            ExperienciaOrt = new DtoEncuestaExperienciaOrtCatalogos
            {
                OpcionesSiNo = EncuestaInicialOpciones.OpcionesSiNo,
                Valoraciones = EncuestaInicialOpciones.Valoraciones,
                PublicidadesOrt = uow.PublicidadOpcionesAdmisions.GetAll()
                    .Select(p => Combo(p.IdPublicidad, p.NombrePublicidad))
                    .ToList()
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

    public Task<OperationResult<DtoEncuestaInicialCatalogosResponse>> ObtenerEncuestaInicialAsync()
    {
        return Task.FromResult(ObtenerEncuestaInicial());
    }

    public OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>> ObtenerCarreras(long codigoPersona, PropuestaAcademica propuestaAcademica)
    {
        if (!Enum.IsDefined(propuestaAcademica))
        {
            return OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>>.IsFailed(
                "CAT_CARRERAS_01",
                nameof(ObtenerCarreras),
                "Propuesta académica no válida.",
                400,
                default);
        }

        using var uow = _uowFactory.Create();

        var items = propuestaAcademica switch
        {
            PropuestaAcademica.CarreraUniversitaria =>
                uow.VdProductosDisponibles1y2s.GetProductosDisponibles(codigoPersona, idNivelProducto: 1).Select(ToCarreraCatalogoItem),
            PropuestaAcademica.Tecnicatura =>
                uow.VdProductosDisponibles1y2s.GetProductosDisponibles(codigoPersona, idNivelProducto: 2).Select(ToCarreraCatalogoItem),
            _ =>
                uow.VdOfertasDisponibles3y4s.GetProductosDisponibles().Select(ToCarreraCatalogoItem)
        };

        var esActualizacionProfesional = propuestaAcademica == PropuestaAcademica.ActualizacionProfesional;

        var response = items
            .GroupBy(x => new { x.IdNivelProducto, x.NombreNivelProducto })
            .OrderBy(g => g.Key.IdNivelProducto)
            .Select(nivel => new DtoCarrerasPorNivelResponse
            {
                IdNivelProducto = nivel.Key.IdNivelProducto,
                NombreNivelProducto = nivel.Key.NombreNivelProducto,
                Escuelas = nivel
                    .GroupBy(x => new { x.IdEscuela, x.NombreEscuela })
                    .OrderBy(g => g.Min(x => x.OrdenEscuela ?? long.MaxValue))
                    .ThenBy(g => g.Key.NombreEscuela)
                    .Select(escuela => esActualizacionProfesional
                        ? new DtoCarrerasPorEscuelaResponse
                        {
                            IdEscuela = escuela.Key.IdEscuela,
                            NombreEscuela = escuela.Key.NombreEscuela,
                            Seminarios = escuela
                                // TieneSeminario siempre es bool concreto para nivel 3/4 (ver ToCarreraCatalogoItem(VdOfertasDisponibles3y4)).
                                .GroupBy(x => x.TieneSeminario!.Value)
                                .OrderBy(g => g.Key)
                                .Select(seminario => new DtoCarrerasPorSeminarioResponse
                                {
                                    TieneSeminario = seminario.Key,
                                    Productos = MapProductos(seminario)
                                })
                                .ToList()
                        }
                        : new DtoCarrerasPorEscuelaResponse
                        {
                            IdEscuela = escuela.Key.IdEscuela,
                            NombreEscuela = escuela.Key.NombreEscuela,
                            Productos = MapProductos(escuela)
                        })
                    .ToList()
            })
            .ToList();

        return OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>>.Ok(response, nameof(ObtenerCarreras));
    }

    private static List<DtoCarreraResponse> MapProductos(IEnumerable<CarreraCatalogoItem> items) => items
        .GroupBy(x => x.IdProducto)
        .Select(g => g.First())
        .OrderBy(x => x.OrdenProducto ?? long.MaxValue)
        .ThenBy(x => x.NombreProducto)
        .Select(x => new DtoCarreraResponse
        {
            IdProducto = x.IdProducto,
            NombreProducto = x.NombreProducto,
            IdProceso = x.IdProceso
        })
        .ToList();

    private static CarreraCatalogoItem ToCarreraCatalogoItem(VdProductosDisponibles1y2 producto) => new(
        producto.IdProducto,
        producto.NombreWebProducto,
        producto.IdNivelProducto,
        producto.NombreNivelProducto,
        producto.IdEscuela,
        producto.NombreExtensoEscuela,
        producto.OrdenListadoEscuela,
        producto.OrdenListadoNivelProducto,
        null,
        null);

    private static CarreraCatalogoItem ToCarreraCatalogoItem(VdOfertasDisponibles3y4 oferta) => new(
        oferta.IdProducto!.Value,
        oferta.NombreWebProducto,
        oferta.IdNivelProducto,
        oferta.NombreNivelProducto,
        oferta.IdEscuela,
        oferta.NombreExtensoEscuela,
        null,
        null,
        (long)oferta.IdProceso,
        oferta.ConSeminarios == "SI");

    private sealed record CarreraCatalogoItem(
        long IdProducto,
        string? NombreProducto,
        long IdNivelProducto,
        string? NombreNivelProducto,
        long IdEscuela,
        string? NombreEscuela,
        long? OrdenEscuela,
        long? OrdenProducto,
        long? IdProceso,
        bool? TieneSeminario);

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

        if (EsNivelProducto3y4(producto.IdNivelProducto))
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
                        HorarioReferencia = null,
                        FechaReferencia = oferta.FechaReferencia,
                        DescripcionOferta = oferta.DescripcionOferta
                    };
                })
                .ToList();

            return OperationResult<List<OfertaInscripcionDto>>.Ok(response, nameof(ObtenerTurnos));
        }

        if (!EsNivelProducto1y2(producto.IdNivelProducto))
        {
            return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                "CAT_TURNOS_04",
                nameof(ObtenerTurnos),
                "Nivel de producto no soportado para obtener turnos.",
                400,
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

    private OperationResult<IEnumerable<DtoBancoDevart>> ObtenerBancos()
    {
        using var uow = _uowFactory.Create();
        var entidades = uow.Bancos.GetAllHabilitados();
        return OperationResult<IEnumerable<DtoBancoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerBancos));
    }

    public Task<OperationResult<IEnumerable<DtoBancoDevart>>> ObtenerBancosAsync()
    {
        return Task.FromResult(ObtenerBancos());
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

    // Agrupación de IdNivelProducto ya usada en el resto del código para separar fuentes de datos
    // (VdOfertasDisponibles3y4 / VdProductosDisponibles1y2).
    private static bool EsNivelProducto3y4(long idNivelProducto) => idNivelProducto is 3 or 4;

    private static bool EsNivelProducto1y2(long idNivelProducto) => idNivelProducto is 1 or 2;
}
