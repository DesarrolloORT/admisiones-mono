using AppLogic.Constants;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Helpers
{
    public static class InteresProductoEntityFactoryHelper
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

        public static Actividad CrearActividad(decimal idActividad, long idProceso, DateTime fechaActual)
        {
            return new Actividad
            {
                IdActividad = idActividad,
                IdFormaContacto = InscripcionesConstants.InteresProducto.FormaContactoWeb,
                IdTipoActividad = InscripcionesConstants.InteresProducto.TipoActividadMonoAccion,
                IdTipoAccion = InscripcionesConstants.InteresProducto.TipoAccionRegistroSitioAdmisiones,
                IdEstadoAccion = InscripcionesConstants.InteresProducto.EstadoAccionRealizada,
                UsernameGeneradorActividad = Constantes.kUSERNAME_USUARIO_ADMISIONES,
                FechaGeneradorActividad = fechaActual,
                UsernameRealizadoActividad = Constantes.kUSERNAME_USUARIO_ADMISIONES,
                FechaRealizadoActividad = fechaActual,
                IdProceso = idProceso
            };
        }

        public static Accion CrearAccion(decimal idAccion, decimal idActividad, long codigoPersona, DateTime fechaActual)
        {
            return new Accion
            {
                IdAccion = idAccion,
                IdActividad = idActividad,
                CodigoPersona = codigoPersona,
                FechaRealizadoAccion = fechaActual,
                UsuarioRealizadoAccion = Constantes.kUSERNAME_USUARIO_ADMISIONES,
                IdEstadoAccion = InscripcionesConstants.InteresProducto.EstadoAccionRealizada,
                IdAccionResultado = InscripcionesConstants.InteresProducto.ResultadoAccionRealizada
            };
        }
    }
}
