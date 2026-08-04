using AppLogic.ApiClients.Dtos;
using AppLogic.Catalogos.Interfaces;
using AppLogic.Catalogos.Dtos;
using AppLogic.DevartDTOs;
using Utilities;

namespace WebApiAdmisiones.Security.Cache
{
    /// <summary>
    /// Decorator de <see cref="ICatalogosService"/> que agrega cache distribuido (Redis) para los
    /// catálogos que no dependen de la persona ni cambian por acción del usuario: países/estados/
    /// ciudades, bancos y los combos de la encuesta inicial. El resto de los métodos delega directo
    /// al service real, sin cache. Movido desde CatalogosController para que el controller no conozca
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
        private const string PaisesCacheKey = "catalogos:paises-estados-ciudades";
        private const string PaisesOriginMethod = "ObtenerPaisesEstadosCiudades";
        private const string EncuestaInicialCacheKey = "catalogos:encuesta-inicial";
        private const string EncuestaInicialOriginMethod = "ObtenerEncuestaInicial";
        private const string BancosCacheKey = "catalogos:bancos";
        private const string BancosOriginMethod = "ObtenerBancos";

        public async Task<OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>> ObtenerPaisesEstadosCiudadesAsync()
        {
            var ttlHours = configuration.GetValue<int?>("Cache:CatalogosTTLHours") ?? 24;

            var cachedData = await cache.GetOrSetAsync(
                PaisesCacheKey,
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
            return OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>.Ok(cachedData, PaisesOriginMethod);
        }

        public async Task<OperationResult<DtoEncuestaInicialCatalogosResponse>> ObtenerEncuestaInicialAsync()
        {
            var ttlHours = configuration.GetValue<int?>("Cache:CatalogosTTLHours") ?? 24;

            var cachedData = await cache.GetOrSetAsync(
                EncuestaInicialCacheKey,
                async () =>
                {
                    var serviceResult = await inner.ObtenerEncuestaInicialAsync();
                    return serviceResult.Success ? serviceResult.Data : null;
                },
                TimeSpan.FromHours(ttlHours));

            if (cachedData == null)
            {
                return await inner.ObtenerEncuestaInicialAsync();
            }

            return OperationResult<DtoEncuestaInicialCatalogosResponse>.Ok(cachedData, EncuestaInicialOriginMethod);
        }

        public OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>> ObtenerCarreras(long codigoPersona, PropuestaAcademica propuestaAcademica)
            => inner.ObtenerCarreras(codigoPersona, propuestaAcademica);

        public OperationResult<IEnumerable<DtoComienzoResponse>> ObtenerComienzos(long idCarrera)
            => inner.ObtenerComienzos(idCarrera);

        public Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerTurnos(long idCarrera, long idProceso)
            => inner.ObtenerTurnos(idCarrera, idProceso);

        public async Task<OperationResult<IEnumerable<DtoBancoDevart>>> ObtenerBancosAsync()
        {
            var ttlHours = configuration.GetValue<int?>("Cache:CatalogosTTLHours") ?? 24;

            var cachedData = await cache.GetOrSetAsync(
                BancosCacheKey,
                async () =>
                {
                    var serviceResult = await inner.ObtenerBancosAsync();
                    return serviceResult.Success ? serviceResult.Data : null;
                },
                TimeSpan.FromHours(ttlHours));

            if (cachedData == null)
            {
                return await inner.ObtenerBancosAsync();
            }

            return OperationResult<IEnumerable<DtoBancoDevart>>.Ok(cachedData, BancosOriginMethod);
        }

        public OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado)
            => inner.ObtenerInstituciones(codigoPais, codigoEstado);

        public OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto)
            => inner.ObtenerFondosDeBecaPorProducto(idProducto);
    }
}
