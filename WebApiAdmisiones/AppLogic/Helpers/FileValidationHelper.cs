using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Utilities;

namespace AppLogic.Helpers
{
    /// <summary>
    /// Helper para validaciÃ³n de archivos subidos por usuarios.
    /// Implementa mÃºltiples capas de seguridad:
    /// - ValidaciÃ³n de extensiÃ³n mediante whitelist estricta
    /// - ValidaciÃ³n de contenido mediante magic bytes
    /// - ValidaciÃ³n de tamaÃ±o de archivo
    /// - SanitizaciÃ³n de nombres de archivo
    /// </summary>
    public static class FileValidationHelper
    {
        // Caracteres no permitidos en nombres de archivo (Windows y Linux)
        private static readonly char[] InvalidFileNameChars = new char[]
        {
            '<', '>', ':', '"', '/', '\\', '|', '?', '*',
            '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007',
            '\u0008', '\u0009', '\u000a', '\u000b', '\u000c', '\u000d', '\u000e', '\u000f',
            '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017',
            '\u0018', '\u0019', '\u001a', '\u001b', '\u001c', '\u001d', '\u001e', '\u001f'
        };

        // Nombres de archivo reservados en Windows
        private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        // Extensiones permitidas para validaciÃ³n de contenido (PDF, JPG, JPEG)
        private static readonly string[] DefaultContentValidationExtensions = { ".pdf", ".jpg" };

