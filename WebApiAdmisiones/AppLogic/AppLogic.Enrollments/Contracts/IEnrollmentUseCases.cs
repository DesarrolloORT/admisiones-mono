using AppLogic.Enrollments.Dtos;
using Utilities;

namespace AppLogic.Enrollments.Contracts;

/// <summary>Paso 1: registra el interés de la persona por un producto, proceso y ofertas habilitadas.</summary>
public interface IRegisterProductInterest
{
    OperationResult<bool> Execute(long personId, ProductInterestRequest request);
}

/// <summary>Paso 2: confirma la preinscripción de una o varias ofertas.</summary>
public interface IConfirmPreEnrollment
{
    Task<OperationResult<ConfirmPreEnrollmentResponse>> ExecuteAsync(long personId, ConfirmPreEnrollmentRequest request);
}

/// <summary>Reactiva una inscripción dada de baja creando una nueva para la misma oferta.</summary>
public interface IReactivateEnrollment
{
    Task<OperationResult<ConfirmPreEnrollmentResponse>> ExecuteAsync(long personId, ReactivateEnrollmentRequest request);
}

/// <summary>Detalle de una inscripción de "Mis carreras" según su estado.</summary>
public interface IGetEnrollmentDetails
{
    /// <param name="personId">Persona dueña de la inscripción.</param>
    /// <param name="productId">Producto de la tarjeta que se abrió.</param>
    /// <param name="admissionProcessId">Proceso de admisión de la tarjeta que se abrió.</param>
    /// <param name="status">
    /// Estado de la tarjeta que se abrió. Un producto y proceso puede tener ofertas en más de un
    /// estado (seminarios de nivel 3 y 4), y cada estado es una tarjeta distinta en "Mis carreras":
    /// sin este dato el detalle mezcla las ofertas de todos los estados. Null: comportamiento
    /// anterior, gana la fila con el comienzo más temprano.
    /// </param>
    Task<OperationResult<EnrollmentDetailsResponse>> ExecuteAsync(long personId, long productId, long admissionProcessId, string? status = null);
}

/// <summary>Indica si la persona ya aceptó el reglamento estudiantil.</summary>
public interface IGetStudentRegulationsAcceptance
{
    OperationResult<StudentRegulationsAcceptanceResponse> Execute(long personId);
}
