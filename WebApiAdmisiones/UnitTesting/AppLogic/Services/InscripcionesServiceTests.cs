using System.Collections.Generic;
using AppLogic.DTOs;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class InscripcionesServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly InscripcionesService _service;

        public InscripcionesServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new InscripcionesService(_uowFactoryMock.Object);
        }

        [Fact]
        public void ObtenerUltimaInscripcionActiva_NotFound_ReturnsFailed()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.GetUltimaInscripcionActiva(123)).Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.ObtenerUltimaInscripcionActiva(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_UI_01", result.ErrorCode);
            Assert.Equal(204, result.HttpCode);
        }

        [Fact]
        public void ObtenerProductosConInteresActivo_ReturnsMappedItems()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetProductosConInteresActivo(123)).Returns(new List<Producto>
            {
                new Producto
                {
                    IdProducto = 10,
                    NombreProducto = "Producto A",
                    NombreExtensoProducto = "Producto Extenso A",
                    IdNivelProducto = 2,
                    ProcesoProductos = new List<ProcesoProducto>
                    {
                        new ProcesoProducto
                        {
                            IdProceso = 7,
                            Proceso = new Proceso { IdProceso = 7, NombreProceso = "Proceso A" }
                        }
                    }
                }
            });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = _service.ObtenerProductosConInteresActivo(123);

            Assert.True(result.Success);
            var list = new List<DTOProductoAdmisiones>(result.Data!);
            Assert.Single(list);
            Assert.Equal(10, list[0].IdProducto);
            Assert.Equal(7, list[0].IdProceso);
            Assert.Equal("Proceso A", list[0].NombreProceso);
        }

        [Fact]
        public void TieneInscripcionActivaParaProceso_ReturnsRepositoryValue()
        {
            var repo = new Mock<IVdEsFrescoAdmisionRepository>();
            repo.Setup(r => r.TieneInscripcionActivaParaProceso(1, 2, 3)).Returns(true);
            _uowMock.Setup(u => u.VdEsFrescoAdmisions).Returns(repo.Object);

            var result = _service.TieneInscripcionActivaParaProceso(1, 2, 3);

            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void TieneInscripcionAdmisiones_ReturnsRepositoryValue()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.TieneInscripcionAdmisiones(1, 2, 3)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.TieneInscripcionAdmisiones(1, 2, 3);

            Assert.True(result.Success);
            Assert.False(result.Data);
        }
    }
}
