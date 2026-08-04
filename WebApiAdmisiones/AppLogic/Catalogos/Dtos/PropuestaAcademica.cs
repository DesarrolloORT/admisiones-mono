namespace AppLogic.Catalogos.Dtos;

/// <summary>
/// Opción elegida por la persona en el paso "Propuesta académica" del registro de admisión.
/// </summary>
public enum PropuestaAcademica
{
    /// <summary>Nivel de producto 1: formación de grado.</summary>
    CarreraUniversitaria = 1,

    /// <summary>Nivel de producto 2: carreras cortas/técnicas.</summary>
    Tecnicatura = 2,

    /// <summary>Nivel de producto 3 y 4 combinados: siempre se devuelven juntos, nunca uno solo.</summary>
    ActualizacionProfesional = 3
}
