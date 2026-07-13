using AppLogic.Registro.Requests;
using AppLogic.Registro.Responses;
using AppLogic.Registro.Dtos;
using System;
using System.Collections.Generic;
using AzureService.DTOs;
using AzureService.Interfaces;
using AppLogic.DevartDTOs;
using AppLogic.Registro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Models;
using Xunit;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Captcha;
using AppLogic.Registro.Interfaces;

namespace UnitTesting.Controllers
{
    public class RegistroControllerTests
    {
        [Theory]
        [InlineData(nameof(RegistroController.EvaluarDocumento))]
        [InlineData(nameof(RegistroController.VerificarIdentidad))]
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

        [Theory]
        [InlineData(nameof(RegistroController.EvaluarDocumento), CaptchaActions.EvaluarDocumento)]
        [InlineData(nameof(RegistroController.VerificarIdentidad), CaptchaActions.VerificarIdentidad)]
        [InlineData(nameof(RegistroController.AnalizarAdjunto), CaptchaActions.AnalizarAdjunto)]
        [InlineData(nameof(RegistroController.ConfirmarNuevaPersona), CaptchaActions.ConfirmarNuevaPersona)]
        [InlineData(nameof(RegistroController.ConfirmarSolicitudAlta), CaptchaActions.ConfirmarSolicitudAlta)]
        public void PublicCaptchaEndpoints_HaveExpectedCaptchaConfiguration(string methodName, string expectedAction)
        {
            var method = typeof(RegistroController).GetMethod(methodName);

            Assert.NotNull(method);
            var attribute = Assert.Single(
                method!.GetCustomAttributes(typeof(RequireCaptchaAttribute), inherit: true)
                    .OfType<RequireCaptchaAttribute>());
            Assert.Equal(CaptchaValidationMode.ScoreOnly, attribute.Arguments[0]);
            Assert.Equal(expectedAction, attribute.Arguments[1]);
        }

        [Fact]
        public async Task EvaluarDocumento_DelegatesToServiceAndReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var controller = CrearController(serviceMock.Object);
            var request = new DtoRegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2"
            };
            serviceMock
                .Setup(s => s.EvaluarDocumentoAsync(request))
                .ReturnsAsync(OperationResult<DtoRegistroEvaluacionResponse>.Ok(
                    new DtoRegistroEvaluacionResponse { RequiereAltaPersona = true },
                    nameof(IRegistroService.EvaluarDocumentoAsync)));

