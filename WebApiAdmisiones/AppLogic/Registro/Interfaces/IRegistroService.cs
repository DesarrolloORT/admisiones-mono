using AppLogic.Registro.Requests;
using AppLogic.Registro.Responses;
using AppLogic.Registro.Dtos;
using Utilities;

namespace AppLogic.Registro.Interfaces
{
    public interface IRegistroService
    {
        Task<OperationResult<DtoRegistroEvaluacionResponse>> EvaluarDocumentoAsync(DtoRegistroEvaluarDocumentoRequest request);
        Task<OperationResult<object?>> VerificarIdentidadAsync(DtoRegistroVerificarIdentidadRequest request);
        Task<OperationResult<object?>> ConfirmarSolicitudAltaAsync(DtoRegistroPersonaRequest request);

        /// <summary>
        /// Solo ejecuta las validaciones de ConfirmarNuevaPersona sin crear nada en DB ni LDAP.
        /// Usado por RegistroFlowService antes de almacenar en Redis.
        /// </summary>
        Task<OperationResult<object?>> ValidarNuevaPersonaAsync(DtoRegistroPersonaRequest request);

        /// <summary>
        /// Completa la creación de una nueva persona usando datos almacenados en Redis.
        /// Crea t_persona, registra interés, crea usuario LDAP con la contraseña definitiva.
        /// Retorna el CodigoPersona recien creado.
        /// </summary>
        Task<OperationResult<long>> CompletarNuevaPersonaAsync(
            DtoRegistroPendingPersona data,
            string passwordNueva,
            DtoRegistroDocumentoImagenesTemporales? imagenes = null);
    }
}
