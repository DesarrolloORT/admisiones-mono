using AppLogic.Constants;
using AppLogic.DTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Helpers
{
    internal static class InteresProductoRegistroHelper
    {
        public static OperationResult<bool> RegistrarInteresProducto(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            InteresProductoRequest request,
            Oferta oferta,
            DateTime fechaActual,
            string methodName)
        {
            var intereses = uow.Interes.GetInteresesPersonaProcesosHabilitados(codigoPersona).ToList();

            ResetearInteresesProductos(uow, intereses, fechaActual);

            var interes = intereses.FirstOrDefault(i => i.IdProceso == request.IdProcesoSeleccionado)
                ?? CrearInteres(uow, dbConnectionContext, codigoPersona, request.IdProcesoSeleccionado);

            ActivarInteresProducto(uow, interes, request.IdProducto, fechaActual);
            // TODO Tivenos: encolar AltaInteresXSeleccionEnSitio para el interes producto registrado.
            AsegurarPersonaAdmite(uow, codigoPersona, fechaActual);
            AsegurarInteresProductoOferta(uow, interes, request.IdProducto, request.IdOferta);

            return ActualizarEncuestaInicial(
                uow,
                codigoPersona,
                request.IdProducto,
                request.IdProcesoSeleccionado,
                oferta.Supraoferta.IdComienzo,
                methodName);
        }

        private static void ResetearInteresesProductos(IUnitOfWork uow, IEnumerable<Intere> intereses, DateTime fechaActual)
        {
            foreach (var interes in intereses)
            {
                foreach (var interesProducto in interes.InteresProductos)
                {
                    var interesProductoActual = uow.InteresProductos.GetByKey(interesProducto.IdInteres, interesProducto.IdProducto);
                    if (interesProductoActual == null)
                    {
                        continue;
                    }

                    if (interesProductoActual.IdGradoInteres == Constantes.kGRADO_INTERES_INSCRIPTO)
                    {
                        continue;
                    }

                    interesProductoActual.IdGradoInteresAnt = interesProductoActual.IdGradoInteres;
                    interesProductoActual.IdGradoInteres = Constantes.kGRADO_INTERES_DESINTERESADO;
                    interesProductoActual.UsuarioModifInteresProd = Constantes.kUSERNAME_USUARIO_ADMISIONES;
                    interesProductoActual.FechaModifInteresProd = fechaActual;
                    interesProductoActual.IdgradoantModifInteresProd = interesProductoActual.IdGradoInteresAnt;
                    uow.InteresProductos.Update(interesProductoActual);
                }
            }
        }

        private static Intere CrearInteres(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            long idProceso)
        {
            var interes = InteresProductoEntityFactoryHelper.CrearInteres(
                dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES),
                codigoPersona,
                idProceso);
            uow.Interes.Add(interes);
            return interes;
        }

        private static void ActivarInteresProducto(IUnitOfWork uow, Intere interes, long idProducto, DateTime fechaActual)
        {
            var interesProductoExistente = interes.InteresProductos.FirstOrDefault(ip => ip.IdProducto == idProducto);
            if (interesProductoExistente == null)
            {
                uow.InteresProductos.Add(
                    InteresProductoEntityFactoryHelper.CrearInteresProducto(interes.IdInteres, idProducto, fechaActual));
                return;
            }

            var interesProductoActual = uow.InteresProductos.GetByKey(interes.IdInteres, idProducto);
            if (interesProductoActual == null || interesProductoActual.IdGradoInteres == Constantes.kGRADO_INTERES_INSCRIPTO)
            {
                return;
            }

            interesProductoActual.IdGradoInteresAnt = interesProductoActual.IdGradoInteres;
            interesProductoActual.IdGradoInteres = Constantes.kGRADO_INTERES_ALTO;
            interesProductoActual.FechaInteresProd = fechaActual;
            interesProductoActual.UsuarioModifInteresProd = Constantes.kUSERNAME_USUARIO_ADMISIONES;
            interesProductoActual.FechaModifInteresProd = fechaActual;
            interesProductoActual.IdgradoantModifInteresProd = interesProductoActual.IdGradoInteresAnt;
            uow.InteresProductos.Update(interesProductoActual);
        }

        private static void AsegurarPersonaAdmite(IUnitOfWork uow, long codigoPersona, DateTime fechaActual)
        {
            var personaAdmite = uow.PersonaAdmites.GetByKey(codigoPersona);
            if (personaAdmite == null)
            {
                uow.PersonaAdmites.Add(
                    InteresProductoEntityFactoryHelper.CrearPersonaAdmite(codigoPersona, fechaActual));
                return;
            }

            if (!personaAdmite.FechaFrescoPersonaAdmite.HasValue)
            {
                personaAdmite.FechaFrescoPersonaAdmite = fechaActual;
                uow.PersonaAdmites.Update(personaAdmite);
            }
        }

        private static void AsegurarInteresProductoOferta(IUnitOfWork uow, Intere interes, long idProducto, long idOferta)
        {
            var idInteres = (long)interes.IdInteres;
            var existente = uow.InteresProductoOfertas.GetByKey(idInteres, idProducto, idOferta);
            if (existente != null)
            {
                return;
            }

            uow.InteresProductoOfertas.Add(
                InteresProductoEntityFactoryHelper.CrearInteresProductoOferta(idInteres, idProducto, idOferta));
        }

        private static OperationResult<bool> ActualizarEncuestaInicial(
            IUnitOfWork uow,
            long codigoPersona,
            long idProducto,
            long idProceso,
            long idComienzo,
            string methodName)
        {
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
            {
                return OperationResult<bool>.Ok(true, methodName);
            }

            encuesta.IdProducto = idProducto;
            encuesta.IdProceso = idProceso;
            encuesta.IdComienzo = idComienzo;
            return OperationResult<bool>.Ok(true, methodName);
        }
    }
}
