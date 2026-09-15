using AppLogic.Identity.Dtos;
using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Constants;
using AppLogic.Identity;
using AppLogic.Contracts;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Identity.Services;

public static class IdentityDocumentService
{
    private const string TipoImagenDocumentoIdentidadPersistido = "1";

    /// <summary>Código de error cuando falta subir el documento (frente o dorso), sea temporal o definitivo.</summary>
    private const string CodigoDocumentoFaltante = "INS_CPI_09";

    public static bool IsValidDocumentSide(int tipo)
    {
        return tipo == IdentityDocumentConstants.ImageType.Front
            || tipo == IdentityDocumentConstants.ImageType.Back;
    }

    public static OperationResult<IdentityDocumentQuery?> GetOptionalDocumentForQuery(
        IUnitOfWork uow,
        long personId,
        int tipo,
        DateTime? finalDocumentExpirationDate,
        string methodName)
    {
        if (!IsValidDocumentSide(tipo))
        {
            return OperationResult<IdentityDocumentQuery?>.IsFailed(
                "GEN_DA_01",
                methodName,
                "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).",
                400);
        }

        var temporal = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(personId, tipo);
        if (temporal is not null)
        {
            var temporaryValidation = ValidateTemporaryDocumentForQuery(temporal, methodName);
            if (!temporaryValidation.Success)
                return temporaryValidation.Failure().As<IdentityDocumentQuery?>(methodName);

            return OperationResult<IdentityDocumentQuery?>.Ok(
                new IdentityDocumentQuery
                {
                    File = new IdentityDocumentFile
                    {
                        FileName = temporal.NombreImagen,
                        Content = temporaryValidation.Data
                    },
                    ExpirationDate = temporal.FechaVtoDocumentoPersona
                },
                methodName);
        }

        var definitivo = uow.Imagens.GetDocumentoByPersonaAndTipo(personId, tipo);
        if (definitivo is null)
        {
            return OperationResult<IdentityDocumentQuery?>.Ok(null, methodName);
        }

        var finalValidation = ValidateFinalDocumentForQuery(
            definitivo,
            finalDocumentExpirationDate,
            methodName);
        if (!finalValidation.Success)
            return finalValidation.Failure().As<IdentityDocumentQuery?>(methodName);

        return OperationResult<IdentityDocumentQuery?>.Ok(
            new IdentityDocumentQuery
            {
                File = new IdentityDocumentFile
                {
                    FileName = definitivo.NombreImagen,
                    Content = finalValidation.Data
                },
                ExpirationDate = finalDocumentExpirationDate
            },
            methodName);
    }

    public static OperationResult<bool> ValidateIdentityDocumentsForConfirmation(
        IUnitOfWork uow,
        Persona person,
        string methodName)
    {
        var (temporales, _) = ValidateTemporaryPairForConfirmation(uow, person.CodigoPersona, methodName);
        if (temporales.Success)
        {
            return temporales;
        }

        var (definitivos, definitivoEsDorso) = ValidateFinalPairForConfirmation(uow, person, methodName);
        if (definitivos.Success)
        {
            return definitivos;
        }

        if (!IsMissingDocument(temporales))
        {
            return temporales;
        }

        if (!IsMissingDocument(definitivos) || definitivoEsDorso)
        {
            return definitivos;
        }

        return temporales;
    }

    private static bool IsMissingDocument(OperationResult<bool> result)
        => result.ErrorCode == CodigoDocumentoFaltante;

    public static OperationResult<bool> ValidateDocumentExpiration(
        DateTime? dueDate,
        string methodName,
        string errorCode)
    {
        if (dueDate.HasValue && dueDate.Value.Date < DateTime.Today)
        {
            return OperationResult<bool>.IsFailed(
                errorCode,
                methodName,
                "El documento de identidad se encuentra vencido.",
                409);
        }

        return OperationResult<bool>.Ok(true, methodName);
    }

