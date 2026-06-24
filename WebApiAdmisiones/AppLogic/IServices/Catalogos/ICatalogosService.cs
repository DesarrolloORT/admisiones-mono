using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices.Catalogos
{
    public interface ICatalogosService
    {
        // Versión síncrona (legacy, mantener para compatibilidad)
        OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>> ObtenerPaisesEstadosCiudades();
        // Versión asíncrona (preferida, soporta cache distribuido)
        Task<OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>> ObtenerPaisesEstadosCiudadesAsync();
        OperationResult<DtoEncuestaInicialCatalogosResponse> ObtenerEncuestaInicial();
        OperationResult<IEnumerable<DtoCarreraResponse>> ObtenerCarreras();
        OperationResult<IEnumerable<DtoComienzoResponse>> ObtenerComienzos(long idCarrera);
        Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerTurnos(long idCarrera, long idProceso);
        OperationResult<IEnumerable<DtoBancoDevart>> ObtenerBancos();
        OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado);
        //OperationResult<IEnumerable<DtoProductoBeca>> ObtenerProductosBeca(long codigoPersona);
        OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto);
    }
}
