using AppLogic.Enrollments.Mapping;
using AppLogic.Enrollments.Dtos;
using AppLogic.Contracts.Constants;
using AppLogic.Integrations.Tivenos.Dtos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using AppLogic.Contracts;
using Utilities;

namespace AppLogic.Enrollments.Rules;

internal static class ProductInterestRegistration
{
    public static OperationResult<DtoTivenosAltaInteresRequest?> RegisterProductInterestRecord(
        IUnitOfWork uow,
        IDbConnectionContext dbConnectionContext,
        long personId,
        ProductInterestRequest request,
        List<Oferta> offerings,
        DateTime currentDate,
        string methodName)
    {
        var intereses = uow.Interes.GetInteresesPersonaProcesosHabilitados(personId).ToList();

        var interes = intereses.FirstOrDefault(i => i.IdProceso == request.AdmissionProcessId);
        var esInteresNuevo = interes == null;
        interes ??= CreateInterest(uow, dbConnectionContext, personId, request.AdmissionProcessId);

        var tivenosOperation = ActivarInteresProducto(uow, interes, request.ProductId, currentDate, esInteresNuevo);
        EnsurePersonAdmission(uow, personId, currentDate);
        foreach (var idOferta in request.OfferingIds)
        {
            EnsureProductInterestOffering(uow, interes, request.ProductId, idOferta);
        }

        var surveyResult = UpdateInitialSurvey(
            uow,
            personId,
            request.ProductId,
            request.AdmissionProcessId,
            offerings[0].Supraoferta?.IdComienzo ?? 0,
            methodName);
        if (!surveyResult.Success)
            return surveyResult.Failure().As<DtoTivenosAltaInteresRequest?>(methodName);

        return OperationResult<DtoTivenosAltaInteresRequest?>.Ok(
            BuildTivenosRequest(personId, request, tivenosOperation),
            methodName);
    }

    private static Intere CreateInterest(
        IUnitOfWork uow,
        IDbConnectionContext dbConnectionContext,
        long personId,
        long admissionProcessId)
    {
        var interes = ProductInterestMapper.CreateInterest(
            dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES),
            personId,
            admissionProcessId);
        uow.Interes.Add(interes);
        return interes;
    }

    private static TivenosAltaInteresOperacion? ActivarInteresProducto(
        IUnitOfWork uow,
        Intere interes,
        long productId,
        DateTime currentDate,
        bool esInteresNuevo)
    {
        var existingProductInterest = interes.InteresProductos.FirstOrDefault(ip => ip.IdProducto == productId);
        if (existingProductInterest == null)
        {
            uow.InteresProductos.Add(
                ProductInterestMapper.CreateProductInterest(interes.IdInteres, productId, currentDate));
            return esInteresNuevo
                ? TivenosAltaInteresOperacion.CreateProductInterest()
                : TivenosAltaInteresOperacion.CreateOrUpdateInterest();
        }

        var currentProductInterest = uow.InteresProductos.GetByKey(interes.IdInteres, productId);
        if (currentProductInterest == null || currentProductInterest.IdGradoInteres == Constantes.kGRADO_INTERES_INSCRIPTO)
        {
            return null;
        }

        currentProductInterest.IdGradoInteresAnt = currentProductInterest.IdGradoInteres;
        currentProductInterest.IdGradoInteres = Constantes.kGRADO_INTERES_ALTO;
        currentProductInterest.FechaInteresProd = currentDate;
        currentProductInterest.UsuarioModifInteresProd = Constantes.kUSERNAME_USUARIO_ADMISIONES;
        currentProductInterest.FechaModifInteresProd = currentDate;
        currentProductInterest.IdgradoantModifInteresProd = currentProductInterest.IdGradoInteresAnt;
        uow.InteresProductos.Update(currentProductInterest);
        return TivenosAltaInteresOperacion.UpdateInterest();
    }

    private static DtoTivenosAltaInteresRequest? BuildTivenosRequest(
        long personId,
        ProductInterestRequest request,
        TivenosAltaInteresOperacion? operacion)
    {
        if (operacion == null)
        {
            return null;
        }

        return new DtoTivenosAltaInteresRequest
        {
            CodigoPersona = personId,
            IdProducto = request.ProductId,
            IdProceso = request.AdmissionProcessId,
            Operacion = operacion,
        };
    }

    private static void EnsurePersonAdmission(IUnitOfWork uow, long personId, DateTime currentDate)
    {
        var personAdmission = uow.PersonaAdmites.GetByKey(personId);
        if (personAdmission == null)
        {
            uow.PersonaAdmites.Add(
                ProductInterestMapper.CreatePersonAdmission(personId, currentDate));
            return;
        }

        if (!personAdmission.FechaFrescoPersonaAdmite.HasValue)
        {
            personAdmission.FechaFrescoPersonaAdmite = currentDate;
            uow.PersonaAdmites.Update(personAdmission);
        }
    }

    private static void EnsureProductInterestOffering(IUnitOfWork uow, Intere interes, long productId, long idOferta)
    {
        var idInteres = (long)interes.IdInteres;
        var existing = uow.InteresProductoOfertas.GetByKey(idInteres, productId, idOferta);
        if (existing != null)
        {
            return;
        }

        uow.InteresProductoOfertas.Add(
            ProductInterestMapper.CreateProductInterestOffering(idInteres, productId, idOferta));
    }

    private static OperationResult<bool> UpdateInitialSurvey(
        IUnitOfWork uow,
        long personId,
        long productId,
        long admissionProcessId,
        long idComienzo,
        string methodName)
    {
        var survey = uow.EncuestaIniAdmisions.GetByPersona(personId);
        if (survey == null)
        {
            return OperationResult<bool>.Ok(true, methodName);
        }

        survey.IdProducto = productId;
        survey.IdProceso = admissionProcessId;
        survey.IdComienzo = idComienzo;
        uow.EncuestaIniAdmisions.Update(survey);
        return OperationResult<bool>.Ok(true, methodName);
    }
}
