using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using AppLogic.Services;
using AppLogic.Interfaces;
using BusinessLogic.Entities;
using AppLogic.DevartDTOs;
using Utilities;
using BusinessLogic.IDevartRepositories;

namespace UnitTesting.AppLogic.Services
{
    public class ProcesoComienzoServicesTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly ProcesoComienzoServices _service;

        public ProcesoComienzoServicesTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new ProcesoComienzoServices(_uowFactoryMock.Object);
        }

        [Fact]
        public void ObtenerProcesoComienzos_ReturnsAll()
        {
            var repoMock = new Mock<BusinessLogic.IDevartRepositories.IProcesoComienzoRepository>();
            repoMock.Setup(r => r.GetAll()).Returns(new List<ProcesoComienzo>
            {
                new ProcesoComienzo { IdProceso = 1, IdComienzo = 10 },
                new ProcesoComienzo { IdProceso = 2, IdComienzo = 20 }
            });
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(repoMock.Object);

            var result = _service.ObtenerProcesoComienzos();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            var list = new List<DtoProcesoComienzoDevart>(result.Data);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void ObtenerProcesoComienzo_Found_ReturnsDto()
        {
            var repoMock = new Mock<BusinessLogic.IDevartRepositories.IProcesoComienzoRepository>();
            repoMock.Setup(r => r.GetByKey(1, 10))
                    .Returns(new ProcesoComienzo { IdProceso = 1, IdComienzo = 10, HoraIngreso = "10:00:00" });
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(repoMock.Object);

            var result = _service.ObtenerProcesoComienzo(1, 10);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.IdProceso);
            Assert.Equal(10, result.Data.IdComienzo);
            Assert.Equal("10:00:00", result.Data.HoraIngreso);
        }

        [Fact]
        public void ObtenerProcesoComienzo_NotFound_ReturnsFailed()
        {
            var repoMock = new Mock<BusinessLogic.IDevartRepositories.IProcesoComienzoRepository>();
            repoMock.Setup(r => r.GetByKey(It.IsAny<long>(), It.IsAny<long>()))
                    .Returns((ProcesoComienzo)null);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(repoMock.Object);

            var result = _service.ObtenerProcesoComienzo(99, 99);

            Assert.False(result.Success);
            Assert.Equal("PC_GPC_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ActualizarProcesoComienzo_NotFound_ReturnsFailed()
        {
            var repoMock = new Mock<BusinessLogic.IDevartRepositories.IProcesoComienzoRepository>();
            repoMock.Setup(r => r.GetByKey(It.IsAny<long>(), It.IsAny<long>()))
                    .Returns((ProcesoComienzo)null);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(repoMock.Object);

            var dto = new DtoProcesoComienzoDevart { IdProceso = 1, IdComienzo = 10 };
            var result = _service.ActualizarProcesoComienzo(dto);

            Assert.False(result.Success);
            Assert.Equal("PC_APC_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ActualizarProcesoComienzo_Found_UpdatesAndReturnsDto()
        {
            var entidad = new ProcesoComienzo { IdProceso = 1, IdComienzo = 10, HoraIngreso = "08:00:00", UsuarioIngreso = "userOld" };
            var repoMock = new Mock<BusinessLogic.IDevartRepositories.IProcesoComienzoRepository>();
            repoMock.Setup(r => r.GetByKey(1, 10)).Returns(entidad);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(repoMock.Object);

            var dto = new DtoProcesoComienzoDevart
            {
                IdProceso = 1,
                IdComienzo = 10,
                HoraIngreso = "15:00:00",
                FechaIngreso = new DateTime(2026, 3, 2),
                UsuarioIngreso = "userNew"
            };
            var result = _service.ActualizarProcesoComienzo(dto);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("15:00:00", result.Data.HoraIngreso);
            Assert.Equal("userNew", result.Data.UsuarioIngreso);
            repoMock.Verify(r => r.Update(entidad), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }
    }
}