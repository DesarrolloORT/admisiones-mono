namespace AppLogic.Constants
{
    public static class PersonaAdmisionConstants
    {
        public const string PersonaNoEncontradaMessage = "Persona no encontrada.";
        public const string TipoPersonaSgi = "SGI";
        public const int TipoImagenFoto = 3;

        public static class Parametros
        {
            public static readonly DateTime FechaMinimaNacimiento =
                new(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            public const long UruguayCodigoPais = 1;
            public const long ExteriorInstitucionOrt = 2898;
            public const long TituloGenericoSextoExterior = 5;
        }

        public static class DocumentoAlumno
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
}
