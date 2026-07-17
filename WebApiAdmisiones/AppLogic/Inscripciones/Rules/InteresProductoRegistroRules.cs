using AppLogic.Inscripciones.Dtos;
using AppLogic.Common.Constants;
using AppLogic.Tivenos.Dtos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Inscripciones.Rules
{
    internal static class InteresProductoRegistroRules
    {
        public static OperationResult<DtoTivenosAltaInteresRequest?> RegistrarInteresProducto(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            DtoInteresProductoRequest request,
            List<Oferta> ofertas,
            DateTime fechaActual,
            string methodName)
        {
            var intereses = uow.Interes.GetInteresesPersonaProcesosHabilitados(codigoPersona).ToList();

            var interes = intereses.FirstOrDefault(i => i.IdProceso == request.IdProcesoSeleccionado);
            var esInteresNuevo = interes == null;
            interes ??= CrearInteres(uow, dbConnectionContext, codigoPersona, request.IdProcesoSeleccionado);

            var operacionTivenos = ActivarInteresProducto(uow, interes, request.IdProducto, fechaActual, esInteresNuevo);
            AsegurarPersonaAdmite(uow, codigoPersona, fechaActual);
            foreach (var idOferta in request.IdsOferta)
            {
                AsegurarInteresProductoOferta(uow, interes, request.IdProducto, idOferta);
            }

            var resultadoEncuesta = ActualizarEncuestaInicial(
                uow,
                codigoPersona,
                request.IdProducto,
                request.IdProcesoSeleccionado,
                ofertas[0].Supraoferta.IdComienzo,
                methodName);
            if (!resultadoEncuesta.Success)
            {
                return OperationResult<DtoTivenosAltaInteresRequest?>.IsFailed(
                    resultadoEncuesta.ErrorCode,
                    methodName,
                    resultadoEncuesta.Message,
                    resultadoEncuesta.HttpCode);
            }

            return OperationResult<DtoTivenosAltaInteresRequest?>.Ok(
                CrearRequestTivenos(codigoPersona, request, operacionTivenos),
                methodName);
        }

        private static Intere CrearInteres(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            long idProceso)
        {
            var interes = InteresProductoEntityFactory.CrearInteres(
                dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES),
                codigoPersona,
                idProceso);
            uow.Interes.Add(interes);
            return interes;
        }

        private static TivenosAltaInteresOperacion? ActivarInteresProducto(
            IUnitOfWork uow,
            Intere interes,
            long idProducto,
            DateTime fechaActual,
            bool esInteresNuevo)
        {
            var interesProductoExistente = interes.InteresProductos.FirstOrDefault(ip => ip.IdProducto == idProducto);
            if (interesProductoExistente == null)
            {
                uow.InteresProductos.Add(
                    InteresProductoEntityFactory.CrearInteresProducto(interes.IdInteres, idProducto, fechaActual));
                return esInteresNuevo
                    ? TivenosAltaInteresOperacion.AltaInteresProducto()
                    : TivenosAltaInteresOperacion.AltaActualizarInteres();
            }

            var interesProductoActual = uow.InteresProductos.GetByKey(interes.IdInteres, idProducto);
            if (interesProductoActual == null || interesProductoActual.IdGradoInteres == Constantes.kGRADO_INTERES_INSCRIPTO)
            {
                return null;
            }

            interesProductoActual.IdGradoInteresAnt = interesProductoActual.IdGradoInteres;
            interesProductoActual.IdGradoInteres = Constantes.kGRADO_INTERES_ALTO;
            interesProductoActual.FechaInteresProd = fechaActual;
            interesProductoActual.UsuarioModifInteresProd = Constantes.kUSERNAME_USUARIO_ADMISIONES;
            interesProductoActual.FechaModifInteresProd = fechaActual;
            interesProductoActual.IdgradoantModifInteresProd = interesProductoActual.IdGradoInteresAnt;
            uow.InteresProductos.Update(interesProductoActual);
            return TivenosAltaInteresOperacion.ModificarActualizarInteres();
        }

        private static DtoTivenosAltaInteresRequest? CrearRequestTivenos(
            long codigoPersona,
            DtoInteresProductoRequest request,
            TivenosAltaInteresOperacion? operacion)
        {
            if (operacion == null)
            {
                return null;
            }

            return new DtoTivenosAltaInteresRequest
            {
                CodigoPersona = codigoPersona,
                IdProducto = request.IdProducto,
                IdProceso = request.IdProcesoSeleccionado,
                Operacion = operacion,
            };
        }

        private static void AsegurarPersonaAdmite(IUnitOfWork uow, long codigoPersona, DateTime fechaActual)
        {
            var personaAdmite = uow.PersonaAdmites.GetByKey(codigoPersona);
            if (personaAdmite == null)
            {
                uow.PersonaAdmites.Add(
                    InteresProductoEntityFactory.CrearPersonaAdmite(codigoPersona, fechaActual));
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
                InteresProductoEntityFactory.CrearInteresProductoOferta(idInteres, idProducto, idOferta));
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
