using AppLogic.DTOs;

namespace AppLogic.IServices.Registro;

public interface IRegistroDocumentoImagenCacheService
{
    Task GuardarAsync(
        string tipoDocumento,
        string documento,
        RegistroDocumentoImagenesTemporales imagenes);

    Task<RegistroDocumentoImagenesTemporales?> ObtenerAsync(string tipoDocumento, string documento);

    Task EliminarAsync(string tipoDocumento, string documento);
}
