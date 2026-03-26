using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
{
    public class CatalogosService : ICatalogosService
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public CatalogosService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaises()
        {
            using var uow = _uowFactory.Create();
            var dtoList = uow.Paises.GetPaisesOrdenados().ToDtos().ToList();
            return OperationResult<IEnumerable<DtoPaisDevart>>.Ok(dtoList, nameof(ObtenerPaises));
        }

        public OperationResult<DtoPaisDevart> ObtenerPais(long idPais)
        {
            using var uow = _uowFactory.Create();
            var pais = uow.Paises.GetPaisConEstadosYCiudades(idPais);
            if (pais == null)
                return OperationResult<DtoPaisDevart>.IsFailed("FDP_GPAC_01", nameof(ObtenerPais), "País no encontrado.", 404);

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
                return OperationResult<DtoAnioBachillerDevart>.IsFailed("GEN_ANB_01", nameof(ObtenerAnioBachiller), "Año de bachillerato no encontrado.", 204);

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

            // 2. Inscripciones pendientes en workflow (T_INSTANCIA_WORKFLOW)
            var instancias = uow.InstanciaWorkflows.GetInscripcionesPendientes(codigoPersona);
            var ids = instancias.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesDict = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(ids)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            var pendientes = instancias
                .Where(iw => inscripcionesDict.ContainsKey(iw.IdInstanciaWorkflow)
                          && inscripcionesDict[iw.IdInstanciaWorkflow].IdProducto.HasValue)
                .Select(iw =>
                {
                    var iwi = inscripcionesDict[iw.IdInstanciaWorkflow];
                    var idProducto = (long)iwi.IdProducto!.Value;
                    var producto = uow.Productos.GetByKey(idProducto);
                    var comienzo = iwi.IdComienzo.HasValue
                        ? uow.Comienzos.GetByKey((long)iwi.IdComienzo.Value) : null;
                    var turno = iwi.IdTurno.HasValue
                        ? uow.Turnos.GetByKey((long)iwi.IdTurno.Value) : null;

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

        public OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.TipoDescuentos.GetFondosDeBecaVigentesPorProducto(idProducto);
            return OperationResult<IEnumerable<DtoTipoDescuentoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerFondosDeBecaPorProducto));
        }
    }
}
