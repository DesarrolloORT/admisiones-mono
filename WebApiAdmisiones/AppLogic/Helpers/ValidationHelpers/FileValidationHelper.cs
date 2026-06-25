using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Utilities;

namespace AppLogic.Helpers.ValidationHelpers
{
    /// <summary>
    /// Helper para validación de archivos subidos por usuarios.
    /// Implementa múltiples capas de seguridad:
    /// - Validación de extensión mediante whitelist estricta
    /// - Validación de contenido mediante magic bytes
    /// - Validación de tamaño de archivo
    /// - Sanitización de nombres de archivo
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

        private const string JpegExtension = ".jpeg";

        // Extensiones permitidas para validación de contenido (PDF, JPG, JPEG)
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
            { JpegExtension, new List<byte[]> 
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
                    new byte[] { 0x50, 0x4B, 0x05, 0x06 }, // ZIP vacío
                    new byte[] { 0x50, 0x4B, 0x07, 0x08 }  // ZIP spanned
                } 
            },
            { ".xls", new List<byte[]>
                {
                    new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }
                }
            },
            { ".xlsx", new List<byte[]>
                {
                    new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                    new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                    new byte[] { 0x50, 0x4B, 0x07, 0x08 }
                }
            },
            { ".ppt", new List<byte[]>
                {
                    new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }
                }
            },
            { ".pptx", new List<byte[]>
                {
                    new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                    new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                    new byte[] { 0x50, 0x4B, 0x07, 0x08 }
                }
            }
        };

        // Tamaños máximos por tipo de archivo (en bytes)
        private static readonly Dictionary<string, long> MaxFileSizes = new()
        {
            { ".pdf", 10 * 1024 * 1024 },    // 10 MB para PDFs
            { ".jpg", 5 * 1024 * 1024 },     // 5 MB para imágenes
            { JpegExtension, 5 * 1024 * 1024 },    // 5 MB para imágenes
            { ".png", 5 * 1024 * 1024 },     // 5 MB para imágenes
            { ".doc", 10 * 1024 * 1024 },    // 10 MB para documentos
            { ".docx", 10 * 1024 * 1024 },   // 10 MB para documentos
            { ".xls", 10 * 1024 * 1024 },    // 10 MB para planillas
            { ".xlsx", 10 * 1024 * 1024 },   // 10 MB para planillas
            { ".ppt", 10 * 1024 * 1024 },    // 10 MB para presentaciones
            { ".pptx", 10 * 1024 * 1024 }    // 10 MB para presentaciones
        };

        /// <summary>
        /// Valida un archivo basándose en extensión, magic bytes y tamaño.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="fileName">Nombre del archivo incluyendo extensión.</param>
        /// <param name="allowedExtensions">Lista de extensiones permitidas (whitelist). Debe incluir el punto, ej: ".pdf"</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult indicando si el archivo es válido.</returns>
        public static OperationResult<bool> ValidateFile(
            byte[] fileContent, 
            string fileName, 
            List<string> allowedExtensions,
            string originMethod)
        {
            // Validación 1: Verificar que el contenido no esté vacío
            var contentValidation = ValidateFileContent(fileContent, originMethod);
            if (!contentValidation.Success)
            {
                return contentValidation;
            }

            // Validación 2: Verificar que el nombre del archivo sea válido
            var fileNameValidation = ValidateFileNameNotEmpty(fileName, originMethod);
            if (!fileNameValidation.Success)
            {
                return fileNameValidation;
            }

            // Obtener y validar la extensión del archivo
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

            // Validación adicional: Verificar que la extensión no sea nula (defensa en profundidad)
            if (string.IsNullOrWhiteSpace(extension))
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_08",
                    originMethod,
                    "Error interno: la extensión del archivo no pudo ser determinada.",
                    500);
            }

            // Validación 4: Verificar magic bytes (segunda capa de seguridad - validación de contenido)
            var magicBytesValidation = ValidateMagicBytes(fileContent, extension, originMethod);
            if (!magicBytesValidation.Success)
            {
                return magicBytesValidation;
            }

            // Validación 5: Verificar tamaño máximo del archivo
            var sizeValidation = ValidateFileSize(fileContent, extension, originMethod);
            if (!sizeValidation.Success)
            {
                return sizeValidation;
            }

            // Todas las validaciones pasaron
            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Valida que el contenido del archivo no esté vacío.
        /// </summary>
        private static OperationResult<bool> ValidateFileContent(byte[] fileContent, string originMethod)
        {
            if (fileContent == null || fileContent.Length == 0)
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_01",
                    originMethod,
                    "El archivo está vacío o no se pudo leer.",
                    400);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Valida que el nombre del archivo no esté vacío.
        /// </summary>
        private static OperationResult<bool> ValidateFileNameNotEmpty(string fileName, string originMethod)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_02",
                    originMethod,
                    "El nombre del archivo es inválido.",
                    400);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Obtiene y valida la extensión del archivo contra la whitelist.
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
                    "El archivo no tiene extensión.",
                    400);
            }

            // Validación 3: Verificar extensión contra whitelist (primera capa de seguridad)
            if (!allowedExtensions.Contains(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_VAL_04",
                    originMethod,
                    $"La extensión '{extension}' no está permitida. Solo se permiten: {string.Join(", ", allowedExtensions)}",
                    400);
            }

            return OperationResult<string>.Ok(extension, originMethod);
        }

        /// <summary>
        /// Valida que el contenido del archivo coincida con los magic bytes esperados para la extensión.
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
                    $"No hay configuración de validación para la extensión '{extension}'.",
                    500);
            }

            var magicBytesMatch = CheckMagicBytesMatch(fileContent, validMagicBytes);

            if (!magicBytesMatch)
            {
                return OperationResult<bool>.IsFailed(
                    "FILE_VAL_06",
                    originMethod,
                    $"El contenido del archivo no corresponde a un archivo '{extension}' válido. Posible intento de suplantación de tipo de archivo.",
                    400);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Verifica si el contenido del archivo coincide con alguno de los magic bytes válidos.
        /// </summary>
        private static bool CheckMagicBytesMatch(byte[] fileContent, List<byte[]> validMagicBytes)
        {
            return validMagicBytes.Any(magicByte => IsMagicByteMatch(fileContent, magicByte));
        }

        /// <summary>
        /// Compara el contenido del archivo con una secuencia específica de magic bytes.
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
        /// Valida que el tamaño del archivo no exceda el máximo permitido para su extensión.
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
                    $"El archivo excede el tamaño máximo permitido. Tamaño actual: {currentSizeMB:F2} MB, máximo: {maxSizeMB:F2} MB",
                    400);
            }

            return OperationResult<bool>.Ok(true, originMethod);
        }

        /// <summary>
        /// Valida específicamente un archivo PDF.
        /// Método de conveniencia que llama a ValidateFile con la whitelist de PDF.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo PDF en bytes.</param>
        /// <param name="fileName">Nombre del archivo incluyendo extensión.</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult indicando si el PDF es válido.</returns>
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
        /// <param name="fileName">Nombre del archivo incluyendo extensión.</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult indicando si la imagen es válida.</returns>
        public static OperationResult<bool> ValidateImageFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            var allowedExtensions = new List<string> { ".jpg", JpegExtension, ".png" };
            return ValidateFile(fileContent, fileName, allowedExtensions, originMethod);
        }

        /// <summary>
        /// Valida archivos adjuntos de declaración jurada (PDF o imágenes).
        /// </summary>
        /// <param name="fileContent">Contenido del archivo adjunto en bytes.</param>
        /// <param name="fileName">Nombre del archivo incluyendo extensión.</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult indicando si el adjunto es válido.</returns>
        public static OperationResult<bool> ValidateDeclaracionJuradaAttachment(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            var allowedExtensions = new List<string> { ".pdf", ".jpg", JpegExtension, ".png" };
            return ValidateFile(fileContent, fileName, allowedExtensions, originMethod);
        }

        /// <summary>
        /// Valida archivos de documentos (PDF, DOC, DOCX).
        /// </summary>
        /// <param name="fileContent">Contenido del archivo de documento en bytes.</param>
        /// <param name="fileName">Nombre del archivo incluyendo extensión.</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult indicando si el documento es válido.</returns>
        public static OperationResult<bool> ValidateDocumentFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            var allowedExtensions = new List<string> { ".pdf", ".doc", ".docx" };
            return ValidateFile(fileContent, fileName, allowedExtensions, originMethod);
        }

        /// <summary>
        /// Valida documentos de identidad reconocidos desde el onboarding (PDF o imagen).
        /// </summary>
        public static OperationResult<bool> ValidateIdentityDocumentFile(
            byte[] fileContent,
            string fileName,
            string originMethod)
        {
            var allowedExtensions = new List<string> { ".pdf", ".jpg", JpegExtension, ".png" };
            return ValidateFile(fileContent, fileName, allowedExtensions, originMethod);
        }

        /// <summary>
        /// Valida el contenido de un archivo (magic bytes) sin validar el nombre.
        /// Útil cuando solo se tiene el contenido del archivo y se necesita verificar que sea de un tipo específico.
        /// Valida que el contenido coincida con PDF, JPG o JPEG.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult indicando si el contenido del archivo es válido y qué tipo de archivo es.</returns>
        public static OperationResult<string> ValidateFileContentOnly(
            byte[] fileContent,
            string originMethod)
        {
            // Validación 1: Verificar que el contenido no esté vacío
            if (fileContent == null || fileContent.Length == 0)
            {
                return OperationResult<string>.IsFailed(
                    "FILE_CONTENT_01",
                    originMethod,
                    "El archivo está vacío o no se pudo leer.",
                    400);
            }

            // Validación 2: Verificar que tenga suficientes bytes para validar magic bytes
            if (fileContent.Length < 4)
            {
                return OperationResult<string>.IsFailed(
                    "FILE_CONTENT_02",
                    originMethod,
                    "El archivo es demasiado pequeño para ser válido.",
                    400);
            }

            // Validación 3: Detectar tipo de archivo por magic bytes
            var detectedType = DetectFileTypeByMagicBytes(fileContent, DefaultContentValidationExtensions);

            if (detectedType != null)
            {
                return OperationResult<string>.Ok(detectedType, originMethod);
            }

            // Si no coincide con ningún magic byte conocido
            return OperationResult<string>.IsFailed(
                "FILE_CONTENT_03",
                originMethod,
                "El archivo no es un PDF, JPG o JPEG válido. El tipo de archivo no está permitido.",
                400);
        }

        /// <summary>
        /// Detecta el tipo de archivo basándose en sus magic bytes.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="extensionsToCheck">Lista de extensiones a verificar.</param>
        /// <returns>La extensión detectada o null si no coincide con ninguna.</returns>
        private static string? DetectFileTypeByMagicBytes(byte[] fileContent, string[] extensionsToCheck)
        {
            return extensionsToCheck.FirstOrDefault(extension => TryMatchExtension(fileContent, extension));
        }

        /// <summary>
        /// Intenta hacer match del contenido del archivo con los magic bytes de una extensión específica.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="extension">Extensión a verificar (ej: ".pdf", ".jpg").</param>
        /// <returns>True si el contenido coincide con algún magic byte de la extensión.</returns>
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
        /// Método de conveniencia que valida el contenido y devuelve bool.
        /// </summary>
        /// <param name="fileContent">Contenido del archivo en bytes.</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult con bool indicando si el archivo es válido.</returns>
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
        /// Sanitiza el nombre de un archivo removiendo caracteres peligrosos y validando contra múltiples extensiones.
        /// Previene ataques como 'archivo.php.pdf' detectando múltiples puntos que podrían indicar extensiones maliciosas ocultas.
        /// </summary>
        /// <param name="fileName">Nombre del archivo original.</param>
        /// <param name="allowedExtensions">Lista de extensiones permitidas (con punto, ej: ".pdf").</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado o error si la validación falla.</returns>
        public static OperationResult<string> SanitizeFileName(
            string fileName,
            List<string> allowedExtensions,
            string originMethod)
        {
            // Validar que el nombre no sea nulo o vacío
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_01",
                    originMethod,
                    "El nombre del archivo no puede estar vacío.",
                    400);
            }

            // Trim espacios al inicio y final
            fileName = fileName.Trim();

            // Validar longitud máxima (255 caracteres es el límite común)
            if (fileName.Length > 255)
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_02",
                    originMethod,
                    "El nombre del archivo es demasiado largo. Máximo 255 caracteres.",
                    400);
            }

            // Obtener extensión actual
            var extension = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_03",
                    originMethod,
                    "El archivo debe tener una extensión válida.",
                    400);
            }

            // Validar que la extensión esté en la whitelist
            if (!allowedExtensions.Contains(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_04",
                    originMethod,
                    $"La extensión '{extension}' no está permitida. Solo se permiten: {string.Join(", ", allowedExtensions)}",
                    400);
            }

            // Obtener nombre sin extensión
            var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

            // Validar y sanitizar el nombre del archivo
            return ProcessFileNameSanitization(fileNameWithoutExtension, extension, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre de un archivo cuando el nombre y la extensión vienen por separado.
        /// Previene ataques detectando extensiones peligrosas ocultas en el nombre del archivo.
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo SIN extensión.</param>
        /// <param name="extension">Extensión del archivo (con o sin punto inicial, ej: ".pdf" o "pdf").</param>
        /// <param name="allowedExtensions">Lista de extensiones permitidas (con punto, ej: ".pdf").</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado (nombre + extensión) o error si la validación falla.</returns>
        public static OperationResult<string> SanitizeFileName(
            string fileNameWithoutExtension,
            string extension,
            List<string> allowedExtensions,
            string originMethod)
        {
            // Validar que el nombre no sea nulo o vacío
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_01",
                    originMethod,
                    "El nombre del archivo no puede estar vacío.",
                    400);
            }

            // Validar que la extensión no sea nula o vacía
            if (string.IsNullOrWhiteSpace(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_03",
                    originMethod,
                    "La extensión del archivo no puede estar vacía.",
                    400);
            }

            // Normalizar la extensión: asegurar que tenga punto inicial y esté en minúsculas
            if (!extension.StartsWith('.'))
            {
                extension = "." + extension;
            }
            extension = extension.ToLowerInvariant();

            // Validar que la extensión esté en la whitelist
            if (!allowedExtensions.Contains(extension))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_04",
                    originMethod,
                    $"La extensión '{extension}' no está permitida. Solo se permiten: {string.Join(", ", allowedExtensions)}",
                    400);
            }

            // Trim espacios al inicio y final del nombre
            fileNameWithoutExtension = fileNameWithoutExtension.Trim();

            // Validar longitud máxima del nombre completo (255 caracteres es el límite común)
            if (fileNameWithoutExtension.Length + extension.Length > 255)
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_02",
                    originMethod,
                    "El nombre del archivo es demasiado largo. Máximo 255 caracteres en total.",
                    400);
            }

            // Validar y sanitizar el nombre del archivo
            return ProcessFileNameSanitization(fileNameWithoutExtension, extension, originMethod);
        }

        /// <summary>
        /// Procesa la sanitización del nombre de archivo detectando extensiones peligrosas,
        /// removiendo caracteres no permitidos y validando contra nombres reservados.
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo sin extensión</param>
        /// <param name="extension">Extensión del archivo (con punto inicial y en minúsculas)</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación</param>
        /// <returns>OperationResult con el nombre sanitizado o error si la validación falla</returns>
        private static OperationResult<string> ProcessFileNameSanitization(
            string fileNameWithoutExtension,
            string extension,
            string originMethod)
        {
            // SEGURIDAD CRÍTICA: Detectar extensiones peligrosas ocultas en el nombre
            var dangerousExtensionCheck = CheckForDangerousExtensions(fileNameWithoutExtension, originMethod);
            if (!dangerousExtensionCheck.Success)
            {
                return dangerousExtensionCheck;
            }

            // Sanitizar el nombre removiendo caracteres no permitidos
            var sanitizedName = SanitizeFileNameCharacters(fileNameWithoutExtension);

            // Validar que después de la sanitización quede algo
            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                return OperationResult<string>.IsFailed(
                    "FILE_SAN_06",
                    originMethod,
                    "El nombre del archivo no contiene caracteres válidos después de la sanitización.",
                    400);
            }

            // Validar contra nombres reservados de Windows
            var reservedNameCheck = CheckForReservedNames(sanitizedName, originMethod);
            if (!reservedNameCheck.Success)
            {
                return reservedNameCheck;
            }

            // Construir el nombre final sanitizado con la extensión
            var sanitizedFileName = $"{sanitizedName}{extension}";

            return OperationResult<string>.Ok(sanitizedFileName, originMethod);
        }

        /// <summary>
        /// Detecta si el nombre del archivo contiene extensiones potencialmente peligrosas ocultas.
        /// Ejemplo: "archivo.php" en "archivo.php.pdf"
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo sin la extensión final</param>
        /// <param name="originMethod">Método que invoca la validación</param>
        /// <returns>OperationResult indicando si se detectó una extensión peligrosa</returns>
        private static OperationResult<string> CheckForDangerousExtensions(
            string fileNameWithoutExtension,
            string originMethod)
        {
            var dotsInFileName = fileNameWithoutExtension.Count(c => c == '.');
            if (dotsInFileName > 0)
            {
                // Verificar si alguno de los segmentos entre puntos parece una extensión de archivo ejecutable o script
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
                        $"El nombre del archivo contiene una extensión potencialmente peligrosa: '{dangerousSegment}'. " +
                        $"No se permiten extensiones que puedan ocultar archivos ejecutables o scripts.",
                        400);
                }
            }

            return OperationResult<string>.Ok(fileNameWithoutExtension, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre del archivo removiendo caracteres no permitidos,
        /// puntos adicionales, espacios múltiples y guiones bajos consecutivos.
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo sin extensión</param>
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

            // Remover espacios múltiples y reemplazar espacios por guiones bajos
            sanitizedName = Regex.Replace(sanitizedName, @"\s+", "_", RegexOptions.None, TimeSpan.FromMilliseconds(100));

            // Remover guiones bajos múltiples consecutivos
            sanitizedName = Regex.Replace(sanitizedName, @"_{2,}", "_", RegexOptions.None, TimeSpan.FromMilliseconds(100));

            // Remover guiones bajos al inicio y final
            sanitizedName = sanitizedName.Trim('_');

            return sanitizedName;
        }

        /// <summary>
        /// Verifica si el nombre del archivo corresponde a un nombre reservado del sistema Windows.
        /// </summary>
        /// <param name="sanitizedName">Nombre sanitizado a validar</param>
        /// <param name="originMethod">Método que invoca la validación</param>
        /// <returns>OperationResult indicando si el nombre es válido</returns>
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
        /// Método de conveniencia para archivos PDF.
        /// </summary>
        /// <param name="fileName">Nombre del archivo original.</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado.</returns>
        public static OperationResult<string> SanitizePdfFileName(string fileName, string originMethod)
        {
            return SanitizeFileName(fileName, new List<string> { ".pdf" }, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre de un archivo PDF cuando el nombre y la extensión vienen por separado.
        /// Método de conveniencia para archivos PDF.
        /// </summary>
        /// <param name="fileNameWithoutExtension">Nombre del archivo SIN extensión.</param>
        /// <param name="extension">Extensión del archivo (con o sin punto inicial).</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado.</returns>
        public static OperationResult<string> SanitizePdfFileName(
            string fileNameWithoutExtension, 
            string extension, 
            string originMethod)
        {
            return SanitizeFileName(fileNameWithoutExtension, extension, new List<string> { ".pdf" }, originMethod);
        }

        /// <summary>
        /// Sanitiza el nombre de un archivo adjunto de declaración jurada.
        /// </summary>
        /// <param name="fileName">Nombre del archivo original.</param>
        /// <param name="originMethod">Nombre del método que invoca esta validación.</param>
        /// <returns>OperationResult con el nombre de archivo sanitizado.</returns>
        public static OperationResult<string> SanitizeDeclaracionJuradaAttachmentName(string fileName, string originMethod)
        {
            return SanitizeFileName(fileName, new List<string> { ".pdf", ".jpg", JpegExtension, ".png" }, originMethod);
        }
    }
}
