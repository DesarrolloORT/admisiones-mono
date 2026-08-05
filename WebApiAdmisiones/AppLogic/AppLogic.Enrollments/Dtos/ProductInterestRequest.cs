namespace AppLogic.Enrollments.Dtos;

/// <summary>Interés de la persona por un producto y proceso de admisión.</summary>
public class ProductInterestRequest
{
    /// <summary>Producto (carrera) elegido.</summary>
    public long ProductId { get; set; }

    /// <summary>Proceso de admisión elegido para ese producto.</summary>
    public long AdmissionProcessId { get; set; }

    /// <summary>
    /// Ofertas de interes. Para productos de nivel 1 y 2 debe traer exactamente una;
    /// para nivel 3 y 4 puede traer varias.
    /// </summary>
    public List<long> OfferingIds { get; set; } = new();
}
