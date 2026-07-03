using Utilities;
using Xunit;

namespace UnitTesting.Utilities
{
    public class FileValidatorTests
    {
        [Theory]
        [InlineData("image.jpg")]
        [InlineData("image.exe")]
        [InlineData("image")]
        public void ValidateFile_PngBytesIgnoresFileNameExtension_ReturnsSuccess(string fileName)
        {
            var pngContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var whitelist = new List<string> { ".jpg", ".png" };

            var result = FileValidator.ValidateFile(pngContent, fileName, whitelist, nameof(ValidateFile_PngBytesIgnoresFileNameExtension_ReturnsSuccess));

            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void ValidateFile_PdfBytesWithImageWhitelist_ReturnsFileVal06()
        {
            var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var whitelist = new List<string> { ".jpg", ".png" };

            var result = FileValidator.ValidateFile(pdfContent, "test.pdf", whitelist, nameof(ValidateFile_PdfBytesWithImageWhitelist_ReturnsFileVal06));

            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
        }

        [Fact]
        public void ValidateFile_EmptyContent_ReturnsFileVal01()
        {
            var whitelist = new List<string> { ".pdf", ".jpg", ".png" };

            var result = FileValidator.ValidateFile(Array.Empty<byte>(), "documento.jpg", whitelist, nameof(ValidateFile_EmptyContent_ReturnsFileVal01));

            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_01", result.ErrorCode);
        }

        [Fact]
        public void ValidateFile_InvalidBytes_ReturnsFileVal06()
        {
            var whitelist = new List<string> { ".pdf", ".jpg", ".png" };

            var result = FileValidator.ValidateFile(new byte[] { 0x01, 0x02, 0x03, 0x04 }, "documento.jpg", whitelist, nameof(ValidateFile_InvalidBytes_ReturnsFileVal06));

            Assert.False(result.Success);
            Assert.Equal("FILE_VAL_06", result.ErrorCode);
        }
    }
}
