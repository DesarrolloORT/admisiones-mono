using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public class RegistroEvaluarDocumentoRequest
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class RegistroConfirmarRequest
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public long IdProducto { get; set; }
        public long IdProceso { get; set; }
        public string PrimerApellido { get; set; } = string.Empty;
        public string SegundoApellido { get; set; } = string.Empty;
        public string PrimerNombre { get; set; } = string.Empty;
        public string SegundoNombre { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono1 { get; set; } = string.Empty;
        public string Telefono2 { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string VerificacionMail { get; set; } = string.Empty;
        public long CodigoPais { get; set; }
        public long CodigoEstado { get; set; }
        public long CodigoCiudad { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class RegistroVerificarPersonaRequest
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string PrimerApellido { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
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
    }

    [ExcludeFromCodeCoverage]
    public class RegistroCarreraResponse
    {
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public long IdNivelProducto { get; set; }
        public string? NombreNivelProducto { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class RegistroComienzoResponse
    {
        public long IdProceso { get; set; }
        public string? NombreProceso { get; set; }
    }
}
