namespace AppLogic.Catalogs.Dtos;

/// <summary>
/// Opción elegida por la persona en el paso "Propuesta académica" del registro de admisión.
/// </summary>
public enum AcademicOffer
{
    /// <summary>Nivel de producto 1: formación de grado.</summary>
    UniversityDegree = 1,

    /// <summary>Nivel de producto 2: carreras cortas/técnicas.</summary>
    TechnicalDegree = 2,

    /// <summary>Nivel de producto 3 y 4 combinados: siempre se devuelven juntos, nunca uno solo.</summary>
    ProfessionalUpdate = 3
}
