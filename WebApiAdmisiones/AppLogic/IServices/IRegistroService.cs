using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices
{
    public interface IRegistroService
    {
        Task<OperationResult<RegistroEvaluacionResponse>> EvaluarDocumentoAsync(RegistroEvaluarDocumentoRequest request);
        Task<OperationResult<object?>> VerificarIdentidadAsync(RegistroVerificarIdentidadRequest request);
        Task<OperationResult<object?>> ConfirmarPersonaExistenteAsync(RegistroConfirmarPersonaExistenteRequest request);
        Task<OperationResult<object?>> ConfirmarNuevaPersonaAsync(RegistroPersonaRequest request);
        Task<OperationResult<object?>> ConfirmarSolicitudAltaAsync(RegistroPersonaRequest request);

        /// <summary>
        /// Solo ejecuta las validaciones de ConfirmarNuevaPersona sin crear nada en DB ni LDAP.
        /// Usado por RegistroFlowService antes de almacenar en Redis.
        /// </summary>
        Task<OperationResult<object?>> ValidarNuevaPersonaAsync(RegistroPersonaRequest request);

        /// <summary>
        /// Completa la creación de una nueva persona usando datos almacenados en Redis.
        /// Crea t_persona, registra interés, crea usuario LDAP con la contraseña definitiva.
        /// Retorna el CodigoPersona recien creado.
        /// </summary>
        Task<OperationResult<long>> CompletarNuevaPersonaAsync(RegistroPendingPersona data, string passwordNueva);
    }
}
