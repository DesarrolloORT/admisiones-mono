using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


namespace WebApiAdmisiones.Models
{
    /// <summary>Archivo subido por el front: contenido en base64 más su nombre original.</summary>
    [ExcludeFromCodeCoverage]
    public class FilePayload
    {
        /// <summary>Nombre original del archivo, con extensión: de ahí sale el tipo permitido.</summary>
        public string? FileName { get; set; }

        /// <summary>Contenido del archivo.</summary>
        public byte[]? Content { get; set; }
    }

    /// <summary>Alta o reemplazo de la foto de perfil.</summary>
    [ExcludeFromCodeCoverage]
    public class UploadPersonPhotoRequest
    {
        /// <summary>Imagen JPG, JPEG o PNG.</summary>
        public FilePayload File { get; set; } = new();
    }

    /// <summary>Alta o reemplazo de las dos caras del documento de identidad.</summary>
    [ExcludeFromCodeCoverage]
    public class UploadPersonIdentityDocumentRequest
    {
        /// <summary>Fecha de vencimiento del documento.</summary>
        [JsonRequired]
        public DateTime ExpirationDate { get; set; }

        /// <summary>Frente del documento.</summary>
        public FilePayload Front { get; set; } = new();

        /// <summary>Dorso del documento.</summary>
        public FilePayload Back { get; set; } = new();
    }

    /// <summary>Documento a analizar con Azure Document Intelligence.</summary>
    [ExcludeFromCodeCoverage]
    public class RecognizeDocumentApiRequest
    {
        /// <summary>Tipo MIME del adjunto: se usa para resolver la extensión si el nombre no la trae.</summary>
        public string? MimeType { get; set; }

        /// <summary>Imagen o PDF del documento.</summary>
        public FilePayload File { get; set; } = new();
    }

    /// <summary>Comprobante de un ingreso mensual de la declaración jurada.</summary>
    [ExcludeFromCodeCoverage]
    public class UploadIncomeFileRequest
    {
        /// <summary>Ingreso mensual al que se adjunta el comprobante.</summary>
        [JsonRequired]
        public long MonthlyIncomeId { get; set; }

        /// <summary>Comprobante.</summary>
        public FilePayload File { get; set; } = new();
    }

    /// <summary>Comprobante de un egreso mensual de la declaración jurada.</summary>
    [ExcludeFromCodeCoverage]
    public class UploadExpenseFileRequest
    {
        /// <summary>Egreso mensual al que se adjunta el comprobante.</summary>
        [JsonRequired]
        public long MonthlyExpenseId { get; set; }

        /// <summary>Comprobante.</summary>
        public FilePayload File { get; set; } = new();
    }

    /// <summary>Formulario de reválida firmado de la declaración jurada.</summary>
    [ExcludeFromCodeCoverage]
    public class UploadRevalidationFileRequest
    {
        /// <summary>Declaración jurada a la que se adjunta el formulario.</summary>
        [JsonRequired]
        public long AffidavitId { get; set; }

        /// <summary>Formulario de reválida.</summary>
        public FilePayload File { get; set; } = new();
    }
}
