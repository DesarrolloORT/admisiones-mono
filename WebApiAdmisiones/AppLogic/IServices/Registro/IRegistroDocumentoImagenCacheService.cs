using AppLogic.Dtos.Registro;

namespace AppLogic.IServices.Registro;

public interface IRegistroDocumentoImagenCacheService
{
    Task GuardarAsync(
        string tipoDocumento,
        string documento,
        DtoRegistroDocumentoImagenesTemporales imagenes);

    Task<DtoRegistroDocumentoImagenesTemporales?> ObtenerAsync(string tipoDocumento, string documento);

    Task EliminarAsync(string tipoDocumento, string documento);
}
