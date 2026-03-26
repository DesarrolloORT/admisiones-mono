using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Requests
{
    [ExcludeFromCodeCoverage]
    public class ActualizarPersonaRequest
    {
        public string PrimerApellido { get; set; } = string.Empty;
        public string SegundoApellido { get; set; } = string.Empty;
        public string PrimerNombre { get; set; } = string.Empty;
        public string SegundoNombre { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string VerificacionMail { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string Telefono1 { get; set; } = string.Empty;
        public string Telefono2 { get; set; } = string.Empty;
        public long CodigoPais { get; set; }
        public long CodigoEstado { get; set; }
        public long CodigoCiudad { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public string TrabajaActualmente { get; set; } = string.Empty;
        public long TipoJornada { get; set; }
    }

    public class EmpresaEncuestaRequest
    {
        public long CodigoEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class PublicidadEncuestaRequest
    {
        public long IdPublicidad { get; set; }
        public string NombrePublicidad { get; set; } = string.Empty;
    }

    public class MotivoEncuestaRequest
    {
        public long IdMotivo { get; set; }
        public string NombreMotivo { get; set; } = string.Empty;
    }

    public class GuardarDatosPersonaEncuestaRequest
    {
        public string PrimerApellido { get; set; } = string.Empty;
        public string SegundoApellido { get; set; } = string.Empty;
        public string PrimerNombre { get; set; } = string.Empty;
        public string SegundoNombre { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string VerificacionMail { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string TrabajaActualmente { get; set; } = string.Empty;
        public long TipoJornada { get; set; }
        public string Telefono1 { get; set; } = string.Empty;
        public string Telefono2 { get; set; } = string.Empty;
        public long CodigoPais { get; set; }
        public long CodigoEstado { get; set; }
        public long CodigoCiudad { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public long IdProducto { get; set; }
        public long IdProceso { get; set; }
        public long? CodigoTitulo { get; set; }
        public long UltimoAnioSexto { get; set; }
        public int VecesSexto { get; set; }
        public bool VecesSextoBool { get; set; }
        public int InstruccionPadre { get; set; }
        public int InstruccionMadre { get; set; }
        public int? DecisionCarrera { get; set; }
        public int? DecisionUniversidad { get; set; }
        public string InfoOtrasUniversidadesAntes { get; set; } = string.Empty;
        public string InfoOtrasLinea1 { get; set; } = string.Empty;
        public string InfoOtrasLinea2 { get; set; } = string.Empty;
        public int CompartidoCon { get; set; }
        public long? CodigoInstitucionBac { get; set; }
        public string InformarEncuesta { get; set; } = string.Empty;
        public string NombreInstitucion { get; set; } = string.Empty;
        public long UltimoAnioSecundaria { get; set; }
        public bool? TieneEducacionSuperior { get; set; }
        public long NivelDecision { get; set; }
        public bool? AsesoramientoOrt { get; set; }
        public long ValoracionAsesoramientoOrt { get; set; }
        public bool? VistaSitioWebOrt { get; set; }
        public long ValoracionSitioWeb { get; set; }
        public bool? VistaInstalacionesOrt { get; set; }
        public long ValoracionInstalacionesOrt { get; set; }
        public bool? PublicidadOrt { get; set; }
        public bool? InstruccionMadreOrt { get; set; }
        public bool? InstruccionPadreOrt { get; set; }
        public List<EmpresaEncuestaRequest> UniversidadesConsideradas { get; set; } = [];
        public List<EmpresaEncuestaRequest> UniversidadesEducacionSuperior { get; set; } = [];
        public List<PublicidadEncuestaRequest> OpcionesPublicidadSeleccionadas { get; set; } = [];
        public List<MotivoEncuestaRequest> OpcionesMotivosSeleccionados { get; set; } = [];
    }
}
