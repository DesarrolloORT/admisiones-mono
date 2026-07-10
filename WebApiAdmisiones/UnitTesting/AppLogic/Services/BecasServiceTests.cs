using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using System.Linq;
using Xunit;
using AppLogic.Becas.Services;

namespace UnitTesting.AppLogic.Services
{
    public class BecasServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly BecasService _service;

        public BecasServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new BecasService(_uowFactoryMock.Object);
        }

        [Fact]
        public void ObtenerMisInscripcionesConfirmadas_ReturnsOnlyConfirmadas()
        {
            var repo = new Mock<IVdInscripcionesFresco1y2Repository>();
            repo.Setup(r => r.GetInscripcionesFrescoHabilitadas(123)).Returns(
            [
                new VdInscripcionesFresco1y2 { IdProducto = 10, EstadoInscripcion = "Confirmada" },
                new VdInscripcionesFresco1y2 { IdProducto = 20, EstadoInscripcion = "Pendiente" }
            ]);
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(repo.Object);

            var result = _service.ObtenerMisInscripcionesConfirmadas(123);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(10, item.IdProducto);
            Assert.Equal("Confirmada", item.EstadoInscripcion);
        }
    }
}
