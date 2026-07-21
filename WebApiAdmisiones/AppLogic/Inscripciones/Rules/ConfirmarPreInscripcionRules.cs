using AppLogic.Inscripciones.Constants;
using AppLogic.Inscripciones.Dtos;
using AppLogic.Inscripciones.Encuesta.Rules;
using AppLogic.ApiClients.Dtos;
using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;
using AppLogic.Common.Constants;
using AppLogic.Inscripciones.Constants;
using ModBandejaAppLogic.DevartDTOs;

namespace AppLogic.Inscripciones.Rules
{
    internal static class ConfirmarPreInscripcionRules
    {
        public static OperationResult<bool> ValidarRequest(DtoConfirmarPreInscripcionRequest request, string methodName)
        {
            if (request == null)
            {
                return OperationResult<bool>.IsFailed("INS_CPI_01", methodName, "Request invalido.", 400);
            }

            if (request.IdsOfertasSeleccionadas == null
                || request.IdsOfertasSeleccionadas.Count == 0
                || request.IdsOfertasSeleccionadas.Any(id => id <= 0))
            {
                return OperationResult<bool>.IsFailed("INS_CPI_03", methodName, "Debe indicar al menos una oferta seleccionada valida.", 400);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        public static OperationResult<DatosConfirmacionOferta> ObtenerDatosConfirmacionOferta(
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
                return OperationResult<DatosConfirmacionOferta>.IsFailed(
                    "INS_CPI_08",
                    methodName,
                    "La oferta seleccionada no contiene producto, comienzo o turno validos.",
                    400);
            }

            if (!string.Equals(oferta.InscripcionesAbiertasOferta, CommonConstants.Booleanos.Si, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(oferta.Supraoferta?.EstadoSupraoferta, CommonConstants.EstadoSupraoferta.Definitivo, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<DatosConfirmacionOferta>.IsFailed(
                    "INS_CPI_16",
                    methodName,
                    "La oferta seleccionada no se encuentra abierta para inscripcion.",
                    409);
            }

            var encuestaAdmision = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuestaAdmision != null
                && string.Equals(encuestaAdmision.EstadoEncuestaIniAdmision, EncuestaInicialState.EstadoDefinitivo, StringComparison.OrdinalIgnoreCase))
            {
                return ArmarDatosConfirmacionDesdeEncuestaDefinitiva(
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

                return OperationResult<DatosConfirmacionOferta>.IsFailed(
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

            return OperationResult<DatosConfirmacionOferta>.Ok(
                new DatosConfirmacionOferta(
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

        public static ConfirmarPreInscripcionMultipleApiRequest CrearApiRequestMultiple(
            DatosConfirmacionOferta contexto,
            List<long> idsOfertasSeleccionadas)
        {
            return new ConfirmarPreInscripcionMultipleApiRequest
            {
                IdProducto = contexto.IdProducto,
                IdProceso = contexto.IdProceso,
                IdsOfertasSeleccionadas = idsOfertasSeleccionadas,
                TipoInscripcion = InscripcionesConstants.TipoInscripcion.Online,
                Turno = new DtoTurno { IdTurno = contexto.IdTurno }
            };
        }

        public static OperationResult<bool> ValidarNivelCorporativo(DatosConfirmacionOferta contexto, string methodName)
        {
            var idNivel = contexto.Producto?.IdNivelProducto;
            if (idNivel != 3 && idNivel != 4)
            {
                return OperationResult<bool>.IsFailed(
                    "INS_CPI_17",
                    methodName,
                    "La inscripcion corporativa solo esta disponible para productos de nivel 3 o 4.",
                    400);
            }
            return OperationResult<bool>.Ok(true, methodName);
        }

        public static string CrearXmlInstanciaCorporativa(IReadOnlyList<DatosConfirmacionOferta> ofertas, Persona persona)
        {
            // Producto es compartido por todas las ofertas seleccionadas (la validacion de compatibilidad lo exige).
            var producto = ofertas[0].Producto?.NombreProducto;
            var nombreAlumno = $"({persona.CodigoPersona}) {persona.PrimerNombre} {persona.PrimerApellido}".Trim();
            // Cada oferta lleva su propio comienzo (en nivel 3 y 4 pueden diferir), por eso no hay un field Comienzo aparte.
            var listaOfertas = string.Join(" | ",
                ofertas.Select(o => $"({o.IdOferta}) {o.Comienzo?.NombreComienzo} - {o.Turno?.NombreTurno}"));
            return "<TAREA>" +
                $"<FIELD propertyName=\"Alumno\" name=\"Alumno\" data=\" {nombreAlumno}\"></FIELD>" +
                $"<FIELD propertyName=\"Producto\" name=\"Producto\" data=\" {producto}\"></FIELD>" +
                $"<FIELD propertyName=\"Oferta\" name=\"Oferta Turno\" data=\" {listaOfertas}\"></FIELD>" +
                "<FIELD propertyName=\"Motivo\" name=\"Motivo\" data=\" Inscripcion corporativa\"></FIELD>" +
                "</TAREA>";
        }

        public static DtoTramiteBandejaDevartModBandeja CrearDtoTramiteCorporativo(long codigoPersona)
        {
            return new DtoTramiteBandejaDevartModBandeja
            {
                CodigoPersona = codigoPersona,
                IdGrupoResponsable = InscripcionesConstants.BandejaCorporativa.IdGrupoResponsable,
                IdProceso = InscripcionesConstants.BandejaCorporativa.IdProceso,
                ObservacionesTramiteBandeja = string.Empty,
                TitularTramiteBandeja = "Inscripcion corporativa",
            };
        }

        public static DtoInstanciaWorkflowDevartModBandeja CrearDtoInstanciaCorporativa(
            DatosConfirmacionOferta contexto,
            long codigoPersona,
            string xml)
        {
            var ahora = DateTime.Now;
            return new DtoInstanciaWorkflowDevartModBandeja
            {
                IdProceso = InscripcionesConstants.BandejaCorporativa.IdProceso,
                DescripcionInstanciaWorkflow = "Inscripcion corporativa",
                SolicitanteInstanciaWorkflow = codigoPersona,
                // IdObjetoInstanciaWorkflow es un solo long: referencia la primera oferta; el detalle completo va en el XML.
                IdObjetoInstanciaWorkflow = contexto.IdOferta,
                FechaVtoInstanciaWorkflow = ahora.AddDays(5),
                XmlInstanciaWorkflow = xml,
                IdDepartamento = contexto.Producto?.IdDepartamento,
            };
        }

        public static IEnumerable<DtoBandejaDevartModBandeja> CrearBandejasCorporativas(string usuarioIngreso)
        {
            var ahora = DateTime.Now;
            var hora = ahora.ToString(InscripcionesConstants.InteresProducto.FormatoHora);

            return new[]
            {
                // Paso 1 (inicio de tramite): se auto-completa al instante para avanzar al paso real, igual que OrdenWF==1 en legacy (proceso 75).
                new DtoBandejaDevartModBandeja
                {
                    IdEstadoProceso = InscripcionesConstants.BandejaCorporativa.IdEstadoProcesoInicio,
                    IdGrupoResponsable = InscripcionesConstants.BandejaCorporativa.IdGrupoResponsable,
                    FechaIngresoBandeja = ahora,
                    FechaTomadoBandeja = ahora,
                    UsuarioTomadoBandeja = usuarioIngreso,
                    FechaRealizadoBandeja = ahora,
                    FechaVencimientoBandeja = ahora,
                    AccionMenu = "SIGUIENTE",
                    UsuarioAccionMenu = usuarioIngreso,
                    FechaAccionMenu = ahora,
                    FechaReasignadoBandeja = ahora,
                    ReasignadoPorBandeja = usuarioIngreso,
                },
                // Paso 2 (solicitud): queda pendiente de verdad para el grupo responsable (TipoPara "PARA_UN_GRUPO" en legacy).
                new DtoBandejaDevartModBandeja
                {
                    IdEstadoProceso = InscripcionesConstants.BandejaCorporativa.IdEstadoProcesoSolicitud,
                    IdGrupoResponsable = InscripcionesConstants.BandejaCorporativa.IdGrupoResponsable,
                    FechaIngresoBandeja = ahora,
                    FechaTomadoBandeja = DateTime.MinValue,
                    UsuarioTomadoBandeja = string.Empty,
                    FechaVencimientoBandeja = ahora.AddDays(5),
                    FechaReasignadoBandeja = ahora,
                    ReasignadoPorBandeja = usuarioIngreso,
                }
            };
        }

        public static DtoConfirmarPreInscripcionResponse MapearResultadoCorporativo(IReadOnlyList<DatosConfirmacionOferta> ofertas)
        {
            var cabecera = ofertas[0];
            return new DtoConfirmarPreInscripcionResponse
            {
                Confirmada = false,
                EnEspera = true,
                Resumen = new DtoCabeceraInscripcion
                {
                    IdProducto = cabecera.IdProducto,
                    Carrera = cabecera.Producto?.NombreExtensoProducto ?? cabecera.Producto?.NombreProducto
                },
                Inscripciones = ofertas
                    .Select(o => new DtoInscripcionOferta
                    {
                        IdOferta = o.IdOferta,
                        Comienzo = o.Comienzo?.NombreComienzo,
                        Turno = o.Turno?.NombreTurno
                    })
                    .ToList()
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

        private static OperationResult<DatosConfirmacionOferta> ArmarDatosConfirmacionDesdeEncuestaDefinitiva(
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
                return OperationResult<DatosConfirmacionOferta>.IsFailed(
                    "INS_CPI_12",
                    methodName,
                    "La encuesta inicial de admision se encuentra vencida.",
                    409);
            }

            if (!TieneDatosConfirmacionValidos(encuestaAdmision.IdProducto, encuestaAdmision.IdProceso, encuestaAdmision.IdComienzo))
            {
                return OperationResult<DatosConfirmacionOferta>.IsFailed(
                    "INS_CPI_08",
                    methodName,
                    "La encuesta inicial de admision no contiene producto, proceso o comienzo validos.",
                    400);
            }

            var procesoInteres = uow.InteresProductoOfertas.GetProcesoPorInteresActivoOferta(
                codigoPersona,
                idProductoOferta,
                oferta.IdOferta);
            if (procesoInteres == null || procesoInteres.IdProceso <= 0)
            {
                return ErrorInteresOfertaNoEncontrado(methodName);
            }

            return OperationResult<DatosConfirmacionOferta>.Ok(
                new DatosConfirmacionOferta(
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

        internal static decimal SumarPagoReserva(IEnumerable<CarritoPagoReservaApiDto>? carritos)
        {
            return carritos?.Sum(c => c.PagoReserva) ?? 0;
        }

        private static OperationResult<DatosConfirmacionOferta> ErrorInteresOfertaNoEncontrado(string methodName)
        {
            return OperationResult<DatosConfirmacionOferta>.IsFailed(
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

    internal sealed record DatosConfirmacionOferta(
        long IdOferta,
        long IdProducto,
        long IdProceso,
        long IdComienzo,
        long IdTurno,
        Producto? Producto,
        Comienzo? Comienzo,
        Turno? Turno);
}
