using Utilities;

namespace AppLogic.Enrollments.Constants;

/// <summary>
/// Motivos de rechazo de la confirmación de preinscripción que dependen solo del request,
/// sin consultar la base. Los que requieren cargar la oferta viven en
/// <c>PreEnrollmentConfirmationRules</c>, que sigue devolviendo <c>OperationResult</c> por ser
/// un loader con ramas y no un validador hoja.
/// </summary>
public enum PreEnrollmentRejection
{
    None = 0,
    InvalidRequest,
    InvalidSelectedOfferings,
    CorporateRequiresLevel3Or4
}

/// <summary>Único lugar donde viven estos códigos <c>INS_CPI_*</c> y sus mensajes.</summary>
public static class PreEnrollmentRejectionExtensions
{
    public static OperationResult<T> ToFailure<T>(this PreEnrollmentRejection rejection, string methodName) =>
        rejection switch
        {
            PreEnrollmentRejection.InvalidRequest =>
                OperationResult<T>.IsFailed("INS_CPI_01", methodName, "Request invalido.", 400),
            PreEnrollmentRejection.InvalidSelectedOfferings =>
                OperationResult<T>.IsFailed("INS_CPI_03", methodName, "Debe indicar al menos una oferta seleccionada valida.", 400),
            PreEnrollmentRejection.CorporateRequiresLevel3Or4 =>
                OperationResult<T>.IsFailed("INS_CPI_17", methodName, "La inscripcion corporativa solo esta disponible para productos de nivel 3 o 4.", 400),
            _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, "Motivo de rechazo sin mapeo HTTP.")
        };
}
