using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using AppLogic.Services;
using AppLogic.DTOs;
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
        private readonly Mock<IProcesoComienzoRepository> _repoMock;
        private readonly ProcesoComienzoServices _service;

        public ProcesoComienzoServicesTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<IProcesoComienzoRepository>();

            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(_repoMock.Object);

            _service = new ProcesoComienzoServices(_uowFactoryMock.Object);
        }

        #region ObtenerProcesoComienzos Tests

        [Fact]
        public void ObtenerProcesoComienzos_ReturnsAllEntities_Success()
        {
            var entities = new List<ProcesoComienzo>
            {
                new() { IdProceso = 1, IdComienzo = 10, HoraIngreso = "08:00:00" },
                new() { IdProceso = 2, IdComienzo = 20, HoraIngreso = "09:00:00" }
            };

            _repoMock.Setup(r => r.GetAllWithRelated()).Returns(entities);

            var result = _service.ObtenerProcesoComienzos();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            var list = result.Data.ToList();
            Assert.Equal(2, list.Count);
            _repoMock.Verify(r => r.GetAllWithRelated(), Times.Once);
        }

        [Fact]
        public void ObtenerProcesoComienzos_EmptyList_ReturnsEmptyCollection()
        {
            _repoMock.Setup(r => r.GetAllWithRelated()).Returns(new List<ProcesoComienzo>());

            var result = _service.ObtenerProcesoComienzos();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        #endregion

        #region ObtenerProcesoComienzo Tests

        [Fact]
        public void ObtenerProcesoComienzo_Found_ReturnsDto()
        {
            var entity = new ProcesoComienzo 
            { 
                IdProceso = 1, 
                IdComienzo = 10, 
                HoraIngreso = "10:00:00",
                FechaIngreso = new DateTime(2026, 1, 15),
                UsuarioIngreso = "testuser"
            };

            _repoMock.Setup(r => r.GetByKeyWithRelated(1, 10)).Returns(entity);

            var result = _service.ObtenerProcesoComienzo(1, 10);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.IdProceso);
            Assert.Equal(10, result.Data.IdComienzo);
            Assert.Equal("10:00:00", result.Data.HoraIngreso);
            _repoMock.Verify(r => r.GetByKeyWithRelated(1, 10), Times.Once);
        }

        [Fact]
        public void ObtenerProcesoComienzo_NotFound_ReturnsFailed()
        {
            _repoMock.Setup(r => r.GetByKeyWithRelated(99, 99)).Returns((ProcesoComienzo)null);

            var result = _service.ObtenerProcesoComienzo(99, 99);

            Assert.False(result.Success);
            Assert.Equal("PC_GPC_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            Assert.Equal("ProcesoComienzo no encontrado.", result.Message);
        }

        #endregion

        #region GuardarProcesoComienzo Tests

        [Fact]
        public void GuardarProcesoComienzo_NewEntity_CreatesSuccessfully()
        {
            var dto = new ProcesoComienzoRequest
            {
                IdProceso = 1,
                IdComienzo = 10,
                HoraIngreso = "08:30:00",
                FechaIngreso = new DateTime(2026, 1, 15),
                UsuarioIngreso = "admin"
            };

            _repoMock.Setup(r => r.GetByKey(1, 10)).Returns((ProcesoComienzo)null);

            var result = _service.GuardarProcesoComienzo(dto);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.IdProceso);
            Assert.Equal(10, result.Data.IdComienzo);
            Assert.Equal("08:30:00", result.Data.HoraIngreso);
            _repoMock.Verify(r => r.Add(It.Is<ProcesoComienzo>(
                e => e.IdProceso == 1 && 
                     e.IdComienzo == 10 && 
                     e.HoraIngreso == "08:30:00" &&
                     e.UsuarioIngreso == "admin")), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void GuardarProcesoComienzo_AlreadyExists_ReturnsFailed()
        {
            var existingEntity = new ProcesoComienzo { IdProceso = 1, IdComienzo = 10 };
            var dto = new ProcesoComienzoRequest
            {
                IdProceso = 1,
                IdComienzo = 10,
                HoraIngreso = "08:30:00"
            };

            _repoMock.Setup(r => r.GetByKey(1, 10)).Returns(existingEntity);

            var result = _service.GuardarProcesoComienzo(dto);

            Assert.False(result.Success);
            Assert.Equal("PC_SPC_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("ProcesoComienzo ya existe.", result.Message);
            _repoMock.Verify(r => r.Add(It.IsAny<ProcesoComienzo>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarProcesoComienzo_WithNullableFields_CreatesSuccessfully()
        {
            var dto = new ProcesoComienzoRequest
            {
                IdProceso = 1,
                IdComienzo = 10,
                HoraIngreso = null,
                FechaIngreso = null,
                UsuarioIngreso = null
            };

            _repoMock.Setup(r => r.GetByKey(1, 10)).Returns((ProcesoComienzo)null);

            var result = _service.GuardarProcesoComienzo(dto);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            _repoMock.Verify(r => r.Add(It.Is<ProcesoComienzo>(
                e => e.IdProceso == 1 && 
                     e.IdComienzo == 10)), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        #endregion

        #region ModificarProcesoComienzo Tests

        [Fact]
        public void ModificarProcesoComienzo_Found_UpdatesSuccessfully()
        {
            var entity = new ProcesoComienzo 
            { 
                IdProceso = 1, 
                IdComienzo = 10, 
                HoraIngreso = "08:00:00",
                FechaIngreso = new DateTime(2025, 12, 1),
                UsuarioIngreso = "olduser"
            };

            var dto = new ProcesoComienzoRequest
            {
                IdProceso = 1,
                IdComienzo = 10,
                HoraIngreso = "15:00:00",
                FechaIngreso = new DateTime(2026, 3, 2),
                UsuarioIngreso = "newuser"
            };

            _repoMock.Setup(r => r.GetByKey(1, 10)).Returns(entity);

            var result = _service.ModificarProcesoComienzo(dto);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("15:00:00", result.Data.HoraIngreso);
            Assert.Equal("15:00:00", entity.HoraIngreso);
            Assert.Equal(new DateTime(2026, 3, 2), entity.FechaIngreso);
            Assert.Equal("newuser", entity.UsuarioIngreso);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ModificarProcesoComienzo_NotFound_ReturnsFailed()
        {
            var dto = new ProcesoComienzoRequest
            {
                IdProceso = 99,
                IdComienzo = 99,
                HoraIngreso = "15:00:00"
            };

            _repoMock.Setup(r => r.GetByKey(99, 99)).Returns((ProcesoComienzo)null);

            var result = _service.ModificarProcesoComienzo(dto);

            Assert.False(result.Success);
            Assert.Equal("PC_APC_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            Assert.Equal("ProcesoComienzo no encontrado.", result.Message);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void ModificarProcesoComienzo_WithNullableFields_UpdatesSuccessfully()
        {
            var entity = new ProcesoComienzo 
            { 
                IdProceso = 1, 
                IdComienzo = 10, 
                HoraIngreso = "08:00:00",
                FechaIngreso = new DateTime(2025, 12, 1),
                UsuarioIngreso = "olduser"
            };

            var dto = new ProcesoComienzoRequest
            {
                IdProceso = 1,
                IdComienzo = 10,
                HoraIngreso = null,
                FechaIngreso = null,
                UsuarioIngreso = null
            };

            _repoMock.Setup(r => r.GetByKey(1, 10)).Returns(entity);

            var result = _service.ModificarProcesoComienzo(dto);

            Assert.True(result.Success);
            Assert.Null(entity.HoraIngreso);
            Assert.Null(entity.FechaIngreso);
            Assert.Null(entity.UsuarioIngreso);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        #endregion

        #region UnitOfWork Disposal Tests

        [Fact]
        public void ObtenerProcesoComienzos_DisposesUnitOfWork()
        {
            _repoMock.Setup(r => r.GetAllWithRelated()).Returns(new List<ProcesoComienzo>());

            _service.ObtenerProcesoComienzos();

            _uowMock.Verify(u => u.Dispose(), Times.Once);
        }

        [Fact]
        public void GuardarProcesoComienzo_DisposesUnitOfWork()
        {
            var dto = new ProcesoComienzoRequest { IdProceso = 1, IdComienzo = 10 };
            _repoMock.Setup(r => r.GetByKey(1, 10)).Returns((ProcesoComienzo)null);

            _service.GuardarProcesoComienzo(dto);

            _uowMock.Verify(u => u.Dispose(), Times.Once);
        }

        #endregion
    }
}