using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using AppLogic.Services;
using BusinessLogic.Entities;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;
using BusinessLogic.IDevartRepositories;

namespace UnitTesting.AppLogic.Services
{
    public class FondoDeBecaServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly FondoDeBecaServices _service;

        public FondoDeBecaServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new FondoDeBecaServices(_uowFactoryMock.Object);
        }

        #region TIPOS DECLARACIÓN JURADA

        [Fact]
        public void ObtenerTiposParentesco_ReturnsAllTypes()
        {
            // Arrange
            var tiposParentesco = new List<TipoParentesco>
            {
                new TipoParentesco { IdTipoParentesco = 1, DescripcionTp = "Padre" },
                new TipoParentesco { IdTipoParentesco = 2, DescripcionTp = "Madre" },
                new TipoParentesco { IdTipoParentesco = 3, DescripcionTp = "Hermano" }
            };

            var tipoParentescoRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoParentescoRepository>();
            tipoParentescoRepo.Setup(r => r.GetAll()).Returns(tiposParentesco);
            _uowMock.Setup(u => u.TipoParentescos).Returns(tipoParentescoRepo.Object);

            // Act
            var result = _service.ObtenerTiposParentesco();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.Count());
            tipoParentescoRepo.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void ObtenerTiposParentesco_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var tipoParentescoRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoParentescoRepository>();
            tipoParentescoRepo.Setup(r => r.GetAll()).Returns(new List<TipoParentesco>());
            _uowMock.Setup(u => u.TipoParentescos).Returns(tipoParentescoRepo.Object);

            // Act
            var result = _service.ObtenerTiposParentesco();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public void ObtenerTiposEgreso_ReturnsActivosOrderedByOrden()
        {
            // Arrange
            var tiposEgreso = new List<TipoEgresoDj>
            {
                new TipoEgresoDj { IdTipoEgresoDj = 1, NombreTipoEgresoDj = "Servicios", Activo = "SI", Orden = 2 },
                new TipoEgresoDj { IdTipoEgresoDj = 2, NombreTipoEgresoDj = "Alquiler", Activo = "SI", Orden = 1 },
                new TipoEgresoDj { IdTipoEgresoDj = 3, NombreTipoEgresoDj = "Inactivo", Activo = "NO", Orden = 3 }
            };

            var tipoEgresoRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoEgresoDjRepository>();
            tipoEgresoRepo.Setup(r => r.GetAll()).Returns(tiposEgreso);
            _uowMock.Setup(u => u.TipoEgresoDjs).Returns(tipoEgresoRepo.Object);

            // Act
            var result = _service.ObtenerTiposEgreso();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            var list = result.Data.ToList();
            Assert.Equal(2, list.Count); // Solo activos
            Assert.Equal("Alquiler", list[0].NombreTipoEgresoDj); // Orden 1 primero
            Assert.Equal("Servicios", list[1].NombreTipoEgresoDj); // Orden 2 segundo
        }

        [Fact]
        public void ObtenerTiposEgreso_NoActivos_ReturnsEmpty()
        {
            // Arrange
            var tiposEgreso = new List<TipoEgresoDj>
            {
                new TipoEgresoDj { IdTipoEgresoDj = 1, NombreTipoEgresoDj = "Inactivo", Activo = "NO", Orden = 1 }
            };

            var tipoEgresoRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoEgresoDjRepository>();
            tipoEgresoRepo.Setup(r => r.GetAll()).Returns(tiposEgreso);
            _uowMock.Setup(u => u.TipoEgresoDjs).Returns(tipoEgresoRepo.Object);

            // Act
            var result = _service.ObtenerTiposEgreso();

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.Data);
        }

        [Fact]
        public void ObtenerTiposVivienda_ReturnsAllTypes()
        {
            // Arrange
            var tiposVivienda = new List<TipoVivienda>
            {
                new TipoVivienda { IdTipoVivienda = 1, NombreTipoVivienda = "Casa" },
                new TipoVivienda { IdTipoVivienda = 2, NombreTipoVivienda = "Apartamento" },
                new TipoVivienda { IdTipoVivienda = 3, NombreTipoVivienda = "Pieza" }
            };

            var tipoViviendaRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoViviendaRepository>();
            tipoViviendaRepo.Setup(r => r.GetAll()).Returns(tiposVivienda);
            _uowMock.Setup(u => u.TipoViviendas).Returns(tipoViviendaRepo.Object);

            // Act
            var result = _service.ObtenerTiposVivienda();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.Count());
            tipoViviendaRepo.Verify(r => r.GetAll(), Times.Once);
        }

        #endregion TIPOS DECLARACIÓN JURADA

        #region UNIVERSIDADES

        [Fact]
        public void ObtenerUniversidades_ValidCountry_ReturnsUniversidades()
        {
            // Arrange
            long codigoPais = 1;
            var pais = new Pais { CodigoPais = 1, Nombre = "Uruguay" };
            var universidades = new List<Empresa>
            {
                new Empresa { CodigoEmpresa = 1, Nombre = "Universidad A", CodigoPais = 1 },
                new Empresa { CodigoEmpresa = 2, Nombre = "Universidad B", CodigoPais = 1 }
            };

            var paisRepo = new Mock<IPaisRepository>();
            paisRepo.Setup(r => r.GetPaisConEstadosYCiudades(codigoPais)).Returns(pais);
            _uowMock.Setup(u => u.Paises).Returns(paisRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetUniversidades(codigoPais)).Returns(universidades);
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            // Act
            var result = _service.ObtenerUniversidades(codigoPais);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count());
            paisRepo.Verify(r => r.GetPaisConEstadosYCiudades(codigoPais), Times.Once);
            empresaRepo.Verify(r => r.GetUniversidades(codigoPais), Times.Once);
        }

        [Fact]
        public void ObtenerUniversidades_InvalidCountry_ReturnsFailed()
        {
            // Arrange
            long codigoPais = 999;
            var paisRepo = new Mock<IPaisRepository>();
            paisRepo.Setup(r => r.GetPaisConEstadosYCiudades(codigoPais)).Returns((Pais)null);
            _uowMock.Setup(u => u.Paises).Returns(paisRepo.Object);

            // Act
            var result = _service.ObtenerUniversidades(codigoPais);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FDB_UV_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            paisRepo.Verify(r => r.GetPaisConEstadosYCiudades(codigoPais), Times.Once);
        }

        [Fact]
        public void ObtenerFormulariosDeclaracionJuradaWeb_ValidPersona_ReturnsForms()
        {
            // Arrange
            long codigoPersona = 1;

            var comienzo = new Comienzo { IdComienzo = 1, NombreComienzo = "2024-1" };
            var tipoDescuento = new TipoDescuento { IdTipoDescuento = 1, NombreTipoDescuento = "Beca A" };

            var declaraciones = new List<DeclaracionJuradaWeb>
            {
                new DeclaracionJuradaWeb
                {
                    IdDeclaracionjuradaWeb = 1,
                    CodigoPersona = 1,
                    IdInscriptoPrueba = 100,
                    Persona = new Persona { CodigoPersona = 1, PrimerNombre = "Test" },
                    Producto = new Producto { IdProducto = 1, NombreProducto = "Prod1" },
                    TipoDescuento = tipoDescuento
                }
            };

            var inscriptoPrueba = new InscriptoPrueba { IdInscriptoPrueba = 100, IdPrueba = 50 };
            var prueba = new Prueba
            {
                IdPrueba = 50,
                TipoDescuento = tipoDescuento,
                Comienzo = comienzo
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormulariosAdmisionesVigentes(codigoPersona)).Returns(declaraciones);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(100)).Returns(inscriptoPrueba);
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(prueba);
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            // Act
            var result = _service.ObtenerFormulariosDeclaracionJuradaWeb(codigoPersona);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            djRepo.Verify(r => r.GetFormulariosAdmisionesVigentes(codigoPersona), Times.Once);
        }

        [Fact]
        public void ObtenerFormulariosDeclaracionJuradaWeb_NoForms_ReturnsFailed()
        {
            // Arrange
            long codigoPersona = 999;
            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormulariosAdmisionesVigentes(codigoPersona))
                .Returns(new List<DeclaracionJuradaWeb>());
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            // Act
            var result = _service.ObtenerFormulariosDeclaracionJuradaWeb(codigoPersona);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FDB_FDJ_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            djRepo.Verify(r => r.GetFormulariosAdmisionesVigentes(codigoPersona), Times.Once);
        }

        [Fact]
        public void ObtenerFormularioDeclaracionJuradaWebDetalle_ValidIds_ReturnsDetail()
        {
            // Arrange
            long codigoPersona = 1;
            long idInscriptoPrueba = 100;

            var comienzo = new Comienzo { IdComienzo = 1, NombreComienzo = "2024-1" };
            var tipoDescuento = new TipoDescuento { IdTipoDescuento = 1, NombreTipoDescuento = "Beca A" };
            var tipoVivienda = new TipoVivienda { IdTipoVivienda = 5, NombreTipoVivienda = "Casa" };
            var bachillerato = new Empresa { CodigoEmpresa = 10, Nombre = "Bachillerato A" };

            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 1,
                CodigoPersona = 1,
                IdInscriptoPrueba = 100,
                CodigoInstitucionBac = 10,
                IdTipoVivienda = 5,
                Persona = new Persona { CodigoPersona = 1, PrimerNombre = "Test" },
                Producto = new Producto { IdProducto = 1 },
                TipoDescuento = tipoDescuento
            };

            var inscriptoPrueba = new InscriptoPrueba { IdInscriptoPrueba = 100, IdPrueba = 50 };
            var prueba = new Prueba
            {
                IdPrueba = 50,
                TipoDescuento = tipoDescuento,
                Comienzo = comienzo
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisiones(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(100)).Returns(inscriptoPrueba);
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(prueba);
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(10)).Returns(bachillerato);
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tipoViviendaRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoViviendaRepository>();
            tipoViviendaRepo.Setup(r => r.GetByKey(5)).Returns(tipoVivienda);
            _uowMock.Setup(u => u.TipoViviendas).Returns(tipoViviendaRepo.Object);

            // Act
            var result = _service.ObtenerFormularioDeclaracionJuradaWebDetalle(codigoPersona, idInscriptoPrueba);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Bachillerato A", result.Data.NombreBachillerato);
            Assert.NotNull(result.Data.ObjTipoVivienda);
            djRepo.Verify(r => r.GetFormularioAdmisiones(codigoPersona, idInscriptoPrueba), Times.Once);
        }

        [Fact]
        public void ObtenerFormularioDeclaracionJuradaWebDetalle_NotFound_ReturnsFailed()
        {
            // Arrange
            long codigoPersona = 1;
            long idInscriptoPrueba = 999;

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisiones(codigoPersona, idInscriptoPrueba))
                .Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            // Act
            var result = _service.ObtenerFormularioDeclaracionJuradaWebDetalle(codigoPersona, idInscriptoPrueba);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FDB_FDJ_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            djRepo.Verify(r => r.GetFormularioAdmisiones(codigoPersona, idInscriptoPrueba), Times.Once);
        }

        [Fact]
        public void ObtenerFormularioDeclaracionJuradaWebDetalle_WithoutBachillerato_ReturnsDetailWithoutBachillerato()
        {
            // Arrange
            long codigoPersona = 1;
            long idInscriptoPrueba = 100;

            var comienzo = new Comienzo { IdComienzo = 1, NombreComienzo = "2024-1" };
            var tipoDescuento = new TipoDescuento { IdTipoDescuento = 1, NombreTipoDescuento = "Beca A" };
            var tipoVivienda = new TipoVivienda { IdTipoVivienda = 5, NombreTipoVivienda = "Casa" };

            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 1,
                CodigoPersona = 1,
                IdInscriptoPrueba = 100,
                CodigoInstitucionBac = null, // Sin bachillerato
                IdTipoVivienda = 5,
                Persona = new Persona { CodigoPersona = 1, PrimerNombre = "Test" },
                Producto = new Producto { IdProducto = 1 },
                TipoDescuento = tipoDescuento
            };

            var inscriptoPrueba = new InscriptoPrueba { IdInscriptoPrueba = 100, IdPrueba = 50 };
            var prueba = new Prueba
            {
                IdPrueba = 50,
                TipoDescuento = tipoDescuento,
                Comienzo = comienzo
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisiones(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(100)).Returns(inscriptoPrueba);
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(prueba);
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((Empresa)null);
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tipoViviendaRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoViviendaRepository>();
            tipoViviendaRepo.Setup(r => r.GetByKey(5)).Returns(tipoVivienda);
            _uowMock.Setup(u => u.TipoViviendas).Returns(tipoViviendaRepo.Object);

            // Act
            var result = _service.ObtenerFormularioDeclaracionJuradaWebDetalle(codigoPersona, idInscriptoPrueba);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Null(result.Data.NombreBachillerato);
        }

        [Fact]
        public void ObtenerFormularioDeclaracionJuradaWebDetalle_WithoutTipoVivienda_ReturnsDetailWithoutTipoVivienda()
        {
            // Arrange
            long codigoPersona = 1;
            long idInscriptoPrueba = 100;

            var comienzo = new Comienzo { IdComienzo = 1, NombreComienzo = "2024-1" };
            var tipoDescuento = new TipoDescuento { IdTipoDescuento = 1, NombreTipoDescuento = "Beca A" };
            var bachillerato = new Empresa { CodigoEmpresa = 10, Nombre = "Bachillerato A" };

            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 1,
                CodigoPersona = 1,
                IdInscriptoPrueba = 100,
                CodigoInstitucionBac = 10,
                IdTipoVivienda = null, // Sin tipo de vivienda
                Persona = new Persona { CodigoPersona = 1, PrimerNombre = "Test" },
                Producto = new Producto { IdProducto = 1 },
                TipoDescuento = tipoDescuento
            };

            var inscriptoPrueba = new InscriptoPrueba { IdInscriptoPrueba = 100, IdPrueba = 50 };
            var prueba = new Prueba
            {
                IdPrueba = 50,
                TipoDescuento = tipoDescuento,
                Comienzo = comienzo
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisiones(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(100)).Returns(inscriptoPrueba);
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(prueba);
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(10)).Returns(bachillerato);
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tipoViviendaRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoViviendaRepository>();
            tipoViviendaRepo.Setup(r => r.GetByKey(It.IsAny<decimal>())).Returns((TipoVivienda)null);
            _uowMock.Setup(u => u.TipoViviendas).Returns(tipoViviendaRepo.Object);

            // Act
            var result = _service.ObtenerFormularioDeclaracionJuradaWebDetalle(codigoPersona, idInscriptoPrueba);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Null(result.Data.ObjTipoVivienda);
        }

        [Fact]
        public void SubirArchivoEgreso_CuandoNoPerteneceALaPersona_ReturnsForbidden()
        {
            var egreso = new EgresoMensualNfDj
            {
                IdEgresoMensualNfDj = 10,
                IdDeclaracionjuradaWeb = 99
            };

            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            egresoRepo.Setup(r => r.GetByKey(10)).Returns(egreso);
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(99)).Returns(new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 99,
                CodigoPersona = 999
            });
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _service.SubirArchivoEgreso(1, 10, jpegContent, "egreso.jpg");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAE_02", result.ErrorCode);
            Assert.Equal(403, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirArchivoRevalidaDJ_CuandoNoPerteneceALaPersona_ReturnsForbidden()
        {
            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 999
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirArchivoRevalidaDJ(1, 55, pdfContent, "revalida.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAR_02", result.ErrorCode);
            Assert.Equal(403, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        #endregion UNIVERSIDADES
    }
}
