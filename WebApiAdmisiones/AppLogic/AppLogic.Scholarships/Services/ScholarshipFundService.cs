using AppLogic.Scholarships.Dtos;
using AppLogic.DevartDTOs;
using AppLogic.Scholarships.Validators;
using AppLogic.Scholarships.Interfaces;
using AppLogic.Contracts;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Scholarships.Services;

public class ScholarshipFundService(IUnitOfWorkFactory uowFactory) : IScholarshipFundService
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    #region TIPOS DECLARACIÓN JURADA

    public OperationResult<IEnumerable<DtoTipoParentescoDevart>> GetKinshipTypes()
    {
        using var uow = _uowFactory.Create();
        var entidades = uow.TipoParentescos.GetAll().ToList();
        return OperationResult<IEnumerable<DtoTipoParentescoDevart>>.Ok(
            entidades.ToDtos(),
            nameof(GetKinshipTypes));
    }

    public OperationResult<IEnumerable<DtoTipoEgresoDjDevart>> GetExpenseTypes()
    {
        using var uow = _uowFactory.Create();
        var entidades = uow.TipoEgresoDjs.GetActivosOrdenados().ToList();
        return OperationResult<IEnumerable<DtoTipoEgresoDjDevart>>.Ok(
            entidades.ToDtos(),
            nameof(GetExpenseTypes));
    }

    public OperationResult<IEnumerable<DtoTipoViviendaDevart>> GetHousingTypes()
    {
        using var uow = _uowFactory.Create();
        var entidades = uow.TipoViviendas.GetAll().ToList();
        return OperationResult<IEnumerable<DtoTipoViviendaDevart>>.Ok(
            entidades.ToDtos(),
            nameof(GetHousingTypes));
    }

    #endregion

    #region UNIVERSIDADES

    public OperationResult<IEnumerable<DtoEmpresaDevart>> GetUniversities(long countryId)
    {
        using var uow = _uowFactory.Create();

        var country = uow.Paises.GetPaisConEstadosYCiudades(countryId);
        if (country == null)
            return OperationResult<IEnumerable<DtoEmpresaDevart>>.IsFailed(
                "FDB_UV_01", nameof(GetUniversities), "El país indicado es inválido.", 400);

        var entidades = uow.Empresas.GetUniversidades(countryId).ToList();
        return OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(
            entidades.ToDtos(),
            nameof(GetUniversities));
    }

    #endregion

    #region DECLARACIÓN JURADA

    public OperationResult<bool> UploadIncomeFile(long personId, long idIngresoMensualNF, byte[] fileContent, string fileName)
    {
        var validatedFile = AffidavitValidation.ValidateAttachment(fileContent, fileName, nameof(UploadIncomeFile));
        if (!validatedFile.Success)
            return validatedFile.Failure().As<bool>();

        using var uow = _uowFactory.Create();
        var ingresoResult = GetAuthorizedIncome(uow, personId, idIngresoMensualNF, nameof(UploadIncomeFile), "FDB_SAI");
        if (!ingresoResult.Success || ingresoResult.Data is null)
            return ingresoResult.Failure().As<bool>();

        var ingreso = ingresoResult.Data;
        ingreso.NombreArchivoIngreso = Path.GetFileNameWithoutExtension(validatedFile.Data) ?? string.Empty;
        ingreso.ExtensionArchivoIngreso = Path.GetExtension(validatedFile.Data) ?? string.Empty;
        ingreso.ArchivoIngresoNfDj = fileContent;
        uow.Save();

        return OperationResult<bool>.Ok(true, nameof(UploadIncomeFile));
    }

    public OperationResult<FileDownload> DownloadIncomeFile(long personId, long idIngresoMensualNF)
    {
        using var uow = _uowFactory.Create();
        var ingresoResult = GetAuthorizedIncome(uow, personId, idIngresoMensualNF, nameof(DownloadIncomeFile), "FDB_DAI");
        if (!ingresoResult.Success || ingresoResult.Data is null)
            return ingresoResult.Failure().As<FileDownload>();

        var ingreso = ingresoResult.Data;
        var file = ingreso.ArchivoIngresoNfDj;
        if (file is null || file.Length == 0)
            return OperationResult<FileDownload>.IsFailed(
                "FDB_DAI_05", nameof(DownloadIncomeFile), "El ingreso mensual indicado no tiene archivo adjunto.", 404);

        var extension = NormalizeExtension(ingreso.ExtensionArchivoIngreso);
        var fileName = BuildFileName(ingreso.NombreArchivoIngreso, extension, $"ingreso_{idIngresoMensualNF}");

        return OperationResult<FileDownload>.Ok(
            new FileDownload
            {
                Content = file,
                FileName = fileName,
                ContentType = GetContentType(extension)
            },
            nameof(DownloadIncomeFile));
    }

    public OperationResult<bool> DeleteIncomeFile(long personId, long idIngresoMensualNF)
    {
        using var uow = _uowFactory.Create();
        var ingresoResult = GetAuthorizedIncome(uow, personId, idIngresoMensualNF, nameof(DeleteIncomeFile), "FDB_EAI");
        if (!ingresoResult.Success || ingresoResult.Data is null)
            return ingresoResult.Failure().As<bool>();

        var ingreso = ingresoResult.Data;
        ingreso.NombreArchivoIngreso = string.Empty;
        ingreso.ExtensionArchivoIngreso = string.Empty;
        ingreso.ArchivoIngresoNfDj = null;
        uow.Save();

        return OperationResult<bool>.Ok(true, nameof(DeleteIncomeFile));
    }

    public OperationResult<bool> UploadExpenseFile(long personId, long idEgresoMensualNF, byte[] fileContent, string fileName)
    {
        var validatedFile = AffidavitValidation.ValidateAttachment(fileContent, fileName, nameof(UploadExpenseFile));
        if (!validatedFile.Success)
            return validatedFile.Failure().As<bool>();

        using var uow = _uowFactory.Create();
        var egresoResult = GetAuthorizedExpense(uow, personId, idEgresoMensualNF, nameof(UploadExpenseFile), "FDB_SAE");
        if (!egresoResult.Success || egresoResult.Data is null)
            return egresoResult.Failure().As<bool>();

        var egreso = egresoResult.Data;
        egreso.NombreArchivoEgreso = Path.GetFileNameWithoutExtension(validatedFile.Data) ?? string.Empty;
        egreso.ExtensionArchivoEgreso = Path.GetExtension(validatedFile.Data) ?? string.Empty;
        egreso.ArchivoEgresoMensualNfDj = fileContent;
        uow.Save();

        return OperationResult<bool>.Ok(true, nameof(UploadExpenseFile));
    }

    public OperationResult<FileDownload> DownloadExpenseFile(long personId, long idEgresoMensualNF)
    {
        using var uow = _uowFactory.Create();
        var egresoResult = GetAuthorizedExpense(uow, personId, idEgresoMensualNF, nameof(DownloadExpenseFile), "FDB_DAE");
        if (!egresoResult.Success || egresoResult.Data is null)
            return egresoResult.Failure().As<FileDownload>();

        var egreso = egresoResult.Data;
        var file = egreso.ArchivoEgresoMensualNfDj;
        if (file is null || file.Length == 0)
            return OperationResult<FileDownload>.IsFailed(
                "FDB_DAE_03", nameof(DownloadExpenseFile), "El egreso mensual indicado no tiene archivo adjunto.", 404);

        var extension = NormalizeExtension(egreso.ExtensionArchivoEgreso);
        var fileName = BuildFileName(egreso.NombreArchivoEgreso, extension, $"egreso_{idEgresoMensualNF}");

        return OperationResult<FileDownload>.Ok(
            new FileDownload
            {
                Content = file,
                FileName = fileName,
                ContentType = GetContentType(extension)
            },
            nameof(DownloadExpenseFile));
    }

    public OperationResult<bool> DeleteExpenseFile(long personId, long idEgresoMensualNF)
    {
        using var uow = _uowFactory.Create();
        var egresoResult = GetAuthorizedExpense(uow, personId, idEgresoMensualNF, nameof(DeleteExpenseFile), "FDB_EAE");
        if (!egresoResult.Success || egresoResult.Data is null)
            return egresoResult.Failure().As<bool>();

        var egreso = egresoResult.Data;
        egreso.NombreArchivoEgreso = string.Empty;
        egreso.ExtensionArchivoEgreso = string.Empty;
        egreso.ArchivoEgresoMensualNfDj = null;
        uow.Save();

        return OperationResult<bool>.Ok(true, nameof(DeleteExpenseFile));
    }

    public OperationResult<bool> UploadRevalidationFile(long personId, long idDeclaracionJuradaWeb, byte[] fileContent, string fileName)
    {
        var validatedFile = AffidavitValidation.ValidateAttachment(fileContent, fileName, nameof(UploadRevalidationFile));
        if (!validatedFile.Success)
            return validatedFile.Failure().As<bool>();

        using var uow = _uowFactory.Create();
        var affidavitResult = GetAuthorizedAffidavit(uow, personId, idDeclaracionJuradaWeb, nameof(UploadRevalidationFile), "FDB_SAR");
        if (!affidavitResult.Success || affidavitResult.Data is null)
            return affidavitResult.Failure().As<bool>();

        var affidavit = affidavitResult.Data;
        affidavit.NombrePdfRevalidasDj = Path.GetFileNameWithoutExtension(validatedFile.Data);
        affidavit.ExtensionPdfRevalidasDj = Path.GetExtension(validatedFile.Data);
        affidavit.PdfFormRevalidasDj = fileContent;
        uow.Save();

        return OperationResult<bool>.Ok(true, nameof(UploadRevalidationFile));
    }

    public OperationResult<FileDownload> DownloadRevalidationFile(long personId, long idDeclaracionJuradaWeb)
    {
        using var uow = _uowFactory.Create();
        var affidavitResult = GetAuthorizedAffidavit(uow, personId, idDeclaracionJuradaWeb, nameof(DownloadRevalidationFile), "FDB_DAR");
        if (!affidavitResult.Success || affidavitResult.Data is null)
            return affidavitResult.Failure().As<FileDownload>();

        var affidavit = affidavitResult.Data;
        if (affidavit.PdfFormRevalidasDj is null || affidavit.PdfFormRevalidasDj.Length == 0)
            return OperationResult<FileDownload>.IsFailed(
                "FDB_DAR_03", nameof(DownloadRevalidationFile), "La declaración jurada indicada no tiene archivo de reválida adjunto.", 404);

        var extension = NormalizeExtension(affidavit.ExtensionPdfRevalidasDj);
        var fileName = BuildFileName(affidavit.NombrePdfRevalidasDj, extension, $"revalida_{idDeclaracionJuradaWeb}");

        return OperationResult<FileDownload>.Ok(
            new FileDownload
            {
                Content = affidavit.PdfFormRevalidasDj,
                FileName = fileName,
                ContentType = GetContentType(extension)
            },
            nameof(DownloadRevalidationFile));
    }

    public OperationResult<bool> DeleteRevalidationFile(long personId, long idDeclaracionJuradaWeb)
    {
        using var uow = _uowFactory.Create();
        var affidavitResult = GetAuthorizedAffidavit(uow, personId, idDeclaracionJuradaWeb, nameof(DeleteRevalidationFile), "FDB_EAR");
        if (!affidavitResult.Success || affidavitResult.Data is null)
            return affidavitResult.Failure().As<bool>();

        var affidavit = affidavitResult.Data;
        affidavit.NombrePdfRevalidasDj = string.Empty;
        affidavit.ExtensionPdfRevalidasDj = string.Empty;
        affidavit.PdfFormRevalidasDj = null;
        uow.Save();

        return OperationResult<bool>.Ok(true, nameof(DeleteRevalidationFile));
    }

    #endregion DECLARACIÓN JURADA

    #region MÉTODOS PRIVADOS

    private static bool BelongsToPerson(IUnitOfWork uow, decimal idDeclaracionJuradaWeb, long personId)
    {
        var affidavit = uow.DeclaracionJuradaWebs.GetByKey(idDeclaracionJuradaWeb);
        return affidavit?.CodigoPersona == personId;
    }

    private static OperationResult<BusinessLogic.Entities.IngresoMensualNfDj> GetAuthorizedIncome(
        IUnitOfWork uow,
        long personId,
        long idIngresoMensualNF,
        string methodName,
        string errorPrefix)
    {
        var ingreso = uow.IngresoMensualNfDjs.GetWithIntegranteYDeclaracion(idIngresoMensualNF);
        if (ingreso is null)
            return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.IsFailed(
                $"{errorPrefix}_01", methodName, "No se encontró el ingreso mensual indicado.", 404);

        var integrante = ingreso.IntegranteNfDj;
        if (integrante is null)
            return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.IsFailed(
                $"{errorPrefix}_02", methodName, "No se encontró el integrante asociado al ingreso mensual indicado.", 404);

        var affidavit = integrante.DeclaracionJuradaWeb;
        if (affidavit is null)
            return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.IsFailed(
                $"{errorPrefix}_03", methodName, "No se encontró la declaración jurada asociada al ingreso mensual indicado.", 404);

        if (affidavit.CodigoPersona != personId)
            return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.IsFailed(
                $"{errorPrefix}_04", methodName, "El ingreso mensual indicado no pertenece a la persona autenticada.", 403);

        return OperationResult<BusinessLogic.Entities.IngresoMensualNfDj>.Ok(ingreso, methodName);
    }

    private static OperationResult<BusinessLogic.Entities.EgresoMensualNfDj> GetAuthorizedExpense(
        IUnitOfWork uow,
        long personId,
        long idEgresoMensualNF,
        string methodName,
        string errorPrefix)
    {
        var egreso = uow.EgresoMensualNfDjs.GetByKey(idEgresoMensualNF);
        if (egreso is null)
            return OperationResult<BusinessLogic.Entities.EgresoMensualNfDj>.IsFailed(
                $"{errorPrefix}_01", methodName, "No se encontró el egreso mensual indicado.", 404);

        if (!BelongsToPerson(uow, egreso.IdDeclaracionjuradaWeb, personId))
            return OperationResult<BusinessLogic.Entities.EgresoMensualNfDj>.IsFailed(
                $"{errorPrefix}_02", methodName, "El egreso mensual indicado no pertenece a la persona autenticada.", 403);

        return OperationResult<BusinessLogic.Entities.EgresoMensualNfDj>.Ok(egreso, methodName);
    }

    private static OperationResult<BusinessLogic.Entities.DeclaracionJuradaWeb> GetAuthorizedAffidavit(
        IUnitOfWork uow,
        long personId,
        long idDeclaracionJuradaWeb,
        string methodName,
        string errorPrefix)
    {
        var affidavit = uow.DeclaracionJuradaWebs.GetByKey(idDeclaracionJuradaWeb);
        if (affidavit is null)
            return OperationResult<BusinessLogic.Entities.DeclaracionJuradaWeb>.IsFailed(
                $"{errorPrefix}_01", methodName, "No se encontró la declaración jurada indicada.", 404);

        if (affidavit.CodigoPersona != personId)
            return OperationResult<BusinessLogic.Entities.DeclaracionJuradaWeb>.IsFailed(
                $"{errorPrefix}_02", methodName, "La declaración jurada indicada no pertenece a la persona autenticada.", 403);

        return OperationResult<BusinessLogic.Entities.DeclaracionJuradaWeb>.Ok(affidavit, methodName);
    }

    private static string BuildFileName(string? nombreBase, string extension, string fallback)
    {
        var name = string.IsNullOrWhiteSpace(nombreBase) ? fallback : nombreBase.Trim();
        return $"{name}{extension}";
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        var ext = extension.Trim();
        return ext.StartsWith('.') ? ext : $".{ext}";
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".html" => "text/html",
        _ => "application/octet-stream"
    };

    #endregion MÉTODOS PRIVADOS
}
