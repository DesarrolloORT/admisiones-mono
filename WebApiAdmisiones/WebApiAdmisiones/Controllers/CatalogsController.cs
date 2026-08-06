using AppLogic.Catalogs.Interfaces;
using AppLogic.Catalogs.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security.Authentication;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("catalogs")]
    public class CatalogsController(
        ICatalogService catalogosService,
        ILogger<CatalogsController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<CatalogsController>(logger, currentUser)
    {
        #region CATALOGOS

        /// <summary>
        /// Lista paises, estados y ciudades para formularios de admision.
        /// </summary>
        /// <remarks>
        /// Endpoint publico para poblar combos de ubicacion. Devuelve una respuesta liviana con codigos y nombres, manteniendo Uruguay primero, luego paises por nombre, y estados/ciudades ordenados alfabeticamente.
        /// </remarks>
        /// <returns>Paises con sus estados y ciudades disponibles.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">Solicitud invalida.</response>
        [AllowAnonymous]
        [HttpGet("countries-states-cities")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<CountryStateCityResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<CountryStateCityResponse>>), 400)]
        public async Task<IActionResult> GetCountriesStatesCities()
        {
            var result = await catalogosService.GetCountriesStatesCitiesAsync();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Lista todos los combos estaticos necesarios para la encuesta inicial de admision.
        /// </summary>
        /// <remarks>
        /// Endpoint para poblar la encuesta inicial con los valores canonicos que valida y persiste backend.
        /// </remarks>
        /// <returns>Catalogos de encuesta inicial agrupados por campo.</returns>
        /// <response code="200">Catalogos obtenidos correctamente.</response>
        [HttpGet("initial-survey")]
        [ProducesResponseType(typeof(OperationResult<InitialSurveyCatalogsResponse>), 200)]
        public async Task<IActionResult> GetInitialSurvey()
        {
            var result = await catalogosService.GetInitialSurveyCatalogsAsync();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Lista las carreras vigentes para el registro de admision.
        /// </summary>
        /// <remarks>
        /// Endpoint para poblar la seleccion inicial de carrera/producto. Devuelve identificadores y nombres necesarios para que el front luego consulte comienzos.
        /// </remarks>
        /// <param name="academicOffer">Opción elegida: CarreraUniversitaria, Tecnicatura o ActualizacionProfesional.</param>
        /// <returns>Carreras vigentes disponibles para admision.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">No se informó la propuesta académica o el valor no es válido.</response>
        [HttpGet("degree-programs")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DegreeProgramsByLevelResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DegreeProgramsByLevelResponse>>), 400)]
        public IActionResult GetDegreePrograms([FromQuery] AcademicOffer academicOffer)
        {
            var result = catalogosService.GetDegreePrograms(_currentUser.GetUserId(), academicOffer);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Lista los comienzos habilitados para una carrera.
        /// </summary>
        /// <remarks>
        /// Endpoint para poblar el combo de comienzo/proceso luego de seleccionar una carrera. El parametro degreeProgramId debe ser el IdProducto recibido desde el endpoint de carreras.
        /// </remarks>
        /// <param name="degreeProgramId">Identificador de la carrera/producto seleccionado.</param>
        /// <returns>Comienzos habilitados para la carrera indicada.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">Carrera invalida o solicitud invalida.</response>
        [HttpGet("intakes")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<IntakeResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<IntakeResponse>>), 400)]
        public IActionResult GetIntakes([FromQuery] long degreeProgramId)
        {
            var result = catalogosService.GetIntakes(degreeProgramId);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Lista las ofertas disponibles para una carrera y proceso.
        /// </summary>
        /// <remarks>
        /// Endpoint para obtener las ofertas disponibles en Inscripciones y Pagos. En niveles 3 y 4 se
        /// excluyen las ofertas en las que la persona autenticada ya tiene una inscripcion vigente.
        /// </remarks>
        /// <param name="degreeProgramId">Identificador de la carrera/producto seleccionado.</param>
        /// <param name="admissionProcessId">Identificador del proceso/comienzo seleccionado.</param>
        /// <returns>Ofertas disponibles para la combinacion indicada.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">Carrera, proceso o solicitud invalida.</response>
        [HttpGet("shifts")]
        [ProducesResponseType(typeof(OperationResult<List<OfferingResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<List<OfferingResponse>>), 400)]
        public async Task<IActionResult> GetShifts([FromQuery] long degreeProgramId, [FromQuery] long admissionProcessId)
        {
            var result = await catalogosService.GetShifts(_currentUser.GetUserId(), degreeProgramId, admissionProcessId);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los bancos disponibles para pagos.
        /// </summary>
        /// <returns>Lista de bancos disponibles.</returns>
        /// <response code="200">Bancos obtenidos correctamente.</response>
        /// <response code="400">Solicitud invalida o rechazada por la API interna.</response>
        /// <response code="500">Error inesperado al obtener bancos.</response>
        /// <response code="502">La API interna no devolvio datos validos.</response>
        [HttpGet("banks")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<BankResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<BankResponse>>), 400)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<BankResponse>>), 500)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<BankResponse>>), 502)]
        public async Task<IActionResult> GetBanks()
        {
            var result = await catalogosService.GetBanksAsync();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las instituciones educativas para un país y estado dados.
        /// </summary>
        /// <param name="countryId">Código del país.</param>
        /// <param name="stateId">Código del estado/departamento.</param>
        /// <returns>Lista de instituciones educativas.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("institutions")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<InstitutionResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<InstitutionResponse>>), 400)]
        public IActionResult GetInstitutions([FromQuery] long countryId, [FromQuery] long stateId)
        {
            var result = catalogosService.GetInstitutions(countryId, stateId);
            return ValidateResponse(result);
        }

        #endregion
    }
}
