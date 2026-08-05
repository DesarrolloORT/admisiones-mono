using AppLogic.Scholarships.Dtos;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.Scholarships.Interfaces;

public interface IScholarshipFundService
{
    #region TIPOS DECLARACIÓN JURADA

    /// <summary>
    /// Devuelve todos los tipos de parentesco disponibles.
    /// </summary>
    OperationResult<IEnumerable<DtoTipoParentescoDevart>> GetKinshipTypes();

    /// <summary>
    /// Devuelve los tipos de egreso activos ordenados por campo Orden.
    /// </summary>
    OperationResult<IEnumerable<DtoTipoEgresoDjDevart>> GetExpenseTypes();

    /// <summary>
    /// Devuelve todos los tipos de vivienda disponibles.
    /// </summary>
    OperationResult<IEnumerable<DtoTipoViviendaDevart>> GetHousingTypes();

    #endregion



    /// <summary>
    /// Devuelve las universidades disponibles. Valida que el país indicado exista.
    /// </summary>
    OperationResult<IEnumerable<DtoEmpresaDevart>> GetUniversities(long countryId);

    OperationResult<bool> UploadIncomeFile(long personId, long idIngresoMensualNF, byte[] fileContent, string fileName);

    OperationResult<FileDownload> DownloadIncomeFile(long personId, long idIngresoMensualNF);

    OperationResult<bool> DeleteIncomeFile(long personId, long idIngresoMensualNF);

    OperationResult<bool> UploadExpenseFile(long personId, long idEgresoMensualNF, byte[] fileContent, string fileName);

    OperationResult<FileDownload> DownloadExpenseFile(long personId, long idEgresoMensualNF);

    OperationResult<bool> DeleteExpenseFile(long personId, long idEgresoMensualNF);

    OperationResult<bool> UploadRevalidationFile(long personId, long idDeclaracionJuradaWeb, byte[] fileContent, string fileName);

    OperationResult<FileDownload> DownloadRevalidationFile(long personId, long idDeclaracionJuradaWeb);

    OperationResult<bool> DeleteRevalidationFile(long personId, long idDeclaracionJuradaWeb);

}
