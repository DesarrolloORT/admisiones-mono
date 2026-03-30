using System.Collections.Generic;
using System.Linq;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
{
    public class RegistroService : IRegistroService
    {
        private readonly ICatalogosService _catalogosService;
        private readonly IPreinscripcionService _preinscripcionService;
        private readonly IUnitOfWorkFactory _uowFactory;

        public RegistroService(
            ICatalogosService catalogosService,
            IPreinscripcionService preinscripcionService,
            IUnitOfWorkFactory uowFactory)
        {
            _catalogosService = catalogosService;
            _preinscripcionService = preinscripcionService;
            _uowFactory = uowFactory;
        }

        public OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaises()
        {
            return _catalogosService.ObtenerPaises();
        }

        public OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos()
        {
            return _catalogosService.ObtenerTipoDocumentos();
        }

        public OperationResult<IEnumerable<DtoProcesoDevart>> ObtenerProcesosHabilitadosPorProducto(long idProducto)
        {
            return _preinscripcionService.ObtenerProcesosHabilitadosPorProducto(idProducto);
        }

        public OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaisesEstadosCiudades()
        {
            using var uow = _uowFactory.Create();

            var paises = uow.Paises.GetPaisesConEstadosYCiudades().ToList();

            return OperationResult<IEnumerable<DtoPaisDevart>>.Ok(paises.ToDtosWithRelated(2), nameof(ObtenerPaisesEstadosCiudades));
        }

        public OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosVigentes()
        {
            using var uow = _uowFactory.Create();

            var entidades = uow.Productos.GetProductosVigentesParaRegistro();
            var dtos = entidades.Select(MapProductoAdmisiones);

            return OperationResult<IEnumerable<DtoProductoAdmisiones>>.Ok(
                dtos,
                nameof(ObtenerProductosVigentes));
        }

        private static DtoProductoAdmisiones MapProductoAdmisiones(Producto producto)
        {
            var proceso = producto.ProcesoProductos?.FirstOrDefault()?.Proceso;

            return new DtoProductoAdmisiones
            {
                IdProducto = producto.IdProducto,
                NombreProducto = producto.NombreProducto,
                NombreExtensoProducto = producto.NombreExtensoProducto,
                IdNivelProducto = producto.IdNivelProducto,
                NombreNivelProducto = producto.NivelProducto?.NombreNivelProducto,
                AliasProducto = producto.AliasProducto,
                InscribibleProducto = producto.InscribibleProducto,
                IntermedioProducto = producto.IntermedioProducto,
                VisibleAdmisionesProducto = producto.VisibleAdmisionesProducto,
                IdProceso = proceso?.IdProceso ?? 0,
                NombreProceso = proceso?.NombreProceso
            };
        }
    }
}
