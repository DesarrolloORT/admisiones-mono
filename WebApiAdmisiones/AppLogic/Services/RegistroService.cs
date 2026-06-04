using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.IServices;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Utilities;

namespace AppLogic.Services
{
    public class RegistroService : IRegistroService
    {
        private const string requestErrorCode = "REG_REQUEST_01";
        private const string requestErrorMessage = "La solicitud es obligatoria.";
        private const string documentTypeErrorCode = "REG_DOC_03";

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly ILdap _ldap;
        private readonly IPasswordActivationService? _passwordActivationService;
        private readonly IServiceScopeFactory? _serviceScopeFactory;

        public RegistroService(
            ICatalogosService catalogosService,
            IPreinscripcionService preinscripcionService,
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            ILdap ldap,
            IPasswordActivationService? passwordActivationService = null,
            IServiceScopeFactory? serviceScopeFactory = null)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _ldap = ldap;
            _passwordActivationService = passwordActivationService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<OperationResult<RegistroEvaluacionResponse>> EvaluarDocumentoAsync(RegistroEvaluarDocumentoRequest request)
        {
            if (request == null)
            {
                return OperationResult<RegistroEvaluacionResponse>.IsFailed(
                    requestErrorCode,
                    nameof(EvaluarDocumentoAsync),
                    requestErrorMessage,
                    400);
            }

            var validacion = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!validacion.IsValid)
            {
                return OperationResult<RegistroEvaluacionResponse>.IsFailed(
                    ObtenerCodigoValidacionDocumento(validacion.Error),
                    nameof(EvaluarDocumentoAsync),
                    validacion.Message,
                    400);
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            var documento = DocumentUtils.Normalizar(request.Documento);

            using var uow = _uowFactory.Create();
            if (tipoDocumento != "CI")
            {
                var solicitudAlta = uow.SolicitudAltas.GetByTipoDocumentoYDocumento(tipoDocumento, documento);
                if (solicitudAlta != null)
                {
                    return OperationResult<RegistroEvaluacionResponse>.IsSuccess(
                        new RegistroEvaluacionResponse
                        {
                            SolicitudAltaExistente = true
                        },
                        nameof(EvaluarDocumentoAsync),
                        "El documento ingresado está en revisión.");
                }

                return OperationResult<RegistroEvaluacionResponse>.IsSuccess(
                    new RegistroEvaluacionResponse
                    {
                        RequiereAltaSolicitud = true
                    },
                    nameof(EvaluarDocumentoAsync),
                    "No existe solicitud de alta para el documento indicado. Se puede continuar con la solicitud de alta.");
            }

            var persona = uow.Personas.GetByTipoDocumentoYDocumento(tipoDocumento, documento);
            if (persona == null)
            {
                return OperationResult<RegistroEvaluacionResponse>.IsSuccess(
                    new RegistroEvaluacionResponse
                    {
                        RequiereAltaPersona = true
                    },
                    nameof(EvaluarDocumentoAsync),
                    "La persona no existe. Se puede continuar con el alta.");
            }

            var existeUsuario = await ExisteUsuarioLdapAsync(persona.CodigoPersona.ToString(CultureInfo.InvariantCulture));
            if (existeUsuario)
            {
                return OperationResult<RegistroEvaluacionResponse>.IsSuccess(
                    new RegistroEvaluacionResponse
                    {
                        UsuarioExistente = true
                    },
                    nameof(EvaluarDocumentoAsync),
                    "La cedula ingresada ya está registrada.");
            }

            return OperationResult<RegistroEvaluacionResponse>.IsSuccess(
                new RegistroEvaluacionResponse
                {
                    RequiereVerificacion = true
                },
                nameof(EvaluarDocumentoAsync),
                "La persona existe y requiere verificación de apellido y correo.");
        }

