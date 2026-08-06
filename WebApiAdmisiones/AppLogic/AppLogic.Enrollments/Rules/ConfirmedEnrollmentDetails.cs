using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Mapping;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.Enrollments.Rules;

/// <summary>
/// Detalle de una inscripción confirmada. Lo consultan el detalle de "Mis carreras" y el pago
/// con cuenta personal, por eso vive acá y no dentro de un caso de uso.
/// </summary>
public static class ConfirmedEnrollmentDetails
{
    /// <summary>Devuelve null si ninguna de las inscripciones indicadas existe para la persona.</summary>
    public static ConfirmedEnrollmentDetailsResponse? Build(IUnitOfWork uow, long personId, IEnumerable<long> idsInscripcion)
    {
        var offerings = new List<(Inscripto Inscripto, ICollection<VdInscriptoCreditoAlumno> Materias)>();
        foreach (var id in idsInscripcion)
        {
            var enrollment = uow.Inscriptos.GetDetalleByKey(id, personId);
            if (enrollment == null)
                continue;

            offerings.Add((enrollment, uow.VdInscriptoCreditoAlumnos.GetByInscripto(id)));
        }

        if (offerings.Count == 0)
            return null;

        var coordinadores = uow.VdInscriptoCoordinadores.GetByInscripto(offerings[0].Inscripto.IdInscripto);
        return EnrollmentMapper.MapConfirmed(personId, offerings, coordinadores);
    }
}
