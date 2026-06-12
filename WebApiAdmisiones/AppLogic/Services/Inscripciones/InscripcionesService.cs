using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.IServices.Inscripciones;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Services.Inscripciones
{
    public class InscripcionesService : IInscripcionesService
    {
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
        public OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.VdEsFrescoAdmisions.TieneInscripcionActivaParaProceso(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionActivaParaProceso));
        }

        public OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var tiene = uow.Inscriptos.TieneInscripcionAdmisiones(codigoPersona, idProducto, idProceso);
            return OperationResult<bool>.Ok(tiene, nameof(TieneInscripcionAdmisiones));
        }

        #region PASO 1 - REGISTRAR INTERES POR PRODUCTO
        public OperationResult<bool> RegistrarInteresProducto(long codigoPersona, InteresProductoRequest request)
        {
            using var uow = _uowFactory.Create();

            var validacion = InteresProductoValidationHelper.ValidarRegistroInteresProducto(
                uow,
                codigoPersona,
                request.IdProducto,
                request.IdProcesoSeleccionado,
                nameof(RegistrarInteresProducto));

            if (!validacion.Success)
            {
                return validacion;
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
                    interesExistente = CrearInteres(uow, codigoPersona, request.IdProcesoSeleccionado);
                }

                ActivarInteresProducto(uow, interesExistente, request.IdProducto, fechaActual);
                // TODO Tivenos: encolar AltaInteresXSeleccionEnSitio para el interes producto registrado.
                AsegurarPersonaAdmite(uow, codigoPersona, fechaActual);
                uow.InteresProductoOfertas.Add(
                    InteresProductoEntityFactoryHelper.CrearInteresProductoOferta((long)interesExistente.IdInteres, request.IdProducto, request.IdOferta));
                var resultadoEncuesta = ActualizarEncuestaInicial(uow, codigoPersona, request.IdProducto, request.IdProcesoSeleccionado);
                if (!resultadoEncuesta.Success)
                {
                    uow.Rollback();
                    return resultadoEncuesta;
                }

                uow.Commit();

                return OperationResult<bool>.Ok(true, nameof(RegistrarInteresProducto));
            }
            catch
            {
                uow.Rollback();
                throw;
            }
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

        private Intere CrearInteres(IUnitOfWork uow, long codigoPersona, long idProceso)
        {
            var interes = InteresProductoEntityFactoryHelper.CrearInteres(
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES),
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
        private static OperationResult<bool> ActualizarEncuestaInicial(IUnitOfWork uow, long codigoPersona, long idProducto, long idProceso)
        {
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
            {
                return OperationResult<bool>.Ok(true, nameof(RegistrarInteresProducto));
            }

            var idComienzo = uow.ProcesoComienzos.GetComienzoActivoPorProcesoOProducto(idProducto, idProceso);
            if (!idComienzo.HasValue || idComienzo.Value == 0)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_IP_06",
                    nameof(RegistrarInteresProducto),
                    $"No existe comienzo activo, producto:{idProducto} proceso:{idProceso}.",
                    400);
            }

            encuesta.IdProceso = idProceso;
            encuesta.IdComienzo = idComienzo.Value;
            return OperationResult<bool>.Ok(true, nameof(RegistrarInteresProducto));
        }

        #endregion PASO 1 - REGISTRAR INTERES POR PRODUCTO

        #region PASO 2 - ENCUESTA INICIAL y PREINSCRIPCIÓN

        public OperationResult<DtoEncuestaInicialAdmisionResponse> ObtenerEncuestaInicial(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
                return OperationResult<DtoEncuestaInicialAdmisionResponse>.IsFailed(
                    "GEN_OEI_01",
                    nameof(ObtenerEncuestaInicial),
                    "Persona no encontrada.",
                    404);

            var validacionDocumento = DocumentUtils.ValidarDocumentoBase(persona.TipoDocumento, persona.Documento);
            if (!validacionDocumento.IsValid)
                return OperationResult<DtoEncuestaInicialAdmisionResponse>.IsFailed(
                    "GEN_OEI_02",
                    nameof(ObtenerEncuestaInicial),
                    validacionDocumento.Message,
                    400);

            var tipoDocumento = DocumentUtils.Normalizar(persona.TipoDocumento);
            var documento = DocumentUtils.Normalizar(persona.Documento);

            if (!TieneDerechoAEncuestaInicial(tipoDocumento, documento, uow))
                return OperationResult<DtoEncuestaInicialAdmisionResponse>.IsSuccess(
                    new DtoEncuestaInicialAdmisionResponse { TieneDerechoEncuesta = false },
                    nameof(ObtenerEncuestaInicial),
                    "La persona no tiene derecho a encuesta inicial.",
                    200);

            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
                return OperationResult<DtoEncuestaInicialAdmisionResponse>.IsSuccess(
                    new DtoEncuestaInicialAdmisionResponse { TieneDerechoEncuesta = true },
                    nameof(ObtenerEncuestaInicial),
                    "La Persona ya completó la encuesta inicial.",
                    200);

            return OperationResult<DtoEncuestaInicialAdmisionResponse>.Ok(
                new DtoEncuestaInicialAdmisionResponse { TieneDerechoEncuesta = true, Encuesta = encuesta.ToDtoWithRelated(1) },
                nameof(ObtenerEncuestaInicial));
        }

        public OperationResult<DtoAceptacionReglamentoEstDevart> RegistrarAceptacionReglamentoEstudiantil(long codigoPersona)
        {
            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
            {
                return OperationResult<DtoAceptacionReglamentoEstDevart>.IsFailed(
                    "GEN_RARE_01",
                    nameof(RegistrarAceptacionReglamentoEstudiantil),
                    "Persona no encontrada.",
                    404);
            }

            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
            {
                return OperationResult<DtoAceptacionReglamentoEstDevart>.IsFailed(
                    "GEN_RARE_02",
                    nameof(RegistrarAceptacionReglamentoEstudiantil),
                    "No se encontró una encuesta inicial de admisión vigente para la persona.",
                    404);
            }

            if (!encuesta.IdProducto.HasValue || encuesta.IdProducto.Value <= 0
                || !encuesta.IdComienzo.HasValue || encuesta.IdComienzo.Value <= 0)
            {
                return OperationResult<DtoAceptacionReglamentoEstDevart>.IsFailed(
                    "GEN_RARE_03",
                    nameof(RegistrarAceptacionReglamentoEstudiantil),
                    "La encuesta inicial de admisión no contiene producto o comienzo válidos.",
                    400);
            }

            var existente = uow.AceptacionReglamentoEsts.GetByPersonaProductoComienzo(codigoPersona, encuesta.IdProducto.Value, encuesta.IdComienzo.Value);
            if (existente != null)
            {
                return OperationResult<DtoAceptacionReglamentoEstDevart>.IsFailed(
                    "GEN_RARE_04",
                    nameof(RegistrarAceptacionReglamentoEstudiantil),
                    "Ya existe una aceptación del reglamento estudiantil para la persona, producto y comienzo indicados.",
                    409);
            }

            var entidad = new AceptacionReglamentoEst
            {
                IdAceptacionReglamentoEst = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST),
                CodigoPersona = codigoPersona,
                IdProducto = encuesta.IdProducto.Value,
                IdComienzo = encuesta.IdComienzo.Value,
                IdSistema = CommonConstants.IdSistemaAdmisiones
            };

            uow.AceptacionReglamentoEsts.Add(entidad);
            uow.Save();

            return OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(
                entidad.ToDto(),
                nameof(RegistrarAceptacionReglamentoEstudiantil));
        }
        private static bool TieneDerechoAEncuestaInicial(string tipoDocumento, string documento, IUnitOfWork uow)
        {
            if (uow.VdEsFrescoAdmisions.ExistePorDocumento(tipoDocumento, documento))
                return false;

            if (uow.EncuestaInis.ExistePorDocumento(tipoDocumento, documento))
                return false;

            if (uow.EncuestaIniAdmisions.ExisteCompletaPorDocumento(tipoDocumento, documento))
                return false;

            return true;
        }

        #endregion PASO 2 - ENCUESTA INICIAL y PREINSCRIPCIÓN

        #region PASO 3 - PAGOS

        #endregion PASO 3 - PAGOS
    }
}
