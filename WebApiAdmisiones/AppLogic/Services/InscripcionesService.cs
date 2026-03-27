using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
{
    public class InscripcionesService : IInscripcionesService
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public InscripcionesService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public OperationResult<DtoUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscripto = uow.Inscriptos.GetUltimaInscripcionActiva(codigoPersona);
            if (inscripto == null)
                return OperationResult<DtoUltimaInscripcion>.IsFailed("GEN_UI_01", nameof(ObtenerUltimaInscripcionActiva), "No se encontró inscripción para la persona.", 204);

            var dto = new DtoUltimaInscripcion
            {
                IdInscripto = inscripto.IdInscripto,
                IdProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                NombreProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreProducto,
                NombreExtensoProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                IdComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.IdComienzo ?? 0,
                NombreComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
            };
            return OperationResult<DtoUltimaInscripcion>.Ok(dto, nameof(ObtenerUltimaInscripcionActiva));
        }

        public OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosVigentesConInteres(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosVigentesConInteres(codigoPersona);
            var dtos = entidades.Select(MapProductoAdmisiones);
            return OperationResult<IEnumerable<DtoProductoAdmisiones>>.Ok(dtos, nameof(ObtenerProductosVigentesConInteres));
        }

        public OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosConInteresActivo(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosConInteresActivo(codigoPersona);
            var dtos = entidades.Select(MapProductoAdmisiones);
            return OperationResult<IEnumerable<DtoProductoAdmisiones>>.Ok(dtos, nameof(ObtenerProductosConInteresActivo));
        }

        public OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.VdEsFrescoAdmisions.TieneInscripcionActivaParaProceso(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionActivaParaProceso));
        }

        public OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesPendientes(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var instancias = uow.InstanciaWorkflows.GetInscripcionesPendientes(codigoPersona);
            var dtos = MapearInstanciasWorkflowConInscripcion(uow, instancias);

            return OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>.Ok(dtos, nameof(ObtenerInscripcionesPendientes));
        }

        public OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesCanceladas(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var instancias = uow.InstanciaWorkflows.GetInscripcionesCanceladas(codigoPersona);
            var dtos = MapearInstanciasWorkflowConInscripcion(uow, instancias);

            return OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>.Ok(dtos, nameof(ObtenerInscripcionesCanceladas));
        }

        public OperationResult<IEnumerable<DtoInscripcionRealizada>> ObtenerInscripcionesRealizadas(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscriptos = uow.Inscriptos.GetInscripcionesRealizadas(codigoPersona);
            var dtos = inscriptos
                .Select(i => new DtoInscripcionRealizada
                {
                    FechaInscripcion = i.FechaInscr ?? DateTime.MinValue,
                    IdProducto = i.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                    NombreProducto = i.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                    NombreComienzo = i.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
                    NombreTurno = i.Oferta?.Turno?.NombreTurno,
                })
                .GroupBy(d => d.IdProducto)
                .Select(g => g.OrderBy(d => d.FechaInscripcion).First())
                .ToList();
            return OperationResult<IEnumerable<DtoInscripcionRealizada>>.Ok(dtos, nameof(ObtenerInscripcionesRealizadas));
        }

        public OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.Inscriptos.TieneInscripcionAdmisiones(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionAdmisiones));
        }

        private static IEnumerable<DtoInstanciaWorkflowDevart> MapearInstanciasWorkflowConInscripcion(
            IUnitOfWork uow,
            IEnumerable<InstanciaWorkflow> instancias)
        {
            var instanciasList = instancias.ToList();
            var ids = instanciasList.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesPorInstanciaId = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(ids)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            return instanciasList.Select(iw =>
            {
                var dto = iw.ToDto();
                if (inscripcionesPorInstanciaId.TryGetValue(iw.IdInstanciaWorkflow, out var inscripcion))
                {
                    dto.InstWorkflowInscripcion = inscripcion.ToDto();
                }

                return dto;
            });
        }

        private static DtoProductoAdmisiones MapProductoAdmisiones(BusinessLogic.Entities.Producto p)
        {
            var proceso = p.ProcesoProductos?.FirstOrDefault()?.Proceso;
            return new DtoProductoAdmisiones
            {
                IdProducto = p.IdProducto,
                NombreProducto = p.NombreProducto,
                NombreExtensoProducto = p.NombreExtensoProducto,
                IdNivelProducto = p.IdNivelProducto,
                NombreNivelProducto = p.NivelProducto?.NombreNivelProducto,
                AliasProducto = p.AliasProducto,
                InscribibleProducto = p.InscribibleProducto,
                IntermedioProducto = p.IntermedioProducto,
                VisibleAdmisionesProducto = p.VisibleAdmisionesProducto,
                IdProceso = proceso?.IdProceso ?? 0,
                NombreProceso = proceso?.NombreProceso,
            };
        }
    }
}
