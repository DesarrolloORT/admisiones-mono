using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Validation;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.UseCases;

/// <summary>
/// Reactiva una o varias inscripciones dadas de baja creando inscripciones nuevas para las mismas
/// ofertas: valida que existan y esten de baja, y delega la creacion en la confirmacion normal.
/// Nivel 1 y 2 manda una sola inscripcion; nivel 3 y 4 puede mandar una por seminario.
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

        if (request == null || !RequestedEnrollments.HasValidIds(request.EnrollmentIds))
        {
            return OperationResult<ConfirmPreEnrollmentResponse>.IsFailed("INS_REA_00", methodName, "Request invalido.", 400);
        }

        // El uow se mantiene vivo durante la confirmación, igual que en la versión anterior:
        // el UoW comparte un ModelContext scoped y disponerlo antes cambiaría el comportamiento.
        using var uow = _uowFactory.Create();

        // Todo o nada: si una sola inscripcion no sirve no se reactiva ninguna.
        var offeringIds = new List<long>();
        foreach (var enrollmentId in request.EnrollmentIds)
        {
            var cancelledEnrollment = uow.Inscriptos.GetDetalleByKey(enrollmentId, personId);
            if (cancelledEnrollment == null)
            {
                return OperationResult<ConfirmPreEnrollmentResponse>.IsFailed("INS_REA_01", methodName, $"No se encontro la inscripcion {enrollmentId} para la persona.", 404);
            }

            if (cancelledEnrollment.BajaInscr == null || cancelledEnrollment.IdOferta == null)
            {
                return OperationResult<ConfirmPreEnrollmentResponse>.IsFailed("INS_REA_02", methodName, $"La inscripcion {enrollmentId} no esta dada de baja.", 409);
            }

            offeringIds.Add(cancelledEnrollment.IdOferta.Value);
        }

        // La compatibilidad entre ofertas (mismo producto, y mismo turno y comienzo en nivel 1 y 2) la
        // valida SelectedOfferingsCompatibility dentro de la confirmacion.
        return await _confirmPreEnrollment.ExecuteAsync(personId, new ConfirmPreEnrollmentRequest
        {
            // Dos bajas de la misma oferta no deben generar dos inscripciones nuevas.
            SelectedOfferingIds = offeringIds.Distinct().ToList(),
            AcceptedRegulations = true
        });
    }
}
