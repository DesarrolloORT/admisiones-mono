using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using BusinessLogic.Entities;
using AppLogic.DevartDTOs;
using Utilities;
using BusinessLogic.IDevartRepositories;
using AppLogic.Scholarships.Services;

namespace UnitTesting.AppLogic.Services
{
    public class ScholarshipFundServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly ScholarshipFundService _service;

        public ScholarshipFundServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new ScholarshipFundService(_uowFactoryMock.Object);
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
            var result = _service.GetKinshipTypes();

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
            var result = _service.GetKinshipTypes();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public void ObtenerTiposEgreso_ReturnsActivosOrderedByOrden()
        {
            // Arrange: la BD ya devuelve solo activos y ordenados por Orden (filtro/orden en SQL).
            var tiposEgreso = new List<TipoEgresoDj>
            {
                new TipoEgresoDj { IdTipoEgresoDj = 2, NombreTipoEgresoDj = "Alquiler", Activo = "SI", Orden = 1 },
                new TipoEgresoDj { IdTipoEgresoDj = 1, NombreTipoEgresoDj = "Servicios", Activo = "SI", Orden = 2 }
            };

            var tipoEgresoRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoEgresoDjRepository>();
            tipoEgresoRepo.Setup(r => r.GetActivosOrdenados()).Returns(tiposEgreso);
            _uowMock.Setup(u => u.TipoEgresoDjs).Returns(tipoEgresoRepo.Object);

            // Act
            var result = _service.GetExpenseTypes();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            var list = result.Data.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal("Alquiler", list[0].NombreTipoEgresoDj); // Orden 1 primero
            Assert.Equal("Servicios", list[1].NombreTipoEgresoDj); // Orden 2 segundo
        }

        [Fact]
        public void ObtenerTiposEgreso_NoActivos_ReturnsEmpty()
        {
            // Arrange: sin activos, la BD ya devuelve la lista vacía.
            var tipoEgresoRepo = new Mock<BusinessLogic.IDevartRepositories.ITipoEgresoDjRepository>();
            tipoEgresoRepo.Setup(r => r.GetActivosOrdenados()).Returns(new List<TipoEgresoDj>());
            _uowMock.Setup(u => u.TipoEgresoDjs).Returns(tipoEgresoRepo.Object);

            // Act
            var result = _service.GetExpenseTypes();

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
            var result = _service.GetHousingTypes();

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
            long countryId = 1;
            var country = new Pais { CodigoPais = 1, Nombre = "Uruguay" };
            var universidades = new List<Empresa>
            {
                new Empresa { CodigoEmpresa = 1, Nombre = "Universidad A", CodigoPais = 1 },
                new Empresa { CodigoEmpresa = 2, Nombre = "Universidad B", CodigoPais = 1 }
            };

            var paisRepo = new Mock<IPaisRepository>();
            paisRepo.Setup(r => r.GetPaisConEstadosYCiudades(countryId)).Returns(country);
            _uowMock.Setup(u => u.Paises).Returns(paisRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetUniversidades(countryId)).Returns(universidades);
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            // Act
            var result = _service.GetUniversities(countryId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count());
            paisRepo.Verify(r => r.GetPaisConEstadosYCiudades(countryId), Times.Once);
            empresaRepo.Verify(r => r.GetUniversidades(countryId), Times.Once);
        }

