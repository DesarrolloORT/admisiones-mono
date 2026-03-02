using AppLogic.Helpers;
using Xunit;

namespace UnitTesting.AppLogic.Helpers
{
    public class FileValidationHelperTests
    {
        [Fact]
        public void ValidatePdfFile_ValidPdf_ReturnsSuccess()
        {
            // Arrange - PDF vÃ¡lido que comienza con %PDF
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
            var fileName = "curriculum.pdf";

            // Act
            var result = FileValidationHelper.ValidatePdfFile(pdfContent, fileName, nameof(ValidatePdfFile_ValidPdf_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void ValidatePdfFile_InvalidMagicBytes_ReturnsFailed()
        {
            // Arrange - Archivo con extensiÃ³n .pdf pero contenido que no es PDF
            var invalidContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // Magic bytes de JPEG
            var fileName = "fake.pdf";

            // Act
            var result = FileValidationHelper.ValidatePdfFile(invalidContent, fileName, nameof(ValidatePdfFile_InvalidMagicBytes_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
            Assert.Contains("no corresponde a un archivo '.pdf' vÃ¡lido", result.Message);
        }

        [Fact]
        public void ValidatePdfFile_WrongExtension_ReturnsFailed()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var fileName = "curriculum.jpg"; // ExtensiÃ³n incorrecta

            // Act
            var result = FileValidationHelper.ValidatePdfFile(pdfContent, fileName, nameof(ValidatePdfFile_WrongExtension_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_04", result.ErrorCode);
            Assert.Contains("no estÃ¡ permitida", result.Message);
        }

        [Fact]
        public void ValidatePdfFile_EmptyFile_ReturnsFailed()
        {
            // Arrange
            var emptyContent = new byte[] { };
            var fileName = "empty.pdf";

            // Act
            var result = FileValidationHelper.ValidatePdfFile(emptyContent, fileName, nameof(ValidatePdfFile_EmptyFile_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_01", result.ErrorCode);
            Assert.Contains("vacÃ­o", result.Message);
        }

        [Fact]
        public void ValidatePdfFile_NullContent_ReturnsFailed()
        {
            // Arrange
            byte[] nullContent = null;
            var fileName = "test.pdf";

            // Act
            var result = FileValidationHelper.ValidatePdfFile(nullContent, fileName, nameof(ValidatePdfFile_NullContent_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_01", result.ErrorCode);
        }

        [Fact]
        public void ValidatePdfFile_NoExtension_ReturnsFailed()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var fileName = "curriculum"; // Sin extensiÃ³n

            // Act
            var result = FileValidationHelper.ValidatePdfFile(pdfContent, fileName, nameof(ValidatePdfFile_NoExtension_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_03", result.ErrorCode);
            Assert.Contains("no tiene extensiÃ³n", result.Message);
        }

        [Fact]
        public void ValidatePdfFile_EmptyFileName_ReturnsFailed()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var fileName = "";

            // Act
            var result = FileValidationHelper.ValidatePdfFile(pdfContent, fileName, nameof(ValidatePdfFile_EmptyFileName_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_02", result.ErrorCode);
        }

        [Fact]
        public void ValidatePdfFile_LargeFile_ReturnsFailed()
        {
            // Arrange - Archivo de 11 MB (mayor al lÃ­mite de 10 MB)
            var largeContent = new byte[11 * 1024 * 1024];
            // Agregar magic bytes vÃ¡lidos al inicio
            largeContent[0] = 0x25; // %
            largeContent[1] = 0x50; // P
            largeContent[2] = 0x44; // D
            largeContent[3] = 0x46; // F
            var fileName = "large.pdf";

            // Act
            var result = FileValidationHelper.ValidatePdfFile(largeContent, fileName, nameof(ValidatePdfFile_LargeFile_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_07", result.ErrorCode);
            Assert.Contains("excede el tamaÃ±o mÃ¡ximo", result.Message);
        }

        [Fact]
        public void ValidateImageFile_ValidJpeg_ReturnsSuccess()
        {
            // Arrange - JPEG vÃ¡lido
            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
            var fileName = "photo.jpg";

            // Act
            var result = FileValidationHelper.ValidateImageFile(jpegContent, fileName, nameof(ValidateImageFile_ValidJpeg_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void ValidateImageFile_ValidPng_ReturnsSuccess()
        {
            // Arrange - PNG vÃ¡lido
            var pngContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var fileName = "image.png";

            // Act
            var result = FileValidationHelper.ValidateImageFile(pngContent, fileName, nameof(ValidateImageFile_ValidPng_ReturnsSuccess));

            // Assert
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
            var result = FileValidationHelper.ValidateImageFile(pdfContent, fileName, nameof(ValidateImageFile_PdfAsImage_ReturnsFailed));

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
            var result = FileValidationHelper.ValidateDocumentFile(pdfContent, fileName, nameof(ValidateDocumentFile_ValidPdf_ReturnsSuccess));

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
            var result = FileValidationHelper.ValidateFile(pdfContent, fileName, whitelist, nameof(ValidateFile_CustomWhitelist_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateFile_NotInWhitelist_ReturnsFailed()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var fileName = "test.pdf";
            var whitelist = new System.Collections.Generic.List<string> { ".jpg", ".png" }; // PDF no estÃ¡ en la whitelist

            // Act
            var result = FileValidationHelper.ValidateFile(pdfContent, fileName, whitelist, nameof(ValidateFile_NotInWhitelist_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_04", result.ErrorCode);
        }

        #region Sanitization Tests

        [Fact]
        public void SanitizePdfFileName_ValidFileName_ReturnsSuccess()
        {
            // Arrange
            var fileName = "curriculum_vitae.pdf";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_ValidFileName_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("curriculum_vitae.pdf", result.Data);
        }

        [Fact]
        public void SanitizePdfFileName_WithInvalidChars_SanitizesCorrectly()
        {
            // Arrange
            var fileName = "my<file>name:test.pdf";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_WithInvalidChars_SanitizesCorrectly));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("my_file_name_test.pdf", result.Data);
        }

        [Fact]
        public void SanitizePdfFileName_MultipleExtensions_ReturnsFailed()
        {
            // Arrange - Intento de ataque con mÃºltiples extensiones
            var fileName = "archivo.php.pdf";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_MultipleExtensions_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_05", result.ErrorCode);
            Assert.Contains("extensiÃ³n potencialmente peligrosa", result.Message);
        }

        [Fact]
        public void SanitizePdfFileName_ExecutableExtensionHidden_ReturnsFailed()
        {
            // Arrange - Intentos de ocultar ejecutables
            var testCases = new[]
            {
                "malware.exe.pdf",
                "script.js.pdf",
                "virus.bat.pdf",
                "trojan.cmd.pdf",
                "backdoor.sh.pdf",
                "exploit.py.pdf"
            };

            foreach (var fileName in testCases)
            {
                // Act
                var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_ExecutableExtensionHidden_ReturnsFailed));

                // Assert
                Assert.False(result.Success, $"IsFailed for: {fileName}");
                Assert.Equal("FILE_SAN_05", result.ErrorCode);
            }
        }

        [Fact]
        public void SanitizePdfFileName_WithDots_RemovesDots()
        {
            // Arrange
            var fileName = "archivo.con.muchos.puntos.pdf";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_WithDots_RemovesDots));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("archivo_con_muchos_puntos.pdf", result.Data);
            // Verificar que solo hay un punto (el de la extensiÃ³n)
            Assert.Equal(1, result.Data.Count(c => c == '.'));
        }

        [Fact]
        public void SanitizePdfFileName_WithSpaces_ReplacesWithUnderscore()
        {
            // Arrange
            var fileName = "mi curriculum vitae.pdf";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_WithSpaces_ReplacesWithUnderscore));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("mi_curriculum_vitae.pdf", result.Data);
        }

        [Fact]
        public void SanitizePdfFileName_EmptyOrNull_ReturnsFailed()
        {
            // Arrange & Act & Assert - Null
            var resultNull = FileValidationHelper.SanitizePdfFileName(null, nameof(SanitizePdfFileName_EmptyOrNull_ReturnsFailed));
            Assert.False(resultNull.Success);
            Assert.Equal("FILE_SAN_01", resultNull.ErrorCode);

            // Empty
            var resultEmpty = FileValidationHelper.SanitizePdfFileName("", nameof(SanitizePdfFileName_EmptyOrNull_ReturnsFailed));
            Assert.False(resultEmpty.Success);
            Assert.Equal("FILE_SAN_01", resultEmpty.ErrorCode);

            // Whitespace
            var resultWhitespace = FileValidationHelper.SanitizePdfFileName("   ", nameof(SanitizePdfFileName_EmptyOrNull_ReturnsFailed));
            Assert.False(resultWhitespace.Success);
            Assert.Equal("FILE_SAN_01", resultWhitespace.ErrorCode);
        }

        [Fact]
        public void SanitizePdfFileName_NoExtension_ReturnsFailed()
        {
            // Arrange
            var fileName = "archivo_sin_extension";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_NoExtension_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_03", result.ErrorCode);
        }

        [Fact]
        public void SanitizePdfFileName_WrongExtension_ReturnsFailed()
        {
            // Arrange
            var fileName = "documento.docx";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_WrongExtension_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_04", result.ErrorCode);
            Assert.Contains("no estÃ¡ permitida", result.Message);
        }

        [Fact]
        public void SanitizePdfFileName_ReservedWindowsName_ReturnsFailed()
        {
            // Arrange
            var reservedNames = new[] { "CON.pdf", "PRN.pdf", "AUX.pdf", "NUL.pdf", "COM1.pdf", "LPT1.pdf" };

            foreach (var fileName in reservedNames)
            {
                // Act
                var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_ReservedWindowsName_ReturnsFailed));

                // Assert
                Assert.False(result.Success, $"IsFailed for: {fileName}");
                Assert.Equal("FILE_SAN_07", result.ErrorCode);
            }
        }

        [Fact]
        public void SanitizePdfFileName_TooLong_ReturnsFailed()
        {
            // Arrange - Nombre de mÃ¡s de 255 caracteres
            var longName = new string('a', 260) + ".pdf"; // 264 caracteres en total

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(longName, nameof(SanitizePdfFileName_TooLong_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_02", result.ErrorCode);
        }

        [Fact]
        public void SanitizePdfFileName_OnlyInvalidChars_ReturnsFailed()
        {
            // Arrange
            var fileName = "<>:|?.pdf";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_OnlyInvalidChars_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_06", result.ErrorCode);
            Assert.Contains("no contiene caracteres vÃ¡lidos", result.Message);
        }

        [Fact]
        public void SanitizePdfFileName_ComplexRealWorldExample_SanitizesCorrectly()
        {
            // Arrange
            var fileName = "CV - Juan PÃ©rez (2024).v2.final.pdf";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileName, nameof(SanitizePdfFileName_ComplexRealWorldExample_SanitizesCorrectly));

            // Assert
            Assert.True(result.Success);
            // Los puntos adicionales y espacios deben ser reemplazados
            Assert.Equal("CV_-_Juan_PÃ©rez_(2024)_v2_final.pdf", result.Data);
            // Verificar que solo hay un punto (el de la extensiÃ³n)
            Assert.Equal(1, result.Data.Count(c => c == '.'));
        }

        #endregion

        #region ValidateFileContentOnly Tests

        [Fact]
        public void ValidateFileContentOnly_ValidPdf_ReturnsSuccess()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            // Act
            var result = FileValidationHelper.ValidateFileContentOnly(pdfContent, nameof(ValidateFileContentOnly_ValidPdf_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.Equal(".pdf", result.Data);
        }

        [Fact]
        public void ValidateFileContentOnly_ValidJpeg_ReturnsSuccess()
        {
            // Arrange
            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            // Act
            var result = FileValidationHelper.ValidateFileContentOnly(jpegContent, nameof(ValidateFileContentOnly_ValidJpeg_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.Equal(".jpg", result.Data);
        }

        [Fact]
        public void ValidateFileContentOnly_NullContent_ReturnsFailed()
        {
            // Arrange
            byte[] nullContent = null;

            // Act
            var result = FileValidationHelper.ValidateFileContentOnly(nullContent, nameof(ValidateFileContentOnly_NullContent_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_CONTENT_01", result.ErrorCode);
            Assert.Contains("vacÃ­o", result.Message);
        }

        [Fact]
        public void ValidateFileContentOnly_EmptyContent_ReturnsFailed()
        {
            // Arrange
            var emptyContent = new byte[] { };

            // Act
            var result = FileValidationHelper.ValidateFileContentOnly(emptyContent, nameof(ValidateFileContentOnly_EmptyContent_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_CONTENT_01", result.ErrorCode);
        }

        [Fact]
        public void ValidateFileContentOnly_TooSmall_ReturnsFailed()
        {
            // Arrange - Solo 3 bytes, menos del mÃ­nimo de 4
            var tooSmallContent = new byte[] { 0x25, 0x50, 0x44 };

            // Act
            var result = FileValidationHelper.ValidateFileContentOnly(tooSmallContent, nameof(ValidateFileContentOnly_TooSmall_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_CONTENT_02", result.ErrorCode);
            Assert.Contains("demasiado pequeÃ±o", result.Message);
        }

        [Fact]
        public void ValidateFileContentOnly_InvalidMagicBytes_ReturnsFailed()
        {
            // Arrange - Contenido que no coincide con ningÃºn magic byte conocido
            var invalidContent = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 };

            // Act
            var result = FileValidationHelper.ValidateFileContentOnly(invalidContent, nameof(ValidateFileContentOnly_InvalidMagicBytes_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_CONTENT_03", result.ErrorCode);
            Assert.Contains("no es un PDF, JPG o JPEG vÃ¡lido", result.Message);
        }

        #endregion

        #region ValidatePdfOrImageContent Tests

        [Fact]
        public void ValidatePdfOrImageContent_ValidPdf_ReturnsSuccess()
        {
            // Arrange
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };

            // Act
            var result = FileValidationHelper.ValidatePdfOrImageContent(pdfContent, nameof(ValidatePdfOrImageContent_ValidPdf_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void ValidatePdfOrImageContent_ValidJpeg_ReturnsSuccess()
        {
            // Arrange
            var jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };

            // Act
            var result = FileValidationHelper.ValidatePdfOrImageContent(jpegContent, nameof(ValidatePdfOrImageContent_ValidJpeg_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void ValidatePdfOrImageContent_InvalidContent_ReturnsFailed()
        {
            // Arrange
            var invalidContent = new byte[] { 0x00, 0x00, 0x00, 0x00 };

            // Act
            var result = FileValidationHelper.ValidatePdfOrImageContent(invalidContent, nameof(ValidatePdfOrImageContent_InvalidContent_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_CONTENT_03", result.ErrorCode);
        }

        [Fact]
        public void ValidatePdfOrImageContent_NullContent_ReturnsFailed()
        {
            // Arrange
            byte[] nullContent = null;

            // Act
            var result = FileValidationHelper.ValidatePdfOrImageContent(nullContent, nameof(ValidatePdfOrImageContent_NullContent_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_CONTENT_01", result.ErrorCode);
        }

        #endregion

        #region SanitizeFileName Overload Tests (Separate Name and Extension)

        [Fact]
        public void SanitizeFileName_SeparateParams_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var fileNameWithoutExt = "curriculum_vitae";
            var extension = ".pdf";
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_ValidInput_ReturnsSuccess));

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
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_ExtensionWithoutDot_ReturnsSuccess));

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
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_EmptyName_ReturnsFailed));

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
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_NullName_ReturnsFailed));

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
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_EmptyExtension_ReturnsFailed));

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
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_NullExtension_ReturnsFailed));

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
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_NotInWhitelist_ReturnsFailed));

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
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_TooLong_ReturnsFailed));

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
            var result = FileValidationHelper.SanitizeFileName(fileNameWithoutExt, extension, allowedExtensions, nameof(SanitizeFileName_SeparateParams_WithDangerousExtension_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_SAN_05", result.ErrorCode);
        }

        [Fact]
        public void SanitizePdfFileName_SeparateParams_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var fileNameWithoutExt = "mi_documento";
            var extension = ".pdf";

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileNameWithoutExt, extension, nameof(SanitizePdfFileName_SeparateParams_ValidInput_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("mi_documento.pdf", result.Data);
        }

        [Fact]
        public void SanitizePdfFileName_SeparateParams_ExtensionWithoutDot_ReturnsSuccess()
        {
            // Arrange
            var fileNameWithoutExt = "documento";
            var extension = "pdf"; // Sin punto

            // Act
            var result = FileValidationHelper.SanitizePdfFileName(fileNameWithoutExt, extension, nameof(SanitizePdfFileName_SeparateParams_ExtensionWithoutDot_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("documento.pdf", result.Data);
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
            var result = FileValidationHelper.ValidateImageFile(jpegExifContent, fileName, nameof(ValidateImageFile_JpegExif_ReturnsSuccess));

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
            var result = FileValidationHelper.ValidateImageFile(jpegCanonContent, fileName, nameof(ValidateImageFile_JpegCanon_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateDocumentFile_ValidDoc_ReturnsSuccess()
        {
            // Arrange - DOC vÃ¡lido
            var docContent = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
            var fileName = "document.doc";

            // Act
            var result = FileValidationHelper.ValidateDocumentFile(docContent, fileName, nameof(ValidateDocumentFile_ValidDoc_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateDocumentFile_ValidDocx_ReturnsSuccess()
        {
            // Arrange - DOCX vÃ¡lido (ZIP)
            var docxContent = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 };
            var fileName = "document.docx";

            // Act
            var result = FileValidationHelper.ValidateDocumentFile(docxContent, fileName, nameof(ValidateDocumentFile_ValidDocx_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateDocumentFile_DocxEmptyZip_ReturnsSuccess()
        {
            // Arrange - DOCX con magic bytes de ZIP vacÃ­o
            var docxEmptyContent = new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00 };
            var fileName = "empty.docx";

            // Act
            var result = FileValidationHelper.ValidateDocumentFile(docxEmptyContent, fileName, nameof(ValidateDocumentFile_DocxEmptyZip_ReturnsSuccess));

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
            var result = FileValidationHelper.ValidateDocumentFile(docxSpannedContent, fileName, nameof(ValidateDocumentFile_DocxSpannedZip_ReturnsSuccess));

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void ValidateFile_FileTooSmallForMagicBytes_ReturnsFailed()
        {
            // Arrange - Archivo muy pequeÃ±o que no puede contener los magic bytes completos
            var tinyContent = new byte[] { 0x25, 0x50 }; // Solo 2 bytes, PDF necesita al menos 4
            var fileName = "tiny.pdf";
            var whitelist = new System.Collections.Generic.List<string> { ".pdf" };

            // Act
            var result = FileValidationHelper.ValidateFile(tinyContent, fileName, whitelist, nameof(ValidateFile_FileTooSmallForMagicBytes_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
        }

        [Fact]
        public void ValidateImageFile_LargeImage_ReturnsFailed()
        {
            // Arrange - Imagen de 6 MB (mayor al lÃ­mite de 5 MB)
            var largeContent = new byte[6 * 1024 * 1024];
            largeContent[0] = 0xFF;
            largeContent[1] = 0xD8;
            largeContent[2] = 0xFF;
            largeContent[3] = 0xE0;
            var fileName = "large.jpg";

            // Act
            var result = FileValidationHelper.ValidateImageFile(largeContent, fileName, nameof(ValidateImageFile_LargeImage_ReturnsFailed));

            // Assert
            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_07", result.ErrorCode);
        }

        [Fact]
        public void ValidateDocumentFile_LargeDoc_ReturnsFailed()
        {
            // Arrange - Documento de 11 MB (mayor al lÃ­mite de 10 MB)
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
            var result = FileValidationHelper.ValidateDocumentFile(largeContent, fileName, nameof(ValidateDocumentFile_LargeDoc_ReturnsFailed));

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
            var result = FileValidationHelper.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_WithMultipleSpaces_CollapsesToSingleUnderscore));

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
            var result = FileValidationHelper.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_WithLeadingTrailingSpaces_TrimsCorrectly));

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
            var result = FileValidationHelper.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_WithControlCharacters_RemovesControlChars));

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
                var result = FileValidationHelper.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_AllDangerousExtensions_ReturnsFailed));

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
                var result = FileValidationHelper.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_AllReservedNames_ReturnsFailed));

                // Assert
                Assert.False(result.Success, $"IsFailed for reserved name: {reserved}");
                Assert.Equal("FILE_SAN_07", result.ErrorCode);
            }
        }

        [Fact]
        public void SanitizeFileName_ReservedNameCaseInsensitive_ReturnsFailed()
        {
            // Arrange - Probar que la validaciÃ³n es case-insensitive
            var testCases = new[] { "con.pdf", "Con.pdf", "CON.pdf", "cOn.pdf" };
            var allowedExtensions = new System.Collections.Generic.List<string> { ".pdf" };

            foreach (var fileName in testCases)
            {
                // Act
                var result = FileValidationHelper.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_ReservedNameCaseInsensitive_ReturnsFailed));

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
            var result = FileValidationHelper.SanitizeFileName(fileName, allowedExtensions, nameof(SanitizeFileName_WithMultipleUnderscores_CollapsesToSingle));

            // Assert
            Assert.True(result.Success);
            // Los guiones bajos mÃºltiples deberÃ­an colapsar a uno solo
            Assert.DoesNotContain("__", result.Data);
        }

        #endregion
    }
}