        // Magic bytes para diferentes tipos de archivos
        private static readonly Dictionary<string, List<byte[]>> MagicBytes = new()
        {
            // PDF - Comienza con %PDF
            { ".pdf", new List<byte[]> 
                { 
                    new byte[] { 0x25, 0x50, 0x44, 0x46 } // %PDF
                } 
            },
            // JPEG - Tiene varios posibles magic bytes
            { ".jpg", new List<byte[]> 
                { 
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, // JFIF
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, // Exif
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 }  // Canon
                } 
            },
            { ".jpeg", new List<byte[]> 
                { 
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 }
                } 
            },
            // PNG - Comienza con 89 50 4E 47
            { ".png", new List<byte[]> 
                { 
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
                } 
            },
            // DOC - Comienza con D0 CF 11 E0 A1 B1 1A E1
            { ".doc", new List<byte[]> 
                { 
                    new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }
                } 
            },
            // DOCX - Es un archivo ZIP
            { ".docx", new List<byte[]> 
                { 
                    new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // ZIP
                    new byte[] { 0x50, 0x4B, 0x05, 0x06 }, // ZIP vacÃ­o
                    new byte[] { 0x50, 0x4B, 0x07, 0x08 }  // ZIP spanned
                } 
            }
        };

        // TamaÃ±os mÃ¡ximos por tipo de archivo (en bytes)
        private static readonly Dictionary<string, long> MaxFileSizes = new()
        {
            { ".pdf", 10 * 1024 * 1024 },    // 10 MB para PDFs
            { ".jpg", 5 * 1024 * 1024 },     // 5 MB para imÃ¡genes
            { ".jpeg", 5 * 1024 * 1024 },    // 5 MB para imÃ¡genes
            { ".png", 5 * 1024 * 1024 },     // 5 MB para imÃ¡genes
            { ".doc", 10 * 1024 * 1024 },    // 10 MB para documentos
            { ".docx", 10 * 1024 * 1024 }    // 10 MB para documentos
        };

        /// <summary>
        /// Valida un archivo basÃ¡ndose en extensiÃ³n, magic bytes y tamaÃ±o.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="fileName">Nombre del archivo incluyendo extensiÃ³n.</param>
        /// <param name="allowedExtensions">Lista de extensiones permitidas (whitelist). Debe incluir el punto, ej: ".pdf"</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult indicando si el archivo es vÃ¡lido.</returns>
        public static OperationResult<bool> ValidateFile(
            byte[] fileContent, 
            string fileName, 
            List<string> allowedExtensions,
            string originMethod)
        {
            // ValidaciÃ³n 1: Verificar que el contenido no estÃ© vacÃ­o
            var contentValidation = ValidateFileContent(fileContent, originMethod);
            if (!contentValidation.Success)
            {
                return contentValidation;
            }

            // ValidaciÃ³n 2: Verificar que el nombre del archivo sea vÃ¡lido
            var fileNameValidation = ValidateFileNameNotEmpty(fileName, originMethod);
            if (!fileNameValidation.Success)
            {
                return fileNameValidation;
            }

            // Obtener y validar la extensiÃ³n del archivo
            var extensionResult = GetAndValidateExtension(fileName, allowedExtensions, originMethod);
            if (!extensionResult.Success)
            {
                return OperationResult<bool>.IsFailed(
                    extensionResult.ErrorCode,
                    originMethod,
                    extensionResult.Message,
                    extensionResult.HttpCode);
            }

            var extension = extensionResult.Data;

            // ValidaciÃ³n adicional: Verificar que la extensiÃ³n no sea nula (defensa en profundidad)
            if (string.IsNullOrWhiteSpace(extension))
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_08",
                    originMethod,
                    "Error interno: la extensiÃ³n del archivo no pudo ser determinada.",
                    500);
            }

            // ValidaciÃ³n 4: Verificar magic bytes (segunda capa de seguridad - validaciÃ³n de contenido)
            var magicBytesValidation = ValidateMagicBytes(fileContent, extension, originMethod);
            if (!magicBytesValidation.Success)
            {
                return magicBytesValidation;
            }

            // ValidaciÃ³n 5: Verificar tamaÃ±o mÃ¡ximo del archivo
            var sizeValidation = ValidateFileSize(fileContent, extension, originMethod);
            if (!sizeValidation.Success)
            {
                return sizeValidation;
            }

            // Todas las validaciones pasaron
            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Valida que el contenido del archivo no estÃ© vacÃ­o.
        /// </summary>
        private static OperationResult<bool> ValidateFileContent(byte[] fileContent, string originMethod)
        {
            if (fileContent == null || fileContent.Length == 0)
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_01",
                    originMethod,
                    "El archivo estÃ¡ vacÃ­o o no se pudo leer.",
                    400);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Valida que el nombre del archivo no estÃ© vacÃ­o.
        /// </summary>
        private static OperationResult<bool> ValidateFileNameNotEmpty(string fileName, string originMethod)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_02",
                    originMethod,
                    "El nombre del archivo es invÃ¡lido.",
                    400);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Obtiene y valida la extensiÃ³n del archivo contra la whitelist.
        /// </summary>
        private static OperationResult<string> GetAndValidateExtension(
            string fileName,
            List<string> allowedExtensions,
            string originMethod)
        {
            var extension = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_VAL_03",
                    originMethod,
                    "El archivo no tiene extensiÃ³n.",
                    400);
            }

            // ValidaciÃ³n 3: Verificar extensiÃ³n contra whitelist (primera capa de seguridad)
            if (!allowedExtensions.Contains(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_VAL_04",
                    originMethod,
                    $"La extensiÃ³n '{extension}' no estÃ¡ permitida. Solo se permiten: {string.Join(", ", allowedExtensions)}",
                    400);
            }

            return OperationResult<string>.Ok(extension, originMethod);
        }

        /// <summary>
        /// Valida que el contenido del archivo coincida con los magic bytes esperados para la extensiÃ³n.
        /// </summary>
        private static OperationResult<bool> ValidateMagicBytes(
            byte[] fileContent,
            string extension,
            string originMethod)
        {
            if (!MagicBytes.TryGetValue(extension, out var validMagicBytes))
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_05",
                    originMethod,
                    $"No hay configuraciÃ³n de validaciÃ³n para la extensiÃ³n '{extension}'.",
                    500);
            }

            var magicBytesMatch = CheckMagicBytesMatch(fileContent, validMagicBytes);

            if (!magicBytesMatch)
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_06",
                    originMethod,
                    $"El contenido del archivo no corresponde a un archivo '{extension}' vÃ¡lido. Posible intento de suplantaciÃ³n de tipo de archivo.",
                    400);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Verifica si el contenido del archivo coincide con alguno de los magic bytes vÃ¡lidos.
        /// </summary>
        private static bool CheckMagicBytesMatch(byte[] fileContent, List<byte[]> validMagicBytes)
        {
            return validMagicBytes.Any(magicByte => IsMagicByteMatch(fileContent, magicByte));
        }

        /// <summary>
        /// Compara el contenido del archivo con una secuencia especÃ­fica de magic bytes.
        /// </summary>
        private static bool IsMagicByteMatch(byte[] fileContent, byte[] magicByte)
        {
            if (fileContent.Length < magicByte.Length)
            {
                return false;
            }

            for (int i = 0; i < magicByte.Length; i++)
            {
                if (fileContent[i] != magicByte[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Valida que el tamaÃ±o del archivo no exceda el mÃ¡ximo permitido para su extensiÃ³n.
        /// </summary>
        private static OperationResult<bool> ValidateFileSize(
            byte[] fileContent,
            string extension,
            string originMethod)
        {
            if (MaxFileSizes.TryGetValue(extension, out long maxSize) && fileContent.Length > maxSize)
            {
                var maxSizeMB = maxSize / (1024.0 * 1024.0);
                var currentSizeMB = fileContent.Length / (1024.0 * 1024.0);
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_07",
                    originMethod,
                    $"El archivo excede el tamaÃ±o mÃ¡ximo permitido. TamaÃ±o actual: {currentSizeMB:F2} MB, mÃ¡ximo: {maxSizeMB:F2} MB",
                    400);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Valida especÃ­ficamente un archivo PDF.
        /// MÃ©todo de conveniencia que llama a ValidateFile con la whitelist de PDF.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo PDF en bytes.</param>
        /// <param name="fileName">Nombre del archivo incluyendo extensiÃ³n.</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult indicando si el PDF es vÃ¡lido.</returns>
        public static OperationResult<bool> ValidatePdfFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            var allowedExtensions = new List<string> { ".pdf" };
            return ValidateFile(fileContent, fileName, allowedExtensions, originMethod);
        }

        /// <summary>
        /// Valida archivos de imagen (JPEG, PNG).
        /// </summary>
        /// <param name="fileContent">Contenido del archivo de imagen en bytes.</param>
        /// <param name="fileName">Nombre del archivo incluyendo extensiÃ³n.</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult indicando si la imagen es vÃ¡lida.</returns>
        public static OperationResult<bool> ValidateImageFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            var allowedExtensions = new List<string> { ".jpg", ".jpeg", ".png" };
            return ValidateFile(fileContent, fileName, allowedExtensions, originMethod);
        }

        /// <summary>
        /// Valida archivos de documentos (PDF, DOC, DOCX).
        /// </summary>
        /// <param name="fileContent">Contenido del archivo de documento en bytes.</param>
        /// <param name="fileName">Nombre del archivo incluyendo extensiÃ³n.</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult indicando si el documento es vÃ¡lido.</returns>
        public static OperationResult<bool> ValidateDocumentFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            var allowedExtensions = new List<string> { ".pdf", ".doc", ".docx" };
            return ValidateFile(fileContent, fileName, allowedExtensions, originMethod);
        }

        /// <summary>
        /// Valida el contenido de un archivo (magic bytes) sin validar el nombre.
        /// Ãštil cuando solo se tiene el contenido del archivo y se necesita verificar que sea de un tipo especÃ­fico.
        /// Valida que el contenido coincida con PDF, JPG o JPEG.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult indicando si el contenido del archivo es vÃ¡lido y quÃ© tipo de archivo es.</returns>
        public static OperationResult<string> ValidateFileContentOnly(
            byte[] fileContent,
            string originMethod)
        {
            // ValidaciÃ³n 1: Verificar que el contenido no estÃ© vacÃ­o
            if (fileContent == null || fileContent.Length == 0)
            {
                return OperationResult<string>.IsFailed(
                    "FILE_CONTENT_01",
                    originMethod,
                    "El archivo estÃ¡ vacÃ­o o no se pudo leer.",
                    400);
            }

            // ValidaciÃ³n 2: Verificar que tenga suficientes bytes para validar magic bytes
            if (fileContent.Length < 4)
            {
                return OperationResult<string>.IsFailed(
                    "FILE_CONTENT_02",
                    originMethod,
                    "El archivo es demasiado pequeÃ±o para ser vÃ¡lido.",
                    400);
            }

            // ValidaciÃ³n 3: Detectar tipo de archivo por magic bytes
            var detectedType = DetectFileTypeByMagicBytes(fileContent, DefaultContentValidationExtensions);
            
            if (detectedType != null)
            {
                return OperationResult<string>.Ok(detectedType, originMethod);
            }

            // Si no coincide con ningÃºn magic byte conocido
            return OperationResult<string>.IsFailed(
                "FILE_CONTENT_03",
                originMethod,
                "El archivo no es un PDF, JPG o JPEG vÃ¡lido. El tipo de archivo no estÃ¡ permitido.",
                400);
        }

        /// <summary>
        /// Detecta el tipo de archivo basÃ¡ndose en sus magic bytes.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="extensionsToCheck">Lista de extensiones a verificar.</param>
        /// <returns>La extensiÃ³n detectada o null si no coincide con ninguna.</returns>
        private static string? DetectFileTypeByMagicBytes(byte[] fileContent, string[] extensionsToCheck)
        {
            return extensionsToCheck.FirstOrDefault(extension => TryMatchExtension(fileContent, extension));
        }

        /// <summary>
        /// Intenta hacer match del contenido del archivo con los magic bytes de una extensiÃ³n especÃ­fica.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="extension">ExtensiÃ³n a verificar (ej: ".pdf", ".jpg").</param>
        /// <returns>True si el contenido coincide con algÃºn magic byte de la extensiÃ³n.</returns>
        private static bool TryMatchExtension(byte[] fileContent, string extension)
        {
            if (!MagicBytes.TryGetValue(extension, out var magicBytesList))
            {
                return false;
            }

            return CheckMagicBytesMatch(fileContent, magicBytesList);
        }

        /// <summary>
        /// Valida el contenido de un archivo (magic bytes) para formatos PDF, JPG o JPEG sin validar el nombre.
        /// MÃ©todo de conveniencia que valida el contenido y devuelve bool.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult con bool indicando si el archivo es vÃ¡lido.</returns>
        public static OperationResult<bool> ValidatePdfOrImageContent(
            byte[] fileContent,
            string originMethod)
        {
            var validationResult = ValidateFileContentOnly(fileContent, originMethod);
            if (!validationResult.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validationResult.ErrorCode,
                    originMethod,
                    validationResult.Message,
                    validationResult.HttpCode);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre de un archivo removiendo caracteres peligrosos y validando contra mÃºltiples extensiones.
        /// Previene ataques como 'archivo.php.pdf' detectando mÃºltiples puntos que podrÃ­an indicar extensiones maliciosas ocultas.
        /// </summary>
        /// <param name="fileName">Nombre del archivo original.</param>
        /// <param name="allowedExtensions">Lista de extensiones permitidas (con punto, ej: ".pdf").</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado o error si la validaciÃ³n falla.</returns>
        public static OperationResult<string> SanitizeFileName(
            string fileName,
            List<string> allowedExtensions,
            string originMethod)
        {
            // Validar que el nombre no sea nulo o vacÃ­o
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_01",
                    originMethod,
                    "El nombre del archivo no puede estar vacÃ­o.",
                    400);
            }

            // Trim espacios al inicio y final
            fileName = fileName.Trim();

            // Validar longitud mÃ¡xima (255 caracteres es el lÃ­mite comÃºn)
            if (fileName.Length > 255)
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_02",
                    originMethod,
                    "El nombre del archivo es demasiado largo. MÃ¡ximo 255 caracteres.",
                    400);
            }

            // Obtener extensiÃ³n actual
            var extension = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_03",
                    originMethod,
                    "El archivo debe tener una extensiÃ³n vÃ¡lida.",
                    400);
            }

            // Validar que la extensiÃ³n estÃ© en la whitelist
            if (!allowedExtensions.Contains(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_04",
                    originMethod,
                    $"La extensiÃ³n '{extension}' no estÃ¡ permitida. Solo se permiten: {string.Join(", ", allowedExtensions)}",
                    400);
            }

            // Obtener nombre sin extensiÃ³n
            var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

            // Validar y sanitizar el nombre del archivo
            return ProcessFileNameSanitization(fileNameWithoutExtension, extension, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre de un archivo cuando el nombre y la extensiÃ³n vienen por separado.
        /// Previene ataques detectando extensiones peligrosas ocultas en el nombre del archivo.
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo SIN extensiÃ³n.</param>
        /// <param name="extension">ExtensiÃ³n del archivo (con o sin punto inicial, ej: ".pdf" o "pdf").</param>
        /// <param name="allowedExtensions">Lista de extensiones permitidas (con punto, ej: ".pdf").</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado (nombre + extensiÃ³n) o error si la validaciÃ³n falla.</returns>
        public static OperationResult<string> SanitizeFileName(
            string fileNameWithoutExtension,
            string extension,
            List<string> allowedExtensions,
            string originMethod)
        {
            // Validar que el nombre no sea nulo o vacÃ­o
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_01",
                    originMethod,
                    "El nombre del archivo no puede estar vacÃ­o.",
                    400);
            }

            // Validar que la extensiÃ³n no sea nula o vacÃ­a
            if (string.IsNullOrWhiteSpace(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_03",
                    originMethod,
                    "La extensiÃ³n del archivo no puede estar vacÃ­a.",
                    400);
            }

            // Normalizar la extensiÃ³n: asegurar que tenga punto inicial y estÃ© en minÃºsculas
            if (!extension.StartsWith('.'))
            {
                extension = "." + extension;
            }
            extension = extension.ToLowerInvariant();

            // Validar que la extensiÃ³n estÃ© en la whitelist
            if (!allowedExtensions.Contains(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_04",
                    originMethod,
                    $"La extensiÃ³n '{extension}' no estÃ¡ permitida. Solo se permiten: {string.Join(", ", allowedExtensions)}",
                    400);
            }

            // Trim espacios al inicio y final del nombre
            fileNameWithoutExtension = fileNameWithoutExtension.Trim();

            // Validar longitud mÃ¡xima del nombre completo (255 caracteres es el lÃ­mite comÃºn)
            if (fileNameWithoutExtension.Length + extension.Length > 255)
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_02",
                    originMethod,
                    "El nombre del archivo es demasiado largo. MÃ¡ximo 255 caracteres en total.",
                    400);
            }

            // Validar y sanitizar el nombre del archivo
            return ProcessFileNameSanitization(fileNameWithoutExtension, extension, originMethod);
        }

        /// <summary>
        /// Procesa la sanitizaciÃ³n del nombre de archivo detectando extensiones peligrosas,
        /// removiendo caracteres no permitidos y validando contra nombres reservados.
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo sin extensiÃ³n</param>
        /// <param name="extension">ExtensiÃ³n del archivo (con punto inicial y en minÃºsculas)</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n</param>
        /// <returns>OperationResult con el nombre sanitizado o error si la validaciÃ³n falla</returns>
        private static OperationResult<string> ProcessFileNameSanitization(
            string fileNameWithoutExtension,
            string extension,
            string originMethod)
        {
            // SEGURIDAD CRÃTICA: Detectar extensiones peligrosas ocultas en el nombre
            var dangerousExtensionCheck = CheckForDangerousExtensions(fileNameWithoutExtension, originMethod);
            if (!dangerousExtensionCheck.Success)
            {
                return dangerousExtensionCheck;
            }

            // Sanitizar el nombre removiendo caracteres no permitidos
            var sanitizedName = SanitizeFileNameCharacters(fileNameWithoutExtension);

            // Validar que despuÃ©s de la sanitizaciÃ³n quede algo
            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_06",
                    originMethod,
                    "El nombre del archivo no contiene caracteres vÃ¡lidos despuÃ©s de la sanitizaciÃ³n.",
                    400);
            }

            // Validar contra nombres reservados de Windows
            var reservedNameCheck = CheckForReservedNames(sanitizedName, originMethod);
            if (!reservedNameCheck.Success)
            {
                return reservedNameCheck;
            }

            // Construir el nombre final sanitizado con la extensiÃ³n
            var sanitizedFileName = $"{sanitizedName}{extension}";

            return OperationResult<string>.Ok(sanitizedFileName, originMethod);
        }

        /// <summary>
        /// Detecta si el nombre del archivo contiene extensiones potencialmente peligrosas ocultas.
        /// Ejemplo: "archivo.php" en "archivo.php.pdf"
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo sin la extensiÃ³n final</param>
        /// <param name="originMethod">MÃ©todo que invoca la validaciÃ³n</param>
        /// <returns>OperationResult indicando si se detectÃ³ una extensiÃ³n peligrosa</returns>
        private static OperationResult<string> CheckForDangerousExtensions(
            string fileNameWithoutExtension,
            string originMethod)
        {
            var dotsInFileName = fileNameWithoutExtension.Count(c => c == '.');
            if (dotsInFileName > 0)
            {
                // Verificar si alguno de los segmentos entre puntos parece una extensiÃ³n de archivo ejecutable o script
                var segments = fileNameWithoutExtension.Split('.');
                var dangerousExtensions = new[] {
                    "exe", "bat", "cmd", "com", "pif", "scr", "vbs", "js", "jar",
                    "php", "asp", "aspx", "jsp", "py", "rb", "sh", "pl", "cgi",
                    "dll", "so", "dylib", "app", "deb", "rpm", "msi", "dmg"
                };

                var dangerousSegment = segments.FirstOrDefault(segment => 
                    dangerousExtensions.Contains(segment.ToLowerInvariant()));
                
                if (dangerousSegment != null)
                {
                    return OperationResult<string>.IsFailed(
                        "FILE_SAN_05",
                        originMethod,
                        $"El nombre del archivo contiene una extensiÃ³n potencialmente peligrosa: '{dangerousSegment}'. " +
                        $"No se permiten extensiones que puedan ocultar archivos ejecutables o scripts.",
                        400);
                }
            }

            return OperationResult<string>.Ok(fileNameWithoutExtension, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre del archivo removiendo caracteres no permitidos,
        /// puntos adicionales, espacios mÃºltiples y guiones bajos consecutivos.
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo sin extensiÃ³n</param>
        /// <returns>Nombre sanitizado</returns>
        private static string SanitizeFileNameCharacters(string fileNameWithoutExtension)
        {
            var sanitizedName = fileNameWithoutExtension;

            // Remover caracteres no permitidos en nombres de archivo
            foreach (var invalidChar in InvalidFileNameChars)
            {
                sanitizedName = sanitizedName.Replace(invalidChar.ToString(), "_");
            }

            // Remover puntos adicionales del nombre (convertirlos en guiones bajos)
            sanitizedName = sanitizedName.Replace(".", "_");

            // Remover espacios mÃºltiples y reemplazar espacios por guiones bajos
            sanitizedName = Regex.Replace(sanitizedName, @"\s+", "_", RegexOptions.None, TimeSpan.FromMilliseconds(100));

            // Remover guiones bajos mÃºltiples consecutivos
            sanitizedName = Regex.Replace(sanitizedName, @"_{2,}", "_", RegexOptions.None, TimeSpan.FromMilliseconds(100));

            // Remover guiones bajos al inicio y final
            sanitizedName = sanitizedName.Trim('_');

            return sanitizedName;
        }

        /// <summary>
        /// Verifica si el nombre del archivo corresponde a un nombre reservado del sistema Windows.
        /// </summary>
        /// <param name="sanitizedName">Nombre sanitizado a validar</param>
        /// <param name="originMethod">MÃ©todo que invoca la validaciÃ³n</param>
        /// <returns>OperationResult indicando si el nombre es vÃ¡lido</returns>
        private static OperationResult<string> CheckForReservedNames(
            string sanitizedName,
            string originMethod)
        {
            var nameToCheck = sanitizedName.Split('.')[0]; // Tomar solo el primer segmento
            if (ReservedFileNames.Contains(nameToCheck))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_07",
                    originMethod,
                    $"El nombre '{nameToCheck}' es un nombre reservado del sistema y no puede ser usado.",
                    400);
            }

            return OperationResult<string>.Ok(sanitizedName, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre de un archivo PDF.
        /// MÃ©todo de conveniencia para archivos PDF.
        /// </summary>
        /// <param name="fileName">Nombre del archivo original.</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado.</returns>
        public static OperationResult<string> SanitizePdfFileName(string fileName, string originMethod)
        {
            return SanitizeFileName(fileName, new List<string> { ".pdf" }, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre de un archivo PDF cuando el nombre y la extensiÃ³n vienen por separado.
        /// MÃ©todo de conveniencia para archivos PDF.
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo SIN extensiÃ³n.</param>
        /// <param name="extension">ExtensiÃ³n del archivo (con o sin punto inicial).</param>
        /// <param name="originMethod">Nombre del mÃ©todo que invoca esta validaciÃ³n.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado.</returns>
        public static OperationResult<string> SanitizePdfFileName(
            string fileNameWithoutExtension, 
            string extension, 
            string originMethod)
        {
            return SanitizeFileName(fileNameWithoutExtension, extension, new List<string> { ".pdf" }, originMethod);
        }
    }
}
