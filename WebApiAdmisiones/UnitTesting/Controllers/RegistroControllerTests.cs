using System;
using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Controllers
{
    public class RegistroControllerTests
    {
        [Fact]
        public async Task EvaluarDocumento_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var request = new RegistroEvaluarDocumentoRequest { TipoDocumento = "CI", Documento = "1234567-2" };

            serviceMock.Setup(s => s.EvaluarDocumentoAsync(request))
                .ReturnsAsync(OperationResult<RegistroEvaluacionResponse>.Ok(
                    new RegistroEvaluacionResponse { RequiereAltaPersona = true },
                    nameof(IRegistroService.EvaluarDocumentoAsync)));

            var response = await controller.EvaluarDocumento(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ConfirmarPersonaExistente_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var request = new RegistroConfirmarPersonaExistenteRequest { TipoDocumento = "CI", Documento = "1234567-2" };

            serviceMock.Setup(s => s.ConfirmarPersonaExistenteAsync(request))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(IRegistroService.ConfirmarPersonaExistenteAsync),
                    "Registro realizado correctamente."));

            var response = await controller.ConfirmarPersonaExistente(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ConfirmarNuevaPersona_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var request = new RegistroConfirmarNuevaPersonaRequest { TipoDocumento = "CI", Documento = "1234567-2" };

            serviceMock.Setup(s => s.ConfirmarNuevaPersonaAsync(request))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(IRegistroService.ConfirmarNuevaPersonaAsync),
                    "Registro realizado correctamente."));

            var response = await controller.ConfirmarNuevaPersona(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ConfirmarSolicitudAlta_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var request = new RegistroConfirmarSolicitudAltaRequest { TipoDocumento = "PS", Documento = "A123" };

            serviceMock.Setup(s => s.ConfirmarSolicitudAltaAsync(request))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(IRegistroService.ConfirmarSolicitudAltaAsync),
                    "La solicitud de alta quedó registrada."));

            var response = await controller.ConfirmarSolicitudAlta(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task VerificarIdentidad_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var request = new RegistroVerificarIdentidadRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                Mail = "ana@example.com",
                VerificacionMail = "ana@example.com"
            };

            serviceMock.Setup(s => s.VerificarIdentidadAsync(request))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(IRegistroService.VerificarIdentidadAsync),
                    "Verificacion realizada correctamente."));

            var response = await controller.VerificarIdentidad(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Theory]
        [InlineData(nameof(RegistroController.EvaluarDocumento))]
        [InlineData(nameof(RegistroController.VerificarIdentidad))]
        [InlineData(nameof(RegistroController.ConfirmarPersonaExistente))]
        [InlineData(nameof(RegistroController.ConfirmarNuevaPersona))]
        [InlineData(nameof(RegistroController.ConfirmarSolicitudAlta))]
        public void PublicEndpoints_HaveAllowAnonymous(string methodName)
        {
            var method = typeof(RegistroController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true),
                attribute => attribute is AllowAnonymousAttribute);
        }

        [Theory]
        [InlineData(nameof(RegistroController.ConfirmarPersonaExistente))]
        [InlineData(nameof(RegistroController.ConfirmarNuevaPersona))]
        [InlineData(nameof(RegistroController.ConfirmarSolicitudAlta))]
        public void ConfirmarEndpoints_HaveRequireCaptcha(string methodName)
        {
            var method = typeof(RegistroController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(RequireCaptchaAttribute), inherit: true),
                attribute => attribute is RequireCaptchaAttribute);
        }
    }
}
