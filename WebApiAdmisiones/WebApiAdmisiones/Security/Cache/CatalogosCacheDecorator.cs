using AppLogic.ApiClients;
using AppLogic.Catalogos.Interfaces;
using AppLogic.Catalogos.Responses;
using AppLogic.DevartDTOs;
using Utilities;

namespace WebApiAdmisiones.Security.Cache
{
    /// <summary>
    /// Decorator de <see cref="ICatalogosService"/> que agrega cache distribuido (Redis) solo para
    /// <see cref="ObtenerPaisesEstadosCiudadesAsync"/>. El resto de los métodos delega directo al
    /// service real, sin cache. Movido desde CatalogosController para que el controller no conozca
    /// la key, el TTL ni el fallback de cache.
    /// </summary>
    /// <remarks>
    /// <paramref name="inner"/> se tipa como la interfaz (no la clase concreta) para que el
    /// decorator sea testeable con un mock. En DI, la clase concreta <c>CatalogosService</c> se
    /// registra por separado (ver DomainServicesExtensions) para poder inyectarla acá sin crear
    /// una resolución circular sobre <see cref="ICatalogosService"/>.
    /// </remarks>
    public class CatalogosCacheDecorator(
        ICatalogosService inner,
        IRedisCacheService cache,
        IConfiguration configuration)
        : ICatalogosService
    {
        private const string CacheKey = "catalogos:paises-estados-ciudades";
        private const string OriginMethod = "ObtenerPaisesEstadosCiudades";

        public async Task<OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>> ObtenerPaisesEstadosCiudadesAsync()
        {
            var ttlHours = configuration.GetValue<int?>("Cache:CatalogosTTLHours") ?? 24;

            var cachedData = await cache.GetOrSetAsync(
                CacheKey,
                async () =>
                {
                    // Factory: delegar la consulta al service real
                    var serviceResult = await inner.ObtenerPaisesEstadosCiudadesAsync();

                    // Solo devolver la data si la operación fue exitosa
                    return serviceResult.Success ? serviceResult.Data : null;
                },
                TimeSpan.FromHours(ttlHours));

            // Si la cache devolvió null (error en factory o deserialización), ejecutar sin cache
            if (cachedData == null)
            {
                return await inner.ObtenerPaisesEstadosCiudadesAsync();
            }

            // Envolver la data en un OperationResult para el caller (controller)
            return OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>.Ok(cachedData, OriginMethod);
        }

        public OperationResult<DtoEncuestaInicialCatalogosResponse> ObtenerEncuestaInicial()
            => inner.ObtenerEncuestaInicial();

        public OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>> ObtenerCarreras(long codigoPersona)
            => inner.ObtenerCarreras(codigoPersona);

        public OperationResult<IEnumerable<DtoComienzoResponse>> ObtenerComienzos(long idCarrera)
            => inner.ObtenerComienzos(idCarrera);

        public Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerTurnos(long idCarrera, long idProceso)
            => inner.ObtenerTurnos(idCarrera, idProceso);

        public OperationResult<IEnumerable<DtoBancoDevart>> ObtenerBancos()
            => inner.ObtenerBancos();

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado)
            => inner.ObtenerInstituciones(codigoPais, codigoEstado);

        public OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto)
            => inner.ObtenerFondosDeBecaPorProducto(idProducto);
    }
}
