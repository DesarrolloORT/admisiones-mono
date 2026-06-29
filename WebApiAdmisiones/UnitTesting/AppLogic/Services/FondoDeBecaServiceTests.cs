using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using BusinessLogic.Entities;
using AppLogic.DevartDTOs;
using Utilities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using AppLogic.Services.Becas;

namespace UnitTesting.AppLogic.Services
{
    public class FondoDeBecaServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly FondoDeBecaService _service;

        public FondoDeBecaServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new FondoDeBecaService(_uowFactoryMock.Object, _dbConnectionContextMock.Object);
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
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
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
            djRepo.Verify(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba), Times.Once);
        }

        [Fact]
        public void ObtenerFormularioDeclaracionJuradaWebDetalle_NotFound_ReturnsFailed()
        {
            // Arrange
            long codigoPersona = 1;
            long idInscriptoPrueba = 999;

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba))
                .Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            // Act
            var result = _service.ObtenerFormularioDeclaracionJuradaWebDetalle(codigoPersona, idInscriptoPrueba);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FDB_FDJ_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            djRepo.Verify(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba), Times.Once);
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
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
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
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
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
        public void GuardarFormularioDeclaracionJuradaWeb_HappyPath_GuardaBorrador()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var declaracion = CrearDeclaracionBase();
            var modificada = CrearDtoBase();
            modificada.ObservacionesNfDj = "Actualizado";

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                IdPrueba = 50,
                CodigoPersona = codigoPersona,
                IdProducto = 10
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(new Prueba { IdPrueba = 50, IdTipoBeca = 1, FechaEntregaDjPrueba = DateTime.Today.AddDays(1) });
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tipoViviendaRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoViviendaRepository>();
            _uowMock.Setup(u => u.TipoViviendas).Returns(tipoViviendaRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, modificada, false);

            Assert.True(result.Success);
            Assert.Equal("Actualizado", declaracion.ObservacionesNfDj);
            Assert.Equal("GUARDADO_WEB_SIN_CONFIRMAR", declaracion.Subestado);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_RequestNull_ReturnsBadRequest()
        {
            var result = _service.GuardarFormularioDeclaracionJuradaWeb(1, null!, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_00", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_IdInscriptoPruebaInvalido_ReturnsBadRequest()
        {
            var dto = CrearDtoBase();
            dto.IdInscriptoPrueba = 0;

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(1, dto, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_15", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_CuandoNoExisteYNoHayInscriptoPrueba_ReturnsNotFound()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var dto = CrearDtoBase();
            dto.IdDeclaracionjuradaWeb = 0;

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns((InscriptoPrueba)null);
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, dto, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_16", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_CuandoNoExisteYNoPerteneceALaPersona_ReturnsForbidden()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var dto = CrearDtoBase();
            dto.IdDeclaracionjuradaWeb = 0;

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                CodigoPersona = 999,
                IdProducto = 10,
                IdPrueba = 50
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, dto, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_17", result.ErrorCode);
            Assert.Equal(403, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_CuandoNoExisteYSinProducto_ReturnsNotFound()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var dto = CrearDtoBase();
            dto.IdDeclaracionjuradaWeb = 0;

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                CodigoPersona = codigoPersona,
                IdProducto = null,
                IdPrueba = 50
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, dto, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_18", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_CuandoNoExisteYSinPrueba_ReturnsNotFound()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var dto = CrearDtoBase();
            dto.IdDeclaracionjuradaWeb = 0;

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                CodigoPersona = codigoPersona,
                IdProducto = 10,
                IdPrueba = 50
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns((Prueba)null);
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, dto, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_19", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_SinCambiosEnModificacion_NoGuarda()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var declaracion = CrearDeclaracionBase();
            var modificada = CrearDtoBase();

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                IdPrueba = 50,
                CodigoPersona = codigoPersona,
                IdProducto = 10
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(new Prueba { IdPrueba = 50, IdTipoBeca = 1, FechaEntregaDjPrueba = DateTime.Today.AddDays(1) });
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tipoViviendaRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoViviendaRepository>();
            _uowMock.Setup(u => u.TipoViviendas).Returns(tipoViviendaRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, modificada, false);

            Assert.True(result.Success);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_DeclaracionYaConfirmada_ReturnsConflict()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var declaracion = CrearDeclaracionBase();
            declaracion.Subestado = "CONFIRMADO_WEB";

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, CrearDtoBase(), false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_02", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_ModificacionSinInscriptoPruebaEnPlazo_ReturnsNotFound()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var declaracion = CrearDeclaracionBase();
            var modificada = CrearDtoBase();
            modificada.ObservacionesNfDj = "Cambiado";

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns((InscriptoPrueba)null);
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, modificada, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_04", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_ModificacionSinFechaLimite_ReturnsNotFound()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var declaracion = CrearDeclaracionBase();
            var modificada = CrearDtoBase();
            modificada.ObservacionesNfDj = "Cambiado";

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                IdPrueba = 50,
                CodigoPersona = codigoPersona,
                IdProducto = 10
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(new Prueba { IdPrueba = 50, IdTipoBeca = 1, FechaEntregaDjPrueba = null });
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, modificada, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_05", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_ModificacionFueraDePlazo_ReturnsBadRequest()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var declaracion = CrearDeclaracionBase();
            var modificada = CrearDtoBase();
            modificada.ObservacionesNfDj = "Cambiado";

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                IdPrueba = 50,
                CodigoPersona = codigoPersona,
                IdProducto = 10
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(new Prueba { IdPrueba = 50, IdTipoBeca = 1, FechaEntregaDjPrueba = DateTime.Today.AddDays(-1) });
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, modificada, false);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_06", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_CuandoNoExiste_CreaDeclaracion()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var modificada = CrearDtoBase();
            modificada.IdDeclaracionjuradaWeb = 0;
            modificada.ObservacionesNfDj = "Nueva";

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            DeclaracionJuradaWeb? declaracionAgregada = null;
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns((DeclaracionJuradaWeb)null);
            djRepo.Setup(r => r.Add(It.IsAny<DeclaracionJuradaWeb>()))
                .Callback<DeclaracionJuradaWeb>(d => declaracionAgregada = d);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_DECLARACION_JURADA_WEB))
                .Returns(999);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                IdPrueba = 50,
                CodigoPersona = codigoPersona,
                IdProducto = 10
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(new Prueba { IdPrueba = 50, IdTipoBeca = 1, FechaEntregaDjPrueba = DateTime.Today.AddDays(1) });
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var integranteRepo = new Mock<IIntegranteNfDjRepository>();
            _uowMock.Setup(u => u.IntegranteNfDjs).Returns(integranteRepo.Object);

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, modificada, false);

            Assert.True(result.Success);
            Assert.NotNull(declaracionAgregada);
            Assert.Equal(999, declaracionAgregada!.IdDeclaracionjuradaWeb);
            Assert.Equal(codigoPersona, declaracionAgregada.CodigoPersona);
            Assert.Equal(10, declaracionAgregada.IdProducto);
            Assert.Equal(1, declaracionAgregada.IdTipoDescuento);
            Assert.Equal("Nueva", declaracionAgregada.ObservacionesNfDj);
            Assert.Equal("GUARDADO_WEB_SIN_CONFIRMAR", declaracionAgregada.Subestado);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_CuandoNoExisteYConfirma_CreaYConfirma()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var modificada = CrearDtoBase();
            modificada.IdDeclaracionjuradaWeb = 0;

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            DeclaracionJuradaWeb? declaracionAgregada = null;
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns((DeclaracionJuradaWeb)null);
            djRepo.Setup(r => r.Add(It.IsAny<DeclaracionJuradaWeb>()))
                .Callback<DeclaracionJuradaWeb>(d => declaracionAgregada = d);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);
            _dbConnectionContextMock
                .Setup(c => c.NextId(DbConnectionContext.DbConnectionContextType.TO_DECLARACION_JURADA_WEB))
                .Returns(999);

            var inscriptoPrueba = new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                IdPrueba = 50,
                CodigoPersona = codigoPersona,
                IdProducto = 10
            };

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(inscriptoPrueba);
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(new Prueba { IdPrueba = 50, IdTipoBeca = 1, FechaEntregaDjPrueba = DateTime.Today.AddDays(1) });
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var integranteRepo = new Mock<IIntegranteNfDjRepository>();
            _uowMock.Setup(u => u.IntegranteNfDjs).Returns(integranteRepo.Object);

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, modificada, true);

            Assert.True(result.Success);
            Assert.NotNull(declaracionAgregada);
            Assert.Equal("CONFIRMADO_WEB", declaracionAgregada!.Subestado);
            Assert.NotNull(declaracionAgregada.ConfirmacionDecjurada);
            Assert.Equal("E", inscriptoPrueba.EstadoInscriptoPrueba);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void GuardarFormularioDeclaracionJuradaWeb_ConfirmarSinArchivos_ReturnsBadRequest()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var declaracion = CrearDeclaracionBase();
            var modificada = CrearDtoBase();
            modificada.IntegranteNfDjs[0].IngresoMensualNfDjs[0].ArchivoIngresoNfDj = null;
            modificada.IntegranteNfDjs[0].IngresoMensualNfDjs[0].NombreArchivoIngreso = null;
            modificada.IntegranteNfDjs[0].IngresoMensualNfDjs[0].ExtensionArchivoIngreso = null;

            declaracion.IntegranteNfDjs[0].IngresoMensualNfDjs[0].ArchivoIngresoNfDj = null;
            declaracion.IntegranteNfDjs[0].IngresoMensualNfDjs[0].NombreArchivoIngreso = null;
            declaracion.IntegranteNfDjs[0].IngresoMensualNfDjs[0].ExtensionArchivoIngreso = null;

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba
            {
                IdInscriptoPrueba = idInscriptoPrueba,
                IdPrueba = 50,
                CodigoPersona = codigoPersona,
                IdProducto = 10
            });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(new Prueba { IdPrueba = 50, IdTipoBeca = 1, FechaEntregaDjPrueba = DateTime.Today.AddDays(1) });
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tipoViviendaRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoViviendaRepository>();
            _uowMock.Setup(u => u.TipoViviendas).Returns(tipoViviendaRepo.Object);

            var result = _service.GuardarFormularioDeclaracionJuradaWeb(codigoPersona, modificada, true);

            Assert.False(result.Success);
            Assert.Equal("FDB_GDJ_14", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void ObtenerFormularioDeclaracionJuradaWebDetalle_MapeaRelacionesOpcionales()
        {
            const long codigoPersona = 1;
            const long idInscriptoPrueba = 100;

            var declaracion = CrearDeclaracionBase();
            declaracion.CodigoInstitucionBac = 100;
            declaracion.IdTipoVivienda = 7;
            declaracion.IntegranteNfDjs[0].TipoParentesco = new TipoParentesco { IdTipoParentesco = 2, DescripcionTp = "Padre" };
            declaracion.EgresoMensualNfDjs[0].TipoEgresoDj = new TipoEgresoDj { IdTipoEgresoDj = 3, NombreTipoEgresoDj = "Alquiler" };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetFormularioAdmisionesCompleto(codigoPersona, idInscriptoPrueba)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var inscriptoRepo = new Mock<BusinessLogic.IDevartRepositories.IInscriptoPruebaRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(idInscriptoPrueba)).Returns(new InscriptoPrueba { IdInscriptoPrueba = idInscriptoPrueba, IdPrueba = 50 });
            _uowMock.Setup(u => u.InscriptoPruebas).Returns(inscriptoRepo.Object);

            var pruebaRepo = new Mock<IPruebaRepository>();
            pruebaRepo.Setup(r => r.GetByKey(50)).Returns(new Prueba
            {
                IdPrueba = 50,
                TipoDescuento = new TipoDescuento { IdTipoDescuento = 1, NombreTipoDescuento = "Beca A" },
                Comienzo = new Comienzo { IdComienzo = 1, NombreComienzo = "2026-1" }
            });
            _uowMock.Setup(u => u.Pruebas).Returns(pruebaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(100)).Returns(new Empresa { CodigoEmpresa = 100, Nombre = "Instituto Ejemplo" });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tipoViviendaRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoViviendaRepository>();
            tipoViviendaRepo.Setup(r => r.GetByKey(7)).Returns(new TipoVivienda { IdTipoVivienda = 7, NombreTipoVivienda = "Casa" });
            _uowMock.Setup(u => u.TipoViviendas).Returns(tipoViviendaRepo.Object);

            var result = _service.ObtenerFormularioDeclaracionJuradaWebDetalle(codigoPersona, idInscriptoPrueba);

            Assert.True(result.Success);
            Assert.Equal("Instituto Ejemplo", result.Data!.NombreBachillerato);
            Assert.Equal("Casa", result.Data.ObjTipoVivienda!.NombreTipoVivienda);
            Assert.Equal("Padre", result.Data.IntegranteNfDjs[0].TipoParentesco!.DescripcionTp);
            Assert.Equal("Alquiler", result.Data.EgresoMensualNfDjs[0].TipoEgresoDj!.NombreTipoEgresoDj);
            Assert.NotNull(result.Data.ObjPrueba);
            Assert.NotNull(result.Data.ObjPrueba!.TipoDescuento);
            Assert.NotNull(result.Data.ObjPrueba!.Comienzo);
        }

        [Fact]
        public void SubirArchivoIngreso_HappyPath_GuardaArchivo()
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                IdIntegranteNfDj = 20,
                IntegranteNfDj = new IntegranteNfDj
                {
                    IdIntegranteNfDj = 20,
                    IdDeclaracionjuradaWeb = 30,
                    DeclaracionJuradaWeb = new DeclaracionJuradaWeb
                    {
                        IdDeclaracionjuradaWeb = 30,
                        CodigoPersona = 1
                    }
                }
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirArchivoIngreso(1, 10, pdfContent, "ingreso.pdf");

            Assert.True(result.Success);
            Assert.Equal("ingreso", ingreso.NombreArchivoIngreso);
            Assert.Equal(".pdf", ingreso.ExtensionArchivoIngreso);
            Assert.Equal(pdfContent, ingreso.ArchivoIngresoNfDj);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void SubirArchivoIngreso_CuandoNoExisteIntegrante_ReturnsNotFound()
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                IdIntegranteNfDj = 20,
                IntegranteNfDj = null
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirArchivoIngreso(1, 10, pdfContent, "ingreso.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAI_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirArchivoIngreso_CuandoNoExisteDeclaracion_ReturnsNotFound()
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                IdIntegranteNfDj = 20,
                IntegranteNfDj = new IntegranteNfDj
                {
                    IdIntegranteNfDj = 20,
                    IdDeclaracionjuradaWeb = 30,
                    DeclaracionJuradaWeb = null
                }
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirArchivoIngreso(1, 10, pdfContent, "ingreso.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAI_03", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirArchivoIngreso_CuandoNoPerteneceALaPersona_ReturnsForbidden()
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                IdIntegranteNfDj = 20,
                IntegranteNfDj = new IntegranteNfDj
                {
                    IdIntegranteNfDj = 20,
                    IdDeclaracionjuradaWeb = 30,
                    DeclaracionJuradaWeb = new DeclaracionJuradaWeb
                    {
                        IdDeclaracionjuradaWeb = 30,
                        CodigoPersona = 999
                    }
                }
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirArchivoIngreso(1, 10, pdfContent, "ingreso.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAI_04", result.ErrorCode);
            Assert.Equal(403, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void DescargarArchivoIngreso_HappyPath_ReturnsArchivoConNombreYContentType()
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                NombreArchivoIngreso = "ingreso",
                ExtensionArchivoIngreso = ".pdf",
                ArchivoIngresoNfDj = [1, 2, 3],
                IntegranteNfDj = new IntegranteNfDj
                {
                    IdIntegranteNfDj = 20,
                    DeclaracionJuradaWeb = new DeclaracionJuradaWeb
                    {
                        IdDeclaracionjuradaWeb = 30,
                        CodigoPersona = 1
                    }
                }
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.DescargarArchivoIngreso(1, 10);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("ingreso.pdf", result.Data.NombreArchivo);
            Assert.Equal("application/pdf", result.Data.ContentType);
            Assert.Equal([1, 2, 3], result.Data.Archivo);
        }

        [Theory]
        [InlineData(".jpg", "image/jpeg")]
        [InlineData(".jpeg", "image/jpeg")]
        [InlineData(".png", "image/png")]
        [InlineData(".doc", "application/msword")]
        [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData(".xls", "application/vnd.ms-excel")]
        [InlineData(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [InlineData(".ppt", "application/vnd.ms-powerpoint")]
        [InlineData(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
        [InlineData(".html", "text/html")]
        [InlineData(".bin", "application/octet-stream")]
        [InlineData("jpg", "image/jpeg")]
        public void DescargarArchivoIngreso_CubreContentTypes(string extension, string expectedContentType)
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                NombreArchivoIngreso = "ingreso",
                ExtensionArchivoIngreso = extension,
                ArchivoIngresoNfDj = [1, 2, 3],
                IntegranteNfDj = new IntegranteNfDj
                {
                    IdIntegranteNfDj = 20,
                    DeclaracionJuradaWeb = new DeclaracionJuradaWeb
                    {
                        IdDeclaracionjuradaWeb = 30,
                        CodigoPersona = 1
                    }
                }
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.DescargarArchivoIngreso(1, 10);

            Assert.True(result.Success);
            Assert.Equal(expectedContentType, result.Data!.ContentType);
        }

        [Fact]
        public void DescargarArchivoIngreso_CuandoNoExisteIngreso_ReturnsNotFound()
        {
            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns((IngresoMensualNfDj)null);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.DescargarArchivoIngreso(1, 10);

            Assert.False(result.Success);
            Assert.Equal("FDB_DAI_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoIngreso_CuandoNoExisteIngreso_ReturnsNotFound()
        {
            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns((IngresoMensualNfDj)null);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirArchivoIngreso(1, 10, pdfContent, "ingreso.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAI_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoIngreso_ArchivoInvalido_ReturnsFailed()
        {
            var result = _service.SubirArchivoIngreso(1, 10, [], "ingreso.pdf");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void DescargarArchivoIngreso_SinArchivo_ReturnsNotFound()
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                NombreArchivoIngreso = "ingreso",
                ExtensionArchivoIngreso = ".pdf",
                ArchivoIngresoNfDj = null,
                IntegranteNfDj = new IntegranteNfDj
                {
                    IdIntegranteNfDj = 20,
                    DeclaracionJuradaWeb = new DeclaracionJuradaWeb
                    {
                        IdDeclaracionjuradaWeb = 30,
                        CodigoPersona = 1
                    }
                }
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.DescargarArchivoIngreso(1, 10);

            Assert.False(result.Success);
            Assert.Equal("FDB_DAI_05", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void EliminarArchivoIngreso_HappyPath_LimpiaCamposYGuarda()
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                NombreArchivoIngreso = "ingreso",
                ExtensionArchivoIngreso = ".pdf",
                ArchivoIngresoNfDj = [1, 2, 3],
                IntegranteNfDj = new IntegranteNfDj
                {
                    IdIntegranteNfDj = 20,
                    DeclaracionJuradaWeb = new DeclaracionJuradaWeb
                    {
                        IdDeclaracionjuradaWeb = 30,
                        CodigoPersona = 1
                    }
                }
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.EliminarArchivoIngreso(1, 10);

            Assert.True(result.Success);
            Assert.Equal(string.Empty, ingreso.NombreArchivoIngreso);
            Assert.Equal(string.Empty, ingreso.ExtensionArchivoIngreso);
            Assert.Null(ingreso.ArchivoIngresoNfDj);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void EliminarArchivoIngreso_CuandoNoPerteneceALaPersona_ReturnsForbidden()
        {
            var ingreso = new IngresoMensualNfDj
            {
                IdIngresoMensualNfDj = 10,
                IntegranteNfDj = new IntegranteNfDj
                {
                    IdIntegranteNfDj = 20,
                    DeclaracionJuradaWeb = new DeclaracionJuradaWeb
                    {
                        IdDeclaracionjuradaWeb = 30,
                        CodigoPersona = 999
                    }
                }
            };

            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns(ingreso);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.EliminarArchivoIngreso(1, 10);

            Assert.False(result.Success);
            Assert.Equal("FDB_EAI_04", result.ErrorCode);
            Assert.Equal(403, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
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
        public void SubirArchivoEgreso_HappyPath_GuardaArchivo()
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
                CodigoPersona = 1
            });
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _service.SubirArchivoEgreso(1, 10, jpegContent, "egreso.jpg");

            Assert.True(result.Success);
            Assert.Equal("egreso", egreso.NombreArchivoEgreso);
            Assert.Equal(".jpg", egreso.ExtensionArchivoEgreso);
            Assert.Equal(jpegContent, egreso.ArchivoEgresoMensualNfDj);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void DescargarArchivoEgreso_HappyPath_ReturnsArchivoConNombreYContentType()
        {
            var egreso = new EgresoMensualNfDj
            {
                IdEgresoMensualNfDj = 10,
                IdDeclaracionjuradaWeb = 99,
                NombreArchivoEgreso = "egreso",
                ExtensionArchivoEgreso = ".jpg",
                ArchivoEgresoMensualNfDj = [1, 2, 3]
            };

            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            egresoRepo.Setup(r => r.GetByKey(10)).Returns(egreso);
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(99)).Returns(new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 99,
                CodigoPersona = 1
            });
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DescargarArchivoEgreso(1, 10);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("egreso.jpg", result.Data.NombreArchivo);
            Assert.Equal("image/jpeg", result.Data.ContentType);
            Assert.Equal([1, 2, 3], result.Data.Archivo);
        }

        [Fact]
        public void DescargarArchivoEgreso_CuandoNoExisteEgreso_ReturnsNotFound()
        {
            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            egresoRepo.Setup(r => r.GetByKey(10)).Returns((EgresoMensualNfDj)null);
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var result = _service.DescargarArchivoEgreso(1, 10);

            Assert.False(result.Success);
            Assert.Equal("FDB_DAE_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoEgreso_CuandoNoExisteEgreso_ReturnsNotFound()
        {
            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            egresoRepo.Setup(r => r.GetByKey(10)).Returns((EgresoMensualNfDj)null);
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var result = _service.SubirArchivoEgreso(1, 10, jpegContent, "egreso.jpg");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAE_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoEgreso_ArchivoInvalido_ReturnsFailed()
        {
            var result = _service.SubirArchivoEgreso(1, 10, [], "egreso.jpg");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void DescargarArchivoEgreso_SinArchivo_ReturnsNotFound()
        {
            var egreso = new EgresoMensualNfDj
            {
                IdEgresoMensualNfDj = 10,
                IdDeclaracionjuradaWeb = 99,
                NombreArchivoEgreso = "egreso",
                ExtensionArchivoEgreso = ".jpg",
                ArchivoEgresoMensualNfDj = null
            };

            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            egresoRepo.Setup(r => r.GetByKey(10)).Returns(egreso);
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(99)).Returns(new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 99,
                CodigoPersona = 1
            });
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DescargarArchivoEgreso(1, 10);

            Assert.False(result.Success);
            Assert.Equal("FDB_DAE_03", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void EliminarArchivoEgreso_HappyPath_LimpiaCamposYGuarda()
        {
            var egreso = new EgresoMensualNfDj
            {
                IdEgresoMensualNfDj = 10,
                IdDeclaracionjuradaWeb = 99,
                NombreArchivoEgreso = "egreso",
                ExtensionArchivoEgreso = ".jpg",
                ArchivoEgresoMensualNfDj = [1, 2, 3]
            };

            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            egresoRepo.Setup(r => r.GetByKey(10)).Returns(egreso);
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(99)).Returns(new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 99,
                CodigoPersona = 1
            });
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.EliminarArchivoEgreso(1, 10);

            Assert.True(result.Success);
            Assert.Equal(string.Empty, egreso.NombreArchivoEgreso);
            Assert.Equal(string.Empty, egreso.ExtensionArchivoEgreso);
            Assert.Null(egreso.ArchivoEgresoMensualNfDj);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void EliminarArchivoEgreso_CuandoNoPerteneceALaPersona_ReturnsForbidden()
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

            var result = _service.EliminarArchivoEgreso(1, 10);

            Assert.False(result.Success);
            Assert.Equal("FDB_EAE_02", result.ErrorCode);
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

        [Fact]
        public void SubirArchivoRevalidaDJ_HappyPath_GuardaArchivo()
        {
            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 1
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirArchivoRevalidaDJ(1, 55, pdfContent, "revalida.pdf");

            Assert.True(result.Success);
            Assert.Equal("revalida", declaracion.NombrePdfRevalidasDj);
            Assert.Equal(".pdf", declaracion.ExtensionPdfRevalidasDj);
            Assert.Equal(pdfContent, declaracion.PdfFormRevalidasDj);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void DescargarArchivoRevalidaDJ_HappyPath_ReturnsArchivoConNombreYContentType()
        {
            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 1,
                NombrePdfRevalidasDj = "revalida",
                ExtensionPdfRevalidasDj = ".pdf",
                PdfFormRevalidasDj = [1, 2, 3]
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DescargarArchivoRevalidaDJ(1, 55);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("revalida.pdf", result.Data.NombreArchivo);
            Assert.Equal("application/pdf", result.Data.ContentType);
            Assert.Equal([1, 2, 3], result.Data.Archivo);
        }

        [Fact]
        public void DescargarArchivoRevalidaDJ_CuandoNoExisteDeclaracion_ReturnsNotFound()
        {
            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DescargarArchivoRevalidaDJ(1, 55);

            Assert.False(result.Success);
            Assert.Equal("FDB_DAR_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoRevalidaDJ_CuandoNoExisteDeclaracion_ReturnsNotFound()
        {
            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.SubirArchivoRevalidaDJ(1, 55, pdfContent, "revalida.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAR_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoRevalidaDJ_ArchivoInvalido_ReturnsFailed()
        {
            var result = _service.SubirArchivoRevalidaDJ(1, 55, [], "revalida.pdf");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void DescargarArchivoRevalidaDJ_SinArchivo_ReturnsNotFound()
        {
            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 1,
                NombrePdfRevalidasDj = "revalida",
                ExtensionPdfRevalidasDj = ".pdf",
                PdfFormRevalidasDj = null
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DescargarArchivoRevalidaDJ(1, 55);

            Assert.False(result.Success);
            Assert.Equal("FDB_DAR_03", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void EliminarArchivoRevalidaDJ_HappyPath_LimpiaCamposYGuarda()
        {
            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 1,
                NombrePdfRevalidasDj = "revalida",
                ExtensionPdfRevalidasDj = ".pdf",
                PdfFormRevalidasDj = [1, 2, 3]
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.EliminarArchivoRevalidaDJ(1, 55);

            Assert.True(result.Success);
            Assert.Equal(string.Empty, declaracion.NombrePdfRevalidasDj);
            Assert.Equal(string.Empty, declaracion.ExtensionPdfRevalidasDj);
            Assert.Null(declaracion.PdfFormRevalidasDj);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void EliminarArchivoRevalidaDJ_CuandoNoPerteneceALaPersona_ReturnsForbidden()
        {
            var declaracion = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 999
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(declaracion);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.EliminarArchivoRevalidaDJ(1, 55);

            Assert.False(result.Success);
            Assert.Equal("FDB_EAR_02", result.ErrorCode);
            Assert.Equal(403, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        private static DeclaracionJuradaWeb CrearDeclaracionBase()
        {
            return new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 1,
                CodigoPersona = 1,
                IdProducto = 10,
                IdTipoDescuento = 1,
                IdInscriptoPrueba = 100,
                TienevehiculoNfDj = "NO",
                TienecasaveraneoNfDj = "NO",
                ObservacionesNfDj = "Original",

                Persona = new Persona
                {
                    CodigoPersona = 1,
                    PrimerNombre = "Test"
                },
                Producto = new Producto
                {
                    IdProducto = 10,
                    NombreProducto = "Prod1"
                },
                TipoDescuento = new TipoDescuento
                {
                    IdTipoDescuento = 1,
                    NombreTipoDescuento = "Beca A"
                },

                IntegranteNfDjs =
                [
                    new IntegranteNfDj
            {
                IdIntegranteNfDj = 200,
                IdDeclaracionjuradaWeb = 1,
                IdTipoParentesco = 2,
                NombreIntegranteNfDj = "Padre",
                IngresoMensualNfDjs =
                [
                    new IngresoMensualNfDj
                    {
                        IdIngresoMensualNfDj = 300,
                        IdIntegranteNfDj = 200,
                        NominalIngresoNfDj = 1500,
                        DescuentoslegalesIngresoNf = 200,
                        LiquidoIngresoNfDj = 1300,
                        IdIngresoFront = "ING-1",
                        ArchivoIngresoNfDj = [1, 2, 3],
                        NombreArchivoIngreso = "ingreso",
                        ExtensionArchivoIngreso = ".pdf"
                    }
                ]
            }
                ],
                EgresoMensualNfDjs =
                [
                    new EgresoMensualNfDj
            {
                IdEgresoMensualNfDj = 400,
                IdDeclaracionjuradaWeb = 1,
                IdTipoEgresoDj = 3,
                MontoEgresoMensualNfDj = 700,
                IdEgresoFront = "EGR-1"
            }
                ]
            };
        }

        private static DtoDeclaracionJuradaWebDevart CrearDtoBase()
        {
            return new DtoDeclaracionJuradaWebDevart
            {
                IdDeclaracionjuradaWeb = 1,
                CodigoPersona = 1,
                IdProducto = 10,
                IdTipoDescuento = 1,
                IdInscriptoPrueba = 100,
                TienevehiculoNfDj = "NO",
                TienecasaveraneoNfDj = "NO",
                ObservacionesNfDj = "Original",
                IntegranteNfDjs =
                [
                    new DtoIntegranteNfDjDevart
                    {
                        IdIntegranteNfDj = 200,
                        IdDeclaracionjuradaWeb = 1,
                        IdTipoParentesco = 2,
                        NombreIntegranteNfDj = "Padre",
                        IngresoMensualNfDjs =
                        [
                            new DtoIngresoMensualNfDjDevart
                            {
                                IdIngresoMensualNfDj = 300,
                                IdIntegranteNfDj = 200,
                                NominalIngresoNfDj = 1500,
                                DescuentoslegalesIngresoNf = 200,
                                LiquidoIngresoNfDj = 1300,
                                IdIngresoFront = "ING-1",
                                ArchivoIngresoNfDj = [1, 2, 3],
                                NombreArchivoIngreso = "ingreso",
                                ExtensionArchivoIngreso = ".pdf"
                            }
                        ]
                    }
                ],
                EgresoMensualNfDjs =
                [
                    new DtoEgresoMensualNfDjDevart
                    {
                        IdEgresoMensualNfDj = 400,
                        IdDeclaracionjuradaWeb = 1,
                        IdTipoEgresoDj = 3,
                        MontoEgresoMensualNfDj = 700,
                        IdEgresoFront = "EGR-1"
                    }
                ]
            };
        }

        #endregion UNIVERSIDADES
    }
}
