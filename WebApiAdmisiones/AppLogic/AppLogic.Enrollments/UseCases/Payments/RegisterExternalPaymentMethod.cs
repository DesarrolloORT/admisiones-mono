using AppLogic.Enrollments.Constants;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Validation;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.UseCases.Payments;

public class RegisterExternalPaymentMethod(IUnitOfWorkFactory uowFactory) : IRegisterExternalPaymentMethod
{
    private static readonly HashSet<string> ExternalPaymentMethods =
        [EnrollmentConstants.PaymentType.Abitab, EnrollmentConstants.PaymentType.Paganza];

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<bool> Execute(long personId, RegisterExternalPaymentMethodRequest request)
    {
        const string methodName = nameof(RegisterExternalPaymentMethod);

        if (request == null)
            return OperationResult<bool>.IsFailed("INS_MP_00", methodName, "Request invalido.", 400);

        if (!RequestedEnrollments.HasValidIds(request.EnrollmentIds))
            return OperationResult<bool>.IsFailed("INS_MP_01", methodName, "IdsInscripcion invalido.", 400);

        var normalizedPaymentType = request.PaymentType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ExternalPaymentMethods.Contains(normalizedPaymentType))
        {
            return OperationResult<bool>.IsFailed("INS_MP_02", methodName, "TipoPago invalido.", 400);
        }

        using var uow = _uowFactory.Create();

        uow.BeginTransaction();
        try
        {
            foreach (var idInscripcion in request.EnrollmentIds)
            {
                if (uow.Inscriptos.GetDetalleByKey(idInscripcion, personId) == null)
                {
                    uow.Rollback();
                    return OperationResult<bool>.IsFailed("INS_MP_03", methodName, "No se encontro la inscripcion para la persona.", 404);
                }

                if (uow.InscriptoSeniaMinima.GetByKey(idInscripcion) != null)
                {
                    uow.Rollback();
                    return OperationResult<bool>.IsFailed("INS_MP_04", methodName, "La reserva minima ya fue registrada para la inscripcion.", 409);
                }

                uow.InscriptoSeniaMinima.Add(new InscriptoSeniaMinimum
                {
                    IdInscripto = idInscripcion,
                    MetodoPagoSeniaMinima = normalizedPaymentType
                });
            }

            uow.Save();
            uow.Commit();
        }
        catch
        {
            uow.Rollback();
            throw;
        }

        return OperationResult<bool>.Ok(true, methodName);
    }
}
