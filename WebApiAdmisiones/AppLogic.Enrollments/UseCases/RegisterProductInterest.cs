using AppLogic.Contracts;
using AppLogic.Enrollments.Constants;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Rules;
using AppLogic.Integrations.Tivenos.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Enrollments.UseCases;

public class RegisterProductInterest(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext,
    ITivenosQueueService tivenosQueue) : IRegisterProductInterest
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;
    private readonly ITivenosQueueService _tivenosQueue = tivenosQueue;

    public OperationResult<bool> Execute(long personId, ProductInterestRequest request)
    {
        const string methodName = nameof(RegisterProductInterest);

        using var uow = _uowFactory.Create();

        if (request == null)
        {
            return OperationResult<bool>.IsFailed("GEN_IP_00", methodName, "Request invalido.", 400);
        }

        var requestedOfferings = ProductInterestValidation.ValidateRequestedOfferings(request.OfferingIds);
        if (requestedOfferings != ProductInterestRejection.None)
        {
            return requestedOfferings.ToFailure<bool>(methodName);
        }

        var admision = ProductInterestValidation.ValidateProductInterestRegistration(
            uow,
            personId,
            request.ProductId,
            request.AdmissionProcessId,
            request.OfferingIds);
        if (admision != ProductInterestRejection.None)
        {
            return admision.ToFailure<bool>(methodName);
        }

        var (offerings, rechazoOferta) = ResolveOfferings(uow, request);
        if (rechazoOferta != ProductInterestRejection.None)
            return rechazoOferta.ToFailure<bool>(methodName);

        return RegisterAndEnqueue(uow, personId, request, offerings, methodName);
    }

    /// <summary>Carga cada oferta solicitada; corta en el primer rechazo.</summary>
    private static (List<Oferta> Ofertas, ProductInterestRejection Rejection) ResolveOfferings(
        IUnitOfWork uow, ProductInterestRequest request)
    {
        var offerings = new List<Oferta>();
        foreach (var idOferta in request.OfferingIds)
        {
            var (offering, rechazo) = ProductInterestValidation.GetValidOfferingForInterest(
                uow,
                idOferta,
                request.ProductId,
                request.AdmissionProcessId);
            if (rechazo != ProductInterestRejection.None)
                return ([], rechazo);

            offerings.Add(offering!);
        }

        return (offerings, ProductInterestRejection.None);
    }

    /// <summary>
    /// Registra el interés por producto y encola el alta en Tivenos dentro de una única transacción.
    /// No se divide en dos casos de uso: es una sola unidad transaccional con rollback compartido.
    /// </summary>
    private OperationResult<bool> RegisterAndEnqueue(
        IUnitOfWork uow, long personId, ProductInterestRequest request, List<Oferta> offerings, string methodName)
    {
        var currentDate = DateTime.Now;

        uow.BeginTransaction();
        try
        {
            var result = ProductInterestRegistration.RegisterProductInterestRecord(
                uow,
                _dbConnectionContext,
                personId,
                request,
                offerings,
                currentDate,
                methodName);
            if (!result.Success)
            {
                uow.Rollback();
                return result.Failure().As<bool>(methodName);
            }

            if (result.Data != null)
            {
                _tivenosQueue.EnqueueProductInterestFromSiteSelection(
                    uow,
                    result.Data,
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS));
            }

            uow.Commit();
            return OperationResult<bool>.Ok(true, methodName);
        }
        catch
        {
            uow.Rollback();
            throw;
        }
    }
}
