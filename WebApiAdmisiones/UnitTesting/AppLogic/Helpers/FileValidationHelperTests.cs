using AppLogic.Helpers.ValidationHelpers;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Helpers
{
    public class FileValidationHelperTests
    {
        [Fact]
        public void ValidateImageFile_ValidJpeg_ReturnsSuccess()
        {
            // Arrange - JPEG válido
            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
            var fileName = "photo.jpg";

            // Act
            var result = FileValidator.ValidateImageFile(jpegContent, fileName, nameof(ValidateImageFile_ValidJpeg_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void ValidateImageFile_ValidPng_ReturnsSuccess()
        {
            // Arrange - PNG válido
            var pngContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var fileName = "image.png";

            // Act
            var result = FileValidator.ValidateImageFile(pngContent, fileName, nameof(ValidateImageFile_ValidPng_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Theory]
        [InlineData("image.jpg")]
        [InlineData("image.exe")]
        [InlineData("image")]
        public void ValidateImageFile_PngBytesIgnoresFileNameExtension_ReturnsSuccess(string fileName)
        {
            var pngContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            var result = FileValidator.ValidateImageFile(pngContent, fileName, nameof(ValidateImageFile_PngBytesIgnoresFileNameExtension_ReturnsSuccess));

            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void ValidateImageFile_PdfAsImage_ReturnsFailed()
        {
            // Arrange - PDF disfrazado de imagen
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var fileName = "fake.jpg";

            // Act
            var result = FileValidator.ValidateImageFile(pdfContent, fileName, nameof(ValidateImageFile_PdfAsImage_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
        }

        [Fact]
        public void ValidateDocumentFile_ValidPdf_ReturnsSuccess()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
            var fileName = "document.pdf";

            // Act
            var result = FileValidator.ValidateDocumentFile(pdfContent, fileName, nameof(ValidateDocumentFile_ValidPdf_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateFile_CustomWhitelist_ReturnsSuccess()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var fileName = "test.pdf";
            var whitelist = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.ValidateFile(pdfContent, fileName, whitelist, nameof(ValidateFile_CustomWhitelist_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateFile_NotInWhitelist_ReturnsFailed()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var fileName = "test.pdf";
            var whitelist = new System.Collections.Generic.List<string> { ".jpg", ".png" }; // PDF no está en la whitelist

            // Act
            var result = FileValidator.ValidateFile(pdfContent, fileName, whitelist, nameof(ValidateFile_NotInWhitelist_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
        }

        [Fact]
        public void ValidateFile_EmptyContent_ReturnsFailed()
        {
            var whitelist = new System.Collections.Generic.List<string> { ".pdf", ".jpg", ".png" };

            var result = FileValidator.ValidateFile(Array.Empty<byte>(), "documento.jpg", whitelist, nameof(ValidateFile_EmptyContent_ReturnsFailed));

            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_01", result.ErrorCode);
        }

        [Fact]
        public void ValidateFile_InvalidBytes_ReturnsFailed()
        {
            var whitelist = new System.Collections.Generic.List<string> { ".pdf", ".jpg", ".png" };

            var result = FileValidator.ValidateFile(new byte[] { 0x01, 0x02, 0x03, 0x04 }, "documento.jpg", whitelist, nameof(ValidateFile_InvalidBytes_ReturnsFailed));

            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
        }

        #region SanitizeFileName Overload Tests (Separate Name and Extension)

        [Fact]
        public void SanitizeFileName_SeparateParams_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var fileNameWithoutExt = "curriculum_vitae";
            var extension = ".pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_ValidInput_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("curriculum_vitae.pdf", result.Data);
        }

        [Fact]
        public void SanitizeFileName_SeparateParams_ExtensionWithoutDot_ReturnsSuccess()
        {
            // Arrange
            var fileNameWithoutExt = "documento";
            var extension = "pdf"; // Sin punto
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_ExtensionWithoutDot_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("documento.pdf", result.Data);
        }

        [Fact]
        public void SanitizeFileName_SeparateParams_EmptyName_ReturnsFailed()
        {
            // Arrange
            var fileNameWithoutExt = "";
            var extension = ".pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_EmptyName_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_01", result.ErrorCode);
        }

        [Fact]
        public void SanitizeFileName_SeparateParams_NullName_ReturnsFailed()
        {
            // Arrange
            string fileNameWithoutExt = null;
            var extension = ".pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_NullName_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_01", result.ErrorCode);
        }

        [Fact]
        public void SanitizeFileName_SeparateParams_EmptyExtension_ReturnsFailed()
        {
            // Arrange
            var fileNameWithoutExt = "documento";
            var extension = "";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_EmptyExtension_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_03", result.ErrorCode);
        }

        [Fact]
        public void SanitizeFileName_SeparateParams_NullExtension_ReturnsFailed()
        {
            // Arrange
            var fileNameWithoutExt = "documento";
            string extension = null;
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_NullExtension_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_03", result.ErrorCode);
        }

        [Fact]
        public void SanitizeFileName_SeparateParams_NotInWhitelist_ReturnsFailed()
        {
            // Arrange
            var fileNameWithoutExt = "documento";
            var extension = ".docx";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_NotInWhitelist_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_04", result.ErrorCode);
        }

        [Fact]
        public void SanitizeFileName_SeparateParams_TooLong_ReturnsFailed()
        {
            // Arrange
            var fileNameWithoutExt = new string('a', 252); // 252 + 4 (.pdf) = 256 > 255
            var extension = ".pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_TooLong_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_02", result.ErrorCode);
        }

        [Fact]
        public void SanitizeFileName_SeparateParams_WithDangerousExtension_ReturnsFailed()
        {
            // Arrange
            var fileNameWithoutExt = "archivo.exe";
            var extension = ".pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_WithDangerousExtension_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_05", result.ErrorCode);
        }

        #endregion

        #region Additional Magic Bytes Tests

        [Fact]
        public void ValidateImageFile_JpegExif_ReturnsSuccess()
        {
            // Arrange - JPEG con magic bytes Exif
            var jpegExifContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1, 0x00, 0x00 };
            var fileName = "photo_exif.jpg";

            // Act
            var result = FileValidator.ValidateImageFile(jpegExifContent, fileName, nameof(ValidateImageFile_JpegExif_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateImageFile_JpegCanon_ReturnsSuccess()
        {
            // Arrange - JPEG con magic bytes Canon
            var jpegCanonContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE2, 0x00, 0x00 };
            var fileName = "photo_canon.jpeg";

            // Act
            var result = FileValidator.ValidateImageFile(jpegCanonContent, fileName, nameof(ValidateImageFile_JpegCanon_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateDocumentFile_ValidDoc_ReturnsSuccess()
        {
            // Arrange - DOC válido
            var docContent = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
            var fileName = "document.doc";

            // Act
            var result = FileValidator.ValidateDocumentFile(docContent, fileName, nameof(ValidateDocumentFile_ValidDoc_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateDocumentFile_ValidDocx_ReturnsSuccess()
        {
            // Arrange - DOCX válido (ZIP)
            var docxContent = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 };
            var fileName = "document.docx";

            // Act
            var result = FileValidator.ValidateDocumentFile(docxContent, fileName, nameof(ValidateDocumentFile_ValidDocx_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateDocumentFile_DocxEmptyZip_ReturnsSuccess()
        {
            // Arrange - DOCX con magic bytes de ZIP vacío
            var docxEmptyContent = new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00 };
            var fileName = "empty.docx";

            // Act
            var result = FileValidator.ValidateDocumentFile(docxEmptyContent, fileName, nameof(ValidateDocumentFile_DocxEmptyZip_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateDocumentFile_DocxSpannedZip_ReturnsSuccess()
        {
            // Arrange - DOCX con magic bytes de ZIP spanned
            var docxSpannedContent = new byte[] { 0x50, 0x4B, 0x07, 0x08, 0x00, 0x00 };
            var fileName = "spanned.docx";

            // Act
            var result = FileValidator.ValidateDocumentFile(docxSpannedContent, fileName, nameof(ValidateDocumentFile_DocxSpannedZip_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateFile_FileTooSmallForMagicBytes_ReturnsFailed()
        {
            // Arrange - Archivo muy pequeño que no puede contener los magic bytes completos
            var tinyContent = new byte[] { 0x25, 0x50 }; // Solo 2 bytes, PDF necesita al menos 4
            var fileName = "tiny.pdf";
            var whitelist = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.ValidateFile(tinyContent, fileName, whitelist, nameof(ValidateFile_FileTooSmallForMagicBytes_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
        }

        [Fact]
        public void ValidateImageFile_LargeImage_ReturnsFailed()
        {
            // Arrange - Imagen de 6 MB (mayor al límite de 5 MB)
            var largeContent = new byte[6 * 1024 * 1024];
            largeContent[0] = 0xFF;
            largeContent[1] = 0xD8;
            largeContent[2] = 0xFF;
            largeContent[3] = 0xE0;
            var fileName = "large.jpg";

            // Act
            var result = FileValidator.ValidateImageFile(largeContent, fileName, nameof(ValidateImageFile_LargeImage_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_07", result.ErrorCode);
        }

        [Fact]
        public void ValidateDocumentFile_LargeDoc_ReturnsFailed()
        {
            // Arrange - Documento de 11 MB (mayor al límite de 10 MB)
            var largeContent = new byte[11 * 1024 * 1024];
            largeContent[0] = 0xD0;
            largeContent[1] = 0xCF;
            largeContent[2] = 0x11;
            largeContent[3] = 0xE0;
            largeContent[4] = 0xA1;
            largeContent[5] = 0xB1;
            largeContent[6] = 0x1A;
            largeContent[7] = 0xE1;
            var fileName = "large.doc";

            // Act
            var result = FileValidator.ValidateDocumentFile(largeContent, fileName, nameof(ValidateDocumentFile_LargeDoc_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_07", result.ErrorCode);
        }

        #endregion

        #region Additional Sanitization Edge Cases

        [Fact]
        public void SanitizeFileName_WithMultipleSpaces_CollapsesToSingleUnderscore()
        {
            // Arrange
            var fileName = "archivo    con    muchos    espacios.pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_WithMultipleSpaces_CollapsesToSingleUnderscore));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("archivo_con_muchos_espacios.pdf", result.Data);
        }

        [Fact]
        public void SanitizeFileName_WithLeadingTrailingSpaces_TrimsCorrectly()
        {
            // Arrange
            var fileName = "   archivo.pdf   ";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_WithLeadingTrailingSpaces_TrimsCorrectly));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("archivo.pdf", result.Data);
        }

        [Fact]
        public void SanitizeFileName_WithControlCharacters_RemovesControlChars()
        {
            // Arrange
            var fileName = "archivo\u0001\u0002\u0003test.pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_WithControlCharacters_RemovesControlChars));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("archivo_test.pdf", result.Data);
        }

        [Fact]
        public void SanitizeFileName_AllDangerousExtensions_ReturnsFailed()
        {
            // Arrange - Probar todas las extensiones peligrosas
            var dangerousExtensions = new[] {
                "bat", "cmd", "com", "pif", "scr", "vbs", "jar",
                "asp", "aspx", "jsp", "py", "rb", "pl", "cgi",
                "dll", "so", "dylib", "app", "deb", "rpm", "msi", "dmg"
            };

            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            foreach (var ext in dangerousExtensions)
            {
                var fileName = $"archivo.{ext}.pdf";

                // Act
                var result = FileValidator.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_AllDangerousExtensions_ReturnsFailed));

                // Assert
                Assert.False(result.Success, $"IsFailed for extension: {ext}");
                Assert.Equal("FILE_SAN_05", result.ErrorCode);
            }
        }

        [Fact]
        public void SanitizeFileName_AllReservedNames_ReturnsFailed()
        {
            // Arrange - Probar todos los nombres reservados
            var reservedNames = new[] {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            foreach (var reserved in reservedNames)
            {
                var fileName = $"{reserved}.pdf";

                // Act
                var result = FileValidator.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_AllReservedNames_ReturnsFailed));

                // Assert
                Assert.False(result.Success, $"IsFailed for reserved name: {reserved}");
                Assert.Equal("FILE_SAN_07", result.ErrorCode);
            }
        }

        [Fact]
        public void SanitizeFileName_ReservedNameCaseInsensitive_ReturnsFailed()
        {
            // Arrange - Probar que la validación es case-insensitive
            var testCases = new[] { "con.pdf", "Con.pdf", "CON.pdf", "cOn.pdf" };
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            foreach (var fileName in testCases)
            {
                // Act
                var result = FileValidator.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_ReservedNameCaseInsensitive_ReturnsFailed));

                // Assert
                Assert.False(result.Success, $"IsFailed for: {fileName}");
                Assert.Equal("FILE_SAN_07", result.ErrorCode);
            }
        }

        [Fact]
        public void SanitizeFileName_WithMultipleUnderscores_CollapsesToSingle()
        {
            // Arrange
            var fileName = "archivo___con___muchos___guiones.pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidator.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_WithMultipleUnderscores_CollapsesToSingle));

            // Assert
            Assert.True(result.Success);
            // Los guiones bajos múltiples deberían colapsar a uno solo
            Assert.DoesNotContain("__", result.Data);
        }

        #endregion
    }
}
