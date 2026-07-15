using AppLogic.Common.Constants;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Inscripciones.Rules
{
    public static class InteresProductoValidationRules
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

        public static OperationResult<Oferta> ObtenerOfertaValidaParaInteres(
            IUnitOfWork uow,
            long idOferta,
            long idProducto,
            long idProceso,
            string methodName)
        {
            if (idOferta <= 0)
            {
                return OperationResult<Oferta>.IsFailed("GEN_IP_07", methodName, "La oferta indicada es invalida.", 400);
            }

            var oferta = uow.Ofertas.GetByKeyWithRelated(idOferta);
            if (oferta == null)
            {
                return OperationResult<Oferta>.IsFailed("GEN_IP_07", methodName, "No se encontro la oferta indicada.", 404);
            }

            var idProductoOferta = oferta.Supraoferta?.Paquete?.IdProducto ?? 0;
            var idComienzoOferta = oferta.Supraoferta?.IdComienzo ?? 0;
            if (idProductoOferta <= 0
                || idProductoOferta != idProducto
                || idComienzoOferta <= 0)
            {
                return OperationResult<Oferta>.IsFailed("GEN_IP_08", methodName, "La oferta indicada no corresponde al producto seleccionado.", 400);
            }

            if (!string.Equals(oferta.InscripcionesAbiertasOferta, CommonConstants.Booleanos.Si, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(oferta.Supraoferta?.EstadoSupraoferta, "D", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<Oferta>.IsFailed("GEN_IP_09", methodName, "La oferta indicada no se encuentra abierta para inscripcion.", 409);
            }

            var procesoComienzo = uow.ProcesoComienzos.GetByKeyWithRelated(idProceso, idComienzoOferta);
            if (procesoComienzo == null)
            {
                return OperationResult<Oferta>.IsFailed("GEN_IP_10", methodName, "La oferta indicada no corresponde al proceso seleccionado.", 400);
            }

            return OperationResult<Oferta>.Ok(oferta, methodName);
        }
    }
}
