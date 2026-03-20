using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class PreinscripcionServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly PreinscripcionService _service;

        public PreinscripcionServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new PreinscripcionService(_uowFactoryMock.Object);
        }

        [Fact]
        public void ObtenerFechaVencimientoAdmisiones_ProcesoSinFecha_ReturnsFailed()
        {
            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetByKey(2)).Returns(new Proceso { IdProceso = 2, ComienzoSemestre1Proceso = null });
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var result = _service.ObtenerFechaVencimientoAdmisiones(1, 2);

            Assert.False(result.Success);
            Assert.Equal("GEN_FVA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }
    }
}
