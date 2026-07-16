using AppLogic.Registro.Dtos;

namespace AppLogic.Registro.Interfaces;

/// <summary>
/// Persiste en Redis los datos de una persona pendiente de alta (flujo de registro diferido:
/// evaluar documento → confirmar → completar password). Registro es dueño del ciclo de vida
/// (guarda/borra); Autenticación lo consume para activar el link de la nueva persona.
/// </summary>
public interface IPendingPersonaStore
{
    /// <summary>Guarda la persona pendiente y su mapeo documento→flowId, ambos con el mismo TTL.</summary>
    Task SaveAsync(DtoRegistroPendingPersona pending, TimeSpan ttl);

    /// <summary>JSON crudo de la persona pendiente, o null si la key no existe/expiró (sin parsear).</summary>
    Task<string?> GetRawAsync(string flowId);

    /// <summary>Persona pendiente parseada, o null si no existe o no se pudo deserializar.</summary>
    Task<DtoRegistroPendingPersona?> GetAsync(string flowId);

    /// <summary>Elimina la persona pendiente y su mapeo documento→flowId.</summary>
    Task DeleteAsync(string flowId);

    /// <summary>
    /// Resuelve el flowId de una persona pendiente vigente para el documento indicado.
    /// Si el mapeo apunta a un flowId cuyo pending ya no existe (stale), lo limpia y retorna null.
    /// </summary>
    Task<string?> ResolverFlowIdPorDocumentoAsync(string tipoDocumento, string documento);
}
