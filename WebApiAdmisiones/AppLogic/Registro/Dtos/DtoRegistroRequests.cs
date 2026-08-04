using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Registro.Dtos;

[ExcludeFromCodeCoverage]
public class DtoRegistroEvaluarDocumentoRequest
{
    [Required]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required]
    public string Documento { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public class DtoRegistroPersonaRequest
{
    [Required]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required]
    public string Documento { get; set; } = string.Empty;

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
public class DtoRegistroVerificarIdentidadRequest
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
}
