using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Personas.Dtos;

[ExcludeFromCodeCoverage]
public class DtoActualizarDatosPersonaRequest
{
    public string? TipoDocumento { get; set; }
    public string? Documento { get; set; }
    public string? PrimerNombre { get; set; }
    public string? SegundoNombre { get; set; }
    public string? PrimerApellido { get; set; }
    public string? SegundoApellido { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Sexo { get; set; }
    public long CodigoPais { get; set; }
    public long CodigoEstado { get; set; }
    public long CodigoCiudad { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Telefono1 { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public string VerificacionMail { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public class DtoTelefono
{
    public bool TelefonoValido { get; set; }
    [StringLength(20)]
    public string? TelefonoE164 { get; set; }
    [StringLength(2)]
    public string? Iso2 { get; set; }
    public long CaracteristicaPais { get; set; }
    [StringLength(20)]
    public string? TelefonoSimple { get; set; }
}

