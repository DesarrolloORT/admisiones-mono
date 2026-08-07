using Utilities;

namespace AppLogic.Enrollments.Constants;

/// <summary>
/// Motivos por los que se rechaza el guardado de la encuesta inicial.
/// El validador devuelve este enum; el contrato HTTP lo resuelve
/// <see cref="InitialSurveyRejectionExtensions"/>.
/// </summary>
public enum InitialSurveyRejection
{
    None = 0,
    InvalidRequest,
    InvalidLastSecondaryYearLocation,
    InvalidPreviousHigherEducation,
    InvalidFatherEducationLevel,
    InvalidMotherEducationLevel,
    InvalidCareerDecisionYear,
    InvalidOrtDecisionYear,
    InvalidDecisionSupport,
    InvalidDecisionLevel,
    InvalidAdvisoryRating,
    InvalidWebsiteRating,
    InvalidFacilitiesRating,
    InvalidAdmissionProcess,
    InvalidDegreeTitle,
    InvalidProduct,
    NoEnabledProcessForProduct,
    InvalidHighSchoolYear,
    UniversityRequiresFifthOrSixthYear,
    UncatalogedDegreeTitle,
    InvalidSecondaryInstitutionId,
    UnknownSecondaryInstitution,
    InvalidChoiceReason,
    InvalidAdvertising,
    InvalidRepeatCount,
    HigherEducationUniversityRequired,
    InvalidSelectedUniversity,
    UnknownSelectedUniversity,
    AdvisoryRatingRequired,
    WebsiteRatingRequired,
    FacilitiesRatingRequired,
    FatherOrtGraduateRequired,
    MotherOrtGraduateRequired
}

/// <summary>Único lugar donde viven los códigos <c>INS_EI_*</c> y sus mensajes.</summary>
public static class InitialSurveyRejectionExtensions
{
    public static OperationResult<T> ToFailure<T>(this InitialSurveyRejection rejection, string methodName) =>
        rejection switch
        {
            InitialSurveyRejection.InvalidRequest =>
                Fail<T>("INS_EI_02", methodName, "Request invalido."),
            InitialSurveyRejection.InvalidLastSecondaryYearLocation =>
                Fail<T>("INS_EI_14", methodName, "Ultimo anio de secundaria invalido."),
            InitialSurveyRejection.InvalidPreviousHigherEducation =>
                Fail<T>("INS_EI_50", methodName, "Educacion superior previa invalida."),
            InitialSurveyRejection.InvalidFatherEducationLevel =>
                Fail<T>("INS_EI_08", methodName, "Instruccion padre invalida."),
            InitialSurveyRejection.InvalidMotherEducationLevel =>
                Fail<T>("INS_EI_07", methodName, "Instruccion madre invalida."),
            InitialSurveyRejection.InvalidCareerDecisionYear =>
                Fail<T>("INS_EI_09", methodName, "Decision de carrera invalida."),
            InitialSurveyRejection.InvalidOrtDecisionYear =>
                Fail<T>("INS_EI_10", methodName, "Decision de universidad invalida."),
            InitialSurveyRejection.InvalidDecisionSupport =>
                Fail<T>("INS_EI_11", methodName, "Con quien compartio la decision invalido."),
            InitialSurveyRejection.InvalidDecisionLevel =>
                Fail<T>("INS_EI_15", methodName, "Nivel de decision invalido."),
            InitialSurveyRejection.InvalidAdvisoryRating =>
                Fail<T>("INS_EI_16", methodName, "Valoracion de asesoramiento invalida."),
            InitialSurveyRejection.InvalidWebsiteRating =>
                Fail<T>("INS_EI_17", methodName, "Valoracion del sitio web invalida."),
            InitialSurveyRejection.InvalidFacilitiesRating =>
                Fail<T>("INS_EI_18", methodName, "Valoracion de instalaciones invalida."),
            InitialSurveyRejection.InvalidAdmissionProcess =>
                Fail<T>("INS_EI_05", methodName, "Proceso invalido."),
            InitialSurveyRejection.InvalidDegreeTitle =>
                Fail<T>("INS_EI_21", methodName, "Titulo invalido."),
            InitialSurveyRejection.InvalidProduct =>
                Fail<T>("INS_EI_03", methodName, "El producto indicado es invalido."),
            InitialSurveyRejection.NoEnabledProcessForProduct =>
                Fail<T>("INS_EI_34", methodName, "No existe un proceso habilitado para el producto indicado.", 404),
            InitialSurveyRejection.InvalidHighSchoolYear =>
                Fail<T>("INS_EI_06", methodName, "Ultimo anio de bachillerato invalido."),
            InitialSurveyRejection.UniversityRequiresFifthOrSixthYear =>
                Fail<T>("INS_EI_64", methodName, "Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto año."),
            InitialSurveyRejection.UncatalogedDegreeTitle =>
                Fail<T>("INS_EI_22", methodName, "El titulo indicado es invalido."),
            InitialSurveyRejection.InvalidSecondaryInstitutionId =>
                Fail<T>("INS_EI_19", methodName, "Institucion invalida."),
            InitialSurveyRejection.UnknownSecondaryInstitution =>
                Fail<T>("INS_EI_20", methodName, "La institucion indicada es invalida."),
            InitialSurveyRejection.InvalidChoiceReason =>
                Fail<T>("INS_EI_23", methodName, "Motivo de eleccion invalido."),
            InitialSurveyRejection.InvalidAdvertising =>
                Fail<T>("INS_EI_24", methodName, "Publicidad seleccionada invalida."),
            InitialSurveyRejection.InvalidRepeatCount =>
                Fail<T>("INS_EI_52", methodName, "Debe indicar una cantidad valida de veces que recursa el anio de bachillerato."),
            InitialSurveyRejection.HigherEducationUniversityRequired =>
                Fail<T>("INS_EI_63", methodName, "Debe indicar al menos una universidad de educacion superior."),
            InitialSurveyRejection.InvalidSelectedUniversity =>
                Fail<T>("INS_EI_25", methodName, "Universidad seleccionada invalida."),
            InitialSurveyRejection.UnknownSelectedUniversity =>
                Fail<T>("INS_EI_27", methodName, "Universidad seleccionada invalida."),
            InitialSurveyRejection.AdvisoryRatingRequired =>
                Fail<T>("INS_EI_57", methodName, "Debe indicar valoracion de asesoramiento ORT."),
            InitialSurveyRejection.WebsiteRatingRequired =>
                Fail<T>("INS_EI_58", methodName, "Debe indicar valoracion del sitio web ORT."),
            InitialSurveyRejection.FacilitiesRatingRequired =>
                Fail<T>("INS_EI_59", methodName, "Debe indicar valoracion de instalaciones ORT."),
            InitialSurveyRejection.FatherOrtGraduateRequired =>
                Fail<T>("INS_EI_61", methodName, "Debe indicar si padre/tutor es egresado ORT."),
            InitialSurveyRejection.MotherOrtGraduateRequired =>
                Fail<T>("INS_EI_62", methodName, "Debe indicar si madre/tutor es egresada ORT."),
            _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, "Motivo de rechazo sin mapeo HTTP.")
        };

    private static OperationResult<T> Fail<T>(string errorCode, string methodName, string message, int httpCode = 400)
        => OperationResult<T>.IsFailed(errorCode, methodName, message, httpCode);
}
