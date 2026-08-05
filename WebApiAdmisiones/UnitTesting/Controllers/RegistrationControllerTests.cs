using AppLogic.Identity.Services;
using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Registration.Dtos;
using System;
using System.Collections.Generic;
using AzureService.DTOs;
using AzureService.Interfaces;
using AppLogic.DevartDTOs;
using AppLogic.Registration.Services;
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
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Interfaces;

namespace UnitTesting.Controllers
{
    public class RegistroControllerTests
    {
        [Theory]
        [InlineData(nameof(RegistrationController.EvaluateDocument))]
        [InlineData(nameof(RegistrationController.VerifyIdentity))]
        [InlineData(nameof(RegistrationController.ConfirmNewPerson))]
        [InlineData(nameof(RegistrationController.ConfirmRegistrationRequest))]
        public void PublicEndpoints_HaveAllowAnonymous(string methodName)
        {
            var method = typeof(RegistrationController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true),
                attribute => attribute is AllowAnonymousAttribute);
        }

        [Theory]
        [InlineData(nameof(RegistrationController.ConfirmNewPerson))]
        [InlineData(nameof(RegistrationController.ConfirmRegistrationRequest))]
        public void ConfirmarEndpoints_HaveRequireCaptcha(string methodName)
        {
            var method = typeof(RegistrationController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(RequireCaptchaAttribute), inherit: true),
                attribute => attribute is RequireCaptchaAttribute);
        }

        [Theory]
        [InlineData(nameof(RegistrationController.EvaluateDocument), CaptchaActions.EvaluateDocument)]
        [InlineData(nameof(RegistrationController.VerifyIdentity), CaptchaActions.VerifyIdentity)]
        [InlineData(nameof(RegistrationController.AnalyzeAttachment), CaptchaActions.AnalyzeAttachment)]
        [InlineData(nameof(RegistrationController.ConfirmNewPerson), CaptchaActions.ConfirmNewPerson)]
        [InlineData(nameof(RegistrationController.ConfirmRegistrationRequest), CaptchaActions.ConfirmRegistrationRequest)]
        public void PublicCaptchaEndpoints_HaveExpectedCaptchaConfiguration(string methodName, string expectedAction)
        {
            var method = typeof(RegistrationController).GetMethod(methodName);

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
            var serviceMock = new Mock<IEvaluateDocument>();
            var controller = CrearController(serviceMock.Object);
            var request = new EvaluateDocumentRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2"
            };
            serviceMock
                .Setup(s => s.ExecuteAsync(request))
                .ReturnsAsync(OperationResult<DocumentEvaluationResponse>.Ok(
                    new DocumentEvaluationResponse { RequiresPersonRegistration = true },
                    nameof(IEvaluateDocument)));

            var response = await controller.EvaluateDocument(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ExecuteAsync(request), Times.Once);
        }

        [Fact]
        public async Task EvaluarDocumento_WithUsuarioExistente_DoesNotCreateFlowId()
        {
            var serviceMock = new Mock<IEvaluateDocument>();
            var flowServiceMock = new Mock<IRegistrationFlowService>();
            var controller = CrearController(serviceMock.Object, registroFlowService: flowServiceMock.Object);
            var request = new EvaluateDocumentRequest
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2"
            };
            serviceMock
                .Setup(s => s.ExecuteAsync(request))
                .ReturnsAsync(OperationResult<DocumentEvaluationResponse>.Ok(
                    new DocumentEvaluationResponse { UserAlreadyRegistered = true },
                    nameof(IEvaluateDocument)));

            var response = await controller.EvaluateDocument(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var body = Assert.IsType<OperationResult<DocumentEvaluationResponse>>(okResult.Value);
            Assert.Null(body.Data!.FlowId);
            flowServiceMock.Verify(
                s => s.CreateFlowSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>()),
                Times.Never);
        }

        [Fact]
        public async Task AnalizarAdjunto_WithNullRequest_ReturnsBadRequest()
        {
            var reconocimientoMock = new Mock<IReconocimientoDocumento>();
            var controller = CrearController(reconocimientoDocumentoService: reconocimientoMock.Object);

            var response = await controller.AnalyzeAttachment(null);

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
            var request = new RecognizeDocumentApiRequest
            {
                MimeType = "application/pdf",
                File = new FilePayload
                {
                    FileName = "documento.pdf",
                    Content = [1, 2, 3]
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

            var response = await controller.AnalyzeAttachment(request);

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
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
            var controller = CrearController(
                reconocimientoDocumentoService: reconocimientoMock.Object,
                documentoImagenCacheService: cacheMock.Object);
            var request = new RecognizeDocumentApiRequest
            {
                MimeType = "application/pdf",
                File = new FilePayload
                {
                    FileName = "documento.pdf",
                    Content = [0x25, 0x50, 0x44, 0x46, 1]
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

            var response = await controller.AnalyzeAttachment(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            cacheMock.Verify(
                s => s.SaveTemporaryImagesIfApplicableAsync(
                    "CI",
                    "12345672",
                    new DateTime(2030, 1, 1),
                    It.Is<TemporaryDocumentFile>(a =>
                        a.FileName == "documento.pdf"
                        && a.Content.SequenceEqual(request.File.Content!)),
                    It.Is<TemporaryDocumentFile?>(a =>
                        a != null && a.FileName == "cara.jpg")),
                Times.Once);
        }

        [Fact]
        public async Task AnalizarAdjunto_WithoutNumeroDocumento_DelegatesDecisionToCacheService()
        {
            // El controller ya no decide si corresponde guardar (esa decisión ahora vive en
            // RedisIdentityDocumentImageCache.SaveTemporaryImagesIfApplicableAsync):
            // siempre delega, incluso con NumeroDocumento ausente.
            var reconocimientoMock = new Mock<IReconocimientoDocumento>();
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
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

            var response = await controller.AnalyzeAttachment(new RecognizeDocumentApiRequest
            {
                MimeType = "application/pdf",
                File = new FilePayload
                {
                    FileName = "documento.pdf",
                    Content = [0x25, 0x50, 0x44, 0x46, 1]
                }
            });

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            cacheMock.Verify(
                s => s.SaveTemporaryImagesIfApplicableAsync(
                    "CI",
                    null,
                    It.IsAny<DateTime?>(),
                    It.IsAny<TemporaryDocumentFile>(),
                    It.IsAny<TemporaryDocumentFile?>()),
                Times.Once);
        }

        [Fact]
        public async Task AnalizarAdjunto_WhenCacheFails_ReturnsRecognitionResult()
        {
            // Usa el service REAL (no un mock de la interfaz) porque el try/catch no-bloqueante
            // ahora vive dentro de RedisIdentityDocumentImageCache, no en el controller.
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
            var realCacheService = new RedisIdentityDocumentImageCache(configuration, connectionMock.Object);
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

            var response = await controller.AnalyzeAttachment(new RecognizeDocumentApiRequest
            {
                MimeType = "application/pdf",
                File = new FilePayload
                {
                    FileName = "documento.pdf",
                    Content = [0x25, 0x50, 0x44, 0x46, 1]
                }
            });

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ConfirmarNuevaPersona_DelegatesToServiceAndReturnsOk()
        {
            var flowServiceMock = new Mock<IRegistrationFlowService>();
            flowServiceMock
                .Setup(s => s.ValidateFlowSessionAsync(It.IsAny<string>(), "evaluado"))
                .ReturnsAsync((OperationResult<object?>?)null);
            flowServiceMock
                .Setup(s => s.ConfirmNewPersonAsync(It.IsAny<RegisterPersonRequest>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<RegistrationFlowResult>.IsSuccess(
                    new RegistrationFlowResult("Registro realizado correctamente."),
                    nameof(IRegistrationFlowService.ConfirmNewPersonAsync),
                    "Registro realizado correctamente."));
            var controller = CrearController(registroFlowService: flowServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.Request.Headers["X-Flow-Id"] = "test-flow-id";
            var request = new RegisterPersonRequest
            {
                DocumentType = "PS",
                DocumentNumber = "A123"
            };

            var response = await controller.ConfirmNewPerson(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            flowServiceMock.Verify(s => s.ConfirmNewPersonAsync(request, "test-flow-id"), Times.Once);
        }

        private static RegistrationController CrearController(
            IEvaluateDocument? evaluateDocument = null,
            IReconocimientoDocumento? reconocimientoDocumentoService = null,
            IRegistrationFlowService? registroFlowService = null,
            IIdentityDocumentImageCache? documentoImagenCacheService = null)
        {
            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.Setup(c => c.UserId).Returns(1);

            var flowServiceMock = registroFlowService ?? CreateDefaultRegistroFlowServiceMock();

            return new RegistrationController(
                evaluateDocument ?? Mock.Of<IEvaluateDocument>(),
                Mock.Of<IVerifyIdentity>(),
                Mock.Of<IConfirmRegistrationRequest>(),
                flowServiceMock,
                reconocimientoDocumentoService ?? Mock.Of<IReconocimientoDocumento>(),
                documentoImagenCacheService ?? Mock.Of<IIdentityDocumentImageCache>(),
                Mock.Of<ILogger<RegistrationController>>(),
                currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        /// <summary>
        /// Crea un mock de IRegistrationFlowService que no bloquea el flujo
        /// (ValidateFlowSessionAsync retorna null = válido, CreateFlowSessionAsync retorna un flowId).
        /// </summary>
        private static IRegistrationFlowService CreateDefaultRegistroFlowServiceMock()
        {
            var mock = new Mock<IRegistrationFlowService>();
            mock.Setup(f => f.ValidateFlowSessionAsync(It.IsAny<string?>(), It.IsAny<string>()))
                .ReturnsAsync((OperationResult<object?>?)null);
            mock.Setup(f => f.CreateFlowSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>()))
                .ReturnsAsync("test-flow-id");
            mock.Setup(f => f.UpdateStepAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mock.Setup(f => f.ConfirmNewPersonAsync(It.IsAny<RegisterPersonRequest>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<RegistrationFlowResult>.Ok(
                    new RegistrationFlowResult("Registro realizado correctamente."),
                    "ConfirmNewPersonAsync"));
            return mock.Object;
        }
    }
}
