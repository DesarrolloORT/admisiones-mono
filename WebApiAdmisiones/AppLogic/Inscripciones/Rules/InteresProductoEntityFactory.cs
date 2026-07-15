using AppLogic.Inscripciones.Constants;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Inscripciones.Rules
{
    public static class InteresProductoEntityFactory
    {
        public static Intere CrearInteres(decimal idInteres, long codigoPersona, long idProceso)
        {
            return new Intere
            {
                IdInteres = idInteres,
                CodigoPersona = codigoPersona,
                IdProceso = idProceso,
                IdFormaContacto = InscripcionesConstants.InteresProducto.FormaContactoWeb,
                ContactadorInteres = Constantes.kUSERNAME_USUARIO_ADMISIONES,
                ObservacionesInteres = InscripcionesConstants.InteresProducto.ObservacionesWeb,
                IdLugar = InscripcionesConstants.InteresProducto.LugarInteresWeb,
                IdGradoPureza = Constantes.kGRADO_PUREZA_PURO
            };
        }

        public static InteresProducto CrearInteresProducto(decimal idInteres, long idProducto, DateTime fechaActual)
        {
            return new InteresProducto
            {
                IdInteres = idInteres,
                IdProducto = idProducto,
                IdTipoInteres = Constantes.KTIPO_INTERES_COMUN,
                IdGradoInteres = Constantes.kGRADO_INTERES_ALTO,
                IdGradoInteresAnt = Constantes.kGRADO_INTERES_DESINTERESADO,
                FechaInteresProd = fechaActual,
                FechaAltaInteresProd = fechaActual,
                ObservacionesInteresProd = InscripcionesConstants.InteresProducto.ObservacionesWeb
            };
        }

        public static PersonaAdmite CrearPersonaAdmite(long codigoPersona, DateTime fechaActual)
        {
            return new PersonaAdmite
            {
                CodigoPersona = codigoPersona,
                FechaFrescoPersonaAdmite = fechaActual
            };
        }

        public static InteresProductoOferta CrearInteresProductoOferta(long idInteres, long idProducto, long idOferta)
        {
            return new InteresProductoOferta
            {
                IdInteres = idInteres,
                IdProducto = idProducto,
                IdOferta = idOferta
            };
        }
    }
}
