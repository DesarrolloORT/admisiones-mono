using AppLogic.Identity;
using AppLogic.Identity.Services;
using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.People.UseCases;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class IdentityDocumentServiceTests
    {
        private static readonly byte[] PdfBytes = { 0x25, 0x50, 0x44, 0x46, 0x01, 0x02 };
        private static readonly byte[] JpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02 };
        private static readonly byte[] UnknownBytes = { 0x00, 0x01, 0x02, 0x03 };

        private static TemporaryDocumentImages CrearImagenes(
            byte[]? documentoBytes = null,
            byte[]? caraBytes = null)
        {
            return new TemporaryDocumentImages
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-8",
                DocumentFront = new TemporaryDocumentFile
                {
                    Content = documentoBytes ?? JpegBytes,
                    FileName = "documento.jpg"
                },
                PersonFace = caraBytes is null
                    ? null
                    : new TemporaryDocumentFile
                    {
                        Content = caraBytes,
                        FileName = "cara.jpg"
                    }
            };
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithNullImagenes_ReturnsOk()
        {
            var result = IdentityDocumentService.ValidateRecognizedDocumentImages(
                null,
                nameof(ValidarImagenesDocumentoReconocido_WithNullImagenes_ReturnsOk));

            Assert.True(result.Success);
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithPdfDocument_Fails()
        {
            var images = CrearImagenes(documentoBytes: PdfBytes);

            var result = IdentityDocumentService.ValidateRecognizedDocumentImages(
                images,
                nameof(ValidarImagenesDocumentoReconocido_WithPdfDocument_Fails));

            Assert.False(result.Success);
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithUnknownDocumentContentType_Fails()
        {
            var images = CrearImagenes(documentoBytes: UnknownBytes);

            var result = IdentityDocumentService.ValidateRecognizedDocumentImages(
                images,
                nameof(ValidarImagenesDocumentoReconocido_WithUnknownDocumentContentType_Fails));

            Assert.False(result.Success);
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithValidCaraPersona_ReturnsOk()
        {
            var images = CrearImagenes(caraBytes: JpegBytes);

            var result = IdentityDocumentService.ValidateRecognizedDocumentImages(
                images,
                nameof(ValidarImagenesDocumentoReconocido_WithValidCaraPersona_ReturnsOk));

            Assert.True(result.Success);
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithInvalidCaraPersona_Fails()
        {
            var images = CrearImagenes(caraBytes: UnknownBytes);

            var result = IdentityDocumentService.ValidateRecognizedDocumentImages(
                images,
                nameof(ValidarImagenesDocumentoReconocido_WithInvalidCaraPersona_Fails));

            Assert.False(result.Success);
        }

        [Fact]
        public void ConstruirNombrePersistido_BuildsExpectedName()
        {
            var name = IdentityDocumentService.BuildPersistedName(123, 1, ".jpg");

            Assert.Equal("123_1.jpg", name);
        }

        [Fact]
        public void ResolverExtensionPersistida_WithValidFileName_ReturnsItsExtension()
        {
            var extension = IdentityDocumentService.ResolvePersistedExtension("foto.PNG", ".jpg");

            Assert.Equal(".png", extension);
        }

        [Fact]
        public void ResolverExtensionPersistida_WithFileNameWithoutExtension_ReturnsDefault()
        {
            var extension = IdentityDocumentService.ResolvePersistedExtension("foto", ".jpg");

            Assert.Equal(".jpg", extension);
        }

        [Fact]
        public void ResolverExtensionPersistida_WithNullFileName_ReturnsDefault()
        {
            var extension = IdentityDocumentService.ResolvePersistedExtension(null, ".jpg");

            Assert.Equal(".jpg", extension);
        }

        [Fact]
        public void ResolverCodigoValidacionDocumento_WithInvalidDocumentType_ReturnsTipoInvalidoCode()
        {
            var code = IdentityDocumentService.ResolveDocumentValidationCode(
                IdentityDocumentRules.DocumentValidationError.InvalidDocumentType,
                "COD_TIPO",
                "COD_OTRO");

            Assert.Equal("COD_TIPO", code);
        }

        [Fact]
        public void ResolverCodigoValidacionDocumento_WithOtherError_ReturnsOtroCode()
        {
            var code = IdentityDocumentService.ResolveDocumentValidationCode(
                IdentityDocumentRules.DocumentValidationError.InvalidDocument,
                "COD_TIPO",
                "COD_OTRO");

            Assert.Equal("COD_OTRO", code);
        }

        [Fact]
        public async Task ObtenerImagenesTemporalesSeguroAsync_WithNullCacheService_ReturnsNull()
        {
            var result = await IdentityDocumentService.GetTemporaryImagesSafeAsync(
                null,
                "CI",
                "12345678",
                null);

            Assert.Null(result);
        }

        [Fact]
        public async Task ObtenerImagenesTemporalesSeguroAsync_WithBlankDocumento_ReturnsNullWithoutCallingCache()
        {
            var cacheMock = new Mock<IIdentityDocumentImageCache>();

            var result = await IdentityDocumentService.GetTemporaryImagesSafeAsync(
                cacheMock.Object,
                "CI",
                "   ",
                null);

            Assert.Null(result);
            cacheMock.Verify(
                c => c.GetAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ObtenerImagenesTemporalesSeguroAsync_WhenCacheHasImages_ReturnsThem()
        {
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
            var images = CrearImagenes();
            cacheMock
                .Setup(c => c.GetAsync("CI", "1234567-8"))
                .ReturnsAsync(images);

            var result = await IdentityDocumentService.GetTemporaryImagesSafeAsync(
                cacheMock.Object,
                "CI",
                "1234567-8",
                null);

            Assert.Same(images, result);
        }

        [Fact]
        public async Task ObtenerImagenesTemporalesSeguroAsync_WhenCacheThrows_LogsWarningAndReturnsNull()
        {
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
            cacheMock
                .Setup(c => c.GetAsync("CI", "1234567-8"))
                .ThrowsAsync(new InvalidOperationException("redis down"));
            var loggerMock = new Mock<ILogger>();

            var result = await IdentityDocumentService.GetTemporaryImagesSafeAsync(
                cacheMock.Object,
                "CI",
                "1234567-8",
                loggerMock.Object);

            Assert.Null(result);
            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task EliminarImagenesTemporalesSeguroAsync_WithNullCacheService_DoesNotThrow()
        {
            await IdentityDocumentService.DeleteTemporaryImagesSafeAsync(
                null,
                "CI",
                "12345678",
                null);
        }

        [Fact]
        public async Task EliminarImagenesTemporalesSeguroAsync_WithBlankDocumento_DoesNotCallCache()
        {
            var cacheMock = new Mock<IIdentityDocumentImageCache>();

            await IdentityDocumentService.DeleteTemporaryImagesSafeAsync(
                cacheMock.Object,
                "   ",
                "12345678",
                null);

            cacheMock.Verify(
                c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task EliminarImagenesTemporalesSeguroAsync_WhenValid_CallsCacheDelete()
        {
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
            cacheMock
                .Setup(c => c.DeleteAsync("CI", "1234567-8"))
                .Returns(Task.CompletedTask);

            await IdentityDocumentService.DeleteTemporaryImagesSafeAsync(
                cacheMock.Object,
                "CI",
                "1234567-8",
                null);

            cacheMock.Verify(c => c.DeleteAsync("CI", "1234567-8"), Times.Once);
        }

        [Fact]
        public async Task EliminarImagenesTemporalesSeguroAsync_WhenCacheThrows_LogsWarningWithoutRethrowing()
        {
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
            cacheMock
                .Setup(c => c.DeleteAsync("CI", "1234567-8"))
                .ThrowsAsync(new InvalidOperationException("redis down"));
            var loggerMock = new Mock<ILogger>();

            await IdentityDocumentService.DeleteTemporaryImagesSafeAsync(
                cacheMock.Object,
                "CI",
                "1234567-8",
                loggerMock.Object);

            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
