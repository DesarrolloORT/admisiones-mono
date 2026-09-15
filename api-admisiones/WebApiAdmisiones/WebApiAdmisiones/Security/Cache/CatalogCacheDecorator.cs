using AppLogic.Catalogs.Interfaces;
using AppLogic.Catalogs.Dtos;
using Utilities;

namespace WebApiAdmisiones.Security.Cache
{
    /// <summary>
    /// Decorator de <see cref="ICatalogService"/> que agrega cache distribuido (Redis) para los
    /// catálogos que no dependen de la persona ni cambian por acción del usuario: países/estados/
    /// ciudades, bancos y los combos de la encuesta inicial. El resto de los métodos delega directo
    /// al service real, sin cache. Movido desde CatalogsController para que el controller no conozca
    /// la key, el TTL ni el fallback de cache.
    /// </summary>
    /// <remarks>
    /// <paramref name="inner"/> se tipa como la interfaz (no la clase concreta) para que el
    /// decorator sea testeable con un mock. En DI, la clase concreta <c>CatalogService</c> se
    /// registra por separado (ver DomainServicesExtensions) para poder inyectarla acá sin crear
    /// una resolución circular sobre <see cref="ICatalogService"/>.
    /// </remarks>
    public class CatalogCacheDecorator(
        ICatalogService inner,
        IRedisCacheService cache,
        IConfiguration configuration)
        : ICatalogService
    {
        private const string PaisesCacheKey = "catalogos:paises-estados-ciudades";
        private const string PaisesOriginMethod = "GetCountriesStatesCities";
        private const string EncuestaInicialCacheKey = "catalogos:encuesta-inicial";
        private const string EncuestaInicialOriginMethod = "GetInitialSurvey";
        private const string BancosCacheKey = "catalogos:bancos";
        private const string BancosOriginMethod = "GetBanks";

        public async Task<OperationResult<IEnumerable<CountryStateCityResponse>>> GetCountriesStatesCitiesAsync()
        {
            var ttlHours = configuration.GetValue<int?>("Cache:CatalogosTTLHours") ?? 24;

            var cachedData = await cache.GetOrSetAsync(
                PaisesCacheKey,
                async () =>
                {
                    // Factory: delegar la consulta al service real
                    var serviceResult = await inner.GetCountriesStatesCitiesAsync();

                    // Solo devolver la data si la operación fue exitosa
                    return serviceResult.Success ? serviceResult.Data : null;
                },
                TimeSpan.FromHours(ttlHours));

            // Si la cache devolvió null (error en factory o deserialización), ejecutar sin cache
            if (cachedData == null)
            {
                return await inner.GetCountriesStatesCitiesAsync();
            }

            // Envolver la data en un OperationResult para el caller (controller)
            return OperationResult<IEnumerable<CountryStateCityResponse>>.Ok(cachedData, PaisesOriginMethod);
        }

        public async Task<OperationResult<InitialSurveyCatalogsResponse>> GetInitialSurveyCatalogsAsync()
        {
            var ttlHours = configuration.GetValue<int?>("Cache:CatalogosTTLHours") ?? 24;

            var cachedData = await cache.GetOrSetAsync(
                EncuestaInicialCacheKey,
                async () =>
                {
                    var serviceResult = await inner.GetInitialSurveyCatalogsAsync();
                    return serviceResult.Success ? serviceResult.Data : null;
                },
                TimeSpan.FromHours(ttlHours));

            if (cachedData == null)
            {
                return await inner.GetInitialSurveyCatalogsAsync();
            }

            return OperationResult<InitialSurveyCatalogsResponse>.Ok(cachedData, EncuestaInicialOriginMethod);
        }

        public OperationResult<IEnumerable<DegreeProgramsByLevelResponse>> GetDegreePrograms(long personId, AcademicOffer academicOffer)
            => inner.GetDegreePrograms(personId, academicOffer);

        public OperationResult<IEnumerable<IntakeResponse>> GetIntakes(long degreeProgramId)
            => inner.GetIntakes(degreeProgramId);

        public OperationResult<List<OfferingResponse>> GetShifts(long personId, long degreeProgramId, long admissionProcessId)
            => inner.GetShifts(personId, degreeProgramId, admissionProcessId);

        public async Task<OperationResult<IEnumerable<BankResponse>>> GetBanksAsync()
        {
            var ttlHours = configuration.GetValue<int?>("Cache:CatalogosTTLHours") ?? 24;

            var cachedData = await cache.GetOrSetAsync(
                BancosCacheKey,
                async () =>
                {
                    var serviceResult = await inner.GetBanksAsync();
                    return serviceResult.Success ? serviceResult.Data : null;
                },
                TimeSpan.FromHours(ttlHours));

            if (cachedData == null)
            {
                return await inner.GetBanksAsync();
            }

            return OperationResult<IEnumerable<BankResponse>>.Ok(cachedData, BancosOriginMethod);
        }

        public OperationResult<IEnumerable<InstitutionResponse>> GetInstitutions(long countryId, long stateId)
            => inner.GetInstitutions(countryId, stateId);
    }
}
