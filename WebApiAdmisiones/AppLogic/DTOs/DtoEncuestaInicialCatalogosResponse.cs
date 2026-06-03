namespace AppLogic.DTOs
{
    public sealed class DtoComboOption
    {
        public int Value { get; init; }
        public string Label { get; init; } = string.Empty;
    }

    public sealed class DtoEncuestaInicialCatalogosResponse
    {
        public IReadOnlyList<DtoComboOption> NivelConocimiento { get; init; } = [];
        public IReadOnlyList<DtoComboOption> DecisionCarrera { get; init; } = [];
        public IReadOnlyList<DtoComboOption> CompartidoCon { get; init; } = [];
        public IReadOnlyList<DtoComboOption> FormacionTutores { get; init; } = [];
        public IReadOnlyList<DtoComboOption> EstadoEducacionSuperior { get; init; } = [];
        public IReadOnlyList<DtoComboOption> DecisionUniversidad { get; init; } = [];
        public IReadOnlyList<DtoComboOption> AniosAprobadosEducacionSuperior { get; init; } = [];
    }
}
