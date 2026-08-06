using AppLogic.Catalogs.Dtos;
using Utilities;

namespace AppLogic.Catalogs.Interfaces;

public interface ICatalogService
{
    /// <summary>
    /// Obtiene el catálogo de países con sus estados y ciudades para poblar formularios de admisión.
    /// </summary>
    /// <returns>Países con sus estados y ciudades disponibles.</returns>
    Task<OperationResult<IEnumerable<CountryStateCityResponse>>> GetCountriesStatesCitiesAsync();

    /// <summary>
    /// Obtiene todos los combos estáticos necesarios para la encuesta inicial de admisión.
    /// </summary>
    /// <returns>Catálogos de la encuesta inicial agrupados por sección.</returns>
    Task<OperationResult<InitialSurveyCatalogsResponse>> GetInitialSurveyCatalogsAsync();

    /// <summary>
    /// Obtiene las carreras vigentes disponibles para la persona, para la propuesta académica indicada.
    /// </summary>
    /// <param name="personId">Código de la persona para la que se filtran las carreras disponibles.</param>
    /// <param name="academicOffer">Opción elegida en el paso "Propuesta académica" (carrera universitaria, tecnicatura o actualización profesional).</param>
    /// <returns>Carreras vigentes agrupadas por nivel de producto y escuela.</returns>
    OperationResult<IEnumerable<DegreeProgramsByLevelResponse>> GetDegreePrograms(long personId, AcademicOffer academicOffer);

    /// <summary>
    /// Obtiene los comienzos habilitados para una carrera.
    /// </summary>
    /// <param name="degreeProgramId">Identificador del producto/carrera seleccionado.</param>
    /// <returns>Comienzos habilitados para la carrera indicada.</returns>
    OperationResult<IEnumerable<IntakeResponse>> GetIntakes(long degreeProgramId);

    /// <summary>
    /// Obtiene los turnos/ofertas disponibles para una carrera y proceso. Según el nivel del producto, resuelve
    /// las ofertas contra las vistas Devart (niveles 3 y 4) o contra la API de Inscripciones y Pagos (niveles 1 y 2).
    /// </summary>
    /// <param name="personId">Persona autenticada: en niveles 3 y 4 se excluyen las ofertas en las que ya está inscripta.</param>
    /// <param name="degreeProgramId">Identificador del producto/carrera seleccionado.</param>
    /// <param name="admissionProcessId">Identificador del proceso/comienzo seleccionado.</param>
    /// <returns>Ofertas disponibles para la combinación indicada.</returns>
    Task<OperationResult<List<OfferingResponse>>> GetShifts(long personId, long degreeProgramId, long admissionProcessId);

    /// <summary>
    /// Obtiene los bancos habilitados para pagos.
    /// </summary>
    /// <returns>Bancos disponibles.</returns>
    Task<OperationResult<IEnumerable<BankResponse>>> GetBanksAsync();

    /// <summary>
    /// Obtiene las instituciones educativas de un país y estado dados.
    /// </summary>
    /// <param name="countryId">Código del país.</param>
    /// <param name="stateId">Código del estado/departamento.</param>
    /// <returns>Instituciones educativas para la ubicación indicada.</returns>
    OperationResult<IEnumerable<InstitutionResponse>> GetInstitutions(long countryId, long stateId);
}
