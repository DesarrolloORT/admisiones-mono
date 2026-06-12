using AppLogic.DTOs;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.IServices.Inscripciones
{
    public interface IInscripcionesService
    {
        OperationResult<DtoUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona);
        OperationResult<bool> RegistrarInteresProducto(long codigoPersona, InteresProductoRequest request);
        OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso);
        OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso);
        OperationResult<DtoEncuestaInicialAdmisionResponse> ObtenerEncuestaInicial(long codigoPersona);
        OperationResult<DtoAceptacionReglamentoEstDevart> RegistrarAceptacionReglamentoEstudiantil(long codigoPersona);
    }
}
