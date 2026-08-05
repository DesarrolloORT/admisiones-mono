using Utilities;

namespace AppLogic.Enrollments.Constants;

/// <summary>
/// Motivos por los que se rechaza el registro de interés por producto.
/// Los validadores devuelven este enum en vez de <c>OperationResult</c>: el caso de uso es el
/// único que arma la respuesta HTTP.
/// </summary>
public enum ProductInterestRejection
{
    None = 0,
    InvalidRequestedOfferings,
    PersonNotFound,
    InvalidProduct,
    ProcessNotEnabled,
    AlreadyEnrolledInProduct,
    PendingEnrollment,
    InterestAlreadyRegistered,
    InvalidOfferingId,
    OfferingNotFound,
    OfferingNotInProduct,
    OfferingClosed,
    OfferingNotInProcess
}

/// <summary>
/// Traduce el motivo de rechazo al contrato HTTP. Único lugar donde viven los códigos
/// <c>GEN_IP_*</c>, sus HTTP y sus mensajes: antes estaban repartidos en el validador.
/// </summary>
public static class ProductInterestRejectionExtensions
{
    public static OperationResult<T> ToFailure<T>(this ProductInterestRejection rejection, string methodName) =>
        rejection switch
        {
            ProductInterestRejection.InvalidRequestedOfferings =>
                OperationResult<T>.IsFailed("GEN_IP_11", methodName, "Debe indicar al menos una oferta valida.", 400),
            ProductInterestRejection.PersonNotFound =>
                OperationResult<T>.IsFailed("GEN_IP_01", methodName, "Persona no encontrada.", 404),
            ProductInterestRejection.InvalidProduct =>
                OperationResult<T>.IsFailed("GEN_IP_02", methodName, "El producto indicado es inválido.", 400),
            ProductInterestRejection.ProcessNotEnabled =>
                OperationResult<T>.IsFailed("GEN_IP_03", methodName, "Proceso no habilitado para el producto seleccionado.", 400),
            ProductInterestRejection.AlreadyEnrolledInProduct =>
                OperationResult<T>.IsFailed("GEN_IP_04", methodName, "Ya fue inscripto una vez al producto indicado.", 409),
            ProductInterestRejection.PendingEnrollment =>
                OperationResult<T>.IsFailed("GEN_IP_05", methodName, "Ya tiene una inscripción pendiente al producto indicado.", 409),
            ProductInterestRejection.InterestAlreadyRegistered =>
                OperationResult<T>.IsFailed("GEN_IP_06", methodName, "Ya tiene interés registrado para la oferta indicada.", 409),
            ProductInterestRejection.InvalidOfferingId =>
                OperationResult<T>.IsFailed("GEN_IP_07", methodName, "La oferta indicada es invalida.", 400),
            ProductInterestRejection.OfferingNotFound =>
                OperationResult<T>.IsFailed("GEN_IP_07", methodName, "No se encontro la oferta indicada.", 404),
            ProductInterestRejection.OfferingNotInProduct =>
                OperationResult<T>.IsFailed("GEN_IP_08", methodName, "La oferta indicada no corresponde al producto seleccionado.", 400),
            ProductInterestRejection.OfferingClosed =>
                OperationResult<T>.IsFailed("GEN_IP_09", methodName, "La oferta indicada no se encuentra abierta para inscripcion.", 409),
            ProductInterestRejection.OfferingNotInProcess =>
                OperationResult<T>.IsFailed("GEN_IP_10", methodName, "La oferta indicada no corresponde al proceso seleccionado.", 400),
            _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, "Motivo de rechazo sin mapeo HTTP.")
        };
}
