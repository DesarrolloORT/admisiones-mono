using AppLogic.Inscripciones.Requests;
using AppLogic.Inscripciones.Responses;
using AppLogic.ApiClients.Dtos;
using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;
using AppLogic.Common.Constants;

namespace AppLogic.Inscripciones.Rules
{
    internal static class ConfirmarPreInscripcionHelper
    {
        private const string EstadoDefinitivo = "DEFINITIVO";

        public static OperationResult<bool> ValidarRequest(DtoConfirmarPreInscripcionRequest request, string methodName)
        {
            if (request == null)
            {
                return OperationResult<bool>.IsFailed("INS_CPI_01", methodName, "Request invalido.", 400);
            }

            if (request.IdOfertaSeleccionada <= 0)
            {
                return OperationResult<bool>.IsFailed("INS_CPI_03", methodName, "La oferta seleccionada es invalida.", 400);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        public static OperationResult<ContextoConfirmacionPreInscripcion> ObtenerContextoConfirmacion(
            IUnitOfWork uow,
            long codigoPersona,
            Oferta oferta,
            string methodName)
        {
            var idOfertaSeleccionada = oferta.IdOferta;
            var idProductoOferta = oferta.Supraoferta?.Paquete?.IdProducto ?? 0;
            var idComienzoOferta = oferta.Supraoferta?.IdComienzo ?? 0;
            if (idProductoOferta <= 0
                || idComienzoOferta <= 0
                || oferta.IdTurno <= 0)
            {
                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    "INS_CPI_08",
                    methodName,
                    "La oferta seleccionada no contiene producto, comienzo o turno validos.",
                    400);
            }

            if (!string.Equals(oferta.InscripcionesAbiertasOferta, CommonConstants.Booleanos.Si, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(oferta.Supraoferta?.EstadoSupraoferta, "D", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    "INS_CPI_16",
                    methodName,
                    "La oferta seleccionada no se encuentra abierta para inscripcion.",
                    409);
            }

            var encuestaAdmision = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuestaAdmision != null
                && string.Equals(encuestaAdmision.EstadoEncuestaIniAdmision, EstadoDefinitivo, StringComparison.OrdinalIgnoreCase))
            {
                return ResolverContextoConEncuestaAdmisionDefinitiva(
                    uow,
                    codigoPersona,
                    oferta,
                    encuestaAdmision,
                    idProductoOferta,
                    idComienzoOferta,
                    methodName);
            }

            var encuestaHistorica = uow.EncuestaInis.GetByPersona(codigoPersona);
            if (encuestaHistorica == null)
            {
                var errorCode = encuestaAdmision == null ? "INS_CPI_06" : "INS_CPI_07";
                var message = encuestaAdmision == null
                    ? "No se encontro una encuesta inicial de admision vigente para la persona."
                    : "La encuesta inicial debe estar en estado DEFINITIVO para confirmar la preinscripcion.";
                var httpCode = encuestaAdmision == null ? 404 : 409;

                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    errorCode,
                    methodName,
                    message,
                    httpCode);
            }

            var proceso = uow.InteresProductoOfertas.GetProcesoPorInteresActivoOferta(
                codigoPersona,
                idProductoOferta,
                idOfertaSeleccionada);
            if (proceso == null || proceso.IdProceso <= 0)
            {
                return ErrorInteresOfertaNoEncontrado(methodName);
            }

            return OperationResult<ContextoConfirmacionPreInscripcion>.Ok(
                new ContextoConfirmacionPreInscripcion(
                    idOfertaSeleccionada,
                    idProductoOferta,
                    proceso.IdProceso,
                    idComienzoOferta,
                    oferta.IdTurno,
                    oferta.Supraoferta!.Paquete!.Producto,
                    oferta.Supraoferta.Comienzo,
                    oferta.Turno),
                methodName);
        }