    public static OperationResult<ImagenTemporal> CreateTemporaryDocument(
        long personId,
        int idImagenTemporal,
        int tipo,
        DateTime fecha,
        byte[] fileContent,
        string fileName,
        string methodName)
    {
        var validation = FileValidator.ValidateImageFile(fileContent, fileName, methodName);
        if (!validation.Success)
            return validation.Failure().As<ImagenTemporal>(methodName);

        return OperationResult<ImagenTemporal>.Ok(
            new ImagenTemporal
            {
                IdImagenTemporal = idImagenTemporal,
                CodigoPersona = personId,
                NombreImagen = BuildPersistedName(
                    personId,
                    tipo,
                    ".jpg"),
                TipoImagen = TipoImagenDocumentoIdentidadPersistido,
                BlobImagen = fileContent,
                FechaVtoDocumentoPersona = fecha
            },
            methodName);
    }

    public static OperationResult<bool> UpdateTemporaryDocument(
        ImagenTemporal existing,
        int tipo,
        DateTime fecha,
        byte[] fileContent,
        string fileName,
        string methodName)
    {
        var validation = FileValidator.ValidateImageFile(fileContent, fileName, methodName);
        if (!validation.Success)
            return validation.Failure().As<bool>(methodName);

        existing.NombreImagen = BuildPersistedName(
            existing.CodigoPersona ?? 0,
            tipo,
            ".jpg");
        existing.TipoImagen = TipoImagenDocumentoIdentidadPersistido;
        existing.BlobImagen = fileContent;
        existing.FechaVtoDocumentoPersona = fecha;

        return OperationResult<bool>.Ok(true, methodName);
    }

    public static OperationResult<bool> ValidateRecognizedDocumentImages(
        TemporaryDocumentImages? images,
        string methodName)
    {
        if (images is null)
        {
            return OperationResult<bool>.Ok(true, methodName);
        }

        var document = images.DocumentFront;
        var documentValidation = FileValidator.ValidateImageFile(
            document.Content,
            ResolveFileName(document.FileName, "documento.jpg"),
            methodName);
        if (!documentValidation.Success)
        {
            return documentValidation;
        }

        if (images.PersonFace is null)
        {
            return OperationResult<bool>.Ok(true, methodName);
        }

        var cara = images.PersonFace;
        return FileValidator.ValidateImageFile(
            cara.Content,
            ResolveFileName(cara.FileName, "cara.jpg"),
            methodName);
    }

    public static void SaveRecognizedDocumentImages(
        IUnitOfWork uow,
        IDbConnectionContext dbConnectionContext,
        Persona person,
        TemporaryDocumentImages? images)
    {
        if (images is null)
        {
            return;
        }

        var document = images.DocumentFront;
        var dueDate = images.ExpirationDate ?? DateTime.Today.AddYears(1);
        var existingDocument = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(
            person.CodigoPersona,
            IdentityDocumentConstants.ImageType.Front);

        if (existingDocument is null)
        {
            uow.ImagenTemporals.Add(new ImagenTemporal
            {
                IdImagenTemporal = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL),
                CodigoPersona = person.CodigoPersona,
                NombreImagen = BuildPersistedName(
                    person.CodigoPersona,
                    IdentityDocumentConstants.ImageType.Front,
                    ".jpg"),
                TipoImagen = TipoImagenDocumentoIdentidadPersistido,
                BlobImagen = document.Content,
                FechaVtoDocumentoPersona = dueDate
            });
        }
        else
        {
            existingDocument.NombreImagen = BuildPersistedName(
                person.CodigoPersona,
                IdentityDocumentConstants.ImageType.Front,
                ".jpg");
            existingDocument.TipoImagen = TipoImagenDocumentoIdentidadPersistido;
            existingDocument.BlobImagen = document.Content;
            existingDocument.FechaVtoDocumentoPersona = dueDate;
            uow.ImagenTemporals.Update(existingDocument);
        }

        person.FechaVtoDocumentoPersona = dueDate;

        if (images.PersonFace is null)
        {
            return;
        }

