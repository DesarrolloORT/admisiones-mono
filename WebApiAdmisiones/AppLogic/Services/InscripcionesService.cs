using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Services
{
    public class InscripcionesService : IInscripcionesService
    {
        private const string EstadoConfirmada = "Confirmada";
        private const string EstadoPendiente = "Pendiente";
        private const string EstadoCancelada = "Cancelada";

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;

        public InscripcionesService(IUnitOfWorkFactory uowFactory, IDbConnectionContext dbConnectionContext)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
        }

        public OperationResult<DtoUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var inscripto = uow.Inscriptos.GetUltimaInscripcionActiva(codigoPersona);
            if (inscripto == null)
            {
                return OperationResult<DtoUltimaInscripcion>.IsFailed(
                    "GEN_UI_01",
                    nameof(ObtenerUltimaInscripcionActiva),
                    "No se encontró inscripción para la persona.",
                    204);
            }

            var dto = new DtoUltimaInscripcion
            {
                IdInscripto = inscripto.IdInscripto,
                IdProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                NombreProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreProducto,
                NombreExtensoProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                IdComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.IdComienzo ?? 0,
                NombreComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
            };
            return OperationResult<DtoUltimaInscripcion>.Ok(dto, nameof(ObtenerUltimaInscripcionActiva));
        }

        public OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosVigentesConInteres(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosVigentesConInteres(codigoPersona);
            var dtos = entidades.Select(MapProductoAdmisiones).ToList();
            return OperationResult<IEnumerable<DtoProductoAdmisiones>>.Ok(dtos, nameof(ObtenerProductosVigentesConInteres));
        }

        public OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosConInteresActivo(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Productos.GetProductosConInteresActivo(codigoPersona);
            var dtos = entidades
                .Select(p => MapProductoConInteresActivo(p, uow.Interes.GetProcesoPorInteresActivo(codigoPersona, p.IdProducto)))
                .ToList();
            return OperationResult<IEnumerable<DtoProductoAdmisiones>>.Ok(dtos, nameof(ObtenerProductosConInteresActivo));
        }

        public OperationResult<bool> RegistrarInteresProducto(long codigoPersona, InteresProductoRequest request)
        {
            using var uow = _uowFactory.Create();

            if (!uow.Personas.ExistePersona(codigoPersona))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_01", nameof(RegistrarInteresProducto), "Persona no encontrada.", 404);
            }

            if (!uow.Productos.EsProductoValidoParaInteres(request.IdProducto))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_02", nameof(RegistrarInteresProducto), "El producto indicado es inválido.", 400);
            }

            if (!uow.Procesos.TieneProcesoHabilitadoPorProducto(request.IdProducto, request.IdProcesoSeleccionado))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_03", nameof(RegistrarInteresProducto), "Proceso no habilitado para el producto seleccionado.", 400);
            }

            if (uow.Inscriptos.TieneInscripcionPreviaAProducto(codigoPersona, request.IdProducto))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_04", nameof(RegistrarInteresProducto), "Ya fue inscripto una vez al producto indicado.", 409);
            }

            if (uow.InstanciaWorkflows.TieneInscripcionPendienteParaProducto(codigoPersona, request.IdProducto))
            {
                return OperationResult<bool>.IsFailed("GEN_IP_05", nameof(RegistrarInteresProducto), "Ya tiene una inscripción pendiente al producto indicado.", 409);
            }

            var fechaActual = DateTime.Now;
            var intereses = uow.Interes.GetInteresesPersonaProcesosHabilitados(codigoPersona).ToList();

            uow.BeginTransaction();
            try
            {
                ResetearInteresesProductos(uow, intereses, fechaActual);

                var interesExistente = intereses.FirstOrDefault(i => i.IdProceso == request.IdProcesoSeleccionado);
                if (interesExistente == null)
                {
                    interesExistente = CrearInteres(uow, codigoPersona, request.IdProcesoSeleccionado, fechaActual);
                }

                ActivarInteresProducto(uow, interesExistente, request.IdProducto, fechaActual);
                AsegurarPersonaAdmite(uow, codigoPersona, fechaActual);
                ActualizarEncuestaInicial(uow, codigoPersona, request.IdProducto, request.IdProcesoSeleccionado);
                RegistrarActividadInteres(uow, codigoPersona, request.IdProcesoSeleccionado, fechaActual);
                uow.Commit();

                return OperationResult<bool>.Ok(true, nameof(RegistrarInteresProducto));
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        public OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.VdEsFrescoAdmisions.TieneInscripcionActivaParaProceso(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionActivaParaProceso));
        }

        public OperationResult<IEnumerable<DtoInscripcionHome>> ObtenerMisInscripciones(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var confirmadas = uow.Inscriptos.GetInscripcionesRealizadas(codigoPersona)
                .Select(MapInscripcionConfirmada);
            var pendientes = ConstruirInscripcionesWorkflow(
                uow,
                uow.InstanciaWorkflows.GetInscripcionesPendientes(codigoPersona),
                EstadoPendiente);
            var canceladas = ConstruirInscripcionesWorkflow(
                uow,
                uow.InstanciaWorkflows.GetInscripcionesCanceladas(codigoPersona),
                EstadoCancelada);

            var dtos = confirmadas
                .Concat(pendientes)
                .Concat(canceladas)
                .ToList();

            return OperationResult<IEnumerable<DtoInscripcionHome>>.Ok(dtos, nameof(ObtenerMisInscripciones));
        }

        public OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.Inscriptos.TieneInscripcionAdmisiones(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionAdmisiones));
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

                    interesProductoActual.IdGradoInteresAnt = interesProductoActual.IdGradoInteres;
                    interesProductoActual.IdGradoInteres = Constantes.kGRADO_INTERES_DESINTERESADO;
                    interesProductoActual.UsuarioModifInteresProd = InscripcionesConstants.InteresProducto.UsuarioAdmisiones;
                    interesProductoActual.FechaModifInteresProd = fechaActual;
                    interesProductoActual.IdgradoantModifInteresProd = interesProductoActual.IdGradoInteresAnt;
                    uow.InteresProductos.Update(interesProductoActual);
                }
            }
        }

        private Intere CrearInteres(IUnitOfWork uow, long codigoPersona, long idProceso, DateTime fechaActual)
        {
            var interes = new Intere
            {
                IdInteres = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES),
                CodigoPersona = codigoPersona,
                IdProceso = idProceso,
                IdFormaContacto = InscripcionesConstants.InteresProducto.FormaContactoWeb,
                ContactadorInteres = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                ObservacionesInteres = InscripcionesConstants.InteresProducto.ObservacionesWeb,
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaIngreso = fechaActual,
                HoraIngreso = fechaActual.ToString(InscripcionesConstants.InteresProducto.FormatoHora),
                IdLugar = InscripcionesConstants.InteresProducto.LugarInteresWeb,
                IdGradoPureza = InscripcionesConstants.InteresProducto.GradoPurezaPuro
            };

            uow.Interes.Add(interes);
            return interes;
        }

        private static void ActivarInteresProducto(IUnitOfWork uow, Intere interes, long idProducto, DateTime fechaActual)
        {
            var interesProductoExistente = interes.InteresProductos.FirstOrDefault(ip => ip.IdProducto == idProducto);
            if (interesProductoExistente == null)
            {
                uow.InteresProductos.Add(new InteresProducto
                {
                    IdInteres = interes.IdInteres,
                    IdProducto = idProducto,
                    IdTipoInteres = Constantes.KTIPO_INTERES_COMUN,
                    IdGradoInteres = Constantes.kGRADO_INTERES_ALTO,
                    IdGradoInteresAnt = Constantes.kGRADO_INTERES_DESINTERESADO,
                    FechaInteresProd = fechaActual,
                    FechaAltaInteresProd = fechaActual,
                    FechaIngreso = fechaActual,
                    HoraIngreso = fechaActual.ToString(InscripcionesConstants.InteresProducto.FormatoHora),
                    UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                    ObservacionesInteresProd = InscripcionesConstants.InteresProducto.ObservacionesWeb
                });
                return;
            }

            var interesProductoActual = uow.InteresProductos.GetByKey(interes.IdInteres, idProducto);
            if (interesProductoActual == null)
            {
                return;
            }

            interesProductoActual.IdGradoInteresAnt = interesProductoActual.IdGradoInteres;
            interesProductoActual.IdGradoInteres = Constantes.kGRADO_INTERES_ALTO;
            interesProductoActual.FechaInteresProd = fechaActual;
            interesProductoActual.UsuarioModifInteresProd = InscripcionesConstants.InteresProducto.UsuarioAdmisiones;
            interesProductoActual.FechaModifInteresProd = fechaActual;
            interesProductoActual.IdgradoantModifInteresProd = interesProductoActual.IdGradoInteresAnt;
            uow.InteresProductos.Update(interesProductoActual);
        }

        private static void AsegurarPersonaAdmite(IUnitOfWork uow, long codigoPersona, DateTime fechaActual)
        {
            var personaAdmite = uow.PersonaAdmites.GetByKey(codigoPersona);
            if (personaAdmite == null)
            {
                uow.PersonaAdmites.Add(new PersonaAdmite
                {
                    CodigoPersona = codigoPersona,
                    FechaFrescoPersonaAdmite = fechaActual
                });
                return;
            }

            if (!personaAdmite.FechaFrescoPersonaAdmite.HasValue)
            {
                personaAdmite.FechaFrescoPersonaAdmite = fechaActual;
                uow.PersonaAdmites.Update(personaAdmite);
            }
        }

        private static void ActualizarEncuestaInicial(IUnitOfWork uow, long codigoPersona, long idProducto, long idProceso)
        {
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
            {
                return;
            }

            var idComienzo = uow.ProcesoComienzos.GetComienzoActivoPorProcesoOProducto(idProducto, idProceso);
            if (!idComienzo.HasValue || idComienzo.Value == 0)
            {
                return;
            }

            encuesta.IdProceso = idProceso;
            encuesta.IdComienzo = idComienzo.Value;
        }

        private void RegistrarActividadInteres(IUnitOfWork uow, long codigoPersona, long idProceso, DateTime fechaActual)
        {
            if (uow.Accions.ExisteAccionParaProcesoPersona(codigoPersona, idProceso))
            {
                return;
            }

            var actividadId = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_3100);
            uow.Actividads.Add(new Actividad
            {
                IdActividad = actividadId,
                IdFormaContacto = InscripcionesConstants.InteresProducto.FormaContactoWeb,
                IdTipoActividad = InscripcionesConstants.InteresProducto.TipoActividadMonoAccion,
                IdTipoAccion = InscripcionesConstants.InteresProducto.TipoAccionRegistroSitioAdmisiones,
                IdEstadoAccion = InscripcionesConstants.InteresProducto.EstadoAccionRealizada,
                UsernameGeneradorActividad = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaGeneradorActividad = fechaActual,
                UsernameRealizadoActividad = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaRealizadoActividad = fechaActual,
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaIngreso = fechaActual,
                HoraIngreso = fechaActual.ToString(InscripcionesConstants.InteresProducto.FormatoHora),
                IdProceso = idProceso
            });

            uow.Accions.Add(new Accion
            {
                IdAccion = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_3100),
                IdActividad = actividadId,
                CodigoPersona = codigoPersona,
                FechaRealizadoAccion = fechaActual,
                UsuarioRealizadoAccion = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaIngreso = fechaActual,
                HoraIngreso = fechaActual.ToString(InscripcionesConstants.InteresProducto.FormatoHora),
                IdEstadoAccion = InscripcionesConstants.InteresProducto.EstadoAccionRealizada
            });
        }

        private static IEnumerable<DtoInstanciaWorkflowDevart> MapearInstanciasWorkflowConInscripcion(
            IUnitOfWork uow,
            IEnumerable<InstanciaWorkflow> instancias)
        {
            var instanciasList = instancias.ToList();
            var ids = instanciasList.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesPorInstanciaId = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(ids)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            return instanciasList.Select(iw =>
            {
                var dto = iw.ToDto();
                if (inscripcionesPorInstanciaId.TryGetValue(iw.IdInstanciaWorkflow, out var inscripcion))
                {
                    dto.InstWorkflowInscripcion = inscripcion.ToDto();
                }

                return dto;
            });
        }

        private static DtoInscripcionHome MapInscripcionConfirmada(Inscripto inscripto)
        {
            return new DtoInscripcionHome
            {
                Estado = EstadoConfirmada,
                IdProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.IdProducto ?? 0,
                NombreProducto = inscripto.Oferta?.Supraoferta?.Paquete?.Producto?.NombreExtensoProducto,
                IdComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.IdComienzo ?? 0,
                NombreComienzo = inscripto.Oferta?.Supraoferta?.Comienzo?.NombreComienzo,
                IdTurno = inscripto.Oferta?.Turno?.IdTurno ?? 0,
                NombreTurno = inscripto.Oferta?.Turno?.NombreTurno
            };
        }

        private static IEnumerable<DtoInscripcionHome> ConstruirInscripcionesWorkflow(
            IUnitOfWork uow,
            IEnumerable<InstanciaWorkflow> instancias,
            string estado)
        {
            var instanciasList = instancias.ToList();
            if (instanciasList.Count == 0)
            {
                return [];
            }

            var instanciaIds = instanciasList.Select(iw => iw.IdInstanciaWorkflow).ToList();
            var inscripcionesPorInstanciaId = uow.InstWorkflowInscripcions
                .GetByInstanciaIds(instanciaIds)
                .ToDictionary(iwi => iwi.IdInstanciaWorkflow);

            var productoIds = inscripcionesPorInstanciaId.Values
                .Where(iwi => iwi.IdProducto.HasValue)
                .Select(iwi => (long)iwi.IdProducto!.Value)
                .Distinct()
                .ToList();
            var productosPorId = uow.Productos
                .GetByKeys(productoIds)
                .ToDictionary(p => p.IdProducto);

            var comienzoIds = inscripcionesPorInstanciaId.Values
                .Where(iwi => iwi.IdComienzo.HasValue)
                .Select(iwi => (long)iwi.IdComienzo!.Value)
                .Distinct()
                .ToList();
            var comienzosPorId = uow.Comienzos
                .GetByKeys(comienzoIds)
                .ToDictionary(c => c.IdComienzo);

            var turnoIds = inscripcionesPorInstanciaId.Values
                .Where(iwi => iwi.IdTurno.HasValue)
                .Select(iwi => (long)iwi.IdTurno!.Value)
                .Distinct()
                .ToList();
            var turnosPorId = uow.Turnos
                .GetByKeys(turnoIds)
                .ToDictionary(t => t.IdTurno);

            return instanciasList.Select(iw =>
            {
                inscripcionesPorInstanciaId.TryGetValue(iw.IdInstanciaWorkflow, out var inscripcion);

                var idProducto = inscripcion?.IdProducto.HasValue == true
                    ? (long)inscripcion.IdProducto.Value
                    : 0;
                var idComienzo = inscripcion?.IdComienzo.HasValue == true
                    ? (long)inscripcion.IdComienzo.Value
                    : 0;
                var idTurno = inscripcion?.IdTurno.HasValue == true
                    ? (long)inscripcion.IdTurno.Value
                    : 0;

                productosPorId.TryGetValue(idProducto, out var producto);
                comienzosPorId.TryGetValue(idComienzo, out var comienzo);
                turnosPorId.TryGetValue(idTurno, out var turno);

                return new DtoInscripcionHome
                {
                    Estado = estado,
                    IdProducto = idProducto,
                    NombreProducto = producto?.NombreExtensoProducto,
                    IdComienzo = idComienzo,
                    NombreComienzo = comienzo?.NombreComienzo,
                    IdTurno = idTurno,
                    NombreTurno = turno?.NombreTurno
                };
            });
        }

        private static DtoProductoAdmisiones MapProductoAdmisiones(BusinessLogic.Entities.Producto p)
        {
            var proceso = p.ProcesoProductos?.FirstOrDefault()?.Proceso;
            return new DtoProductoAdmisiones
            {
                IdProducto = p.IdProducto,
                NombreProducto = p.NombreProducto,
                NombreExtensoProducto = p.NombreExtensoProducto,
                IdNivelProducto = p.IdNivelProducto,
                NombreNivelProducto = p.NivelProducto?.NombreNivelProducto,
                AliasProducto = p.AliasProducto,
                InscribibleProducto = p.InscribibleProducto,
                IntermedioProducto = p.IntermedioProducto,
                VisibleAdmisionesProducto = p.VisibleAdmisionesProducto,
                IdProceso = proceso?.IdProceso ?? 0,
                NombreProceso = proceso?.NombreProceso,
            };
        }

        private static DtoProductoAdmisiones MapProductoConInteresActivo(BusinessLogic.Entities.Producto p, Proceso? procesoInteres)
        {
            var dto = MapProductoAdmisiones(p);
            dto.IdProceso = procesoInteres?.IdProceso ?? 0;
            dto.NombreProceso = procesoInteres?.NombreProceso;
            return dto;
        }
    }
}
