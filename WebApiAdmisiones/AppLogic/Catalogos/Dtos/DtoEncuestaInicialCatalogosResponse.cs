namespace AppLogic.Catalogos.Dtos
{
    public sealed class DtoComboOption
    {
        public long Value { get; init; }
        public string Label { get; init; } = string.Empty;
    }

    public sealed class DtoUniversidadCatalogo
    {
        public long Value { get; init; }
        public string Label { get; init; } = string.Empty;
    }

    public sealed class DtoBachilleratoCatalogo
    {
        public long Value { get; init; }
        public string Label { get; init; } = string.Empty;
        public string? Orientacion { get; init; }
        public string? OrientacionNueva { get; init; }
    }

    public sealed class DtoAnioBachilleratoCatalogo
    {
        public long Value { get; init; }
        public string Label { get; init; } = string.Empty;
        public IReadOnlyList<DtoBachilleratoCatalogo> Orientaciones { get; init; } = [];
    }

    public sealed class DtoEncuestaEducacionCatalogos
    {
        public IReadOnlyList<DtoComboOption> OpcionesSiNo { get; init; } = [];
        public IReadOnlyList<DtoComboOption> UbicacionesUltimoAnioSecundaria { get; init; } = [];
        public IReadOnlyList<DtoAnioBachilleratoCatalogo> AniosBachillerato { get; init; } = [];
        public IReadOnlyList<DtoComboOption> EstadosEducacionSuperiorPrevia { get; init; } = [];
        public IReadOnlyList<DtoUniversidadCatalogo> Universidades { get; init; } = [];
        public IReadOnlyList<DtoComboOption> NivelesFormacionTutores { get; init; } = [];
    }

    public sealed class DtoEncuestaDecisionAcademicaCatalogos
    {
        public IReadOnlyList<DtoComboOption> AniosEducacionMediaSuperior { get; init; } = [];
        public IReadOnlyList<DtoComboOption> ApoyosDecision { get; init; } = [];
        public IReadOnlyList<DtoComboOption> NivelesDecision { get; init; } = [];
        public IReadOnlyList<DtoUniversidadCatalogo> Universidades { get; init; } = [];
        public IReadOnlyList<DtoComboOption> MotivosEleccionOrt { get; init; } = [];
    }

    public sealed class DtoEncuestaExperienciaOrtCatalogos
    {
        public IReadOnlyList<DtoComboOption> OpcionesSiNo { get; init; } = [];
        public IReadOnlyList<DtoComboOption> Valoraciones { get; init; } = [];
        public IReadOnlyList<DtoComboOption> PublicidadesOrt { get; init; } = [];
    }

    public sealed class DtoEncuestaSituacionLaboralCatalogos
    {
        public IReadOnlyList<DtoComboOption> OpcionesSiNo { get; init; } = [];
        public IReadOnlyList<DtoComboOption> TiposJornada { get; init; } = [];
    }

    public sealed class DtoEncuestaInicialCatalogosResponse
    {
        public DtoEncuestaEducacionCatalogos Educacion { get; init; } = new();
        public DtoEncuestaDecisionAcademicaCatalogos DecisionAcademica { get; init; } = new();
        public DtoEncuestaExperienciaOrtCatalogos ExperienciaOrt { get; init; } = new();
        public DtoEncuestaSituacionLaboralCatalogos SituacionLaboral { get; init; } = new();
    }
}
