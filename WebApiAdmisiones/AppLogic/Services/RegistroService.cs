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
                    "REG_REQUEST_01",
                    nameof(EvaluarDocumentoAsync),
                    "La solicitud es obligatoria.",
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
                        "Ya existe una solicitud de alta para el documento indicado.");
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
                    "Ya estás registrado. Para acceder, ingresá con tu número de usuario y tu contraseña.");
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
                    "REG_REQUEST_01",
                    nameof(VerificarIdentidadAsync),
                    "La solicitud es obligatoria.",
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
                    "REG_DOC_03",
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
                    "Ya estás registrado. Para acceder, ingresá con tu número de usuario y tu contraseña.",
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
                    "REG_REQUEST_01",
                    nameof(ConfirmarPersonaExistenteAsync),
                    "La solicitud es obligatoria.",
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
                    "REG_DOC_03",
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
                    "Ya estás registrado. Para acceder, ingresá con tu número de usuario y tu contraseña.",
                    409);
            }

            return await RegistrarInteresYUsuarioAsync(
                uow,
                persona,
                request.IdProducto,
                request.IdProceso,
                nameof(ConfirmarPersonaExistenteAsync));
        }

        public async Task<OperationResult<object?>> ConfirmarNuevaPersonaAsync(RegistroConfirmarNuevaPersonaRequest request)
        {
            if (request == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(ConfirmarNuevaPersonaAsync),
                    "La solicitud es obligatoria.",
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
                    "REG_DOC_03",
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

        public async Task<OperationResult<object?>> ConfirmarSolicitudAltaAsync(RegistroConfirmarSolicitudAltaRequest request)
        {
            if (request == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(ConfirmarSolicitudAltaAsync),
                    "La solicitud es obligatoria.",
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
                    "REG_DOC_03",
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
                UpsertInteres(uow, persona.CodigoPersona, idProducto, idProceso);
                AsegurarPersonaAdmite(uow, persona.CodigoPersona);
                RegistrarActividadYAccion(uow, persona.CodigoPersona, idProceso);

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
            RegistroConfirmarNuevaPersonaRequest request)
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
                UpsertInteres(uow, persona.CodigoPersona, request.IdProducto, request.IdProceso);
                // TODO Tivenos: encolar RegistroDesdeSitioAdmisiones para la persona/interes creado.
                AsegurarPersonaAdmite(uow, persona.CodigoPersona);
                RegistrarActividadYAccion(uow, persona.CodigoPersona, request.IdProceso);

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

            // Queda pendiente cambiar el body del mail para que envie una contraseña provisional o un link para crear la contraseña, en vez de la contraseña fija actual.
            return await EnviarMailLinkPasswordAsync(persona, nameof(ConfirmarNuevaPersonaAsync));
        }

        private async Task<OperationResult<object?>> CrearSolicitudAltaAsync(
            IUnitOfWork uow,
            RegistroConfirmarSolicitudAltaRequest request)
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

        private void UpsertInteres(IUnitOfWork uow, long codigoPersona, long idProducto, long idProceso)
        {
            var now = DateTime.Now;
            var intereses = uow.Interes.GetInteresesPersonaProcesosHabilitados(codigoPersona).ToList();
            var interesExistente = intereses.FirstOrDefault(i => i.IdProceso == idProceso);

            if (interesExistente == null)
            {
                var idInteres = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES);
                uow.Interes.Add(RegistroEntityFactoryHelper.CrearInteres(idInteres, codigoPersona, idProceso, now));
                AgregarInteresProducto(uow, idInteres, idProducto, now);
                return;
            }

            // TODO Tivenos: encolar DesinteresProductoxAlta para los productos que se desinteresan.
            uow.InteresProductos.ResetearGradosPorIntereses(
                intereses.Select(i => i.IdInteres),
                InscripcionesConstants.InteresProducto.GradoInteresDesinteresado);

            var productoExistente = uow.InteresProductos.GetByKey(interesExistente.IdInteres, idProducto);
            if (productoExistente == null)
            {
                AgregarInteresProducto(uow, interesExistente.IdInteres, idProducto, now);
                return;
            }

            productoExistente.IdGradoInteresAnt = productoExistente.IdGradoInteres;
            productoExistente.IdGradoInteres = InscripcionesConstants.InteresProducto.GradoInteresRegistro;
            productoExistente.UsuarioModifInteresProd = InscripcionesConstants.InteresProducto.UsuarioAdmisiones;
            productoExistente.FechaModifInteresProd = now.Date;
            uow.InteresProductos.Update(productoExistente);
        }

        private static void AgregarInteresProducto(IUnitOfWork uow, decimal idInteres, long idProducto, DateTime now)
        {
            uow.InteresProductos.Add(RegistroEntityFactoryHelper.CrearInteresProducto(idInteres, idProducto, now));
        }

        private static void AsegurarPersonaAdmite(IUnitOfWork uow, long codigoPersona)
        {
            var existente = uow.PersonaAdmites.GetByKey(codigoPersona);
            if (existente == null)
            {
                uow.PersonaAdmites.Add(RegistroEntityFactoryHelper.CrearPersonaAdmite(codigoPersona, DateTime.Now));
                return;
            }

            if (!existente.FechaFrescoPersonaAdmite.HasValue)
            {
                existente.FechaFrescoPersonaAdmite = DateTime.Today;
                uow.PersonaAdmites.Update(existente);
            }
        }

        private void RegistrarActividadYAccion(IUnitOfWork uow, long codigoPersona, long idProceso)
        {
            var now = DateTime.Now;
            var idActividad = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ACTIVIDAD);

            uow.Actividads.Add(RegistroEntityFactoryHelper.CrearActividad(idActividad, idProceso, now));
            uow.Accions.Add(RegistroEntityFactoryHelper.CrearAccion(
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ACCION),
                idActividad,
                codigoPersona,
                now));
        }

        private async Task<OperationResult<object?>> EnviarMailLinkPasswordAsync(Persona persona, string originMethod)
        {
            if (_passwordActivationService == null)
            {
                return OperationResult<object?>.IsSuccess(
                    null,
                    originMethod,
                    "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de usuario o contraseña.");
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
                    "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de usuario o contraseña.");
            }

            return OperationResult<object?>.IsSuccess(
                null,
                originMethod,
                "Registro realizado correctamente.");
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

    }
}
