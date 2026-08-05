using AppLogic.Contracts.Constants;
using AppLogic.Enrollments.Constants;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using System.Collections.Generic;
using System.Linq;

namespace AppLogic.Enrollments.Rules;

/// <summary>
/// Reglas de admisión del interés por producto. Devuelven el motivo de rechazo
/// (<see cref="ProductInterestRejection.None"/> si pasa); el código de error, el HTTP y el mensaje
/// los resuelve el caso de uso a través de <see cref="ProductInterestRejectionExtensions"/>.
/// </summary>
public static class ProductInterestValidation
{
    public static ProductInterestRejection ValidateRequestedOfferings(List<long>? idsOferta)
    {
        if (idsOferta == null || idsOferta.Count == 0 || idsOferta.Any(id => id <= 0))
        {
            return ProductInterestRejection.InvalidRequestedOfferings;
        }

        return ProductInterestRejection.None;
    }

    public static ProductInterestRejection ValidateProductInterestRegistration(
        IUnitOfWork uow,
        long personId,
        long productId,
        long admissionProcessId,
        List<long> idsOferta)
    {
        if (!uow.Personas.ExistePersona(personId))
        {
            return ProductInterestRejection.PersonNotFound;
        }

        var product = uow.Productos.GetByKey(productId);
        if (product == null || !uow.Productos.EsProductoValidoParaInteres(productId))
        {
            return ProductInterestRejection.InvalidProduct;
        }

        if (!uow.Procesos.TieneProcesoHabilitadoPorProducto(productId, admissionProcessId))
        {
            return ProductInterestRejection.ProcessNotEnabled;
        }

        // Nivel 3/4 con seminarios admite varias ofertas concurrentes por producto-proceso: el duplicado
        if (product.IdNivelProducto is 3 or 4)
        {
            if (uow.InteresProductoOfertas.TieneInteresRegistradoParaOferta(personId, admissionProcessId, productId, idsOferta))
            {
                return ProductInterestRejection.InterestAlreadyRegistered;
            }

            return ProductInterestRejection.None;
        }

        if (uow.Inscriptos.TieneInscripcionPreviaAProducto(personId, productId))
        {
            return ProductInterestRejection.AlreadyEnrolledInProduct;
        }

        if (uow.InstanciaWorkflows.TieneInscripcionPendienteParaProducto(personId, productId))
        {
            return ProductInterestRejection.PendingEnrollment;
        }

        return ProductInterestRejection.None;
    }

    /// <summary>
    /// Carga la oferta y verifica que sea utilizable para registrar interés.
    /// Devuelve la entidad solo cuando el motivo es <see cref="ProductInterestRejection.None"/>.
    /// </summary>
    public static (Oferta? Oferta, ProductInterestRejection Rejection) GetValidOfferingForInterest(
        IUnitOfWork uow,
        long idOferta,
        long productId,
        long admissionProcessId)
    {
        if (idOferta <= 0)
        {
            return (null, ProductInterestRejection.InvalidOfferingId);
        }

        var offering = uow.Ofertas.GetByKeyWithRelated(idOferta);
        if (offering == null)
        {
            return (null, ProductInterestRejection.OfferingNotFound);
        }

        var offeringProductId = offering.Supraoferta?.Paquete?.IdProducto ?? 0;
        var offeringIntakeId = offering.Supraoferta?.IdComienzo ?? 0;
        if (offeringProductId <= 0
            || offeringProductId != productId
            || offeringIntakeId <= 0)
        {
            return (null, ProductInterestRejection.OfferingNotInProduct);
        }

        if (!string.Equals(offering.InscripcionesAbiertasOferta, SchemaConstants.BooleanFlag.Yes, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(offering.Supraoferta?.EstadoSupraoferta, SchemaConstants.ParentOfferingStatus.Final, StringComparison.OrdinalIgnoreCase))
        {
            return (null, ProductInterestRejection.OfferingClosed);
        }

        var processIntake = uow.ProcesoComienzos.GetByKeyWithRelated(admissionProcessId, offeringIntakeId);
        if (processIntake == null)
        {
            return (null, ProductInterestRejection.OfferingNotInProcess);
        }

        return (offering, ProductInterestRejection.None);
    }
}
