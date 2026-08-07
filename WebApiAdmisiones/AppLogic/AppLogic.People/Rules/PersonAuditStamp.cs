using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.People.Rules;

/// <summary>
/// Sella los campos de auditoría de T_PERSONA antes de persistir.
/// Estaba en <c>RequiredPersonData</c>, pero no valida nada: muta la entidad.
/// </summary>
public static class PersonAuditStamp
{
    public static void Apply(Persona person, long personId, IUnitOfWork uow)
    {
        person.UsuarioModifFdp = personId.ToString();
        person.UsuarioUltimaActualizacion = uow.ObtenerDbUserId();
        person.FechaUltimaActualizacion = DateTime.Now;
        person.HoraUltimaActualizacion = DateTime.Now.ToString("HH:mm:ss");
    }
}
