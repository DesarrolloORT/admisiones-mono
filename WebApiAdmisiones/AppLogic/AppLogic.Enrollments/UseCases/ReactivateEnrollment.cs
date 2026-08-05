using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.UseCases;

/// <summary>
/// Reactiva una inscripción dada de baja creando una nueva para la misma oferta:
/// valida que exista y esté de baja, y delega la creación en la confirmación normal.
/// </summary>
public class ReactivateEnrollment(
    IUnitOfWorkFactory uowFactory,
    IConfirmPreEnrollment confirmPreEnrollment) : IReactivateEnrollment
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IConfirmPreEnrollment _confirmPreEnrollment = confirmPreEnrollment;

    public async Task<OperationResult<ConfirmPreEnrollmentResponse>> ExecuteAsync(long personId, ReactivateEnrollmentRequest request)
    {
        const string methodName = nameof(ReactivateEnrollment);

        if (request == null || request.EnrollmentId <= 0)
        {
            return OperationResult<ConfirmPreEnrollmentResponse>.IsFailed("INS_REA_00", methodName, "Request invalido.", 400);
        }

        // El uow se mantiene vivo durante la confirmación, igual que en la versión anterior:
        // el UoW comparte un ModelContext scoped y disponerlo antes cambiaría el comportamiento.
        using var uow = _uowFactory.Create();

        var cancelledEnrollment = uow.Inscriptos.GetDetalleByKey(request.EnrollmentId, personId);
        if (cancelledEnrollment == null)
        {
            return OperationResult<ConfirmPreEnrollmentResponse>.IsFailed("INS_REA_01", methodName, "No se encontro la inscripcion para la persona.", 404);
        }

        if (cancelledEnrollment.BajaInscr == null || cancelledEnrollment.IdOferta == null)
        {
            return OperationResult<ConfirmPreEnrollmentResponse>.IsFailed("INS_REA_02", methodName, "La inscripcion indicada no esta dada de baja.", 409);
        }

        return await _confirmPreEnrollment.ExecuteAsync(personId, new ConfirmPreEnrollmentRequest
        {
            SelectedOfferingIds = [cancelledEnrollment.IdOferta.Value],
            AcceptedRegulations = true
        });
    }
}
