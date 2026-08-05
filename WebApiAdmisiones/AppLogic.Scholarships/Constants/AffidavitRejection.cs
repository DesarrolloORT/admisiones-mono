using Utilities;

namespace AppLogic.Scholarships.Constants;

/// <summary>
/// Motivos por los que se rechaza guardar o confirmar la declaración jurada del fondo de beca.
/// Los validadores devuelven este enum; el contrato HTTP lo resuelve
/// <see cref="AffidavitRejectionExtensions"/>.
/// </summary>
public enum AffidavitRejection
{
    None = 0,
    ModifiedAffidavitRequired,
    ValidTestEnrollmentRequired,
    AcademicBackgroundRequired,
    UniversityRequired,
    UniversityInconsistent,
    IncomeMustNotBeNegative,
    IncomeMustNotHaveDecimals,
    IncomeBelowMinimum,
    LegalDeductionsMustNotHaveDecimals,
    IncomeFileRequired
}

/// <summary>Único lugar donde viven los códigos <c>FDB_GDJ_*</c> y sus mensajes.</summary>
public static class AffidavitRejectionExtensions
{
    public static OperationResult<T> ToFailure<T>(this AffidavitRejection rejection, string methodName) =>
        rejection switch
        {
            AffidavitRejection.ModifiedAffidavitRequired =>
                OperationResult<T>.IsFailed("FDB_GDJ_00", methodName, "Se requiere la declaración modificada.", 400),
            AffidavitRejection.ValidTestEnrollmentRequired =>
                OperationResult<T>.IsFailed("FDB_GDJ_15", methodName, "Se requiere una inscripción a prueba válida para guardar la declaración jurada.", 400),
            AffidavitRejection.AcademicBackgroundRequired =>
                OperationResult<T>.IsFailed("FDB_GDJ_07", methodName, "Se deben indicar los antecedentes académicos para confirmar la declaración jurada.", 400),
            AffidavitRejection.UniversityRequired =>
                OperationResult<T>.IsFailed("FDB_GDJ_08", methodName, "Debe indicar la universidad.", 400),
            AffidavitRejection.UniversityInconsistent =>
                OperationResult<T>.IsFailed("FDB_GDJ_09", methodName, "Existe una inconsistencia en la información sobre la universidad indicada.", 400),
            AffidavitRejection.IncomeMustNotBeNegative =>
                OperationResult<T>.IsFailed("FDB_GDJ_10", methodName, "El ingreso debe ser mayor o igual a cero.", 400),
            AffidavitRejection.IncomeMustNotHaveDecimals =>
                OperationResult<T>.IsFailed("FDB_GDJ_11", methodName, "Los ingresos nominales no pueden contener decimales.", 400),
            AffidavitRejection.IncomeBelowMinimum =>
                OperationResult<T>.IsFailed("FDB_GDJ_12", methodName, "Los ingresos nominales deben ser mayores o iguales a 1000.", 400),
            AffidavitRejection.LegalDeductionsMustNotHaveDecimals =>
                OperationResult<T>.IsFailed("FDB_GDJ_13", methodName, "Los descuentos legales no pueden contener decimales.", 400),
            AffidavitRejection.IncomeFileRequired =>
                OperationResult<T>.IsFailed("FDB_GDJ_14", methodName, "Para confirmar la declaración jurada, todos los ingresos deben tener archivo cargado.", 400),
            _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, "Motivo de rechazo sin mapeo HTTP.")
        };
}
