using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Helpers
{
    public static class InteresProductoValidationHelper
    {
        public static OperationResult<bool> ValidarRegistroInteresProducto(
            IUnitOfWork uow,
            long codigoPersona,
            long idProducto,
            long idProceso,
            string method)
        {
            if (!uow.Personas.ExistePersona(codigoPersona))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_01", method, "Persona no encontrada.", 404);
            }

            if (!uow.Productos.EsProductoValidoParaInteres(idProducto))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_02", method, "El producto indicado es inválido.", 400);
            }

            if (!uow.Procesos.TieneProcesoHabilitadoPorProducto(idProducto, idProceso))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_03", method, "Proceso no habilitado para el producto seleccionado.", 400);
            }

            if (uow.Inscriptos.TieneInscripcionPreviaAProducto(codigoPersona, idProducto))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_04", method, "Ya fue inscripto una vez al producto indicado.", 409);
            }

            if (uow.InstanciaWorkflows.TieneInscripcionPendienteParaProducto(codigoPersona, idProducto))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_05", method, "Ya tiene una inscripción pendiente al producto indicado.", 409);
            }

            return OperationResult<bool>.Ok(true, method);
        }
    }
}
