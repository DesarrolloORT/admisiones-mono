using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Utilities;
using Xunit;
using AppLogic.Services.Inscripciones;
using AppLogic.IServices.Catalogos;

namespace UnitTesting.AppLogic.Services
{
    public class PreinscripcionServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IGeneralService> _generalServiceMock;
        private readonly PreinscripcionService _service;

        public PreinscripcionServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _generalServiceMock = new Mock<IGeneralService>();

            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new PreinscripcionService(_uowFactoryMock.Object, _generalServiceMock.Object);
        }

        [Fact]
        public void ObtenerFechaVencimientoAdmisiones_ProcesoSinFecha_ReturnsFailed()
        {
            _generalServiceMock
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 2))
                .Returns(OperationResult<DateTime>.IsFailed("GEN_FVA_01", "CalcularFechaVencimientoAdmisiones", "Problema con la carga de fecha del comienzo del proceso.", 400));

            var result = _service.ObtenerFechaVencimientoAdmisiones(1, 2);

            Assert.False(result.Success);
            Assert.Equal("GEN_FVA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void ObtenerFechaVencimientoAdmisiones_HappyPath_DelegatesToGeneralService()
        {
            var fecha = new DateTime(2026, 3, 25);
            _generalServiceMock
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(1, 2))
                .Returns(OperationResult<DateTime>.Ok(fecha, "CalcularFechaVencimientoAdmisiones"));

            var result = _service.ObtenerFechaVencimientoAdmisiones(1, 2);

            Assert.True(result.Success);
            Assert.Equal(fecha, result.Data);
        }
    }
}
