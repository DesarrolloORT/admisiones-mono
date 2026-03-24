using AppLogic.DTOs;
using AppLogic.DevartDTOs;
using AppLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Models;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Controllers
{
    public class FondoDeBecaControllerTests
    {
        [Fact]
        public void DescargarArchivoIngreso_ReturnsFile()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoIngreso(1, 10))
                .Returns(OperationResult<ArchivoDescargaDto>.Ok(
                    new ArchivoDescargaDto
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
        public void DescargarArchivoEgreso_ReturnsFile()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoEgreso(1, 10))
                .Returns(OperationResult<ArchivoDescargaDto>.Ok(
                    new ArchivoDescargaDto
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
        public void DescargarArchivoRevalidaDJ_ReturnsFile()
        {
            var serviceMock = new Mock<IFondoDeBecaServices>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<FondoDeBecaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);

            serviceMock.Setup(s => s.DescargarArchivoRevalidaDJ(1, 55))
                .Returns(OperationResult<ArchivoDescargaDto>.Ok(
                    new ArchivoDescargaDto
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
    }
}
