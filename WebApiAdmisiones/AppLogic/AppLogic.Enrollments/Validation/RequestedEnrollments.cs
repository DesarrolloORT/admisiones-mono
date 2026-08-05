using BusinessLogic.IDevartRepositories;

namespace AppLogic.Enrollments.Validation;

/// <summary>
/// Validaciones sobre las inscripciones que llegan en un request de pago.
/// Devuelven el tipo natural (bool): el caso de uso decide qué código de error y qué HTTP
/// corresponde en cada punto, porque el mismo chequeo tiene códigos distintos según el flujo.
/// </summary>
public static class RequestedEnrollments
{
    /// <summary>Hay al menos un id y todos son positivos.</summary>
    public static bool HasValidIds(List<long>? idsInscripcion) =>
        idsInscripcion is { Count: > 0 } && idsInscripcion.All(id => id > 0);

    /// <summary>Todas las inscripciones existen y pertenecen a la persona autenticada.</summary>
    public static bool AllBelongToPerson(IUnitOfWork uow, IEnumerable<long> idsInscripcion, long personId) =>
        idsInscripcion.All(idInscripto => uow.Inscriptos.GetDetalleByKey(idInscripto, personId) != null);
}
