using AppLogic.Autenticacion.Requests;
using AppLogic.Becas.Responses;
using AppLogic.Personas.Requests;
using AppLogic.Personas.Responses;
using AppLogic.DevartDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using Xunit;
using WebApiAdmisiones.Security.Authentication;
using AppLogic.Personas.Interfaces;
using WebApiAdmisiones.Models;

namespace UnitTesting.Controllers
{
    public class PersonaControllerTests
    {
        private readonly Mock<IPersonaService> _personaServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly PersonaController _controller;

        public PersonaControllerTests()
        {
            _personaServiceMock = new Mock<IPersonaService>();
            _currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<PersonaController>>();

            _controller = new PersonaController(
                _personaServiceMock.Object,
                loggerMock.Object,
                _currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public void ObtenerDatosPersona_UsesAuthenticatedUserAndReturnsOk()
        {
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ObtenerDatosPersona(123))
                .Returns(OperationResult<DtoDatosPersona>.Ok(new DtoDatosPersona(), nameof(IPersonaService.ObtenerDatosPersona)));

            var response = _controller.ObtenerDatosPersona();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _personaServiceMock.Verify(s => s.ObtenerDatosPersona(123), Times.Once);
        }

        [Fact]
        public void ActualizarDatosPersona_UsesAuthenticatedUserAndReturnsOk()
        {
            var request = new DtoActualizarDatosPersonaRequest
            {
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "18 de julio 1234",
                Telefono1 = "24001234",
                Mail = "ana@test.com",
                VerificacionMail = "ana@test.com"
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ActualizarDatosPersona(123, request))
                .Returns(OperationResult<bool>.Ok(true, nameof(IPersonaService.ActualizarDatosPersona)));

            var response = _controller.ActualizarDatosPersona(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _personaServiceMock.Verify(s => s.ActualizarDatosPersona(123, request), Times.Once);
        }

        [Fact]
        public void ObtenerMisInscripciones_UsesAuthenticatedUserAndReturnsOk()
        {
            var inscripciones = new List<DtoVdInscripcionesFresco1y2Devart>
            {
                new() { IdProducto = 10 }
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ObtenerMisInscripciones(123))
                .Returns(OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>.Ok(
                    inscripciones,
                    nameof(IPersonaService.ObtenerMisInscripciones)));

            var response = _controller.ObtenerMisInscripciones();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _personaServiceMock.Verify(s => s.ObtenerMisInscripciones(123), Times.Once);
        }

        [Fact]
        public void ObtenerMisBecas_ReturnsMockBecas()
        {
            var response = _controller.ObtenerMisBecas();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<IEnumerable<DtoBecaPersona>>>(okResult.Value);
            Assert.True(operationResult.Success);
            var becas = Assert.IsAssignableFrom<IEnumerable<DtoBecaPersona>>(operationResult.Data);
            Assert.Equal(2, becas.Count());
        }

        [Fact]
        public async Task CambiarPassword_UsesAuthenticatedUserAndReturnsOk()
        {
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };
            var result = OperationResult<object>.Ok(
                "Se actualizó tu contraseña",
                nameof(IPersonaService.CambiarPasswordAsync));

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.CambiarPasswordAsync(123, request))
                .ReturnsAsync(result);

            var response = await _controller.CambiarPassword(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _personaServiceMock.Verify(s => s.CambiarPasswordAsync(123, request), Times.Once);
        }

        [Fact]
        public async Task CambiarPassword_WithoutAuthenticatedUser_ThrowsUnauthorizedAccessException()
        {
            // GetUserId() lanza cuando el token no trae el claim de usuario; el middleware
            // global (ExceptionHandlingMiddleware) es quien la convierte en 401 (CTL-02).
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };
            _currentUserMock.Setup(c => c.GetUserId()).Throws<UnauthorizedAccessException>();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.CambiarPassword(request));

            _personaServiceMock.Verify(
                s => s.CambiarPasswordAsync(It.IsAny<long>(), It.IsAny<DtoCambiarPasswordRequest>()),
                Times.Never);
        }

