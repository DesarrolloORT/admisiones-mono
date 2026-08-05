namespace AppLogic.Contracts.Dtos;

/// <summary>
/// Par valor/etiqueta para combos del front. Compartido: lo publica Catalogs y lo consume
/// la validación de la encuesta inicial en Enrollments.
/// </summary>
public sealed class ComboOption
{
    /// <summary>Valor que el front devuelve al guardar.</summary>
    public long Value { get; init; }

    /// <summary>Texto a mostrar.</summary>
    public string Label { get; init; } = string.Empty;

    public static ComboOption Of(long value, string? label) => new()
    {
        Value = value,
        Label = label ?? string.Empty
    };
}
