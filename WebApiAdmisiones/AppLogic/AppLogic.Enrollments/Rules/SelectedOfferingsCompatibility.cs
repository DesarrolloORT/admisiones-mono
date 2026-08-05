using AppLogic.Contracts;
using AppLogic.Enrollments.Dtos;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.Rules;

/// <summary>
/// Valida cada oferta seleccionada y que todas sean compatibles entre sí.
/// Nivel 1 y 2 exigen mismo producto, turno y comienzo; nivel 3 y 4 solo mismo producto.
/// </summary>
internal static class SelectedOfferingsCompatibility
{
    internal static OperationResult<List<OfferingConfirmationData>> Resolve(
        IUnitOfWork uow, long personId, ConfirmPreEnrollmentRequest request, string methodName)
    {
        var selected = new List<OfferingConfirmationData>();
        foreach (var selectedOfferingId in request.SelectedOfferingIds)
        {
            var offering = uow.Ofertas.GetByKeyWithRelated(selectedOfferingId);
            if (offering == null)
            {
                return OperationResult<List<OfferingConfirmationData>>.IsFailed("INS_CPI_15", methodName, $"No se encontro la oferta seleccionada: {selectedOfferingId}.", 404);
            }

            var offeringData = PreEnrollmentConfirmationRules.GetOfferingConfirmationData(uow, personId, offering, methodName);
            if (!offeringData.Success)
                return offeringData.Failure().As<List<OfferingConfirmationData>>(methodName);

            if (selected.Count > 0 && !IsCompatibleWithSelection(selected[0], offeringData.Data!))
            {
                return OperationResult<List<OfferingConfirmationData>>.IsFailed(
                    "INS_CPI_17",
                    methodName,
                    "Todas las ofertas seleccionadas deben pertenecer al mismo producto (y al mismo turno y comienzo para nivel 1 y 2).",
                    400);
            }

            selected.Add(offeringData.Data!);
        }

        return OperationResult<List<OfferingConfirmationData>>.Ok(selected, methodName);
    }

    private static bool IsCompatibleWithSelection(OfferingConfirmationData seleccion, OfferingConfirmationData offering)
    {
        if (seleccion.IdProducto != offering.IdProducto)
        {
            return false;
        }

        if (RequiresSameShift(seleccion) && seleccion.IdTurno != offering.IdTurno)
        {
            return false;
        }

        return !RequiresSameIntake(seleccion) || seleccion.IdComienzo == offering.IdComienzo;
    }

    /// <summary>Nivel 1 y 2 exigen que todas las ofertas compartan turno; nivel 3 y 4 no.</summary>
    private static bool RequiresSameShift(OfferingConfirmationData data) =>
        data.Producto?.IdNivelProducto is not (3 or 4);

    /// <summary>Nivel 1 y 2 exigen que todas las ofertas compartan comienzo; nivel 3 y 4 no.</summary>
    private static bool RequiresSameIntake(OfferingConfirmationData data) =>
        data.Producto?.IdNivelProducto is not (3 or 4);
}
