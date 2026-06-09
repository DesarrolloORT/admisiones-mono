using AppLogic.DTOs;
using AppLogic.DevartDTOs;
using AppLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Models;
using Xunit;
using WebApiAdmisiones.Security.Authentication;

namespace UnitTesting.Controllers
{
    public class FondoDeBecaControllerTests
    {
        /*
        [Fact]
        public void GetTiposParentesco_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();

            serviceMock.Setup(s => s.ObtenerTiposParentesco())
                .Returns(OperationResult<IEnumerable<DtoTipoParentescoDevart>>.Ok([], nameof(IFondoDeBecaServices.ObtenerTiposParentesco)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.GetTiposParentesco();

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void GetTiposEgreso_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();

            serviceMock.Setup(s => s.ObtenerTiposEgreso())
                .Returns(OperationResult<IEnumerable<DtoTipoEgresoDjDevart>>.Ok([], nameof(IFondoDeBecaServices.ObtenerTiposEgreso)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.GetTiposEgreso();

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void GetTiposVivienda_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();

            serviceMock.Setup(s => s.ObtenerTiposVivienda())
                .Returns(OperationResult<IEnumerable<DtoTipoViviendaDevart>>.Ok([], nameof(IFondoDeBecaServices.ObtenerTiposVivienda)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.GetTiposVivienda();

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void GetUniversidades_UsesDefaultCountry()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();

            serviceMock.Setup(s => s.ObtenerUniversidades(1))
                .Returns(OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok([], nameof(IFondoDeBecaServices.ObtenerUniversidades)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.GetUniversidades();

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
            serviceMock.Verify(s => s.ObtenerUniversidades(1), Times.Once);
        }

        [Fact]
        public void GetUniversidades_ForwardsProvidedCountry()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();

            serviceMock.Setup(s => s.ObtenerUniversidades(598))
                .Returns(OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok([], nameof(IFondoDeBecaServices.ObtenerUniversidades)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.GetUniversidades(598);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
            serviceMock.Verify(s => s.ObtenerUniversidades(598), Times.Once);
        }

        [Fact]
        public void GetFormulariosDeclaracionJuradaWeb_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.ObtenerFormulariosDeclaracionJuradaWeb(1))
                .Returns(OperationResult<IEnumerable<DtoDeclaracionJuradaAdmisiones>>.Ok([], nameof(IFondoDeBecaServices.ObtenerFormulariosDeclaracionJuradaWeb)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.GetFormulariosDeclaracionJuradaWeb();

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void GetFormularioDeclaracionJuradaWebDetalle_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.ObtenerFormularioDeclaracionJuradaWebDetalle(1, 100))
                .Returns(OperationResult<DtoDeclaracionJuradaWebDevart>.Ok(new DtoDeclaracionJuradaWebDevart { IdInscriptoPrueba = 100 }, nameof(IFondoDeBecaServices.ObtenerFormularioDeclaracionJuradaWebDetalle)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.GetFormularioDeclaracionJuradaWebDetalle(100);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void DescargarArchivoIngreso_ReturnsFile()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoIngreso(1, 10))
                .Returns(OperationResult<DtoArchivoDescarga>.Ok(
                    new DtoArchivoDescarga
                    {
                        Archivo = [1, 2, 3],
                        NombreArchivo = "ingreso.pdf",
                        ContentType = "application/pdf"
                    },
                    nameof(IFondoDeBecaServices.DescargarArchivoIngreso)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.DescargarArchivoIngreso(10);

            var fileResult = Assert.IsType<FileContentResult>(response);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("ingreso.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public void EliminarArchivoIngreso_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.EliminarArchivoIngreso(1, 10))
                .Returns(OperationResult<bool>.Ok(true, nameof(IFondoDeBecaServices.EliminarArchivoIngreso)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.EliminarArchivoIngreso(10);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void DescargarArchivoIngreso_WhenServiceFails_ReturnsObjectResult()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoIngreso(1, 10))
                .Returns(OperationResult<DtoArchivoDescarga>.IsFailed("ERR", nameof(IFondoDeBecaServices.DescargarArchivoIngreso), "No encontrado", 404));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.DescargarArchivoIngreso(10);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, objectResult.StatusCode);
        }

        [Fact]
        public void DescargarArchivoEgreso_ReturnsFile()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoEgreso(1, 10))
                .Returns(OperationResult<DtoArchivoDescarga>.Ok(
                    new DtoArchivoDescarga
                    {
                        Archivo = [1, 2, 3],
                        NombreArchivo = "egreso.jpg",
                        ContentType = "image/jpeg"
                    },
                    nameof(IFondoDeBecaServices.DescargarArchivoEgreso)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.DescargarArchivoEgreso(10);

            var fileResult = Assert.IsType<FileContentResult>(response);
            Assert.Equal("image/jpeg", fileResult.ContentType);
            Assert.Equal("egreso.jpg", fileResult.FileDownloadName);
        }

        [Fact]
        public void DescargarArchivoEgreso_WhenServiceFails_ReturnsObjectResult()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoEgreso(1, 10))
                .Returns(OperationResult<DtoArchivoDescarga>.IsFailed("ERR", nameof(IFondoDeBecaServices.DescargarArchivoEgreso), "No encontrado", 404));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.DescargarArchivoEgreso(10);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, objectResult.StatusCode);
        }

        [Fact]
        public void EliminarArchivoEgreso_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.EliminarArchivoEgreso(1, 10))
                .Returns(OperationResult<bool>.Ok(true, nameof(IFondoDeBecaServices.EliminarArchivoEgreso)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.EliminarArchivoEgreso(10);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void SubirArchivoIngreso_WithNullPayload_UsesEmptyFallback()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.SubirArchivoIngreso(1, 10, It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns(OperationResult<bool>.Ok(true, nameof(IFondoDeBecaServices.SubirArchivoIngreso)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.SubirArchivoIngreso(new UploadArchivoIngresoRequest { IdIngresoMensualNF = 10 });

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
            serviceMock.Verify(s => s.SubirArchivoIngreso(1, 10, It.Is<byte[]>(b => b.Length == 0), string.Empty), Times.Once);
        }

        [Fact]
        public void SubirArchivoEgreso_WithNullPayload_UsesEmptyFallback()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.SubirArchivoEgreso(1, 10, It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns(OperationResult<bool>.Ok(true, nameof(IFondoDeBecaServices.SubirArchivoEgreso)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.SubirArchivoEgreso(new UploadArchivoEgresoRequest { IdEgresoMensualNF = 10 });

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
            serviceMock.Verify(s => s.SubirArchivoEgreso(1, 10, It.Is<byte[]>(b => b.Length == 0), string.Empty), Times.Once);
        }

        [Fact]
        public void DescargarArchivoRevalidaDJ_ReturnsFile()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoRevalidaDJ(1, 55))
                .Returns(OperationResult<DtoArchivoDescarga>.Ok(
                    new DtoArchivoDescarga
                    {
                        Archivo = [1, 2, 3],
                        NombreArchivo = "revalida.pdf",
                        ContentType = "application/pdf"
                    },
                    nameof(IFondoDeBecaServices.DescargarArchivoRevalidaDJ)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.DescargarArchivoRevalidaDJ(55);

            var fileResult = Assert.IsType<FileContentResult>(response);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("revalida.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public void DescargarArchivoRevalidaDJ_WhenServiceFails_ReturnsObjectResult()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoRevalidaDJ(1, 55))
                .Returns(OperationResult<DtoArchivoDescarga>.IsFailed("ERR", nameof(IFondoDeBecaServices.DescargarArchivoRevalidaDJ), "No encontrado", 404));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.DescargarArchivoRevalidaDJ(55);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, objectResult.StatusCode);
        }

        [Fact]
        public void EliminarArchivoRevalidaDJ_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.EliminarArchivoRevalidaDJ(1, 55))
                .Returns(OperationResult<bool>.Ok(true, nameof(IFondoDeBecaServices.EliminarArchivoRevalidaDJ)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.EliminarArchivoRevalidaDJ(55);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void SubirArchivoRevalidaDj_WithNullPayload_UsesEmptyFallback()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.SubirArchivoRevalidaDJ(1, 55, It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns(OperationResult<bool>.Ok(true, nameof(IFondoDeBecaServices.SubirArchivoRevalidaDJ)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.SubirArchivoRevalidaDj(new UploadArchivoRevalidaDjRequest { IdDeclaracionJuradaWeb = 55 });

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
            serviceMock.Verify(s => s.SubirArchivoRevalidaDJ(1, 55, It.Is<byte[]>(b => b.Length == 0), string.Empty), Times.Once);
        }

        [Fact]
        public void PostFormularioDeclaracionJuradaWeb_ReturnsOk()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            var request = new DtoDeclaracionJuradaWebDevart
            {
                IdDeclaracionjuradaWeb = 1,
                CodigoPersona = 1,
                IdProducto = 10,
                IdTipoDescuento = 1,
                IdInscriptoPrueba = 100,
                TienevehiculoNfDj = "NO",
                TienecasaveraneoNfDj = "NO"
            };

            serviceMock.Setup(s => s.GuardarFormularioDeclaracionJuradaWeb(1, request, true))
                .Returns(OperationResult<bool>.Ok(true, nameof(IFondoDeBecaServices.GuardarFormularioDeclaracionJuradaWeb)));

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.PostFormularioDeclaracionJuradaWeb(request, true);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public void PostFormularioDeclaracionJuradaWeb_NullRequest_ReturnsBadRequest()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            var controller = new FondoDeBecaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.PostFormularioDeclaracionJuradaWeb(null!, true);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(400, objectResult.StatusCode);
        }
        */
    }
}
