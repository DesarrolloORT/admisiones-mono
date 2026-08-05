namespace AppLogic.Contracts.Constants;

/// <summary>
/// Valores de ESTADO_INSCRIPCION que devuelven las vistas VD_INSCRIPCIONES_FRESCO_1Y2 / _3Y4.
/// Compartido: lo leen Enrollments y Scholarships.
/// Verificar la grafía exacta contra la definición de la vista Oracle (máx. 14 chars).
/// </summary>
public static class EnrollmentStatus
{
    public const string InProgress = "En proceso";
    public const string PaymentPending = "Pago pendiente";
    public const string Waiting = "A la espera";
    public const string Confirmed = "Confirmada";
    public const string Cancelled = "Dada de baja";
}
