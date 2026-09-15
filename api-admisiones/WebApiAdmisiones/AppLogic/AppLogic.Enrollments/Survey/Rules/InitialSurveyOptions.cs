using AppLogic.Contracts.Dtos;

namespace AppLogic.Enrollments.Survey.Rules;

public static class InitialSurveyOptions
{
    public static IReadOnlyList<ComboOption> YesNoOptions { get; } =
    [
        ComboOption.Of(1, "Sí"),
        ComboOption.Of(2, "No")
    ];

    public static IReadOnlyList<ComboOption> LastSecondaryYearLocations { get; } =
    [
        ComboOption.Of(1, "Uruguay"),
        ComboOption.Of(2, "En el exterior")
    ];

    public static IReadOnlyList<ComboOption> PreviousHigherEducationOptions { get; } =
    [
        ComboOption.Of(1, "Sí, en Uruguay"),
        ComboOption.Of(2, "Sí, en el exterior"),
        ComboOption.Of(3, "No")
    ];

    public static IReadOnlyList<ComboOption> EducationLevels { get; } =
    [
        ComboOption.Of(1, "Primaria"),
        ComboOption.Of(2, "Secundaria"),
        ComboOption.Of(3, "Formación técnica"),
        ComboOption.Of(4, "Formación universitaria incompleta"),
        ComboOption.Of(5, "Formación universitaria completa"),
        ComboOption.Of(6, "Estudios de postgrado"),
        ComboOption.Of(7, "Otros estudios")
    ];

    public static IReadOnlyList<ComboOption> UpperSecondaryYears { get; } =
    [
        ComboOption.Of(2, "1° EMS (4° año)"),
        ComboOption.Of(3, "2° EMS (5° año)"),
        ComboOption.Of(4, "3° EMS (6° año)"),
        ComboOption.Of(0, "Otro")
    ];

    public static IReadOnlyList<ComboOption> DecisionSupports { get; } =
    [
        ComboOption.Of(1, "Padres u otros familiares"),
        ComboOption.Of(2, "Amigos de la familia"),
        ComboOption.Of(3, "Amigos propios, compañeros"),
        ComboOption.Of(4, "Otros"),
        ComboOption.Of(5, "Nadie")
    ];

    public static IReadOnlyList<ComboOption> DecisionLevels { get; } =
    [
        ComboOption.Of(1, "Decidido/a"),
        ComboOption.Of(2, "Con dudas")
    ];

    public static IReadOnlyList<ComboOption> Ratings { get; } =
    [
        ComboOption.Of(1, "1"),
        ComboOption.Of(2, "2"),
        ComboOption.Of(3, "3"),
        ComboOption.Of(4, "4"),
        ComboOption.Of(5, "5")
    ];

    public static bool Contains(IReadOnlyList<ComboOption> options, long? value)
        => !value.HasValue || options.Any(o => o.Value == value.Value);

    public static bool Contains(IReadOnlyList<ComboOption> options, int? value)
        => !value.HasValue || options.Any(o => o.Value == value.Value);
}
