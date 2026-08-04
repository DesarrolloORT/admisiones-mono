namespace AppLogic.Personas.Constants;

public static class PersonaConstants
{
    public const string PersonaNoEncontradaMessage = "Persona no encontrada.";
    public const int TipoImagenFoto = 3;

    public static class Parametros
    {
        public const long UruguayCodigoPais = 1;
    }

    public static class DocumentoPersona
    {
        public const int Frente = 1;
        public const int Dorso = 2;
    }

    public static class CompartidoCon
    {
        public const int Padres = 1;
        public const int AmigoFamilia = 2;
        public const int AmigoPropio = 3;
        public const int Otros = 4;
        public const int Nadie = 5;
    }
}
