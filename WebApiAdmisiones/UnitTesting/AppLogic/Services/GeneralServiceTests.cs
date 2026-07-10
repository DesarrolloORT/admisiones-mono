using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Xunit;
using AppLogic.Catalogos.Services;

namespace UnitTesting.AppLogic.Services
{
    public class GeneralServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly GeneralService _service;

        public GeneralServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new GeneralService(_uowFactoryMock.Object);
        }

        [Fact]
        public void CalcularFechaVencimientoAdmisiones_ProcesoSinFecha_ReturnsFailed()
        {
            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo
                .Setup(r => r.GetByKey(2))
                .Returns(new Proceso { IdProceso = 2, ComienzoSemestre1Proceso = null });
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var result = _service.CalcularFechaVencimientoAdmisiones(_uowMock.Object, 1, 2);

            Assert.False(result.Success);
            Assert.Equal("GEN_FVA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void CalcularFechaVencimientoAdmisiones_DjMasTemprana_ReturnsDjDate()
        {
            var fechaProceso = DateTime.Today.AddDays(10);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo
                .Setup(r => r.GetByKey(2))
                .Returns(new Proceso { IdProceso = 2, ComienzoSemestre1Proceso = fechaProceso });
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var declaracionRepo = new Mock<IDeclaracionJuradaWebRepository>();
            declaracionRepo
                .Setup(r => r.GetFechaEntregaDjAdmisiones(1))
                .Returns(DateTime.Today.AddDays(2));
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(declaracionRepo.Object);

            var feriadoRepo = new Mock<IFeriadoRepository>();
            feriadoRepo.Setup(r => r.EsFeriado(It.IsAny<DateTime>())).Returns(false);
            _uowMock.Setup(u => u.Feriados).Returns(feriadoRepo.Object);

            var result = _service.CalcularFechaVencimientoAdmisiones(_uowMock.Object, 1, 2);

            Assert.True(result.Success);
            Assert.Equal(DateTime.Today.AddDays(2), result.Data);
        }
    }
}
