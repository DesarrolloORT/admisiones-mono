using AppLogic.Catalogos.Dtos;
using AppLogic.ApiClients.Dtos;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.Catalogos.Interfaces
{
    public interface ICatalogosService
    {
        /// <summary>
        /// Obtiene el catálogo de países con sus estados y ciudades para poblar formularios de admisión.
        /// </summary>
        /// <returns>Países con sus estados y ciudades disponibles.</returns>
        Task<OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>> ObtenerPaisesEstadosCiudadesAsync();

        /// <summary>
        /// Obtiene todos los combos estáticos necesarios para la encuesta inicial de admisión.
        /// </summary>
        /// <returns>Catálogos de la encuesta inicial agrupados por sección.</returns>
        OperationResult<DtoEncuestaInicialCatalogosResponse> ObtenerEncuestaInicial();

        /// <summary>
        /// Obtiene las carreras vigentes disponibles para la persona, agrupadas por nivel y escuela.
        /// </summary>
        /// <param name="codigoPersona">Código de la persona para la que se filtran las carreras disponibles.</param>
        /// <returns>Carreras vigentes agrupadas por nivel de producto y escuela.</returns>
        OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>> ObtenerCarreras(long codigoPersona);

        /// <summary>
        /// Obtiene los comienzos habilitados para una carrera.
        /// </summary>
        /// <param name="idCarrera">Identificador del producto/carrera seleccionado.</param>
        /// <returns>Comienzos habilitados para la carrera indicada.</returns>
        OperationResult<IEnumerable<DtoComienzoResponse>> ObtenerComienzos(long idCarrera);

        /// <summary>
        /// Obtiene los turnos/ofertas disponibles para una carrera y proceso. Según el nivel del producto, resuelve
        /// las ofertas contra las vistas Devart (niveles 3 y 4) o contra la API de Inscripciones y Pagos (niveles 1 y 2).
        /// </summary>
        /// <param name="idCarrera">Identificador del producto/carrera seleccionado.</param>
        /// <param name="idProceso">Identificador del proceso/comienzo seleccionado.</param>
        /// <returns>Ofertas disponibles para la combinación indicada.</returns>
        Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerTurnos(long idCarrera, long idProceso);

        /// <summary>
        /// Obtiene los bancos habilitados para pagos.
        /// </summary>
        /// <returns>Bancos disponibles.</returns>
        OperationResult<IEnumerable<DtoBancoDevart>> ObtenerBancos();

        /// <summary>
        /// Obtiene las instituciones educativas de un país y estado dados.
        /// </summary>
        /// <param name="codigoPais">Código del país.</param>
        /// <param name="codigoEstado">Código del estado/departamento.</param>
        /// <returns>Instituciones educativas para la ubicación indicada.</returns>
        OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado);

        /// <summary>
        /// Obtiene los fondos de beca vigentes para un producto.
        /// </summary>
        /// <param name="idProducto">Identificador del producto.</param>
        /// <returns>Fondos de beca vigentes para el producto indicado.</returns>
        OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto);
    }
}
