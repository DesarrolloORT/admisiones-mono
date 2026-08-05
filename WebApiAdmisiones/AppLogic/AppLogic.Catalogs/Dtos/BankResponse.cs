namespace AppLogic.Catalogs.Dtos;

/// <summary>Banco habilitado para pagos.</summary>
public sealed class BankResponse
{
    /// <summary>Identificador del banco en el esquema de admisiones.</summary>
    public long Id { get; init; }

    /// <summary>Nombre del banco.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Código del banco en el esquema de admisiones.</summary>
    public long Code { get; init; }

    /// <summary>Identificador del banco en Sistarbanc, cuando aplica.</summary>
    public string? SistarbancBankId { get; init; }
}
