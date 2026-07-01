namespace AppLogic.Dtos.EncuestaInicial
{
    public class DtoGuardarEncuestaInicialRequest
    {
        public long? CarreraId { get; set; }
        public long? ProcesoId { get; set; }
        public long? OrientacionBachilleratoId { get; set; }
        public long? AnioBachillerato { get; set; }
        public bool? CursaSecundariaActualmente { get; set; }
        public int? VecesRecursaAnioBachillerato { get; set; }
        public bool? RecursaAnioBachillerato { get; set; }
        public int? NivelFormacionPadreTutorId { get; set; }
        public int? NivelFormacionMadreTutorId { get; set; }
        public int? AnioDecisionCarreraId { get; set; }
        public int? AnioDecisionOrtId { get; set; }
        public bool? SeInformoEnOtrasUniversidades { get; set; }
        public string? InformacionOtrasUniversidadesLinea1 { get; set; }
        public string? InformacionOtrasUniversidadesLinea2 { get; set; }
        public int? ApoyoDecisionId { get; set; }
        public long? InstitucionSecundariaId { get; set; }
        public string? NombreInstitucionSecundaria { get; set; }
        public long? UbicacionUltimoAnioSecundariaId { get; set; }
        public long? EstadoEducacionSuperiorPreviaId { get; set; }
        public long? NivelDecisionId { get; set; }
        public bool? TuvoAsesoramientoOrt { get; set; }
        public long? ValoracionAsesoramientoOrtId { get; set; }
        public bool? VisitoSitioWebOrt { get; set; }
        public long? ValoracionSitioWebOrtId { get; set; }
        public bool? VisitoInstalacionesOrt { get; set; }
        public long? ValoracionInstalacionesOrtId { get; set; }
        public bool? RecuerdaPublicidadOrt { get; set; }
        public bool? MadreTutorEgresadoOrt { get; set; }
        public bool? PadreTutorEgresadoOrt { get; set; }
        public bool? TrabajaActualmente { get; set; }
        public long? TipoJornadaId { get; set; }
        public List<long>? UniversidadConsideradaIds { get; set; }
        public List<long>? UniversidadEducacionSuperiorIds { get; set; }
        public List<long>? PublicidadOrtIds { get; set; }
        public List<long>? MotivoEleccionOrtIds { get; set; }
    }

    public sealed class DtoGuardarEncuestaInicialResponse
    {
        public long IdEncuestaIni { get; set; }
        public string Estado { get; set; } = string.Empty;
        public List<string> SeccionesPendientes { get; set; } = [];
        public List<string> CamposPendientes { get; set; } = [];
    }
}
