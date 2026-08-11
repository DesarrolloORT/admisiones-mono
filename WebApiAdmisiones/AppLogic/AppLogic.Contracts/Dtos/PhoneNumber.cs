using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Contracts.Dtos;

/// <summary>
/// Teléfono desglosado, equivalente al <c>DtoTelefono</c> de FDP. El servidor guarda siempre el
/// E.164 que arma él mismo a partir de <see cref="NationalNumber"/> + <see cref="Iso2"/>: los otros
/// campos son informativos y no se persisten.
/// </summary>
[ExcludeFromCodeCoverage]
public class PhoneNumber
{
    /// <summary>Si el front ya lo dio por válido.</summary>
    public bool IsValid { get; set; }

    /// <summary>Número completo en formato E.164 (por ejemplo +59899123456). El servidor lo ignora.</summary>
    [StringLength(20)]
    public string? E164 { get; set; }

    /// <summary>Código de país ISO 3166-1 alfa-2 (por ejemplo UY). Obligatorio para guardar.</summary>
    [StringLength(2)]
    public string? Iso2 { get; set; }

    /// <summary>Prefijo telefónico del país (por ejemplo 598). El servidor lo ignora.</summary>
    public long CountryCode { get; set; }

    /// <summary>Número sin el prefijo del país: es el que se valida y se guarda.</summary>
    [StringLength(20)]
    public string? NationalNumber { get; set; }
}