        [Fact]
        public void ObtenerFotoPersona_WhenServiceSucceeds_ReturnsFileContentResult()
        {
            var bytes = new byte[] { 1, 2, 3 };
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ObtenerFotoPersona(123))
                .Returns(OperationResult<byte[]>.Ok(bytes, nameof(IPersonaService.ObtenerFotoPersona)));

            var response = _controller.ObtenerFotoPersona();

            var fileResult = Assert.IsType<FileContentResult>(response);
            Assert.Equal("image/jpeg", fileResult.ContentType);
            Assert.Equal(bytes, fileResult.FileContents);
        }

        [Fact]
        public void ObtenerFotoPersona_WhenServiceFails_ReturnsOperationResultBody()
        {
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ObtenerFotoPersona(123))
                .Returns(OperationResult<byte[]>.IsFailed(
                    "GEN_FA_01",
                    nameof(IPersonaService.ObtenerFotoPersona),
                    "Foto no encontrada.",
                    404));

            var response = _controller.ObtenerFotoPersona();

            var notFoundResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, notFoundResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<byte[]>>(notFoundResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("GEN_FA_01", operationResult.ErrorCode);
        }

        [Fact]
        public void ObtenerFotoPersona_WhenServiceSucceedsWithNullData_ReturnsOperationResultBody()
        {
            // ⚠️ CONTRATO: antes este caso devolvía 404 sin body (NotFoundResult); ahora cumple
            // el ProducesResponseType(typeof(OperationResult<byte[]>), 404) declarado en el endpoint.
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ObtenerFotoPersona(123))
                .Returns(OperationResult<byte[]>.Ok(null, nameof(IPersonaService.ObtenerFotoPersona)));

            var response = _controller.ObtenerFotoPersona();

            var notFoundResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, notFoundResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<byte[]>>(notFoundResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("GEN_FA_03", operationResult.ErrorCode);
        }

        [Fact]
        public void ObtenerDocumentoPersona_UsesAuthenticatedUserAndReturnsOk()
        {
            var fechaVencimiento = DateTime.Today.AddYears(1);
            var documento = new DtoDocumentoPersonaResponse
            {
                Frente = new DtoDocumentoPersonaArchivo
                {
                    NombreArchivo = "123_1.pdf",
                    Archivo = new byte[] { 1, 2, 3 }
                },
                FechaVencimiento = fechaVencimiento
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ObtenerDocumentoPersona(123))
                .Returns(OperationResult<DtoDocumentoPersonaResponse>.Ok(
                    documento,
                    nameof(IPersonaService.ObtenerDocumentoPersona)));

            var response = _controller.ObtenerDocumentoPersona();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<DtoDocumentoPersonaResponse>>(okResult.Value);
            Assert.Equal(fechaVencimiento, operationResult.Data!.FechaVencimiento);
            _personaServiceMock.Verify(s => s.ObtenerDocumentoPersona(123), Times.Once);
        }

        [Fact]
        public void SubirDocumentoPersona_MapsFrenteDorsoAndFecha()
        {
            var fecha = DateTime.Today.AddYears(1);
            var request = new UploadDocumentoPersonaRequest
            {
                Fecha = fecha,
                Frente = new ArchivoPayload
                {
                    NombreArchivo = "frente.pdf",
                    Archivo = new byte[] { 1, 2, 3 }
                },
                Dorso = new ArchivoPayload
                {
                    NombreArchivo = "dorso.pdf",
                    Archivo = new byte[] { 4, 5, 6 }
                }
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.SubirDocumentoPersona(
                    123,
                    fecha,
                    It.Is<DtoDocumentoPersonaArchivo>(d =>
                        d.NombreArchivo == "frente.pdf" &&
                        d.Archivo!.SequenceEqual(new byte[] { 1, 2, 3 })),
                    It.Is<DtoDocumentoPersonaArchivo>(d =>
                        d.NombreArchivo == "dorso.pdf" &&
                        d.Archivo!.SequenceEqual(new byte[] { 4, 5, 6 }))))
                .Returns(OperationResult<bool>.Ok(true, nameof(IPersonaService.SubirDocumentoPersona)));

            var response = _controller.SubirDocumentoPersona(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _personaServiceMock.VerifyAll();
        }
    }
}
