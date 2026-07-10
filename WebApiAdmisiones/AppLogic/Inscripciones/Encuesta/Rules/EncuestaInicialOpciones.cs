using AppLogic.Catalogos.Responses;

namespace AppLogic.Inscripciones.Encuesta.Rules
{
    public static class EncuestaInicialOpciones
    {
        public static IReadOnlyList<DtoComboOption> OpcionesSiNo { get; } =
        [
            Combo(1, "Sí"),
            Combo(2, "No")
        ];

        public static IReadOnlyList<DtoComboOption> UbicacionesUltimoAnioSecundaria { get; } =
        [
            Combo(1, "Uruguay"),
            Combo(2, "En el exterior")
        ];

        public static IReadOnlyList<DtoComboOption> EstadosEducacionSuperiorPrevia { get; } =
        [
            Combo(1, "Sí, en Uruguay"),
            Combo(2, "Sí, en el exterior"),
            Combo(3, "No")
        ];

        public static IReadOnlyList<DtoComboOption> NivelesFormacionTutores { get; } =
        [
            Combo(1, "Primaria"),
            Combo(2, "Secundaria"),
            Combo(3, "Formación técnica"),
            Combo(4, "Formación universitaria incompleta"),
            Combo(5, "Formación universitaria completa"),
            Combo(6, "Estudios de postgrado"),
            Combo(7, "Otros estudios")
        ];

        public static IReadOnlyList<DtoComboOption> AniosEducacionMediaSuperior { get; } =
        [
            Combo(2, "1° EMS (4° año)"),
            Combo(3, "2° EMS (5° año)"),
            Combo(4, "3° EMS (6° año)"),
            Combo(0, "Otro")
        ];

        public static IReadOnlyList<DtoComboOption> ApoyosDecision { get; } =
        [
            Combo(1, "Padres u otros familiares"),
            Combo(2, "Amigos de la familia"),
            Combo(3, "Amigos propios, compañeros"),
            Combo(4, "Otros"),
            Combo(5, "Nadie")
        ];

        public static IReadOnlyList<DtoComboOption> NivelesDecision { get; } =
        [
            Combo(1, "Decidido/a"),
            Combo(2, "Con dudas")
        ];

        public static IReadOnlyList<DtoComboOption> Valoraciones { get; } =
        [
            Combo(1, "1"),
            Combo(2, "2"),
            Combo(3, "3"),
            Combo(4, "4"),
            Combo(5, "5")
        ];

        public static IReadOnlyList<DtoComboOption> TiposJornada { get; } =
        [
            Combo(1, "Tiempo completo"),
            Combo(2, "Tiempo parcial")
        ];

        public static bool Contiene(IReadOnlyList<DtoComboOption> opciones, long? valor)
            => !valor.HasValue || opciones.Any(o => o.Value == valor.Value);

        public static bool Contiene(IReadOnlyList<DtoComboOption> opciones, int? valor)
            => !valor.HasValue || opciones.Any(o => o.Value == valor.Value);

        private static DtoComboOption Combo(long value, string label) => new()
        {
            Value = value,
            Label = label
        };
    }
}
