namespace AppLogic.Catalogs.Dtos;

/// <summary>
/// Institución educativa para el selector de secundaria de la encuesta inicial. El front devuelve
/// estos dos valores en <c>SaveInitialSurveyRequest.SecondaryInstitutionId</c> y
/// <c>SecondaryInstitutionName</c>: antes se serializaba la fila completa de T_EMPRESA
/// (~38 columnas internas: facturación, RUC, voucher, PSIG…), que nunca fue parte del contrato.
/// </summary>
public sealed class InstitutionResponse
{
    /// <summary>Código de la institución en T_EMPRESA.</summary>
    public long Id { get; init; }

    /// <summary>Nombre de la institución.</summary>
    public string Name { get; init; } = string.Empty;
}
