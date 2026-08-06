using Utilities;

namespace AppLogic.People.Constants;

/// <summary>
/// Dato obligatorio que falta para confirmar los datos personales.
/// Los validadores devuelven este enum; el código de error, el HTTP y el mensaje los resuelve
/// <see cref="PersonDataGapExtensions"/> en un único lugar.
/// </summary>
public enum PersonDataGap
{
    None = 0,
    BirthCountryMissing,
    NationalityMissing,
    DocumentExpiryMissing,
    ResidenceCountryMissing,
    StateMissing,
    CityMissing,
    AddressMissing,
    EmailMissing,
    PrimaryPhoneMissing,
    PhoneCountryCodeMissing
}

/// <summary>Único lugar donde viven los códigos <c>DP_ACDP_*</c> y sus mensajes.</summary>
public static class PersonDataGapExtensions
{
    public static OperationResult<T> ToFailure<T>(this PersonDataGap gap, string methodName) =>
        gap switch
        {
            PersonDataGap.BirthCountryMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_BAS_01", methodName, "Falta país de nacimiento.", 400),
            PersonDataGap.NationalityMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_BAS_02", methodName, "Falta nacionalidad.", 400),
            PersonDataGap.DocumentExpiryMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_DOC_01", methodName, "Falta fecha de vencimiento de documento.", 400),
            PersonDataGap.ResidenceCountryMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_DIR_01", methodName, "Falta país de residencia.", 400),
            PersonDataGap.StateMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_DIR_02", methodName, "Falta estado/provincia.", 400),
            PersonDataGap.CityMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_DIR_03", methodName, "Falta ciudad.", 400),
            PersonDataGap.AddressMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_DIR_04", methodName, "Falta domicilio.", 400),
            PersonDataGap.EmailMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_CON_01", methodName, "Falta email.", 400),
            PersonDataGap.PrimaryPhoneMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_CON_02", methodName, "Falta teléfono principal.", 400),
            PersonDataGap.PhoneCountryCodeMissing =>
                OperationResult<T>.IsFailed("DP_ACDP_CON_03", methodName, "Falta característica país teléfono 1.", 400),
            _ => throw new ArgumentOutOfRangeException(nameof(gap), gap, "Dato faltante sin mapeo HTTP.")
        };
}
