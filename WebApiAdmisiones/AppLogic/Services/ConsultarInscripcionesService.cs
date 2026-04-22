using System.Threading.Tasks;
using AppLogic.ApiClients;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Services
{
    /// <summary>
    /// Servicio de ejemplo que demuestra cómo usar InscripcionesyPagosApiClient.
    /// Este servicio puede agregar validaciones o lógica de negocio antes/después de llamar a la API interna.
    /// </summary>
    public class ConsultarInscripcionesService : IConsultarInscripcionesService
    {
        private readonly InscripcionesyPagosApiClient _inscripcionesyPagosApiClient;
        private readonly ILogger<ConsultarInscripcionesService> _logger;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="inscripcionesyPagosApiClient">Cliente para comunicarse con la API de Inscripciones y Pagos</param>
        /// <param name="logger">Logger para trazabilidad</param>
        public ConsultarInscripcionesService(
            InscripcionesyPagosApiClient inscripcionesyPagosApiClient,
            ILogger<ConsultarInscripcionesService> logger)
        {
            _inscripcionesyPagosApiClient = inscripcionesyPagosApiClient;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene las inscripciones de una persona desde la API interna de Inscripciones y Pagos.
        /// </summary>
        /// <param name="codigoPersona">Código de la persona</param>
        /// <returns>OperationResult con la lista de inscripciones</returns>
        public async Task<OperationResult<InscripcionesListResponse>> ObtenerInscripcionesDePersonaAsync(long codigoPersona)
        {
            _logger.LogInformation(
                "Iniciando consulta de inscripciones para persona {CodigoPersona}",
                codigoPersona
            );

            // Aquí podrías agregar validaciones o lógica de negocio ANTES de llamar a la API
            // Por ejemplo:
            // - Verificar permisos del usuario actual
            // - Validar que la persona existe en tu BD local
            // - Aplicar filtros adicionales
            // - etc.

            // Llamada a la API interna
            // El ServiceAuthenticationHandler inyecta automáticamente:
            // - Authorization: Bearer {token del usuario actual desde cookie}
            // - X-Service-Token: {token firmado que identifica a api-admisiones}
            var resultado = await _inscripcionesyPagosApiClient.ObtenerInscripcionesAsync(codigoPersona);

            if (!resultado.Success)
            {
                _logger.LogError(
                    "Error al obtener inscripciones para persona {CodigoPersona}. ErrorCode: {ErrorCode}, Message: {Message}",
                    codigoPersona,
                    resultado.ErrorCode,
                    resultado.Message
                );

                // Puedes transformar o enriquecer el error aquí si es necesario
                return resultado;
            }

            _logger.LogInformation(
                "Inscripciones obtenidas exitosamente para persona {CodigoPersona}. Total: {Total}",
                codigoPersona,
                resultado.Data!.TotalCount
            );

            // Aquí podrías agregar lógica DESPUÉS de obtener los datos
            // Por ejemplo:
            // - Enriquecer con datos adicionales de tu BD local
            // - Aplicar transformaciones
            // - Cachear el resultado
            // - etc.

            return resultado;
        }
    }

    /// <summary>
    /// Interfaz del servicio.
    /// </summary>
    public interface IConsultarInscripcionesService
    {
        Task<OperationResult<InscripcionesListResponse>> ObtenerInscripcionesDePersonaAsync(long codigoPersona);
    }
}
