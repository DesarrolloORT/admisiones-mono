using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices
{
    public interface ICatalogosService
    {
        OperationResult<DtoPaisDevart> ObtenerPais(long idPais);

        // Versión síncrona (legacy, mantener para compatibilidad)
        OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>> ObtenerPaisesEstadosCiudades();

        // Versión asíncrona (preferida, soporta cache distribuido)
        Task<OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>> ObtenerPaisesEstadosCiudadesAsync();

        OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos();
        OperationResult<IEnumerable<DtoComienzoResponse>> ObtenerComienzos(long idCarrera);
        OperationResult<IEnumerable<DtoCarreraResponse>> ObtenerCarreras();
        OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>> ObtenerMotivosEleccion();
        OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>> ObtenerPublicidadesEleccion();
        OperationResult<IEnumerable<DtoTituloDevart>> ObtenerBachilleratos(long idAnioBachillerato);
        OperationResult<DtoAnioBachillerDevart> ObtenerAnioBachiller(long idAnioBachillerato);
        OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerInstituciones(long codigoPais, long codigoEstado);
        OperationResult<IEnumerable<DtoEmpresaDevart>> ObtenerUniversidades();
        OperationResult<IEnumerable<DtoProductoBeca>> ObtenerProductosBeca(long codigoPersona);
        OperationResult<IEnumerable<DtoTipoDescuentoDevart>> ObtenerFondosDeBecaPorProducto(long idProducto);
    }
}
