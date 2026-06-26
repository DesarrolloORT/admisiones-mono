using AppLogic.DevartDTOs;

namespace AppLogic.Dtos.Catalogos
{
    public sealed class DtoComboOption
    {
        public int Value { get; init; }
        public string Label { get; init; } = string.Empty;
    }

    public sealed class DtoUniversidadCatalogo
    {
        public long CodigoEmpresa { get; init; }
        public string? Nombre { get; init; }
    }

    public sealed class DtoBachilleratoCatalogo
    {
        public long CodigoTitulo { get; init; }
        public string? Nombre { get; init; }
        public string? OrientacionTitulo { get; init; }
        public string? OrientacionNewTitulo { get; init; }
    }

    public sealed class DtoAnioBachilleratoCatalogo
    {
        public decimal IdAnioBachiller { get; init; }
        public string? NombreAnioBachiller { get; init; }
        public decimal? CantAniosAnioBachiller { get; init; }
        public IReadOnlyList<DtoBachilleratoCatalogo> Bachilleratos { get; init; } = [];
    }

    public sealed class DtoEncuestaInicialCatalogosResponse
    {
        public IReadOnlyList<DtoComboOption> NivelConocimiento { get; init; } = [];
        public IReadOnlyList<DtoComboOption> OpcionesEMS { get; init; } = [];
        public IReadOnlyList<DtoComboOption> CompartidoCon { get; init; } = [];
        public IReadOnlyList<DtoComboOption> FormacionTutores { get; init; } = [];
        public IReadOnlyList<DtoComboOption> EstadoEducacionSuperior { get; init; } = [];
        public IReadOnlyList<DtoComboOption> AniosAprobadosEducacionSuperior { get; init; } = [];
        public IReadOnlyList<DtoMotivoOpcionesAdmisionDevart> MotivosEleccion { get; init; } = [];
        public IReadOnlyList<DtoPublicidadOpcionesAdmisionDevart> PublicidadesEleccion { get; init; } = [];
        public IReadOnlyList<DtoAnioBachilleratoCatalogo> AniosBachiller { get; init; } = [];
        public IReadOnlyList<DtoUniversidadCatalogo> Universidades { get; init; } = [];
    }
}
