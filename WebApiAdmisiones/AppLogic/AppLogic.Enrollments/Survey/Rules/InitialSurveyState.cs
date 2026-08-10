using AppLogic.Contracts.Text;
using AppLogic.People.Constants;
using BusinessLogic.Entities;

namespace AppLogic.Enrollments.Survey.Rules;

internal static class InitialSurveyState
{
    internal const string EstadoTemporal = "TEMPORAL";
    internal const string EstadoDefinitivo = "DEFINITIVO";
    internal const string TipoInscripcionSoloEncuesta = "SOLO_ENCUESTA_INI";
    internal const string Si = "SI";
    internal const string No = "NO";
    private const string SiCorto = "S";
    private const string NoCorto = "N";

    /// <summary>
    /// Códigos de PreviousHigherEducationId (ver InitialSurveyOptions.PreviousHigherEducationOptions).
    /// </summary>
    internal static class EstadoEducacionSuperiorPrevia
    {
        internal const int Uruguay = 1;
        internal const int Exterior = 2;
        internal const int Ninguna = 3;
    }

    /// <summary>
    /// Códigos de LastSecondaryYearLocationId (ver InitialSurveyOptions.LastSecondaryYearLocations).
    /// </summary>
    internal static class UbicacionUltimoAnioSecundaria
    {
        internal const int Uruguay = 1;
        internal const int Exterior = 2;
    }

    internal const string Educacion = "educacion";
    internal const string DecisionAcademica = "decisionAcademica";
    internal const string ExperienciaOrt = "experienciaOrt";

    internal static string BoolToYesNo(bool value)
        => value ? Si : No;

    internal static bool? YesNoToBool(string? value)
    {
        if (string.Equals(value, Si, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, SiCorto, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(value, No, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, NoCorto, StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }

    internal static bool IsAnsweredYesNo(string? value) => YesNoToBool(value).HasValue;

    /// <summary>5 (Formación universitaria completa) o 6 (Estudios de postgrado) en NivelesFormacionTutores.</summary>
    internal static bool IsHigherEducationLevel(int? value) => value is 5 or 6;

    internal static int? LeerInt(string? value)
        => int.TryParse(value, out var result) ? result : null;

    internal static long? LeerLong(string? value)
        => long.TryParse(value, out var result) ? result : null;

    internal static void ApplyDecisionSupport(EncuestaIniAdmision survey, int apoyoDecisionId)
    {
        survey.ComparPadresEncuestaIni = BoolToYesNo(apoyoDecisionId == PersonConstants.CompartidoCon.Padres);
        survey.ComparAmigoFamEncuestaIni = BoolToYesNo(apoyoDecisionId == PersonConstants.CompartidoCon.AmigoFamilia);
        survey.ComparAmigoPropEncuestaIni = BoolToYesNo(apoyoDecisionId == PersonConstants.CompartidoCon.AmigoPropio);
        survey.ComparOtrosEncuestaIni = BoolToYesNo(apoyoDecisionId == PersonConstants.CompartidoCon.Otros);
        survey.ComparNadieEncuestaIni = BoolToYesNo(apoyoDecisionId == PersonConstants.CompartidoCon.Nadie);
    }

    internal static bool HasDecisionSupport(EncuestaIniAdmision survey)
    {
        return YesNoToBool(survey.ComparPadresEncuestaIni) == true
            || YesNoToBool(survey.ComparAmigoFamEncuestaIni) == true
            || YesNoToBool(survey.ComparAmigoPropEncuestaIni) == true
            || YesNoToBool(survey.ComparOtrosEncuestaIni) == true
            || YesNoToBool(survey.ComparNadieEncuestaIni) == true;
    }

    internal static int? LeerApoyoDecision(EncuestaIniAdmision survey)
    {
        if (YesNoToBool(survey.ComparPadresEncuestaIni) == true)
            return PersonConstants.CompartidoCon.Padres;
        if (YesNoToBool(survey.ComparAmigoFamEncuestaIni) == true)
            return PersonConstants.CompartidoCon.AmigoFamilia;
        if (YesNoToBool(survey.ComparAmigoPropEncuestaIni) == true)
            return PersonConstants.CompartidoCon.AmigoPropio;
        if (YesNoToBool(survey.ComparOtrosEncuestaIni) == true)
            return PersonConstants.CompartidoCon.Otros;
        if (YesNoToBool(survey.ComparNadieEncuestaIni) == true)
            return PersonConstants.CompartidoCon.Nadie;

        return null;
    }

    internal static string? NormalizeText(string? value)
        => TextNormalization.TrimOrNull(value);
}
