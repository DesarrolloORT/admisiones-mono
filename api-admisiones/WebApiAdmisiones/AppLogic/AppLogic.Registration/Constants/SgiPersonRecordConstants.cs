namespace AppLogic.Registration.Constants;

/// <summary>
/// Valores con los que se sella un registro de persona en las tablas legacy SGI al dar de alta
/// desde admisiones web. Los usa solo el alta de persona (Registro): no son constantes de inscripción.
/// </summary>
public static class SgiPersonRecordConstants
{
    public const string SgiPersonType = "SGI";
    public const string ActiveValidityCode = "SI";
    public const int PendingRequestStatus = 1;
    public const int AdmissionsDataSourceCode = 170;
    public const int AdmissionsSiteRegistrationActionType = 109;
}
