namespace AppLogic.Enrollments.Dtos;

/// <summary>Confirmación de la preinscripción para una o varias ofertas.</summary>
public class ConfirmPreEnrollmentRequest
{
    /// <summary>La persona aceptó el reglamento estudiantil.</summary>
    public bool AcceptedRegulations { get; set; }

    /// <summary>La inscripción se tramita por convenio corporativo, no por el flujo online.</summary>
    public bool IsCorporateEnrollment { get; set; }

    /// <summary>
    /// Ofertas seleccionadas a confirmar. Para productos de nivel 1 y 2 debe traer exactamente una;
    /// para nivel 3 y 4 puede traer varias.
    /// </summary>
    public List<long> SelectedOfferingIds { get; set; } = new();
}
