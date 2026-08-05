namespace AppLogic.Scholarships.Dtos;

/// <summary>Beca que se muestra en la tarjeta de "Mis becas".</summary>
public class ScholarshipSummary
{
    /// <summary>Identificador de la beca.</summary>
    public long ScholarshipId { get; set; }

    /// <summary>Identificador de la postulación de la persona, si ya postuló.</summary>
    public long? ApplicationId { get; set; }

    /// <summary>Nombre de la beca.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Carrera a la que aplica la beca.</summary>
    public string DegreeProgram { get; set; } = string.Empty;

    /// <summary>Estado de la postulación.</summary>
    public string? Status { get; set; }

    /// <summary>Fecha límite para postularse.</summary>
    public DateTime? ApplicationCloseDate { get; set; }

    /// <summary>Fecha de la prueba de la beca.</summary>
    public DateTime? TestDate { get; set; }

    /// <summary>Fecha en que se publican los resultados.</summary>
    public DateTime? ResultsDate { get; set; }

    /// <summary>Acción principal que ofrece la tarjeta al usuario.</summary>
    public string PrimaryAction { get; set; } = string.Empty;

    /// <summary>Indica si la persona puede continuar una postulación iniciada.</summary>
    public bool CanContinueApplication { get; set; }

    /// <summary>Indica si hay material de estudio disponible para descargar.</summary>
    public bool CanDownloadStudyMaterial { get; set; }

    /// <summary>URL del material de estudio, cuando está disponible.</summary>
    public string? StudyMaterialUrl { get; set; }
}
