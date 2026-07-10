using AppLogic.Inscripciones.Encuesta.Requests;

namespace AppLogic.Inscripciones.Encuesta.Responses
{
    public sealed class DtoEncuestaInicialLectura : DtoGuardarEncuestaInicialRequest
    {
        public long IdEncuestaIni { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
