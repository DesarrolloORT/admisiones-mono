using AppLogic.Scholarships.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Scholarships.Mapping;

public static class ConfirmedEnrollmentMapper
{
    public static ConfirmedEnrollmentResponse ToResponse(this VdInscripcionesFresco1y2 enrollment) => new()
    {
        EnrollmentId = enrollment.IdInscripto,
        EnrollmentDate = enrollment.FechaInscripcion,
        EnrollmentUser = enrollment.UsuarioInscripcion,
        EnrollmentStatus = enrollment.EstadoInscripcion,
        ProductId = enrollment.IdProducto,
        ProductFullName = enrollment.NombreExtensoProducto,
        ProductLevelId = enrollment.IdNivelProducto,
        AdmissionProcessId = enrollment.IdProceso,
        IntakeId = enrollment.IdComienzo,
        IntakeName = enrollment.NombreComienzo,
        IntakeStartDate = enrollment.FechaInicioComienzo,
        ShiftId = enrollment.IdTurno,
        ShiftName = enrollment.NombreTurno,
        OfferingId = enrollment.IdOferta,
        ReferenceDate = enrollment.FechaReferencia,
        Origin = enrollment.VengoDe
    };
}
