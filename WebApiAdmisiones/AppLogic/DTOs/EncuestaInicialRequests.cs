namespace AppLogic.DTOs
{
    public sealed class GuardarEncuestaInicialRequest
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
        public List<EncuestaEmpresaRequest>? UniversidadesConsideradas { get; set; }
        public List<EncuestaEmpresaRequest>? UniversidadesEducacionSuperior { get; set; }
        public List<EncuestaPublicidadRequest>? OpcionesPublicidadSeleccionadas { get; set; }
        public List<EncuestaMotivoRequest>? OpcionesMotivosSeleccionados { get; set; }
    }

    public sealed class EncuestaEmpresaRequest
    {
        public long CodigoEmpresa { get; set; }
        public string? Nombre { get; set; }
    }

    public sealed class EncuestaPublicidadRequest
    {
        public long IdPublicidad { get; set; }
        public string? NombrePublicidad { get; set; }
    }

    public sealed class EncuestaMotivoRequest
    {
        public long IdMotivo { get; set; }
        public string? NombreMotivo { get; set; }
    }
}
