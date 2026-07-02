using AppLogic.Constants;
using AppLogic.Utilities;
using BusinessLogic.Entities;

namespace AppLogic.Helpers
{
    internal static class EncuestaInicialState
    {
        internal const string EstadoTemporal = "TEMPORAL";
        internal const string EstadoDefinitivo = "DEFINITIVO";
        internal const string TipoInscripcionSoloEncuesta = "SOLO_ENCUESTA_INI";
        internal const string Si = "SI";
        internal const string No = "NO";

        internal const string Educacion = "educacion";
        internal const string DecisionAcademica = "decisionAcademica";
        internal const string ExperienciaOrt = "experienciaOrt";
        internal const string SituacionLaboral = "situacionLaboral";

        internal static string BoolToSN(bool value)
            => value ? Si : No;

        internal static bool? SNToBool(string? value)
        {
            if (string.Equals(value, Si, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "S", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(value, No, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "N", StringComparison.OrdinalIgnoreCase))
                return false;

            return null;
        }

        internal static bool IsAnsweredSN(string? value) => SNToBool(value).HasValue;

        internal static bool EsInstruccionAlta(int? value) => value is 5 or 6;

        internal static int? LeerInt(string? value)
            => int.TryParse(value, out var result) ? result : null;

        internal static long? LeerLong(string? value)
            => long.TryParse(value, out var result) ? result : null;

        internal static void AplicarApoyoDecision(EncuestaIniAdmision encuesta, int apoyoDecisionId)
        {
            encuesta.ComparPadresEncuestaIni = BoolToSN(apoyoDecisionId == PersonaConstants.CompartidoCon.Padres);
            encuesta.ComparAmigoFamEncuestaIni = BoolToSN(apoyoDecisionId == PersonaConstants.CompartidoCon.AmigoFamilia);
            encuesta.ComparAmigoPropEncuestaIni = BoolToSN(apoyoDecisionId == PersonaConstants.CompartidoCon.AmigoPropio);
            encuesta.ComparOtrosEncuestaIni = BoolToSN(apoyoDecisionId == PersonaConstants.CompartidoCon.Otros);
            encuesta.ComparNadieEncuestaIni = BoolToSN(apoyoDecisionId == PersonaConstants.CompartidoCon.Nadie);
        }

        internal static bool TieneApoyoDecision(EncuestaIniAdmision encuesta)
        {
            return SNToBool(encuesta.ComparPadresEncuestaIni) == true
                || SNToBool(encuesta.ComparAmigoFamEncuestaIni) == true
                || SNToBool(encuesta.ComparAmigoPropEncuestaIni) == true
                || SNToBool(encuesta.ComparOtrosEncuestaIni) == true
                || SNToBool(encuesta.ComparNadieEncuestaIni) == true;
        }

        internal static int? LeerApoyoDecision(EncuestaIniAdmision encuesta)
        {
            if (SNToBool(encuesta.ComparPadresEncuestaIni) == true)
                return PersonaConstants.CompartidoCon.Padres;
            if (SNToBool(encuesta.ComparAmigoFamEncuestaIni) == true)
                return PersonaConstants.CompartidoCon.AmigoFamilia;
            if (SNToBool(encuesta.ComparAmigoPropEncuestaIni) == true)
                return PersonaConstants.CompartidoCon.AmigoPropio;
            if (SNToBool(encuesta.ComparOtrosEncuestaIni) == true)
                return PersonaConstants.CompartidoCon.Otros;
            if (SNToBool(encuesta.ComparNadieEncuestaIni) == true)
                return PersonaConstants.CompartidoCon.Nadie;

            return null;
        }

        internal static string? NormalizarTexto(string? value)
            => DocumentUtils.NormalizarOpcional(value);
    }
}
