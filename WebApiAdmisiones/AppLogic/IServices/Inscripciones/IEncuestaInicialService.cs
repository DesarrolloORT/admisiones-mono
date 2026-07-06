using AppLogic.Dtos.EncuestaInicial;
using Utilities;

namespace AppLogic.IServices.Inscripciones
{
    /// <summary>
    /// Servicio de encuesta inicial de admisión: obtención y guardado (parcial o definitivo).
    /// Expone únicamente las operaciones consumidas por <c>InscripcionesService</c>.
    /// </summary>
    public interface IEncuestaInicialService
    {
        OperationResult<DtoObtenerEncuestaInicialResponse> ObtenerEncuestaInicial(long codigoPersona);

        OperationResult<DtoGuardarEncuestaInicialResponse> GuardarEncuestaInicial(
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request);
    }
}
