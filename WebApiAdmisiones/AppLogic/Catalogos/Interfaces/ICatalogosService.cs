using AppLogic.Catalogos.Dtos;
using AppLogic.ApiClients.Dtos;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.Catalogos.Interfaces
{
    public interface ICatalogosService
    {
        Task<OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>> ObtenerPaisesEstadosCiudadesAsync();
        OperationResult<DtoEncuestaInicialCatalogosResponse> ObtenerEncuestaInicial();
        OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>> ObtenerCarreras(long codigoPersona);
        OperationResult<IEnumerable<DtoComienzoResponse>> ObtenerComienzos(long idCarrera);
        Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerTurnos(long idCarrera, long idProceso);
        OperationResult<IEnumerable<DtoBancoDevart>> ObtenerBancos();
        OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado);
        OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto);
    }
}
