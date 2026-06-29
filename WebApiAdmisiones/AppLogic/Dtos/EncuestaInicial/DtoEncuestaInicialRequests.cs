namespace AppLogic.Dtos.EncuestaInicial
{
    public sealed class DtoGuardarEncuestaInicialRequest
    {
        public long? IdProducto { get; set; }
        public long? IdProceso { get; set; }
        public long? CodigoTitulo { get; set; }
        public long? UltimoAnioSexto { get; set; }
        public int? VecesSexto { get; set; }
        public bool? VecesSextoBool { get; set; }
        public int? InstruccionPadre { get; set; }
        public int? InstruccionMadre { get; set; }
        public int? DecisionCarrera { get; set; }
        public int? DecisionUniversidad { get; set; }
        public string? InfoOtrasUniversidadesAntes { get; set; }
        public string? InfoOtrasLinea1 { get; set; }
        public string? InfoOtrasLinea2 { get; set; }
        public int? CompartidoCon { get; set; }
        public long? CodigoInstitucionBac { get; set; }
        public string? InformarEncuesta { get; set; }
        public string? NombreInstitucion { get; set; }
        public long? UltimoAnioSecundaria { get; set; }
        public bool? TieneEducacionSuperior { get; set; }
        public long? NivelDecision { get; set; }
        public bool? AsesoramientoOrt { get; set; }
        public long? ValoracionAsesoramientoOrt { get; set; }
        public bool? VistaSitioWebOrt { get; set; }
        public long? ValoracionSitioWeb { get; set; }
        public bool? VistaInstalacionesOrt { get; set; }
        public long? ValoracionInstalacionesOrt { get; set; }
        public bool? PublicidadOrt { get; set; }
        public bool? InstruccionMadreOrt { get; set; }
        public bool? InstruccionPadreOrt { get; set; }
        public bool? TrabajaActualmente { get; set; }
        public long? TipoJornada { get; set; }
        public List<DtoEncuestaEmpresaRequest>? UniversidadesConsideradas { get; set; }
        public List<DtoEncuestaEmpresaRequest>? UniversidadesEducacionSuperior { get; set; }
        public List<DtoEncuestaPublicidadRequest>? OpcionesPublicidadSeleccionadas { get; set; }
        public List<DtoEncuestaMotivoRequest>? OpcionesMotivosSeleccionados { get; set; }
    }

    public sealed class DtoEncuestaEmpresaRequest
    {
        public long CodigoEmpresa { get; set; }
        public string? Nombre { get; set; }
    }

    public sealed class DtoEncuestaPublicidadRequest
    {
        public long IdPublicidad { get; set; }
        public string? NombrePublicidad { get; set; }
    }

    public sealed class DtoEncuestaMotivoRequest
    {
        public long IdMotivo { get; set; }
        public string? NombreMotivo { get; set; }
    }
}
