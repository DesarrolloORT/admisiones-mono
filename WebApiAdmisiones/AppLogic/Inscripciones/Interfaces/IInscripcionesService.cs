using AppLogic.Inscripciones.Encuesta.Requests;
using AppLogic.Inscripciones.Encuesta.Responses;
using AppLogic.Inscripciones.Requests;
using AppLogic.Inscripciones.Responses;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.Inscripciones.Interfaces
{
    public interface IInscripcionesService
    {
        OperationResult<bool> RegistrarInteresProducto(long codigoPersona, DtoInteresProductoRequest request);
        OperationResult<DtoAceptacionReglamentoEstudiantilResponse> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona);
        Task<OperationResult<DtoDetalleInscripcionResponse>> ObtenerDetalleInscripcion(long codigoPersona, long idProducto, long idProceso);
        OperationResult<DtoObtenerEncuestaInicialResponse> ObtenerEncuestaInicial(long codigoPersona);
        OperationResult<DtoGuardarEncuestaInicialResponse> GuardarEncuestaInicial(long codigoPersona, DtoGuardarEncuestaInicialRequest request);
        Task<OperationResult<DtoConfirmarPreInscripcionResponse>> ConfirmarPreInscripcion(long codigoPersona, DtoConfirmarPreInscripcionRequest request);
        Task<OperationResult<DtoPagarResponse>> Pagar(long codigoPersona, DtoPagarRequest request);
    }
}
