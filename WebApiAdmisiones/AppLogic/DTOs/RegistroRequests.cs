using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public class RegistroEvaluarDocumentoRequest
    {
        [Required]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        public string Documento { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class RegistroConfirmarPersonaExistenteRequest
    {
        [Required]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        public string Documento { get; set; } = string.Empty;

        [Range(1, long.MaxValue)]
        public long IdProducto { get; set; }

        [Range(1, long.MaxValue)]
        public long IdProceso { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class RegistroPersonaRequest
    {
        [Required]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        public string Documento { get; set; } = string.Empty;

        [Range(1, long.MaxValue)]
        public long IdProducto { get; set; }

        [Range(1, long.MaxValue)]
        public long IdProceso { get; set; }

        [Required]
        [MinLength(2)]
        public string PrimerApellido { get; set; } = string.Empty;

        public string? SegundoApellido { get; set; }

        [Required]
        [MinLength(2)]
        public string PrimerNombre { get; set; } = string.Empty;

        public string? SegundoNombre { get; set; }

        [Range(typeof(DateTime), "1900-01-02", "9999-12-31")]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        [RegularExpression("^[mMfF]$")]
        public string Sexo { get; set; } = string.Empty;

        [Required]
        public string Direccion { get; set; } = string.Empty;

        [Required]
        public string Telefono1 { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Mail { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Mail))]
        public string VerificacionMail { get; set; } = string.Empty;

        [Range(1, long.MaxValue)]
        public long CodigoPais { get; set; }

        [Range(1, long.MaxValue)]
        public long CodigoEstado { get; set; }

        [Range(1, long.MaxValue)]
        public long CodigoCiudad { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class RegistroVerificarIdentidadRequest
    {
        [Required]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        public string Documento { get; set; } = string.Empty;

        [Required]
        [MinLength(2)]
        public string PrimerApellido { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Mail { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Mail))]
        public string VerificacionMail { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class RegistroEvaluacionResponse
    {
        /// <summary>
        /// La persona existe y ya tiene usuario LDAP. El front debe mostrar el mensaje del OperationResult y ofrecer acceso o recuperacion.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool UsuarioExistente { get; set; }

        /// <summary>
        /// La persona existe en SGI pero no tiene usuario LDAP. El front debe pedir primer apellido, mail y verificacion de mail.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool RequiereVerificacion { get; set; }

        /// <summary>
        /// Es una CI valida sin persona existente. El front debe pedir datos completos, incluyendo direccion y ciudad.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool RequiereAltaPersona { get; set; }

        /// <summary>
        /// No existe solicitud de alta para el documento. El front debe pedir datos para crear la solicitud.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool RequiereAltaSolicitud { get; set; }

        /// <summary>
        /// Ya existe una solicitud de alta para el documento ingresado.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SolicitudAltaExistente { get; set; }
    }
}