        [Fact]
        public void ObtenerUniversidades_InvalidCountry_ReturnsFailed()
        {
            // Arrange
            long countryId = 999;
            var paisRepo = new Mock<IPaisRepository>();
            paisRepo.Setup(r => r.GetPaisConEstadosYCiudades(countryId)).Returns((Pais)null);
            _uowMock.Setup(u => u.Paises).Returns(paisRepo.Object);

            // Act
            var result = _service.GetUniversities(countryId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FDB_UV_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            paisRepo.Verify(r => r.GetPaisConEstadosYCiudades(countryId), Times.Once);
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

            var result = _service.UploadIncomeFile(1, 10, pdfContent, "ingreso.pdf");

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

            var result = _service.UploadIncomeFile(1, 10, pdfContent, "ingreso.pdf");

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

            var result = _service.UploadIncomeFile(1, 10, pdfContent, "ingreso.pdf");

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

            var result = _service.UploadIncomeFile(1, 10, pdfContent, "ingreso.pdf");

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

            var result = _service.DownloadIncomeFile(1, 10);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("ingreso.pdf", result.Data.FileName);
            Assert.Equal("application/pdf", result.Data.ContentType);
            Assert.Equal([1, 2, 3], result.Data.Content);
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

            var result = _service.DownloadIncomeFile(1, 10);

            Assert.True(result.Success);
            Assert.Equal(expectedContentType, result.Data!.ContentType);
        }

        [Fact]
        public void DescargarArchivoIngreso_CuandoNoExisteIngreso_ReturnsNotFound()
        {
            var ingresoRepo = new Mock<IIngresoMensualNfDjRepository>();
            ingresoRepo.Setup(r => r.GetWithIntegranteYDeclaracion(10)).Returns((IngresoMensualNfDj)null);
            _uowMock.Setup(u => u.IngresoMensualNfDjs).Returns(ingresoRepo.Object);

            var result = _service.DownloadIncomeFile(1, 10);

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

            var result = _service.UploadIncomeFile(1, 10, pdfContent, "ingreso.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAI_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoIngreso_ArchivoInvalido_ReturnsFailed()
        {
            var result = _service.UploadIncomeFile(1, 10, [], "ingreso.pdf");

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

            var result = _service.DownloadIncomeFile(1, 10);

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

            var result = _service.DeleteIncomeFile(1, 10);

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

            var result = _service.DeleteIncomeFile(1, 10);

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

            var result = _service.UploadExpenseFile(1, 10, jpegContent, "egreso.jpg");

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

            var result = _service.UploadExpenseFile(1, 10, jpegContent, "egreso.jpg");

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

            var result = _service.DownloadExpenseFile(1, 10);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("egreso.jpg", result.Data.FileName);
            Assert.Equal("image/jpeg", result.Data.ContentType);
            Assert.Equal([1, 2, 3], result.Data.Content);
        }

        [Fact]
        public void DescargarArchivoEgreso_CuandoNoExisteEgreso_ReturnsNotFound()
        {
            var egresoRepo = new Mock<IEgresoMensualNfDjRepository>();
            egresoRepo.Setup(r => r.GetByKey(10)).Returns((EgresoMensualNfDj)null);
            _uowMock.Setup(u => u.EgresoMensualNfDjs).Returns(egresoRepo.Object);

            var result = _service.DownloadExpenseFile(1, 10);

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

            var result = _service.UploadExpenseFile(1, 10, jpegContent, "egreso.jpg");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAE_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoEgreso_ArchivoInvalido_ReturnsFailed()
        {
            var result = _service.UploadExpenseFile(1, 10, [], "egreso.jpg");

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

            var result = _service.DownloadExpenseFile(1, 10);

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

            var result = _service.DeleteExpenseFile(1, 10);

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

            var result = _service.DeleteExpenseFile(1, 10);

            Assert.False(result.Success);
            Assert.Equal("FDB_EAE_02", result.ErrorCode);
            Assert.Equal(403, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirArchivoRevalidaDJ_CuandoNoPerteneceALaPersona_ReturnsForbidden()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 999
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(affidavit);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.UploadRevalidationFile(1, 55, pdfContent, "revalida.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAR_02", result.ErrorCode);
            Assert.Equal(403, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void SubirArchivoRevalidaDJ_HappyPath_GuardaArchivo()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 1
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(affidavit);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            var result = _service.UploadRevalidationFile(1, 55, pdfContent, "revalida.pdf");

            Assert.True(result.Success);
            Assert.Equal("revalida", affidavit.NombrePdfRevalidasDj);
            Assert.Equal(".pdf", affidavit.ExtensionPdfRevalidasDj);
            Assert.Equal(pdfContent, affidavit.PdfFormRevalidasDj);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void DescargarArchivoRevalidaDJ_HappyPath_ReturnsArchivoConNombreYContentType()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 1,
                NombrePdfRevalidasDj = "revalida",
                ExtensionPdfRevalidasDj = ".pdf",
                PdfFormRevalidasDj = [1, 2, 3]
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(affidavit);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DownloadRevalidationFile(1, 55);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("revalida.pdf", result.Data.FileName);
            Assert.Equal("application/pdf", result.Data.ContentType);
            Assert.Equal([1, 2, 3], result.Data.Content);
        }

        [Fact]
        public void DescargarArchivoRevalidaDJ_CuandoNoExisteDeclaracion_ReturnsNotFound()
        {
            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns((DeclaracionJuradaWeb)null);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DownloadRevalidationFile(1, 55);

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

            var result = _service.UploadRevalidationFile(1, 55, pdfContent, "revalida.pdf");

            Assert.False(result.Success);
            Assert.Equal("FDB_SAR_01", result.ErrorCode);
        }

        [Fact]
        public void SubirArchivoRevalidaDJ_ArchivoInvalido_ReturnsFailed()
        {
            var result = _service.UploadRevalidationFile(1, 55, [], "revalida.pdf");

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void DescargarArchivoRevalidaDJ_SinArchivo_ReturnsNotFound()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 1,
                NombrePdfRevalidasDj = "revalida",
                ExtensionPdfRevalidasDj = ".pdf",
                PdfFormRevalidasDj = null
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(affidavit);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DownloadRevalidationFile(1, 55);

            Assert.False(result.Success);
            Assert.Equal("FDB_DAR_03", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void EliminarArchivoRevalidaDJ_HappyPath_LimpiaCamposYGuarda()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 1,
                NombrePdfRevalidasDj = "revalida",
                ExtensionPdfRevalidasDj = ".pdf",
                PdfFormRevalidasDj = [1, 2, 3]
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(affidavit);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DeleteRevalidationFile(1, 55);

            Assert.True(result.Success);
            Assert.Equal(string.Empty, affidavit.NombrePdfRevalidasDj);
            Assert.Equal(string.Empty, affidavit.ExtensionPdfRevalidasDj);
            Assert.Null(affidavit.PdfFormRevalidasDj);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void EliminarArchivoRevalidaDJ_CuandoNoPerteneceALaPersona_ReturnsForbidden()
        {
            var affidavit = new DeclaracionJuradaWeb
            {
                IdDeclaracionjuradaWeb = 55,
                CodigoPersona = 999
            };

            var djRepo = new Mock<IDeclaracionJuradaWebRepository>();
            djRepo.Setup(r => r.GetByKey(55)).Returns(affidavit);
            _uowMock.Setup(u => u.DeclaracionJuradaWebs).Returns(djRepo.Object);

            var result = _service.DeleteRevalidationFile(1, 55);

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