        public static ConfirmarPreInscripcionApiRequest CrearApiRequest(
            ContextoConfirmacionPreInscripcion contexto,
            long idOfertaSeleccionada)
        {
            return new ConfirmarPreInscripcionApiRequest
            {
                IdProducto = contexto.IdProducto,
                IdProceso = contexto.IdProceso,
                IdOfertaSeleccionada = idOfertaSeleccionada,
                TipoInscripcion = "ONLINE",
                Turno = new DtoTurno { IdTurno = contexto.IdTurno }
            };
        }

        public static OperationResult<DtoAceptacionReglamentoEstDevart> AsegurarAceptacionReglamentoEstudiantil(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            long codigoPersona,
            long idProducto,
            long idComienzo,
            bool aceptoReglamento,
            string methodName)
        {
            var existente = uow.AceptacionReglamentoEsts.GetByPersonaProductoComienzo(codigoPersona, idProducto, idComienzo);
            if (existente != null)
            {
                return OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(existente.ToDto(), methodName);
            }

            // El reglamento se acepta una sola vez por persona; si no lo acepta ahora pero ya lo
            // aceptó antes, se carga la fila para esta oferta sin volver a pedir aceptación.
            if (!aceptoReglamento
                && uow.AceptacionReglamentoEsts.GetByPersona(codigoPersona) == null)
            {
                return OperationResult<DtoAceptacionReglamentoEstDevart>.IsFailed(
                    "INS_CPI_02",
                    methodName,
                    "Debe aceptar el reglamento estudiantil para confirmar la preinscripcion.",
                    400);
            }

            var entidad = new AceptacionReglamentoEst
            {
                IdAceptacionReglamentoEst = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST),
                CodigoPersona = codigoPersona,
                IdProducto = idProducto,
                IdComienzo = idComienzo,
                IdSistema = CommonConstants.IdSistemaAdmisiones
            };

            uow.BeginTransaction();
            try
            {
                uow.AceptacionReglamentoEsts.Add(entidad);
                uow.Save();
                uow.Commit();
            }
            catch
            {
                uow.Rollback();
                throw;
            }

            return OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(entidad.ToDto(), methodName);
        }

        public static OperationResult<DtoConfirmarPreInscripcionResponse> MapearResultadoApi(
            OperationResult<ConfirmarPreInscripcionApiResponse> apiResult,
            ContextoConfirmacionPreInscripcion contexto,
            string methodName)
        {
            if (!apiResult.Success)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed(apiResult.ErrorCode, methodName, apiResult.Message, apiResult.HttpCode);
            }

            if (apiResult.Data == null)
            {
                return OperationResult<DtoConfirmarPreInscripcionResponse>.IsFailed("INS_CPI_13", methodName, "La API interna no devolvio datos de confirmacion.", 502);
            }

