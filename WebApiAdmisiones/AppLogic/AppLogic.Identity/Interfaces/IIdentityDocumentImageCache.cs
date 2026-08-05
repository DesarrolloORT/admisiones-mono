using AppLogic.Identity.Dtos;

namespace AppLogic.Identity.Interfaces;

public interface IIdentityDocumentImageCache
{
    Task SaveAsync(
        string documentType,
        string document,
        TemporaryDocumentImages images);

    Task<TemporaryDocumentImages?> GetAsync(string documentType, string document);

    Task DeleteAsync(string documentType, string document);

    /// <summary>
    /// Arma el DTO de imágenes temporales de documento reconocido y lo guarda en cache si
    /// <paramref name="tipoDocumento"/>/<paramref name="numeroDocumento"/> son válidos. No lanza:
    /// si falla el guardado (Redis/serialización), se loguea como warning y no corta el flujo.
    /// </summary>
    Task SaveTemporaryImagesIfApplicableAsync(
        string? documentType,
        string? numeroDocumento,
        DateTime? dueDate,
        TemporaryDocumentFile documentFront,
        TemporaryDocumentFile? personFace);
}
