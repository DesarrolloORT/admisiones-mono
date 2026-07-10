using AppLogic.Registro.Dtos;

namespace AppLogic.Registro.Interfaces;

public interface IRegistroDocumentoImagenCacheService
{
    Task GuardarAsync(
        string tipoDocumento,
        string documento,
        DtoRegistroDocumentoImagenesTemporales imagenes);

    Task<DtoRegistroDocumentoImagenesTemporales?> ObtenerAsync(string tipoDocumento, string documento);

    Task EliminarAsync(string tipoDocumento, string documento);

    /// <summary>
    /// Arma el DTO de imágenes temporales de documento reconocido y lo guarda en cache si
    /// <paramref name="tipoDocumento"/>/<paramref name="numeroDocumento"/> son válidos. No lanza:
    /// si falla el guardado (Redis/serialización), se loguea como warning y no corta el flujo.
    /// </summary>
    Task GuardarImagenesTemporalesSiCorrespondeAsync(
        string? tipoDocumento,
        string? numeroDocumento,
        DateTime? fechaVencimiento,
        DtoRegistroDocumentoArchivoTemporal documentoFrente,
        DtoRegistroDocumentoArchivoTemporal? caraPersona);
}