        public async Task<OperationResult<object?>> VerificarIdentidadAsync(RegistroVerificarIdentidadRequest request)
        {
            if (request == null)
            {
                return OperationResult<object?>.IsFailed(
                    requestErrorCode,
                    nameof(VerificarIdentidadAsync),
                    requestErrorMessage,
                    400);
            }

            var documentoValidation = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!documentoValidation.IsValid)
            {
                return OperationResult<object?>.IsFailed(
                    ObtenerCodigoValidacionDocumento(documentoValidation.Error),
                    nameof(VerificarIdentidadAsync),
                    documentoValidation.Message,
                    400);
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            var documento = DocumentUtils.Normalizar(request.Documento);
            if (tipoDocumento != "CI")
            {
                return OperationResult<object?>.IsFailed(
                    documentTypeErrorCode,
                    nameof(VerificarIdentidadAsync),
                    "VerificarIdentidad solo aplica para cédula de identidad.",
                    400);
            }

            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetByDocumento(documento);
            if (persona == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_01",
                    nameof(VerificarIdentidadAsync),
                    "No se pudo traer la persona.",
                    404);
            }

            var existeUsuario = await ExisteUsuarioLdapAsync(persona.CodigoPersona.ToString(CultureInfo.InvariantCulture));
            if (existeUsuario)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_USUARIO_01",
                    nameof(VerificarIdentidadAsync),
                    "La cedula ingresada ya está registrada.",
                    409);
            }

            var verificacion = RegistroValidationHelper.ValidarVerificacionPersonaExistente(
                persona,
                request,
                nameof(VerificarIdentidadAsync));
            if (!verificacion.Success)
            {
                return OperationResult<object?>.IsFailed(
                    verificacion.ErrorCode,
                    nameof(VerificarIdentidadAsync),
                    verificacion.Message,
                    verificacion.HttpCode);
            }

            return OperationResult<object?>.IsSuccess(
                null,
                nameof(VerificarIdentidadAsync),
                "Verificación realizada correctamente.");
        }

        public async Task<OperationResult<object?>> ConfirmarPersonaExistenteAsync(RegistroConfirmarPersonaExistenteRequest request)
        {
            if (request == null)
            {
                return OperationResult<object?>.IsFailed(
                    requestErrorCode,
                    nameof(ConfirmarPersonaExistenteAsync),
                    requestErrorMessage,
                    400);
            }

            var documentoValidation = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!documentoValidation.IsValid)
            {
                return OperationResult<object?>.IsFailed(
                    ObtenerCodigoValidacionDocumento(documentoValidation.Error),
                    nameof(ConfirmarPersonaExistenteAsync),
                    documentoValidation.Message,
                    400);
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            if (tipoDocumento != "CI")
            {
                return OperationResult<object?>.IsFailed(
                    documentTypeErrorCode,
                    nameof(ConfirmarPersonaExistenteAsync),
                    "ConfirmarPersonaExistente solo aplica para cédula de identidad.",
                    400);
            }

            var uow = _uowFactory.Create();
            var commonValidation = RegistroValidationHelper.ValidarProductoYProceso(
                uow,
                request.IdProducto,
                request.IdProceso,
                nameof(ConfirmarPersonaExistenteAsync));
            if (!commonValidation.Success)
            {
                return commonValidation;
            }

            var documento = DocumentUtils.Normalizar(request.Documento);
            var persona = uow.Personas.GetByDocumento(documento);
            if (persona == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_01",
                    nameof(ConfirmarPersonaExistenteAsync),
                    "No se pudo traer la persona.",
                    404);
            }

            var existeUsuario = await ExisteUsuarioLdapAsync(persona.CodigoPersona.ToString(CultureInfo.InvariantCulture));
            if (existeUsuario)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_USUARIO_01",
                    nameof(ConfirmarPersonaExistenteAsync),
                    "La cedula ingresada ya está registrada.",
                    409);
            }

            return await RegistrarInteresYUsuarioAsync(
                uow,
                persona,
                request.IdProducto,
                request.IdProceso,
                nameof(ConfirmarPersonaExistenteAsync));
        }

        public async Task<OperationResult<object?>> ConfirmarNuevaPersonaAsync(RegistroPersonaRequest request)
        {
            if (request == null)
            {
                return OperationResult<object?>.IsFailed(
                    requestErrorCode,
                    nameof(ConfirmarNuevaPersonaAsync),
                    requestErrorMessage,
                    400);
            }

            var documentoValidation = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!documentoValidation.IsValid)
            {
                return OperationResult<object?>.IsFailed(
                    ObtenerCodigoValidacionDocumento(documentoValidation.Error),
                    nameof(ConfirmarNuevaPersonaAsync),
                    documentoValidation.Message,
                    400);
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            if (tipoDocumento != "CI")
            {
                return OperationResult<object?>.IsFailed(
                    documentTypeErrorCode,
                    nameof(ConfirmarNuevaPersonaAsync),
                    "ConfirmarNuevaPersona solo aplica para cédula de identidad.",
                    400);
            }

            var uow = _uowFactory.Create();
            var commonValidation = RegistroValidationHelper.ValidarProductoYProceso(
                uow,
                request.IdProducto,
                request.IdProceso,
                nameof(ConfirmarNuevaPersonaAsync));
            if (!commonValidation.Success)
            {
                return commonValidation;
            }

            var documento = DocumentUtils.Normalizar(request.Documento);
            var persona = uow.Personas.GetByDocumento(documento);
            if (persona != null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_02",
                    nameof(ConfirmarNuevaPersonaAsync),
                    "La persona ya existe.",
                    409);
            }

            return await CrearPersonaInteresAsync(uow, request);
        }

        public async Task<OperationResult<object?>> ValidarNuevaPersonaAsync(RegistroPersonaRequest request)
        {
            if (request == null)
            {
                return OperationResult<object?>.IsFailed(
                    requestErrorCode,
                    nameof(ValidarNuevaPersonaAsync),
                    requestErrorMessage,
                    400);
            }

            var documentoValidation = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!documentoValidation.IsValid)
            {
                return OperationResult<object?>.IsFailed(
                    ObtenerCodigoValidacionDocumento(documentoValidation.Error),
                    nameof(ValidarNuevaPersonaAsync),
                    documentoValidation.Message,
                    400);
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            if (tipoDocumento != "CI")
            {
                return OperationResult<object?>.IsFailed(
                    documentTypeErrorCode,
                    nameof(ValidarNuevaPersonaAsync),
                    "ConfirmarNuevaPersona solo aplica para cédula de identidad.",
                    400);
            }

            using var uow = _uowFactory.Create();
            var commonValidation = RegistroValidationHelper.ValidarProductoYProceso(
                uow,
                request.IdProducto,
                request.IdProceso,
                nameof(ValidarNuevaPersonaAsync));
            if (!commonValidation.Success)
            {
                return commonValidation;
            }

            var documento = DocumentUtils.Normalizar(request.Documento);
            var persona = uow.Personas.GetByDocumento(documento);
            if (persona != null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_02",
                    nameof(ValidarNuevaPersonaAsync),
                    "La persona ya existe.",
                    409);
            }

            return OperationResult<object?>.IsSuccess(
                null,
                nameof(ValidarNuevaPersonaAsync),
                "Validación correcta.");
        }

        public async Task<OperationResult<long>> CompletarNuevaPersonaAsync(RegistroPendingPersona data, string passwordNueva)
        {
            if (data == null)
            {
                return OperationResult<long>.IsFailed(
                    requestErrorCode,
                    nameof(CompletarNuevaPersonaAsync),
                    requestErrorMessage,
                    400,
                    default);
            }

            var validacionPassword = Util.ValidarPasswordNueva(passwordNueva);
            if (!string.IsNullOrWhiteSpace(validacionPassword))
            {
                return OperationResult<long>.IsFailed(
                    "INI_PAS_02",
                    nameof(CompletarNuevaPersonaAsync),
                    validacionPassword,
                    400,
                    default);
            }

            using var uow = _uowFactory.Create();
            var tipoDocumento = DocumentUtils.Normalizar(data.TipoDocumento);
            var documento = DocumentUtils.Normalizar(data.Documento);
            var personaExistente = uow.Personas.GetByDocumento(documento);
            if (personaExistente != null)
            {
                if (!EsMismaPersonaPendiente(personaExistente, data, tipoDocumento, documento))
                {
                    return OperationResult<long>.IsFailed(
                        "REG_PERSONA_02",
                        nameof(CompletarNuevaPersonaAsync),
                        "La persona ya existe.",
                        409,
                        default);
                }

                return await CompletarPasswordPersonaPendienteExistenteAsync(
                    uow,
                    personaExistente,
                    passwordNueva);
            }

            var ciudad = uow.Ciudads.GetByKey(data.CodigoPais, data.CodigoEstado, data.CodigoCiudad);
            if (ciudad == null)
            {
                return OperationResult<long>.IsFailed(
                    "REG_CIUDAD_01",
                    nameof(CompletarNuevaPersonaAsync),
                    "No existe la ciudad indicada.",
                    400,
                    default);
            }

            Persona persona;
            try
            {
                uow.BeginTransaction();
                persona = RegistroEntityFactoryHelper.CrearPersona(
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA),
                    data,
                    ciudad,
                    DateTime.Now);
                uow.Personas.Add(persona);
                uow.Save();
                var fechaActual = _dbConnectionContext.CurrentDateTime();
                UpsertInteres(uow, persona.CodigoPersona, data.IdProducto, data.IdProceso, fechaActual);
                AsegurarPersonaAdmite(uow, persona.CodigoPersona, fechaActual);
                RegistrarActividadYAccion(uow, persona.CodigoPersona, data.IdProceso, fechaActual);

                var crearUsuario = await CrearUsuarioLdapAsync(RegistroEntityFactoryHelper.CrearUsuarioLdapRequest(persona));
                if (!crearUsuario.Success)
                {
                    uow.Rollback();
                    return OperationResult<long>.IsFailed(
                        crearUsuario.ErrorCode,
                        nameof(CompletarNuevaPersonaAsync),
                        crearUsuario.Message,
                        crearUsuario.HttpCode,
                        default);
                }

                var cambioPassword = await CambiarPasswordLdapAsync(
                    persona.CodigoPersona.ToString(CultureInfo.InvariantCulture),
                    passwordNueva);
                if (!cambioPassword.Success)
                {
                    uow.Rollback();
                    return OperationResult<long>.IsFailed(
                        cambioPassword.ErrorCode,
                        nameof(CompletarNuevaPersonaAsync),
                        cambioPassword.Message,
                        cambioPassword.HttpCode,
                        default);
                }

                ActualizarMetadataPassword(uow, persona);
                uow.Commit();
            }
            catch (Exception ex)
            {
                uow.Rollback();
                return OperationResult<long>.IsFailed(
                    "REG_PERSONA_99",
                    nameof(CompletarNuevaPersonaAsync),
                    $"Error al crear la persona: {ex.Message}",
                    500,
                    default);
            }

            return OperationResult<long>.Ok(persona.CodigoPersona, nameof(CompletarNuevaPersonaAsync));
        }

        public async Task<OperationResult<object?>> ConfirmarSolicitudAltaAsync(RegistroPersonaRequest request)
        {
            if (request == null)
            {
                return OperationResult<object?>.IsFailed(
                    requestErrorCode,
                    nameof(ConfirmarSolicitudAltaAsync),
                    requestErrorMessage,
                    400);
            }

            var documentoValidation = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!documentoValidation.IsValid)
            {
                return OperationResult<object?>.IsFailed(
                    ObtenerCodigoValidacionDocumento(documentoValidation.Error),
                    nameof(ConfirmarSolicitudAltaAsync),
                    documentoValidation.Message,
                    400);
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            if (tipoDocumento == "CI")
            {
                return OperationResult<object?>.IsFailed(
                    documentTypeErrorCode,
                    nameof(ConfirmarSolicitudAltaAsync),
                    "ConfirmarSolicitudAlta solo aplica para documentos distintos a cédula de identidad.",
                    400);
            }

            using var uow = _uowFactory.Create();
            var commonValidation = RegistroValidationHelper.ValidarProductoYProceso(
                uow,
                request.IdProducto,
                request.IdProceso,
                nameof(ConfirmarSolicitudAltaAsync));
            if (!commonValidation.Success)
            {
                return commonValidation;
            }

            return await CrearSolicitudAltaAsync(uow, request);
        }

        private async Task<OperationResult<object?>> RegistrarInteresYUsuarioAsync(
            IUnitOfWork uow,
            Persona persona,
            long idProducto,
            long idProceso,
            string originMethod)
        {
            var inscripcion = uow.Inscriptos.GetUltimaInscripcionActiva(persona.CodigoPersona);
            if (inscripcion != null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_INSCRIPCION_01",
                    originMethod,
                    "Ya estás inscripto a un producto activo.",
                    409);
            }

            try
            {
                uow.BeginTransaction();
                var fechaActual = _dbConnectionContext.CurrentDateTime();
                UpsertInteres(uow, persona.CodigoPersona, idProducto, idProceso, fechaActual);
                AsegurarPersonaAdmite(uow, persona.CodigoPersona, fechaActual);
                RegistrarActividadYAccion(uow, persona.CodigoPersona, idProceso, fechaActual);

                var crearUsuario = await CrearUsuarioLdapAsync(RegistroEntityFactoryHelper.CrearUsuarioLdapRequest(persona));
                if (!crearUsuario.Success)
                {
                    uow.Rollback();
                    return OperationResult<object?>.IsFailed(
                        crearUsuario.ErrorCode,
                        originMethod,
                        crearUsuario.Message,
                        crearUsuario.HttpCode);
                }

                uow.Commit();
            }
            catch (Exception ex)
            {
                uow.Rollback();
                return OperationResult<object?>.IsFailed(
                    "REG_INTERES_99",
                    originMethod,
                    $"Error al registrar el interés: {ex.Message}",
                    500);
            }

            return await EnviarMailLinkPasswordAsync(persona, originMethod);
        }

        private async Task<OperationResult<object?>> CrearPersonaInteresAsync(
            IUnitOfWork uow,
            RegistroPersonaRequest request)
        {
            var ciudad = uow.Ciudads.GetByKey(request.CodigoPais, request.CodigoEstado, request.CodigoCiudad);
            if (ciudad == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_CIUDAD_01",
                    nameof(ConfirmarNuevaPersonaAsync),
                    "No existe la ciudad indicada.",
                    400);
            }

            Persona persona;

            try
            {
                uow.BeginTransaction();
                persona = RegistroEntityFactoryHelper.CrearPersona(
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA),
                    request,
                    ciudad,
                    DateTime.Now);
                uow.Personas.Add(persona);
                uow.Save();
                var fechaActual = _dbConnectionContext.CurrentDateTime();
                UpsertInteres(uow, persona.CodigoPersona, request.IdProducto, request.IdProceso, fechaActual);
                // TODO Tivenos: encolar RegistroDesdeSitioAdmisiones para la persona/interes creado.
                AsegurarPersonaAdmite(uow, persona.CodigoPersona, fechaActual);
                RegistrarActividadYAccion(uow, persona.CodigoPersona, request.IdProceso, fechaActual);

                var crearUsuario = await CrearUsuarioLdapAsync(RegistroEntityFactoryHelper.CrearUsuarioLdapRequest(persona));
                if (!crearUsuario.Success)
                {
                    uow.Rollback();
                    return OperationResult<object?>.IsFailed(
                        crearUsuario.ErrorCode,
                        nameof(ConfirmarNuevaPersonaAsync),
                        crearUsuario.Message,
                        crearUsuario.HttpCode);
                }

                uow.Commit();
            }
            catch (Exception ex)
            {
                uow.Rollback();
                return OperationResult<object?>.IsFailed(
                    "REG_PERSONA_99",
                    nameof(ConfirmarNuevaPersonaAsync),
                    $"Error al crear la persona: {ex.Message}",
                    500);
            }

            return await EnviarMailLinkPasswordAsync(persona, nameof(ConfirmarNuevaPersonaAsync));
        }

        private async Task<OperationResult<object?>> CrearSolicitudAltaAsync(
            IUnitOfWork uow,
            RegistroPersonaRequest request)
        {
            try
            {
                uow.BeginTransaction();
                var solicitud = RegistroEntityFactoryHelper.CrearSolicitudAlta(
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_SOLICITUD_ALTA),
                    request);

                uow.SolicitudAltas.Add(solicitud);
                uow.Commit();

                return OperationResult<object?>.IsSuccess(
                    null,
                    nameof(ConfirmarSolicitudAltaAsync),
                    "La solicitud de alta quedó registrada.");
            }
            catch (Exception ex)
            {
                uow.Rollback();
                return OperationResult<object?>.IsFailed(
                    "REG_SOLICITUD_99",
                    nameof(ConfirmarSolicitudAltaAsync),
                    $"Error al crear la solicitud de alta: {ex.Message}",
                    500);
            }
        }

        private void UpsertInteres(IUnitOfWork uow, long codigoPersona, long idProducto, long idProceso, DateTime fechaActual)
        {
            var intereses = uow.Interes.GetInteresesPersonaProcesosHabilitados(codigoPersona).ToList();
            var interesExistente = intereses.FirstOrDefault(i => i.IdProceso == idProceso);

            if (interesExistente == null)
            {
                var idInteres = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES);
                uow.Interes.Add(InteresProductoEntityFactoryHelper.CrearInteres(idInteres, codigoPersona, idProceso));
                AgregarInteresProducto(uow, idInteres, idProducto, fechaActual);
                return;
            }

            // TODO Tivenos: encolar DesinteresProductoxAlta para los productos que se desinteresan.
            uow.InteresProductos.ResetearGradosPorIntereses(
                intereses.Select(i => i.IdInteres),
                Constantes.kGRADO_INTERES_DESINTERESADO);

            var productoExistente = uow.InteresProductos.GetByKey(interesExistente.IdInteres, idProducto);
            if (productoExistente == null)
            {
                AgregarInteresProducto(uow, interesExistente.IdInteres, idProducto, fechaActual);
                return;
            }

            productoExistente.IdGradoInteresAnt = productoExistente.IdGradoInteres;
            productoExistente.IdGradoInteres = Constantes.kGRADO_INTERES_ALTO;
            productoExistente.UsuarioModifInteresProd = Constantes.kUSERNAME_USUARIO_ADMISIONES;
            productoExistente.FechaModifInteresProd = fechaActual;
            uow.InteresProductos.Update(productoExistente);
        }

        private static void AgregarInteresProducto(IUnitOfWork uow, decimal idInteres, long idProducto, DateTime fechaActual)
        {
            uow.InteresProductos.Add(InteresProductoEntityFactoryHelper.CrearInteresProducto(idInteres, idProducto, fechaActual));
        }

        private static void AsegurarPersonaAdmite(IUnitOfWork uow, long codigoPersona, DateTime fechaActual)
        {
            var existente = uow.PersonaAdmites.GetByKey(codigoPersona);
            if (existente == null)
            {
                uow.PersonaAdmites.Add(InteresProductoEntityFactoryHelper.CrearPersonaAdmite(codigoPersona, fechaActual));
                return;
            }

            if (!existente.FechaFrescoPersonaAdmite.HasValue)
            {
                existente.FechaFrescoPersonaAdmite = fechaActual;
                uow.PersonaAdmites.Update(existente);
            }
        }

        private void RegistrarActividadYAccion(IUnitOfWork uow, long codigoPersona, long idProceso, DateTime fechaActual)
        {
            var idActividad = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ACTIVIDAD);

            uow.Actividads.Add(InteresProductoEntityFactoryHelper.CrearActividad(idActividad, idProceso, fechaActual));
            uow.Accions.Add(InteresProductoEntityFactoryHelper.CrearAccion(
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ACCION),
                idActividad,
                codigoPersona,
                fechaActual));
        }

        private async Task<OperationResult<object?>> EnviarMailLinkPasswordAsync(Persona persona, string originMethod)
        {
            if (_passwordActivationService == null)
            {
                return OperationResult<object?>.IsSuccess(
                    null,
                    originMethod,
                    "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de contraseña.");
            }

            OperationResult<object?> mail;
            if (_serviceScopeFactory != null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var passwordActivationService = scope.ServiceProvider.GetRequiredService<IPasswordActivationService>();
                mail = await passwordActivationService.EnviarMailLinkPasswordAsync(persona, originMethod);
            }
            else
            {
                mail = await _passwordActivationService.EnviarMailLinkPasswordAsync(persona, originMethod);
            }

            if (!mail.Success)
            {
                return OperationResult<object?>.IsSuccess(
                    null,
                    originMethod,
                    "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de contraseña.");
            }

            return OperationResult<object?>.IsSuccess(
                null,
                originMethod,
                "Registro realizado correctamente. Revisá tu casilla de mail para activar tu contraseña.");
        }

        private async Task<bool> ExisteUsuarioLdapAsync(string usuario)
        {
            if (_serviceScopeFactory == null)
            {
                return await _ldap.ExisteUsuarioLDAP(usuario);
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var ldap = scope.ServiceProvider.GetRequiredService<ILdap>();
            return await ldap.ExisteUsuarioLDAP(usuario);
        }

        private static string ObtenerCodigoValidacionDocumento(DocumentUtils.DocumentValidationError error)
        {
            return error == DocumentUtils.DocumentValidationError.InvalidDocumentType
                ? "REG_DOC_01"
                : "REG_DOC_02";
        }

        private async Task<OperationResult<bool>> CrearUsuarioLdapAsync(LdapService.DTOs.ParamCrearUsuarioLdap request)
        {
            if (_serviceScopeFactory == null)
            {
                return await _ldap.CrearUsuarioAsync(request);
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var ldap = scope.ServiceProvider.GetRequiredService<ILdap>();
            return await ldap.CrearUsuarioAsync(request);
        }

        private async Task<OperationResult<bool>> CambiarPasswordLdapAsync(string codigoPersona, string passwordNueva)
        {
            if (_serviceScopeFactory == null)
            {
                return await _ldap.ForzarCambiarPasswordAsync(codigoPersona, passwordNueva);
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var ldap = scope.ServiceProvider.GetRequiredService<ILdap>();
            return await ldap.ForzarCambiarPasswordAsync(codigoPersona, passwordNueva);
        }

        private async Task<OperationResult<long>> CompletarPasswordPersonaPendienteExistenteAsync(
            IUnitOfWork uow,
            Persona persona,
            string passwordNueva)
        {
            var cambioPassword = await CambiarPasswordLdapAsync(
                persona.CodigoPersona.ToString(CultureInfo.InvariantCulture),
                passwordNueva);

            if (!cambioPassword.Success)
            {
                return OperationResult<long>.IsFailed(
                    cambioPassword.ErrorCode,
                    nameof(CompletarNuevaPersonaAsync),
                    cambioPassword.Message,
                    cambioPassword.HttpCode,
                    default);
            }

            ActualizarMetadataPassword(uow, persona);
            uow.Save();

            return OperationResult<long>.Ok(persona.CodigoPersona, nameof(CompletarNuevaPersonaAsync));
        }

        private static bool EsMismaPersonaPendiente(
            Persona persona,
            RegistroPendingPersona data,
            string tipoDocumento,
            string documento)
        {
            return DocumentUtils.Normalizar(persona.TipoDocumento) == tipoDocumento
                && DocumentUtils.Normalizar(persona.Documento) == documento
                && string.Equals(
                    DocumentUtils.Normalizar(persona.Email),
                    DocumentUtils.Normalizar(data.Email),
                    StringComparison.Ordinal);
        }

        private static void ActualizarMetadataPassword(IUnitOfWork uow, Persona persona)
        {
            persona.FechaUltModifPassword = DateTime.Today;
            persona.UsuarioUltModifPassword = "ADMISIONES";
            uow.Personas.Update(persona);
        }

    }
}
