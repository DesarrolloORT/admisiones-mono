using System.Collections.Generic;

namespace AppLogic.Inscripciones.Encuesta.Dtos
{
    public sealed class DtoGuardarEncuestaInicialResponse
    {
        public long IdEncuestaIni { get; set; }
        public string Estado { get; set; } = string.Empty;
        public List<string> SeccionesPendientes { get; set; } = [];
        public List<string> CamposPendientes { get; set; } = [];
    }
}
