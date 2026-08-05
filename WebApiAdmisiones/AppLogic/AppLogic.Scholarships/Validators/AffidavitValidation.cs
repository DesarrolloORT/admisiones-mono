using AppLogic.Contracts;
using AppLogic.DevartDTOs;
using AppLogic.Scholarships.Constants;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Scholarships.Validators;

/// <summary>
/// Reglas de la declaración jurada del fondo de beca. Devuelven el motivo de rechazo
/// (<see cref="AffidavitRejection.None"/> si pasa); el contrato HTTP lo arma el caso de uso.
/// </summary>
public static class AffidavitValidation
{
    public static AffidavitRejection ValidateSaveRequest(
        DtoDeclaracionJuradaWebDevart? declaracionModificada)
    {
        if (declaracionModificada is null)
        {
            return AffidavitRejection.ModifiedAffidavitRequired;
        }

        if (declaracionModificada.IdInscriptoPrueba <= 0)
        {
            return AffidavitRejection.ValidTestEnrollmentRequired;
        }

        return AffidavitRejection.None;
    }

    public static AffidavitRejection ValidateForConfirmation(DeclaracionJuradaWeb affidavit)
    {
        var academico = ValidateAcademicBackgroundForConfirmation(affidavit);
        if (academico != AffidavitRejection.None)
        {
            return academico;
        }

        foreach (var ingreso in affidavit.IntegranteNfDjs.SelectMany(i => i.IngresoMensualNfDjs))
        {
            var rechazoIngreso = ValidateIncomeForConfirmation(ingreso);
            if (rechazoIngreso != AffidavitRejection.None)
            {
                return rechazoIngreso;
            }
        }

        return AffidavitRejection.None;
    }

    /// <summary>
    /// Mantiene <c>OperationResult</c> porque delega en <c>FileValidator</c> (Core), que ya lo
    /// devuelve y no se toca en este refactor.
    /// </summary>
    public static OperationResult<string> ValidateAttachment(byte[] fileContent, string fileName, string methodName)
    {
        var validation = FileValidator.ValidateDeclaracionJuradaAttachment(fileContent, fileName, methodName);
        if (!validation.Success)
            return validation.Failure().As<string>(methodName);

        return FileValidator.SanitizeDeclaracionJuradaAttachmentName(fileName, methodName);
    }

    private static AffidavitRejection ValidateAcademicBackgroundForConfirmation(DeclaracionJuradaWeb affidavit)
    {
        if (affidavit.IdTipoDescuento != ScholarshipFundConstants.Declaracion.TipoDescuentoAntecedentesAcademicos)
        {
            return AffidavitRejection.None;
        }

        if (!affidavit.CodigoInstitucionBac.HasValue
            || !affidavit.CodigoInstitucionUniv.HasValue
            || string.IsNullOrWhiteSpace(affidavit.CarreraUniversitariaDj)
            || !affidavit.CantMateriasAprobadasDj.HasValue
            || !affidavit.CantMatExaReprobadosDj.HasValue
            || !affidavit.PromTotalCalificacionesDj.HasValue)
        {
            return AffidavitRejection.AcademicBackgroundRequired;
        }

        if (affidavit.CodigoInstitucionUniv == ScholarshipFundConstants.Declaracion.CodigoInstitucionOrt
            && string.IsNullOrWhiteSpace(affidavit.FacultadUniversidadDj))
        {
            return AffidavitRejection.UniversityRequired;
        }

        if (affidavit.CodigoInstitucionUniv != ScholarshipFundConstants.Declaracion.CodigoInstitucionOrt
            && !string.IsNullOrWhiteSpace(affidavit.FacultadUniversidadDj))
        {
            return AffidavitRejection.UniversityInconsistent;
        }

        return AffidavitRejection.None;
    }

    private static AffidavitRejection ValidateIncomeForConfirmation(IngresoMensualNfDj ingreso)
    {
        if (ingreso.NominalIngresoNfDj < 0)
        {
            return AffidavitRejection.IncomeMustNotBeNegative;
        }

        if (ingreso.NominalIngresoNfDj.HasValue
            && decimal.Truncate(ingreso.NominalIngresoNfDj.Value) != ingreso.NominalIngresoNfDj.Value)
        {
            return AffidavitRejection.IncomeMustNotHaveDecimals;
        }

        if (ingreso.NominalIngresoNfDj.HasValue
            && ingreso.NominalIngresoNfDj.Value > 0
            && ingreso.NominalIngresoNfDj.Value < 1000)
        {
            return AffidavitRejection.IncomeBelowMinimum;
        }

        if (ingreso.DescuentoslegalesIngresoNf.HasValue
            && decimal.Truncate(ingreso.DescuentoslegalesIngresoNf.Value) != ingreso.DescuentoslegalesIngresoNf.Value)
        {
            return AffidavitRejection.LegalDeductionsMustNotHaveDecimals;
        }

        if (ingreso.ArchivoIngresoNfDj is null || ingreso.ArchivoIngresoNfDj.Length == 0)
        {
            return AffidavitRejection.IncomeFileRequired;
        }

        return AffidavitRejection.None;
    }
}
