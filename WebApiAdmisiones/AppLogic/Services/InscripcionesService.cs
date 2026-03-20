using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Interfaces;
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

        public OperationResult<DTOUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscripto = uow.Inscriptos.GetUltimaInscripcionActiva(codigoPersona);
            if (inscripto == null)
                return OperationResult<DTOUltimaInscripcion>.IsFailed("GEN_UI_01", nameof(ObtenerUltimaInscripcionActiva), "No se encontró inscripción para la persona.", 204);

            var dto = new DTOUltimaInscripcion
            {
                IdInscripto = inscripto.IdInscripto,
                IdProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                NombreProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreProducto,
                NombreExtensoProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                IdComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.IdComienzo ?? 0,
                NombreComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
            };
            return OperationResult<DTOUltimaInscripcion>.Ok(dto, nameof(ObtenerUltimaInscripcionActiva));
        }

        public OperationResult<IEnumerable<DTOProductoAdmisiones>> ObtenerProductosVigentesConInteres(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosVigentesConInteres(codigoPersona);
            var dtos = entidades.Select(MapProductoAdmisiones);
            return OperationResult<IEnumerable<DTOProductoAdmisiones>>.Ok(dtos, nameof(ObtenerProductosVigentesConInteres));
        }

        public OperationResult<IEnumerable<DTOProductoAdmisiones>> ObtenerProductosConInteresActivo(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosConInteresActivo(codigoPersona);
            var dtos = entidades.Select(MapProductoAdmisiones);
            return OperationResult<IEnumerable<DTOProductoAdmisiones>>.Ok(dtos, nameof(ObtenerProductosConInteresActivo));
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
            var ids = instancias.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesDict = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(ids)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            var dtos = instancias.Select(iw =>
            {
                var dto = iw.ToDto();
                if (inscripcionesDict.TryGetValue(iw.IdInstanciaWorkflow, out var iwi))
                    dto.InstWorkflowInscripcion = iwi.ToDto();
                return dto;
            });

            return OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>.Ok(dtos, nameof(ObtenerInscripcionesPendientes));
        }

        public OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesCanceladas(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var instancias = uow.InstanciaWorkflows.GetInscripcionesCanceladas(codigoPersona);
            var ids = instancias.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesDict = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(ids)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            var dtos = instancias.Select(iw =>
            {
                var dto = iw.ToDto();
                if (inscripcionesDict.TryGetValue(iw.IdInstanciaWorkflow, out var iwi))
                    dto.InstWorkflowInscripcion = iwi.ToDto();
                return dto;
            });

            return OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>.Ok(dtos, nameof(ObtenerInscripcionesCanceladas));
        }

        public OperationResult<IEnumerable<DTOInscripcionRealizada>> ObtenerInscripcionesRealizadas(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscriptos = uow.Inscriptos.GetInscripcionesRealizadas(codigoPersona);
            var dtos = inscriptos
                .Select(i => new DTOInscripcionRealizada
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
            return OperationResult<IEnumerable<DTOInscripcionRealizada>>.Ok(dtos, nameof(ObtenerInscripcionesRealizadas));
        }

        public OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.Inscriptos.TieneInscripcionAdmisiones(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionAdmisiones));
        }

        private static DTOProductoAdmisiones MapProductoAdmisiones(BusinessLogic.Entities.Producto p)
        {
            var proceso = p.ProcesoProductos?.FirstOrDefault()?.Proceso;
            return new DTOProductoAdmisiones
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
