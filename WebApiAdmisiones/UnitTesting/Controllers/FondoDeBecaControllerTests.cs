using AppLogic.DTOs;
using AppLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
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
    }
}
