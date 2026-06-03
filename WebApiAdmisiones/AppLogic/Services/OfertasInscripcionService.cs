using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using BusinessLogic.IDevartRepositories;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Services
{
    /// <summary>
    /// Servicio de ejemplo que demuestra cómo integrar lógica de negocio local
    /// con llamadas a la API interna de Inscripciones y Pagos.
    /// </summary>
    public class OfertasInscripcionService : IOfertasInscripcionService
    {
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly InscripcionesyPagosApiClient _inscripcionesyPagosApiClient;
        private readonly ILogger<OfertasInscripcionService> _logger;

        public OfertasInscripcionService(
            IUnitOfWorkFactory uowFactory,
            InscripcionesyPagosApiClient inscripcionesyPagosApiClient,
            ILogger<OfertasInscripcionService> logger)
        {
            _uowFactory = uowFactory;
            _inscripcionesyPagosApiClient = inscripcionesyPagosApiClient;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene las ofertas disponibles para inscripción, validando primero los datos locales.
        /// 
        /// FLUJO:
        /// 1. Valida que la persona existe en la base de datos local
        /// 2. Verifica que la persona tenga código de vigencia
        /// 3. Llama a la API interna para obtener las ofertas disponibles
        /// </summary>
        /// <param name="codigoPersona">Código de la persona</param>
        /// <param name="idProducto">ID del producto</param>
        /// <param name="idComienzo">ID del comienzo</param>
        /// <param name="idTurno">ID del turno</param>
        /// <returns>Lista de ofertas disponibles para la persona</returns>
        public async Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerOfertasParaPersonaAsync(
            long codigoPersona,
            long idProducto,
            long idComienzo,
            long idTurno)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                "Iniciando consulta de ofertas para persona {CodigoPersona}, Producto: {IdProducto}, Comienzo: {IdComienzo}, Turno: {IdTurno}",
                codigoPersona,
                idProducto,
                idComienzo,
                idTurno
                );
            }

            // ═══════════════════════════════════════════════════════════════
            // PASO 1: Validar que la persona existe en BD local
            // ═══════════════════════════════════════════════════════════════
            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
            {
                _logger.LogWarning("Persona {CodigoPersona} no encontrada en la base de datos local", codigoPersona);
                return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                    "OFERTAS_PERSONA_01",
                    nameof(ObtenerOfertasParaPersonaAsync),
                    "La persona no existe en el sistema.",
                    404,
                    default!
                );
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                "Persona encontrada: {PrimerNombre} {PrimerApellido} (Documento: {Documento})",
                persona.PrimerNombre,
                persona.PrimerApellido,
                persona.Documento
                );
            }

            // ═══════════════════════════════════════════════════════════════
            // PASO 2: Verificar que la persona tiene código de vigencia
            // ═══════════════════════════════════════════════════════════════
            if (string.IsNullOrWhiteSpace(persona.CodigoVigencia))
            {
                _logger.LogWarning(
                    "Persona {CodigoPersona} no tiene código de vigencia asignado",
                    codigoPersona
                );
                return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                    "OFERTAS_PERSONA_02",
                    nameof(ObtenerOfertasParaPersonaAsync),
                    "La persona no tiene código de vigencia asignado. Debe completar su registro.",
                    422,
                    default!
                );
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                "Persona {CodigoPersona} tiene código de vigencia: {CodigoVigencia}",
                codigoPersona,
                persona.CodigoVigencia
                );
            }

            // ═══════════════════════════════════════════════════════════════
            // PASO 3: Validaciones adicionales de negocio (opcional)
            // ═══════════════════════════════════════════════════════════════
            // Aquí puedes agregar más validaciones según tu lógica de negocio:
            // - Verificar si la persona tiene inscripciones activas
            // - Validar si cumple requisitos académicos
            // - Verificar estado de cuenta corriente
            // - etc.

            var personaAdmite = uow.PersonaAdmites.GetByKey(codigoPersona);
            if (personaAdmite != null)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                    "Persona tiene registro en PersonaAdmite - Estado: {EstadoAdmite}",
                    personaAdmite.EstadoAdmite
                    );
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // PASO 4: Llamar a la API interna para obtener ofertas
            // ═══════════════════════════════════════════════════════════════
            // El ServiceAuthenticationHandler inyecta automáticamente:
            // - Authorization: Bearer {token del usuario actual desde cookie}
            // - X-Service-Token: {token firmado que identifica a api-admisiones}
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                "Llamando a API interna para obtener ofertas disponibles"
                );
            }

            var resultadoApi = await _inscripcionesyPagosApiClient
                .ObtenerOfertasParaInscripcionAdmisionesAsync(
                    idProducto,
                    idComienzo,
                    idTurno
                );

            if (!resultadoApi.Success)
            {
                _logger.LogError(
                    "Error al obtener ofertas desde API interna. ErrorCode: {ErrorCode}, Message: {Message}",
                    resultadoApi.ErrorCode,
                    resultadoApi.Message
                );
                return resultadoApi;
            }

            // ═══════════════════════════════════════════════════════════════
            // PASO 5: Procesar/enriquecer los datos de respuesta (opcional)
            // ═══════════════════════════════════════════════════════════════
            var ofertas = resultadoApi.Data!;

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                "Ofertas obtenidas exitosamente. Total: {TotalOfertas} ofertas disponibles",
                ofertas.Count
                );
            }

            // Aquí podrías enriquecer las ofertas con datos adicionales de tu BD local
            // Por ejemplo: agregar información de cupos, horarios específicos, etc.

            return OperationResult<List<OfertaInscripcionDto>>.Ok(
                ofertas,
                nameof(ObtenerOfertasParaPersonaAsync)
            );
        }

    }

    /// <summary>
    /// Interfaz del servicio de ofertas de inscripción.
    /// </summary>
    public interface IOfertasInscripcionService
    {
        /// <summary>
        /// Obtiene las ofertas disponibles para una persona, validando datos locales.
        /// </summary>
        Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerOfertasParaPersonaAsync(
            long codigoPersona,
            long idProducto,
            long idComienzo,
            long idTurno
        );

    }
}
