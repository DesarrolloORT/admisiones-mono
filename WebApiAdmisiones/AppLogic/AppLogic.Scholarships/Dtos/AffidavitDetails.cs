using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Scholarships.Dtos;

/// <summary>
/// Declaración jurada de una persona junto con los datos de la prueba de beca asociada.
/// </summary>
[ExcludeFromCodeCoverage]
public class AffidavitDetails
{
    /// <summary>Subestado de la declaración jurada dentro del circuito de becas.</summary>
    public string? SubStatus { get; set; }

    /// <summary>Producto al que aplica la declaración jurada.</summary>
    public long ProductId { get; set; }

    /// <summary>Nombre extenso del producto.</summary>
    public string? ProductFullName { get; set; }

    /// <summary>Centro de costos del producto.</summary>
    public string? CostCenterName { get; set; }

    /// <summary>Inscripción a la prueba de beca.</summary>
    public long TestEnrollmentId { get; set; }

    /// <summary>Prueba de beca asociada.</summary>
    public long TestId { get; set; }

    /// <summary>Fondo de beca (T_TIPO_DESCUENTO) al que aplica la declaración.</summary>
    public long ScholarshipFundId { get; set; }

    /// <summary>Fecha límite para descargar la guía de estudio de la prueba.</summary>
    public DateTime? StudyGuideDeadlineDate { get; set; }

    /// <summary>Hora límite para descargar la guía de estudio de la prueba.</summary>
    public string? StudyGuideDeadlineTime { get; set; }

    /// <summary>Fecha de publicación de resultados en el sitio.</summary>
    public DateTime? ResultsPublicationDate { get; set; }

    /// <summary>Alias corto del fondo de beca.</summary>
    public string? ScholarshipFundAlias { get; set; }

    /// <summary>Nivel del producto.</summary>
    public long ProductLevelId { get; set; }

    /// <summary>"SI" / "NO": si el fondo de beca se muestra en el sitio de ORT.</summary>
    public string EnabledOnOrtSite { get; set; } = "SI";

    /// <summary>Detalle del fondo de beca.</summary>
    public string? Detail { get; set; }

    /// <summary>Nombre del fondo de beca.</summary>
    public string? Name { get; set; }

    /// <summary>Fecha límite de entrega de la declaración jurada.</summary>
    public DateTime? AffidavitDeadlineDate { get; set; }

    /// <summary>Hora límite de entrega de la declaración jurada.</summary>
    public string? AffidavitDeadlineTime { get; set; }
}
