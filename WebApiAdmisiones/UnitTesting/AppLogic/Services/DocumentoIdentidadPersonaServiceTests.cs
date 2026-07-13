using AppLogic.Personas.Services;
using AppLogic.Registro.Dtos;
using AppLogic.Registro.Interfaces;
using AppLogic.Common.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class DocumentoIdentidadPersonaServiceTests
    {
        private static readonly byte[] PdfBytes = { 0x25, 0x50, 0x44, 0x46, 0x01, 0x02 };
        private static readonly byte[] JpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02 };
        private static readonly byte[] UnknownBytes = { 0x00, 0x01, 0x02, 0x03 };

        private static DtoRegistroDocumentoImagenesTemporales CrearImagenes(
            byte[]? documentoBytes = null,
            byte[]? caraBytes = null)
        {
            return new DtoRegistroDocumentoImagenesTemporales
            {
                TipoDocumento = "CI",
                Documento = "1234567-8",
                DocumentoFrente = new DtoRegistroDocumentoArchivoTemporal
                {
                    Archivo = documentoBytes ?? JpegBytes,
                    NombreArchivo = "documento.jpg"
                },
                CaraPersona = caraBytes is null
                    ? null
                    : new DtoRegistroDocumentoArchivoTemporal
                    {
                        Archivo = caraBytes,
                        NombreArchivo = "cara.jpg"
                    }
            };
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithNullImagenes_ReturnsOk()
        {
            var result = DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido(
                null,
                nameof(ValidarImagenesDocumentoReconocido_WithNullImagenes_ReturnsOk));

            Assert.True(result.Success);
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithPdfDocument_Fails()
        {
            var imagenes = CrearImagenes(documentoBytes: PdfBytes);

            var result = DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido(
                imagenes,
                nameof(ValidarImagenesDocumentoReconocido_WithPdfDocument_Fails));

            Assert.False(result.Success);
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithUnknownDocumentContentType_Fails()
        {
            var imagenes = CrearImagenes(documentoBytes: UnknownBytes);

            var result = DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido(
                imagenes,
                nameof(ValidarImagenesDocumentoReconocido_WithUnknownDocumentContentType_Fails));

            Assert.False(result.Success);
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithValidCaraPersona_ReturnsOk()
        {
            var imagenes = CrearImagenes(caraBytes: JpegBytes);

            var result = DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido(
                imagenes,
                nameof(ValidarImagenesDocumentoReconocido_WithValidCaraPersona_ReturnsOk));

            Assert.True(result.Success);
        }

        [Fact]
        public void ValidarImagenesDocumentoReconocido_WithInvalidCaraPersona_Fails()
        {
            var imagenes = CrearImagenes(caraBytes: UnknownBytes);

            var result = DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido(
                imagenes,
                nameof(ValidarImagenesDocumentoReconocido_WithInvalidCaraPersona_Fails));

            Assert.False(result.Success);
        }

        [Fact]
        public void ConstruirNombrePersistido_BuildsExpectedName()
        {
            var nombre = DocumentoIdentidadPersonaService.ConstruirNombrePersistido(123, 1, ".jpg");

            Assert.Equal("123_1.jpg", nombre);
        }

        [Fact]
        public void ResolverExtensionPersistida_WithValidFileName_ReturnsItsExtension()
        {
            var extension = DocumentoIdentidadPersonaService.ResolverExtensionPersistida("foto.PNG", ".jpg");

            Assert.Equal(".png", extension);
        }

        [Fact]
        public void ResolverExtensionPersistida_WithFileNameWithoutExtension_ReturnsDefault()
        {
            var extension = DocumentoIdentidadPersonaService.ResolverExtensionPersistida("foto", ".jpg");

            Assert.Equal(".jpg", extension);
        }

        [Fact]
        public void ResolverExtensionPersistida_WithNullFileName_ReturnsDefault()
        {
            var extension = DocumentoIdentidadPersonaService.ResolverExtensionPersistida(null, ".jpg");

            Assert.Equal(".jpg", extension);
        }

        [Fact]
        public void ResolverCodigoValidacionDocumento_WithInvalidDocumentType_ReturnsTipoInvalidoCode()
        {
            var codigo = DocumentoIdentidadPersonaService.ResolverCodigoValidacionDocumento(
                DocumentUtils.DocumentValidationError.InvalidDocumentType,
                "COD_TIPO",
                "COD_OTRO");

            Assert.Equal("COD_TIPO", codigo);
        }

        [Fact]
        public void ResolverCodigoValidacionDocumento_WithOtherError_ReturnsOtroCode()
        {
            var codigo = DocumentoIdentidadPersonaService.ResolverCodigoValidacionDocumento(
                DocumentUtils.DocumentValidationError.InvalidDocument,
                "COD_TIPO",
                "COD_OTRO");

            Assert.Equal("COD_OTRO", codigo);
        }

        [Fact]
        public async Task ObtenerImagenesTemporalesSeguroAsync_WithNullCacheService_ReturnsNull()
        {
            var result = await DocumentoIdentidadPersonaService.ObtenerImagenesTemporalesSeguroAsync(
                null,
                "CI",
                "12345678",
                null);

            Assert.Null(result);
        }

        [Fact]
        public async Task ObtenerImagenesTemporalesSeguroAsync_WithBlankDocumento_ReturnsNullWithoutCallingCache()
        {
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();

            var result = await DocumentoIdentidadPersonaService.ObtenerImagenesTemporalesSeguroAsync(
                cacheMock.Object,
                "CI",
                "   ",
                null);

            Assert.Null(result);
            cacheMock.Verify(
                c => c.ObtenerAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ObtenerImagenesTemporalesSeguroAsync_WhenCacheHasImages_ReturnsThem()
        {
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            var imagenes = CrearImagenes();
            cacheMock
                .Setup(c => c.ObtenerAsync("CI", "1234567-8"))
                .ReturnsAsync(imagenes);

            var result = await DocumentoIdentidadPersonaService.ObtenerImagenesTemporalesSeguroAsync(
                cacheMock.Object,
                "CI",
                "1234567-8",
                null);

            Assert.Same(imagenes, result);
        }

        [Fact]
        public async Task ObtenerImagenesTemporalesSeguroAsync_WhenCacheThrows_LogsWarningAndReturnsNull()
        {
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            cacheMock
                .Setup(c => c.ObtenerAsync("CI", "1234567-8"))
                .ThrowsAsync(new InvalidOperationException("redis down"));
            var loggerMock = new Mock<ILogger>();

            var result = await DocumentoIdentidadPersonaService.ObtenerImagenesTemporalesSeguroAsync(
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
            await DocumentoIdentidadPersonaService.EliminarImagenesTemporalesSeguroAsync(
                null,
                "CI",
                "12345678",
                null);
        }

        [Fact]
        public async Task EliminarImagenesTemporalesSeguroAsync_WithBlankDocumento_DoesNotCallCache()
        {
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();

            await DocumentoIdentidadPersonaService.EliminarImagenesTemporalesSeguroAsync(
                cacheMock.Object,
                "   ",
                "12345678",
                null);

            cacheMock.Verify(
                c => c.EliminarAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task EliminarImagenesTemporalesSeguroAsync_WhenValid_CallsCacheDelete()
        {
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            cacheMock
                .Setup(c => c.EliminarAsync("CI", "1234567-8"))
                .Returns(Task.CompletedTask);

            await DocumentoIdentidadPersonaService.EliminarImagenesTemporalesSeguroAsync(
                cacheMock.Object,
                "CI",
                "1234567-8",
                null);

            cacheMock.Verify(c => c.EliminarAsync("CI", "1234567-8"), Times.Once);
        }

        [Fact]
        public async Task EliminarImagenesTemporalesSeguroAsync_WhenCacheThrows_LogsWarningWithoutRethrowing()
        {
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            cacheMock
                .Setup(c => c.EliminarAsync("CI", "1234567-8"))
                .ThrowsAsync(new InvalidOperationException("redis down"));
            var loggerMock = new Mock<ILogger>();

            await DocumentoIdentidadPersonaService.EliminarImagenesTemporalesSeguroAsync(
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
