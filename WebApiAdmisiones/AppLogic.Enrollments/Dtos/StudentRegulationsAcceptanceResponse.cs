namespace AppLogic.Enrollments.Dtos;

/// <summary>Estado de aceptación del reglamento estudiantil.</summary>
public class StudentRegulationsAcceptanceResponse
{
    /// <summary>La persona ya aceptó el reglamento estudiantil.</summary>
    public bool AcceptedStudentRegulations { get; set; }

    /// <summary>Fecha de la primera aceptación, si existe.</summary>
    public DateTime? AcceptanceDate { get; set; }
}