            return OperationResult<DtoConfirmarPreInscripcionResponse>.Ok(
                MapearConfirmacionPreInscripcion(apiResult.Data, contexto),
                methodName);
        }

        public static DtoEstadoCuenta? MapearEstadoCuenta(EstadoCuentaApiDto? source)
        {
            if (source == null)
            {
                return null;
            }

            return new DtoEstadoCuenta
            {
                SaldoActual = source.SaldoActual
            };
        }

        private static OperationResult<ContextoConfirmacionPreInscripcion> ResolverContextoConEncuestaAdmisionDefinitiva(
            IUnitOfWork uow,
            long codigoPersona,
            Oferta oferta,
            EncuestaIniAdmision encuestaAdmision,
            long idProductoOferta,
            long idComienzoOferta,
            string methodName)
        {
            if (encuestaAdmision.FechaVtoAdmision.HasValue && encuestaAdmision.FechaVtoAdmision.Value.Date < DateTime.Today)
            {
                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    "INS_CPI_12",
                    methodName,
                    "La encuesta inicial de admision se encuentra vencida.",
                    409);
            }

            if (!TieneDatosConfirmacionValidos(encuestaAdmision.IdProducto, encuestaAdmision.IdProceso, encuestaAdmision.IdComienzo))
            {
                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    "INS_CPI_08",
                    methodName,
                    "La encuesta inicial de admision no contiene producto, proceso o comienzo validos.",
                    400);
            }

            if (encuestaAdmision.IdProducto!.Value != idProductoOferta
                || encuestaAdmision.IdComienzo!.Value != idComienzoOferta)
            {
                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    "INS_CPI_15",
                    methodName,
                    "La oferta seleccionada no coincide con la encuesta inicial de admision.",
                    409);
            }

            var procesoInteres = uow.InteresProductoOfertas.GetProcesoPorInteresActivoOferta(
                codigoPersona,
                idProductoOferta,
                oferta.IdOferta);
            if (procesoInteres == null || procesoInteres.IdProceso <= 0)
            {
                return ErrorInteresOfertaNoEncontrado(methodName);
            }

            if (procesoInteres.IdProceso != encuestaAdmision.IdProceso!.Value)
            {
                return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                    "INS_CPI_15",
                    methodName,
                    "La oferta seleccionada no coincide con el proceso de la encuesta inicial de admision.",
                    409);
            }

            return OperationResult<ContextoConfirmacionPreInscripcion>.Ok(
                new ContextoConfirmacionPreInscripcion(
                    oferta.IdOferta,
                    idProductoOferta,
                    procesoInteres.IdProceso,
                    idComienzoOferta,
                    oferta.IdTurno,
                    oferta.Supraoferta!.Paquete!.Producto ?? encuestaAdmision.Producto,
                    oferta.Supraoferta.Comienzo ?? encuestaAdmision.Comienzo,
                    oferta.Turno),
                methodName);
        }

        private static DtoConfirmarPreInscripcionResponse MapearConfirmacionPreInscripcion(
            ConfirmarPreInscripcionApiResponse source,
            ContextoConfirmacionPreInscripcion contexto)
        {
            return new DtoConfirmarPreInscripcionResponse
            {
                Confirmada = source.Confirmada || source.Success,
                EnEspera = source.InscripcionPendiente,
                IdInscripcion = source.IdInscripcion,
                FechaVencimientoPago = source.FechaVencimientoPago,
                Senia = SumarSenias(source.Carritos),
                EstadoCuenta = MapearEstadoCuenta(source.EstadoCuenta),
                Resumen = new DtoResumenInscripcion
                {
                    IdOferta = source.Resumen != null && source.Resumen.IdOferta > 0 ? source.Resumen.IdOferta : contexto.IdOferta,
                    IdProducto = source.Resumen?.IdProducto ?? contexto.IdProducto,
                    Carrera = source.Resumen?.Carrera ?? contexto.Producto?.NombreExtensoProducto ?? contexto.Producto?.NombreProducto,
                    IdComienzo = source.Resumen?.IdComienzo ?? contexto.IdComienzo,
                    Comienzo = source.Resumen?.Comienzo ?? contexto.Comienzo?.NombreComienzo,
                    IdTurno = source.Resumen?.IdTurno ?? contexto.IdTurno,
                    Turno = source.Resumen?.Turno ?? contexto.Turno?.NombreTurno
                }
            };
        }

        internal static decimal SumarSenias(IEnumerable<CarritoSeniaApiDto>? carritos)
        {
            return carritos?.Sum(c => c.Senia) ?? 0;
        }

        private static OperationResult<ContextoConfirmacionPreInscripcion> ErrorInteresOfertaNoEncontrado(string methodName)
        {
            return OperationResult<ContextoConfirmacionPreInscripcion>.IsFailed(
                "INS_CPI_14",
                methodName,
                "No existe interes activo para la oferta seleccionada.",
                409);
        }

        private static bool TieneDatosConfirmacionValidos(long? idProducto, long? idProceso, long? idComienzo)
        {
            return TieneValorPositivo(idProducto)
                && TieneValorPositivo(idProceso)
                && TieneValorPositivo(idComienzo);
        }

        private static bool TieneValorPositivo(long? valor)
        {
            return valor.HasValue && valor.Value > 0;
        }
    }

    internal sealed record ContextoConfirmacionPreInscripcion(
        long IdOferta,
        long IdProducto,
        long IdProceso,
        long IdComienzo,
        long IdTurno,
        Producto? Producto,
        Comienzo? Comienzo,
        Turno? Turno);
}