            var response = await controller.EvaluarDocumento(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.EvaluarDocumentoAsync(request), Times.Once);
        }

        [Fact]
        public async Task EvaluarDocumento_WithUsuarioExistente_DoesNotCreateFlowId()
        {
            var serviceMock = new Mock<IRegistroService>();
            var flowServiceMock = new Mock<IRegistroFlowService>();
            var controller = CrearController(serviceMock.Object, registroFlowService: flowServiceMock.Object);
            var request = new DtoRegistroEvaluarDocumentoRequest
            {
                TipoDocumento = "CI",
                Documento = "1234567-2"
            };
            serviceMock
                .Setup(s => s.EvaluarDocumentoAsync(request))
                .ReturnsAsync(OperationResult<DtoRegistroEvaluacionResponse>.Ok(
                    new DtoRegistroEvaluacionResponse { UsuarioExistente = true },
                    nameof(IRegistroService.EvaluarDocumentoAsync)));

            var response = await controller.EvaluarDocumento(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var body = Assert.IsType<OperationResult<DtoRegistroEvaluacionResponse>>(okResult.Value);
            Assert.Null(body.Data!.FlowId);
            flowServiceMock.Verify(
                s => s.CrearFlowSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>()),
                Times.Never);
        }

        [Fact]
        public async Task AnalizarAdjunto_WithNullRequest_ReturnsBadRequest()
        {
            var reconocimientoMock = new Mock<IReconocimientoDocumento>();
            var controller = CrearController(reconocimientoDocumentoService: reconocimientoMock.Object);

            var response = await controller.AnalizarAdjunto(null);

            var badRequest = Assert.IsType<ObjectResult>(response);
            Assert.Equal(400, badRequest.StatusCode);
            reconocimientoMock.Verify(
                s => s.ReconocerDocumentoAsync(It.IsAny<ReconocimientoDocumentoRequest>()),
                Times.Never);
        }

        [Fact]
        public async Task AnalizarAdjunto_WithFile_DelegatesToRecognitionService()
        {
            var reconocimientoMock = new Mock<IReconocimientoDocumento>();
            var controller = CrearController(reconocimientoDocumentoService: reconocimientoMock.Object);
            var request = new ReconocimientoDocumentoApiRequest
            {
                TipoMime = "application/pdf",
                ArchivoAdjunto = new ArchivoPayload
                {
                    NombreArchivo = "documento.pdf",
                    Archivo = [1, 2, 3]
                }
            };
            reconocimientoMock
                .Setup(s => s.ReconocerDocumentoAsync(It.Is<ReconocimientoDocumentoRequest>(r =>
                    r.NombreArchivo == "documento.pdf"
                    && r.TipoMime == "application/pdf"
                    && r.Archivo.SequenceEqual(new byte[] { 1, 2, 3 }))))
                .ReturnsAsync(OperationResult<ReconocimientoDocumentoResponse>.Ok(
                    new ReconocimientoDocumentoResponse(),
                    nameof(IReconocimientoDocumento.ReconocerDocumentoAsync)));

            var response = await controller.AnalizarAdjunto(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            reconocimientoMock.Verify(
                s => s.ReconocerDocumentoAsync(It.IsAny<ReconocimientoDocumentoRequest>()),
                Times.Once);
        }

        [Fact]
        public async Task AnalizarAdjunto_WithRecognizedDocument_StoresImagesInCache()
        {
            var reconocimientoMock = new Mock<IReconocimientoDocumento>();
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            var controller = CrearController(
                reconocimientoDocumentoService: reconocimientoMock.Object,
                documentoImagenCacheService: cacheMock.Object);
            var request = new ReconocimientoDocumentoApiRequest
            {
                TipoMime = "application/pdf",
                ArchivoAdjunto = new ArchivoPayload
                {
                    NombreArchivo = "documento.pdf",
                    Archivo = [0x25, 0x50, 0x44, 0x46, 1]
                }
            };
            reconocimientoMock
                .Setup(s => s.ReconocerDocumentoAsync(It.IsAny<ReconocimientoDocumentoRequest>()))
                .ReturnsAsync(OperationResult<ReconocimientoDocumentoResponse>.Ok(
                    new ReconocimientoDocumentoResponse
                    {
                        Campos = new CamposDocumentoReconocidoDto
                        {
                            TipoDocumento = "CI",
                            NumeroDocumento = "12345672",
                            FechaVencimiento = new DateTime(2030, 1, 1)
                        },
                        CaraPersona = new ArchivoDescargaDto
                        {
                            Archivo = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                            NombreArchivo = "cara.jpg",
                            ContentType = "image/jpeg"
                        }
                    },
                    nameof(IReconocimientoDocumento.ReconocerDocumentoAsync)));

            var response = await controller.AnalizarAdjunto(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            cacheMock.Verify(
                s => s.GuardarImagenesTemporalesSiCorrespondeAsync(
                    "CI",
                    "12345672",
                    new DateTime(2030, 1, 1),
                    It.Is<DtoRegistroDocumentoArchivoTemporal>(a =>
                        a.NombreArchivo == "documento.pdf"
                        && a.Archivo.SequenceEqual(request.ArchivoAdjunto.Archivo!)),
                    It.Is<DtoRegistroDocumentoArchivoTemporal?>(a =>
                        a != null && a.NombreArchivo == "cara.jpg")),
                Times.Once);
        }

        [Fact]
        public async Task AnalizarAdjunto_WithoutNumeroDocumento_DelegatesDecisionToCacheService()
        {
            // El controller ya no decide si corresponde guardar (esa decisión ahora vive en
            // RegistroDocumentoImagenCacheService.GuardarImagenesTemporalesSiCorrespondeAsync):
            // siempre delega, incluso con NumeroDocumento ausente.
            var reconocimientoMock = new Mock<IReconocimientoDocumento>();
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            var controller = CrearController(
                reconocimientoDocumentoService: reconocimientoMock.Object,
                documentoImagenCacheService: cacheMock.Object);
            reconocimientoMock
                .Setup(s => s.ReconocerDocumentoAsync(It.IsAny<ReconocimientoDocumentoRequest>()))
                .ReturnsAsync(OperationResult<ReconocimientoDocumentoResponse>.Ok(
                    new ReconocimientoDocumentoResponse
                    {
                        Campos = new CamposDocumentoReconocidoDto
                        {
                            TipoDocumento = "CI"
                        }
                    },
                    nameof(IReconocimientoDocumento.ReconocerDocumentoAsync)));

            var response = await controller.AnalizarAdjunto(new ReconocimientoDocumentoApiRequest
            {
                TipoMime = "application/pdf",
                ArchivoAdjunto = new ArchivoPayload
                {
                    NombreArchivo = "documento.pdf",
                    Archivo = [0x25, 0x50, 0x44, 0x46, 1]
                }
            });

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            cacheMock.Verify(
                s => s.GuardarImagenesTemporalesSiCorrespondeAsync(
                    "CI",
                    null,
                    It.IsAny<DateTime?>(),
                    It.IsAny<DtoRegistroDocumentoArchivoTemporal>(),
                    It.IsAny<DtoRegistroDocumentoArchivoTemporal?>()),
                Times.Once);
        }

        [Fact]
        public async Task AnalizarAdjunto_WhenCacheFails_ReturnsRecognitionResult()
        {
            // Usa el service REAL (no un mock de la interfaz) porque el try/catch no-bloqueante
            // ahora vive dentro de RegistroDocumentoImagenCacheService, no en el controller.
            var reconocimientoMock = new Mock<IReconocimientoDocumento>();
            var redisDbMock = new Mock<IDatabase>();
            redisDbMock
                .Setup(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
                .ThrowsAsync(new InvalidOperationException("Redis unavailable"));
            var connectionMock = new Mock<IConnectionMultiplexer>();
            connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDbMock.Object);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            var realCacheService = new RegistroDocumentoImagenCacheService(configuration, connectionMock.Object);
            var controller = CrearController(
                reconocimientoDocumentoService: reconocimientoMock.Object,
                documentoImagenCacheService: realCacheService);
            reconocimientoMock
                .Setup(s => s.ReconocerDocumentoAsync(It.IsAny<ReconocimientoDocumentoRequest>()))
                .ReturnsAsync(OperationResult<ReconocimientoDocumentoResponse>.Ok(
                    new ReconocimientoDocumentoResponse
                    {
                        Campos = new CamposDocumentoReconocidoDto
                        {
                            TipoDocumento = "CI",
                            NumeroDocumento = "12345672"
                        }
                    },
                    nameof(IReconocimientoDocumento.ReconocerDocumentoAsync)));

            var response = await controller.AnalizarAdjunto(new ReconocimientoDocumentoApiRequest
            {
                TipoMime = "application/pdf",
                ArchivoAdjunto = new ArchivoPayload
                {
                    NombreArchivo = "documento.pdf",
                    Archivo = [0x25, 0x50, 0x44, 0x46, 1]
                }
            });

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ConfirmarNuevaPersona_DelegatesToServiceAndReturnsOk()
        {
            var flowServiceMock = new Mock<IRegistroFlowService>();
            flowServiceMock
                .Setup(s => s.ValidarFlowSessionAsync(It.IsAny<string>(), "evaluado"))
                .ReturnsAsync((OperationResult<object?>?)null);
            flowServiceMock
                .Setup(s => s.ConfirmarNuevaPersonaAsync(It.IsAny<DtoRegistroPersonaRequest>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<RegistroFlowResult>.IsSuccess(
                    new RegistroFlowResult("Registro realizado correctamente."),
                    nameof(IRegistroFlowService.ConfirmarNuevaPersonaAsync),
                    "Registro realizado correctamente."));
            var controller = CrearController(registroFlowService: flowServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.Request.Headers["X-Flow-Id"] = "test-flow-id";
            var request = new DtoRegistroPersonaRequest
            {
                TipoDocumento = "PS",
                Documento = "A123"
            };

            var response = await controller.ConfirmarNuevaPersona(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            flowServiceMock.Verify(s => s.ConfirmarNuevaPersonaAsync(request, "test-flow-id"), Times.Once);
        }

        private static RegistroController CrearController(
            IRegistroService? registroService = null,
            IReconocimientoDocumento? reconocimientoDocumentoService = null,
            IRegistroFlowService? registroFlowService = null,
            IRegistroDocumentoImagenCacheService? documentoImagenCacheService = null)
        {
            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.Setup(c => c.UserId).Returns(1);

            var flowServiceMock = registroFlowService ?? CreateDefaultRegistroFlowServiceMock();

            return new RegistroController(
                registroService ?? Mock.Of<IRegistroService>(),
                flowServiceMock,
                reconocimientoDocumentoService ?? Mock.Of<IReconocimientoDocumento>(),
                documentoImagenCacheService ?? Mock.Of<IRegistroDocumentoImagenCacheService>(),
                Mock.Of<ILogger<RegistroController>>(),
                currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        /// <summary>
        /// Crea un mock de IRegistroFlowService que no bloquea el flujo
        /// (ValidarFlowSessionAsync retorna null = válido, CrearFlowSessionAsync retorna un flowId).
        /// </summary>
        private static IRegistroFlowService CreateDefaultRegistroFlowServiceMock()
        {
            var mock = new Mock<IRegistroFlowService>();
            mock.Setup(f => f.ValidarFlowSessionAsync(It.IsAny<string?>(), It.IsAny<string>()))
                .ReturnsAsync((OperationResult<object?>?)null);
            mock.Setup(f => f.CrearFlowSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>()))
                .ReturnsAsync("test-flow-id");
            mock.Setup(f => f.ActualizarStepAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mock.Setup(f => f.ConfirmarNuevaPersonaAsync(It.IsAny<DtoRegistroPersonaRequest>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<RegistroFlowResult>.Ok(
                    new RegistroFlowResult("Registro realizado correctamente."),
                    "ConfirmarNuevaPersonaAsync"));
            return mock.Object;
        }
    }
}
