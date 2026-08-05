namespace AppLogic.Identity.Dtos;

/// <summary>Documento de identidad leído para consulta interna. No cruza el borde HTTP.</summary>
public class IdentityDocumentQuery
{
    /// <summary>Contenido y nombre del archivo.</summary>
    public IdentityDocumentFile? File { get; set; }

    /// <summary>Fecha de vencimiento del documento.</summary>
    public DateTime? ExpirationDate { get; set; }
}
