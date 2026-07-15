
namespace AppLogic.Inscripciones.Encuesta.Dtos
{
    public sealed class DtoEncuestaInicialLectura : DtoGuardarEncuestaInicialRequest
    {
        public long IdEncuestaIni { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