        var cara = images.PersonFace;
        var existingPhoto = uow.Imagens.GetFotoByPersona(person.CodigoPersona);
        if (existingPhoto is null)
        {
            var newPhoto = new Imagen
            {
                IdImagen = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN),
                CodigoPersona = person.CodigoPersona
            };
            ApplyPhotoData(newPhoto, person.CodigoPersona, cara.Content, cara.FileName);
            uow.Imagens.Add(newPhoto);
        }
        else
        {
            ApplyPhotoData(existingPhoto, person.CodigoPersona, cara.Content, cara.FileName);
            uow.Imagens.Update(existingPhoto);
        }
    }

    public static string ResolvePersistedExtension(string? fileName, string defaultExtension)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return string.IsNullOrWhiteSpace(extension) ? defaultExtension : extension;
    }

    public static string BuildPersistedName(long personId, int tipoImagen, string extension)
    {
        return $"{personId}_{tipoImagen}{extension}";
    }

    /// <summary>
    /// Construcción pura de nombre/tipo/blob de la foto de persona (TipoImagenFoto); no valida el
    /// archivo (los callers validan con FileValidator cuando corresponde, cada uno con su propio gating).
    /// </summary>
    public static void ApplyPhotoData(Imagen image, long personId, byte[] fileContent, string? fileName)
    {
        var extension = ResolvePersistedExtension(fileName, ".jpg");
        image.NombreImagen = BuildPersistedName(personId, IdentityDocumentConstants.ImageType.PersonPhoto, extension);
        image.TipoImagen = IdentityDocumentConstants.ImageType.PersonPhoto.ToString();
        image.BlobImagen = fileContent;
    }

    public static string ResolveDocumentValidationCode(
        IdentityDocumentRules.DocumentValidationError error,
        string codigoTipoInvalido,
        string codigoOtro)
    {
        return error == IdentityDocumentRules.DocumentValidationError.InvalidDocumentType
            ? codigoTipoInvalido
            : codigoOtro;
    }

    public static async Task<TemporaryDocumentImages?> GetTemporaryImagesSafeAsync(
        IIdentityDocumentImageCache? cacheService,
        string? documentType,
        string? document,
        ILogger? logger)
    {
        if (cacheService is null ||
            string.IsNullOrWhiteSpace(documentType) ||
            string.IsNullOrWhiteSpace(document))
        {
            return null;
        }

        try
        {
            return await cacheService.GetAsync(documentType, document);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "No se pudieron obtener imagenes temporales de documento para {TipoDocumento}:{Documento}.",
                documentType,
                document);
            return null;
        }
    }

    public static async Task DeleteTemporaryImagesSafeAsync(
        IIdentityDocumentImageCache? cacheService,
        string? documentType,
        string? document,
        ILogger? logger)
    {
        if (cacheService is null ||
            string.IsNullOrWhiteSpace(documentType) ||
            string.IsNullOrWhiteSpace(document))
        {
            return;
        }

        try
        {
            await cacheService.DeleteAsync(documentType, document);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "No se pudieron eliminar imagenes temporales de documento para {TipoDocumento}:{Documento}.",
                documentType,
                document);
        }
    }

    private static (OperationResult<bool> Resultado, bool EsDorso) ValidateTemporaryPairForConfirmation(
        IUnitOfWork uow,
        long personId,
        string methodName)
    {
        var frente = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(
            personId,
            IdentityDocumentConstants.ImageType.Front);
        var frontValidation = ValidateTemporaryDocumentForConfirmation(frente, "frente", methodName);
        if (!frontValidation.Success)
        {
            return (frontValidation, false);
        }

        var dorso = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(
            personId,
            IdentityDocumentConstants.ImageType.Back);
        var backValidation = ValidateTemporaryDocumentForConfirmation(dorso, "dorso", methodName);
        if (!backValidation.Success)
        {
            return (backValidation, true);
        }

        return (OperationResult<bool>.Ok(true, methodName), false);
    }

    private static (OperationResult<bool> Resultado, bool EsDorso) ValidateFinalPairForConfirmation(
        IUnitOfWork uow,
        Persona person,
        string methodName)
    {
        var frente = uow.Imagens.GetDocumentoByPersonaAndTipo(
            person.CodigoPersona,
            IdentityDocumentConstants.ImageType.Front);
        var frontValidation = ValidateFinalDocumentForConfirmation(
            frente,
            person.FechaVtoDocumentoPersona,
            "frente",
            methodName);
        if (!frontValidation.Success)
        {
            return (frontValidation, false);
        }

        var dorso = uow.Imagens.GetDocumentoByPersonaAndTipo(
            person.CodigoPersona,
            IdentityDocumentConstants.ImageType.Back);
        var backValidation = ValidateFinalDocumentForConfirmation(
            dorso,
            person.FechaVtoDocumentoPersona,
            "dorso",
            methodName);
        if (!backValidation.Success)
        {
            return (backValidation, true);
        }

        return (OperationResult<bool>.Ok(true, methodName), false);
    }

    private static OperationResult<byte[]> ValidateTemporaryDocumentForQuery(
        ImagenTemporal document,
        string methodName)
    {
        var dateValidation = ValidateDocumentExpiration(
            document.FechaVtoDocumentoPersona,
            methodName,
            "GEN_DA_03");
        if (!dateValidation.Success)
            return dateValidation.Failure().As<byte[]>(methodName);

        if (document.BlobImagen == null || document.BlobImagen.Length == 0)
        {
            return OperationResult<byte[]>.IsFailed(
                "GEN_DA_04",
                methodName,
                "El documento no contiene imagen.",
                404);
        }

        return OperationResult<byte[]>.Ok(document.BlobImagen, methodName);
    }

    private static OperationResult<byte[]> ValidateFinalDocumentForQuery(
        Imagen document,
        DateTime? dueDate,
        string methodName)
    {
        var dateValidation = ValidateDocumentExpiration(
            dueDate,
            methodName,
            "GEN_DA_03");
        if (!dateValidation.Success)
            return dateValidation.Failure().As<byte[]>(methodName);

        if (document.BlobImagen == null || document.BlobImagen.Length == 0)
        {
            return OperationResult<byte[]>.IsFailed(
                "GEN_DA_04",
                methodName,
                "El documento no contiene imagen.",
                404);
        }

        return OperationResult<byte[]>.Ok(document.BlobImagen, methodName);
    }

    private static OperationResult<bool> ValidateTemporaryDocumentForConfirmation(
        ImagenTemporal? document,
        string lado,
        string methodName)
    {
        if (document == null)
        {
            return MissingDocument(lado, methodName);
        }

        var dateValidation = ExpiredDocument(document.FechaVtoDocumentoPersona, methodName);
        if (!dateValidation.Success)
        {
            return dateValidation;
        }

        if (document.BlobImagen == null || document.BlobImagen.Length == 0)
        {
            return DocumentWithoutImage(lado, methodName);
        }

        return OperationResult<bool>.Ok(true, methodName);
    }

    private static OperationResult<bool> ValidateFinalDocumentForConfirmation(
        Imagen? document,
        DateTime? dueDate,
        string lado,
        string methodName)
    {
        if (document == null)
        {
            return MissingDocument(lado, methodName);
        }

        var dateValidation = ExpiredDocument(dueDate, methodName);
        if (!dateValidation.Success)
        {
            return dateValidation;
        }

        if (document.BlobImagen == null || document.BlobImagen.Length == 0)
        {
            return DocumentWithoutImage(lado, methodName);
        }

        return OperationResult<bool>.Ok(true, methodName);
    }

    private static OperationResult<bool> MissingDocument(string lado, string methodName)
    {
        return OperationResult<bool>.IsFailed(
            CodigoDocumentoFaltante,
            methodName,
            $"Debe subir el documento de identidad ({lado}).",
            404);
    }

    private static OperationResult<bool> ExpiredDocument(DateTime? dueDate, string methodName)
    {
        return ValidateDocumentExpiration(
            dueDate,
            methodName,
            "INS_CPI_10");
    }

    private static OperationResult<bool> DocumentWithoutImage(string lado, string methodName)
    {
        return OperationResult<bool>.IsFailed(
            "INS_CPI_11",
            methodName,
            $"El documento de identidad ({lado}) no contiene imagen.",
            404);
    }

    private static string ResolveFileName(string? fileName, string defaultFileName)
    {
        return string.IsNullOrWhiteSpace(fileName) ? defaultFileName : fileName;
    }
}
