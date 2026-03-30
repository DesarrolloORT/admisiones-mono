using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class RegistroServiceTests
    {
        private readonly Mock<ICatalogosService> _catalogosServiceMock;
        private readonly Mock<IPreinscripcionService> _preinscripcionServiceMock;
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly RegistroService _service;

        public RegistroServiceTests()
        {
            _catalogosServiceMock = new Mock<ICatalogosService>();
            _preinscripcionServiceMock = new Mock<IPreinscripcionService>();
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new RegistroService(_catalogosServiceMock.Object, _preinscripcionServiceMock.Object, _uowFactoryMock.Object);
        }

        [Fact]
        public void ObtenerPaises_DelegatesToCatalogosService()
        {
            var expected = OperationResult<IEnumerable<DtoPaisDevart>>.Ok(
                [new DtoPaisDevart { CodigoPais = 1, Nombre = "Uruguay" }],
                nameof(IRegistroService.ObtenerPaises));
            _catalogosServiceMock.Setup(s => s.ObtenerPaises()).Returns(expected);

            var result = _service.ObtenerPaises();

            Assert.Same(expected, result);
        }

        [Fact]
        public void ObtenerTipoDocumentos_DelegatesToCatalogosService()
        {
            var expected = OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>.Ok(
                [new DtoAcaTipoDocumentoDevart { CodTipoDocumento = 1, Descripcion = "Cedula" }],
                nameof(IRegistroService.ObtenerTipoDocumentos));
            _catalogosServiceMock.Setup(s => s.ObtenerTipoDocumentos()).Returns(expected);

            var result = _service.ObtenerTipoDocumentos();

            Assert.Same(expected, result);
        }

        [Fact]
        public void ObtenerProcesosHabilitadosPorProducto_DelegatesToPreinscripcionService()
        {
            var expected = OperationResult<IEnumerable<DtoProcesoDevart>>.Ok(
                [new DtoProcesoDevart { IdProceso = 20, NombreProceso = "Marzo" }],
                nameof(IRegistroService.ObtenerProcesosHabilitadosPorProducto));
            _preinscripcionServiceMock
                .Setup(s => s.ObtenerProcesosHabilitadosPorProducto(10))
                .Returns(expected);

            var result = _service.ObtenerProcesosHabilitadosPorProducto(10);

            Assert.Same(expected, result);
        }

        [Fact]
        public void ObtenerProductosVigentes_ReturnsMappedItems()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetProductosVigentesParaRegistro()).Returns(
            [
                new Producto
                {
                    IdProducto = 10,
                    NombreProducto = "ATI",
                    NombreExtensoProducto = "Analista en TI",
                    IdNivelProducto = 2,
                    AliasProducto = "ATI",
                    InscribibleProducto = "SI",
                    IntermedioProducto = "NO",
                    VisibleAdmisionesProducto = "SI",
                    NivelProducto = new NivelProducto { IdNivelProducto = 2, NombreNivelProducto = "Carrera" },
                    ProcesoProductos =
                    [
                        new ProcesoProducto
                        {
                            IdProceso = 20,
                            Proceso = new Proceso { IdProceso = 20, NombreProceso = "Marzo" }
                        }
                    ]
                }
            ]);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = _service.ObtenerProductosVigentes();

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(10, item.IdProducto);
            Assert.Equal(20, item.IdProceso);
            Assert.Equal("Marzo", item.NombreProceso);
        }
    }
}
