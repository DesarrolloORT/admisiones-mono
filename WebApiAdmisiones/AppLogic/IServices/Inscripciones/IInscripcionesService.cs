using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Dtos.Inscripciones;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.IServices.Inscripciones
{
    public interface IInscripcionesService
    {
        OperationResult<bool> RegistrarInteresProducto(long codigoPersona, DtoInteresProductoRequest request);
        OperationResult<DtoAceptacionReglamentoEstudiantilResponse> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona);
        Task<OperationResult<DtoDetalleInscripcionResponse>> ObtenerDetalleInscripcion(long codigoPersona, long idProducto, long idProceso);
        OperationResult<DtoEncuestaInicialAdmisionResponse> ObtenerEncuestaInicial(long codigoPersona);
        OperationResult<bool> GuardarEncuestaInicial(long codigoPersona, DtoGuardarEncuestaInicialRequest request);
        Task<OperationResult<DtoConfirmarPreInscripcionResponse>> ConfirmarPreInscripcion(long codigoPersona, DtoConfirmarPreInscripcionRequest request);
        Task<OperationResult<string>> ObtenerUrlFactura(long codigoPersona, DtoObtenerUrlFacturaRequest request);
        OperationResult<bool> GuardarMetodoPago(long codigoPersona, DtoGuardarMetodoPagoRequest request);
    }
}
