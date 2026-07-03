using Utilities;

namespace AppLogic.Helpers.ValidationHelpers
{
    public static class FileValidationHelper
    {
        public static OperationResult<bool> ValidateFile(
            byte[] fileContent,
            string fileName,
            List<string> allowedExtensions,
            string originMethod)
        {
            return FileValidator.ValidateFile(fileContent, fileName, allowedExtensions, originMethod);
        }

        public static OperationResult<bool> ValidateImageFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            return FileValidator.ValidateImageFile(fileContent, fileName, originMethod);
        }

        public static OperationResult<bool> ValidateDeclaracionJuradaAttachment(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            return FileValidator.ValidateDeclaracionJuradaAttachment(fileContent, fileName, originMethod);
        }

        public static OperationResult<bool> ValidateDocumentFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            return FileValidator.ValidateDocumentFile(fileContent, fileName, originMethod);
        }

        public static OperationResult<bool> ValidateIdentityDocumentFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            return FileValidator.ValidateFile(
                fileContent,
                fileName,
                new List<string> { ".pdf", ".jpg", ".jpeg", ".png" },
                originMethod);
        }

        public static OperationResult<string> SanitizeFileName(
            string fileName,
            List<string> allowedExtensions,
            string originMethod)
        {
            return FileValidator.SanitizeFileName(fileName, allowedExtensions, originMethod);
        }

        public static OperationResult<string> SanitizeFileName(
            string fileNameWithoutExtension,
            string extension,
            List<string> allowedExtensions,
            string originMethod)
        {
            return FileValidator.SanitizeFileName(fileNameWithoutExtension, extension, allowedExtensions, originMethod);
        }

        public static OperationResult<string> SanitizeDeclaracionJuradaAttachmentName(string fileName, string originMethod)
        {
            return FileValidator.SanitizeDeclaracionJuradaAttachmentName(fileName, originMethod);
        }
    }
}
