using AppLogic.Enrollments.Constants;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Enrollments.Mapping;

public static class ProductInterestMapper
{
    public static Intere CreateInterest(decimal idInteres, long personId, long admissionProcessId)
    {
        return new Intere
        {
            IdInteres = idInteres,
            CodigoPersona = personId,
            IdProceso = admissionProcessId,
            IdFormaContacto = EnrollmentConstants.ProductInterest.FormaContactoWeb,
            ContactadorInteres = Constantes.kUSERNAME_USUARIO_ADMISIONES,
            ObservacionesInteres = EnrollmentConstants.ProductInterest.ObservacionesWeb,
            IdLugar = EnrollmentConstants.ProductInterest.LugarInteresWeb,
            IdGradoPureza = Constantes.kGRADO_PUREZA_PURO
        };
    }

    public static InteresProducto CreateProductInterest(decimal idInteres, long productId, DateTime currentDate)
    {
        return new InteresProducto
        {
            IdInteres = idInteres,
            IdProducto = productId,
            IdTipoInteres = Constantes.KTIPO_INTERES_COMUN,
            IdGradoInteres = Constantes.kGRADO_INTERES_ALTO,
            IdGradoInteresAnt = Constantes.kGRADO_INTERES_DESINTERESADO,
            FechaInteresProd = currentDate,
            FechaAltaInteresProd = currentDate,
            ObservacionesInteresProd = EnrollmentConstants.ProductInterest.ObservacionesWeb
        };
    }

    public static PersonaAdmite CreatePersonAdmission(long personId, DateTime currentDate)
    {
        return new PersonaAdmite
        {
            CodigoPersona = personId,
            FechaFrescoPersonaAdmite = currentDate
        };
    }

    public static InteresProductoOferta CreateProductInterestOffering(long idInteres, long productId, long idOferta)
    {
        return new InteresProductoOferta
        {
            IdInteres = idInteres,
            IdProducto = productId,
            IdOferta = idOferta
        };
    }
}
