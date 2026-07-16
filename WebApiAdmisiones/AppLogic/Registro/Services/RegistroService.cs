using AppLogic.Registro.Dtos;
using AppLogic.Registro.Rules;
using AppLogic.Registro.Interfaces;
using AppLogic.Registro.Validators;
using AppLogic.DevartDTOs;
using AppLogic.Autenticacion.Interfaces;
using AppLogic.Personas.Services;
using AppLogic.Common.Validation;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Utilities;

namespace AppLogic.Registro.Services
{
    public class RegistroService : IRegistroService
    {
        private const string requestErrorCode = "REG_REQUEST_01";
        private const string requestErrorMessage = "La solicitud es obligatoria.";
        private const string documentTypeErrorCode = "REG_DOC_03";

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly ILdap _ldap;
        private readonly IPasswordActivationService _passwordActivationService;
        private readonly IServiceScopeFactory? _serviceScopeFactory;
        private readonly ILogger<RegistroService> _logger;

        private const string ErrorInesperadoLog = "Error inesperado en {Metodo}";

        public RegistroService(
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            ILdap ldap,
            IPasswordActivationService passwordActivationService,
            ILogger<RegistroService> logger,
            IServiceScopeFactory? serviceScopeFactory = null)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _ldap = ldap;
            _passwordActivationService = passwordActivationService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task<OperationResult<DtoRegistroEvaluacionResponse>> EvaluarDocumentoAsync(DtoRegistroEvaluarDocumentoRequest request)
        {
            if (request == null)
            {
                return OperationResult<DtoRegistroEvaluacionResponse>.IsFailed(
                    requestErrorCode,
                    nameof(EvaluarDocumentoAsync),
                    requestErrorMessage,
                    400);
            }

            var validacion = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!validacion.IsValid)
            {
                return OperationResult<DtoRegistroEvaluacionResponse>.IsFailed(
                    ObtenerCodigoValidacionDocumento(validacion.Error),
                    nameof(EvaluarDocumentoAsync),
                    validacion.Message,
                    400);
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            var documento = DocumentUtils.Normalizar(request.Documento);

            using var uow = _uowFactory.Create();
            if (!DocumentUtils.EsCedula(tipoDocumento))
            {
                var solicitudAlta = uow.SolicitudAltas.GetByTipoDocumentoYDocumento(tipoDocumento, documento);
                if (solicitudAlta != null)
                {
                    return OperationResult<DtoRegistroEvaluacionResponse>.IsSuccess(
                        new DtoRegistroEvaluacionResponse
                        {
                            SolicitudAltaExistente = true
                        },
                        nameof(EvaluarDocumentoAsync),
                        "El documento ingresado está en revisión.");
                }

                return OperationResult<DtoRegistroEvaluacionResponse>.IsSuccess(
                    new DtoRegistroEvaluacionResponse
                    {
                        RequiereAltaSolicitud = true
                    },
                    nameof(EvaluarDocumentoAsync),
                    "No existe solicitud de alta para el documento indicado. Se puede continuar con la solicitud de alta.");
            }

            var persona = uow.Personas.GetByTipoDocumentoYDocumento(tipoDocumento, documento);
            if (persona == null)
            {
                return OperationResult<DtoRegistroEvaluacionResponse>.IsSuccess(
                    new DtoRegistroEvaluacionResponse
                    {
                        RequiereAltaPersona = true
                    },
                    nameof(EvaluarDocumentoAsync),
                    "La persona no existe. Se puede continuar con el alta.");
            }

            var existeUsuario = await ExisteUsuarioLdapAsync(persona.CodigoPersona.ToString(CultureInfo.InvariantCulture));
            if (existeUsuario)
            {
                return OperationResult<DtoRegistroEvaluacionResponse>.IsSuccess(
                    new DtoRegistroEvaluacionResponse
                    {
                        UsuarioExistente = true
                    },
                    nameof(EvaluarDocumentoAsync),
                    "La cedula ingresada ya está registrada.");
            }

            return OperationResult<DtoRegistroEvaluacionResponse>.IsSuccess(
                new DtoRegistroEvaluacionResponse
                {
                    RequiereVerificacion = true
                },
                nameof(EvaluarDocumentoAsync),
                "La persona existe y requiere verificación de apellido y correo.");
        }

        public async Task<OperationResult<DtoRegistroConfirmacionResponse?>> VerificarIdentidadAsync(DtoRegistroVerificarIdentidadRequest request)
        {
            if (request == null)
            {
                return OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    requestErrorCode,
                    nameof(VerificarIdentidadAsync),
                    requestErrorMessage,
                    400);
            }

            var documentoValidation = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!documentoValidation.IsValid)
            {
                return OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    ObtenerCodigoValidacionDocumento(documentoValidation.Error),
                    nameof(VerificarIdentidadAsync),
                    documentoValidation.Message,
                    400);
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            var documento = DocumentUtils.Normalizar(request.Documento);
            if (!DocumentUtils.EsCedula(tipoDocumento))
            {
                return OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    documentTypeErrorCode,
                    nameof(VerificarIdentidadAsync),
                    "VerificarIdentidad solo aplica para cédula de identidad.",
                    400);
            }

            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetByDocumento(documento);
            if (persona == null)
            {
                return OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    "REG_PERSONA_01",
                    nameof(VerificarIdentidadAsync),
                    "No se pudo traer la persona.",
                    404);
            }

            var existeUsuario = await ExisteUsuarioLdapAsync(persona.CodigoPersona.ToString(CultureInfo.InvariantCulture));
            if (existeUsuario)
            {
                return OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    "REG_USUARIO_01",
                    nameof(VerificarIdentidadAsync),
                    "La cedula ingresada ya está registrada.",
                    409);
            }

            var verificacion = RegistroValidation.ValidarVerificacionPersonaExistente(
                persona,
                request,
                nameof(VerificarIdentidadAsync));
            if (!verificacion.Success)
            {
                return OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    verificacion.ErrorCode,
                    nameof(VerificarIdentidadAsync),
                    verificacion.Message,
                    verificacion.HttpCode);
            }

            return await CrearUsuarioRegistrarAdmisionYEnviarMailLinkPasswordAsync(
                uow,
                persona,
                nameof(VerificarIdentidadAsync));
        }

        public Task<OperationResult<object?>> ValidarNuevaPersonaAsync(DtoRegistroPersonaRequest request)
        {
            if (request == null)
            {
                return Task.FromResult(OperationResult<object?>.IsFailed(
                    requestErrorCode,
                    nameof(ValidarNuevaPersonaAsync),
                    requestErrorMessage,
                    400));
            }

            var documentoValidation = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!documentoValidation.IsValid)
            {
                return Task.FromResult(OperationResult<object?>.IsFailed(
                    ObtenerCodigoValidacionDocumento(documentoValidation.Error),
                    nameof(ValidarNuevaPersonaAsync),
                    documentoValidation.Message,
                    400));
            }

            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            if (!DocumentUtils.EsCedula(tipoDocumento))
            {
                return Task.FromResult(OperationResult<object?>.IsFailed(
                    documentTypeErrorCode,
                    nameof(ValidarNuevaPersonaAsync),
                    "ConfirmarNuevaPersona solo aplica para cédula de identidad.",
                    400));
            }

            using var uow = _uowFactory.Create();

            var documento = DocumentUtils.Normalizar(request.Documento);
            var persona = uow.Personas.GetByDocumento(documento);
            if (persona != null)
            {
                return Task.FromResult(OperationResult<object?>.IsFailed(
                    "REG_PERSONA_02",
                    nameof(ValidarNuevaPersonaAsync),
                    "La persona ya existe.",
                    409));
            }

            var ciudad = uow.Ciudads.GetByKey(request.CodigoPais, request.CodigoEstado, request.CodigoCiudad);
            if (ciudad == null)
            {
                return Task.FromResult(OperationResult<object?>.IsFailed(
                    "REG_CIUDAD_01",
                    nameof(ValidarNuevaPersonaAsync),
                    "No existe la ciudad indicada.",
                    400));
            }

            return Task.FromResult(OperationResult<object?>.IsSuccess(
                null,
                nameof(ValidarNuevaPersonaAsync),
                "Validación correcta."));
        }

        public async Task<OperationResult<long>> CompletarNuevaPersonaAsync(
            DtoRegistroPendingPersona data,
            string passwordNueva,
            DtoRegistroDocumentoImagenesTemporales? imagenes = null)
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

            var imagenesValidation = DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido(
                imagenes,
                nameof(CompletarNuevaPersonaAsync));
            if (!imagenesValidation.Success)
            {
                return OperationResult<long>.IsFailed(
                    imagenesValidation.ErrorCode,
                    nameof(CompletarNuevaPersonaAsync),
                    imagenesValidation.Message,
                    imagenesValidation.HttpCode,
                    default);
            }

            Persona persona;
            try
            {
                uow.BeginTransaction();
                persona = RegistroEntityFactory.CrearPersona(
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA),
                    data,
                    ciudad,
                    DateTime.Now);
                uow.Personas.Add(persona);
                uow.Save();
                uow.Commit();
            }
            catch (Exception ex)
            {
                uow.Rollback();
                _logger.LogError(ex, ErrorInesperadoLog, nameof(CompletarNuevaPersonaAsync));
                return OperationResult<long>.IsFailed(
                    "REG_PERSONA_99",
                    nameof(CompletarNuevaPersonaAsync),
                    "Error al crear la persona.",
                    500,
                    default);
            }

            // LDAP fuera de la transacción DB (SRV-01): la persona ya está commiteada, así que un
            // fallo acá no la revierte. Si queda huérfana (sin usuario LDAP o sin password),
            // el reintento la encuentra vía GetByDocumento y sigue por CompletarPasswordPersonaPendienteExistenteAsync.
            var crearUsuario = await CrearUsuarioLdapAsync(RegistroEntityFactory.CrearUsuarioLdapRequest(persona));
            if (!crearUsuario.Success)
            {
                _logger.LogError(
                    "Estado inconsistente: persona {CodigoPersona} creada en DB pero sin usuario LDAP ({ErrorCode}).",
                    persona.CodigoPersona,
                    crearUsuario.ErrorCode);
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
                _logger.LogError(
                    "Estado inconsistente: persona {CodigoPersona} con usuario LDAP creado pero sin password establecida ({ErrorCode}).",
                    persona.CodigoPersona,
                    cambioPassword.ErrorCode);
                return OperationResult<long>.IsFailed(
                    cambioPassword.ErrorCode,
                    nameof(CompletarNuevaPersonaAsync),
                    cambioPassword.Message,
                    cambioPassword.HttpCode,
                    default);
            }

            try
            {
                uow.BeginTransaction();
                ActualizarMetadataPassword(uow, persona);
                RegistrarAdmisionPorPersona(uow, persona.CodigoPersona);
                GuardarImagenesDocumentoReconocido(uow, persona, imagenes);
                uow.Commit();
            }
            catch (Exception ex)
            {
                uow.Rollback();
                _logger.LogError(ex,
                    "Estado inconsistente: persona {CodigoPersona} con LDAP activo pero metadata/admisión/imágenes sin persistir.",
                    persona.CodigoPersona);
                return OperationResult<long>.IsFailed(
                    "REG_PERSONA_99",
                    nameof(CompletarNuevaPersonaAsync),
                    "Error al crear la persona.",
                    500,
                    default);
            }

            return OperationResult<long>.Ok(persona.CodigoPersona, nameof(CompletarNuevaPersonaAsync));
        }

        public async Task<OperationResult<object?>> ConfirmarSolicitudAltaAsync(DtoRegistroPersonaRequest request)
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
            if (DocumentUtils.EsCedula(tipoDocumento))
            {
                return OperationResult<object?>.IsFailed(
                    documentTypeErrorCode,
                    nameof(ConfirmarSolicitudAltaAsync),
                    "ConfirmarSolicitudAlta solo aplica para documentos distintos a cédula de identidad.",
                    400);
            }

            using var uow = _uowFactory.Create();

            return await CrearSolicitudAltaAsync(uow, request);
        }

        private async Task<OperationResult<DtoRegistroConfirmacionResponse?>> CrearUsuarioRegistrarAdmisionYEnviarMailLinkPasswordAsync(
            IUnitOfWork uow,
            Persona persona,
            string originMethod)
        {
            var crearUsuario = await CrearUsuarioLdapAsync(RegistroEntityFactory.CrearUsuarioLdapRequest(persona));
            if (!crearUsuario.Success)
            {
                return OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    crearUsuario.ErrorCode,
                    originMethod,
                    crearUsuario.Message,
                    crearUsuario.HttpCode);
            }

            try
            {
                uow.BeginTransaction();
                RegistrarAdmisionPorPersona(uow, persona.CodigoPersona);
                uow.Commit();
            }
            catch (Exception ex)
            {
                uow.Rollback();
                // SRV-01: el usuario LDAP ya se creó (arriba, fuera de esta tx) y no se revierte.
                _logger.LogError(ex,
                    "Estado inconsistente: persona {CodigoPersona} con usuario LDAP creado pero admisión sin registrar en {Metodo}.",
                    persona.CodigoPersona,
                    originMethod);
                return OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    "REG_ADMISIONES_99",
                    originMethod,
                    "Error al registrar la admisión.",
                    500);
            }

            return await EnviarMailLinkPasswordAsync(persona, originMethod);
        }

        private Task<OperationResult<object?>> CrearSolicitudAltaAsync(
            IUnitOfWork uow,
            DtoRegistroPersonaRequest request)
        {
            try
            {
                uow.BeginTransaction();
                var solicitud = RegistroEntityFactory.CrearSolicitudAlta(
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_SOLICITUD_ALTA),
                    request);

                uow.SolicitudAltas.Add(solicitud);
                RegistrarAdmisionPorSolicitudAlta(uow, solicitud.IdSolicitudAlta);
                uow.Commit();

                return Task.FromResult(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(ConfirmarSolicitudAltaAsync),
                    "La solicitud de alta quedó registrada."));
            }
            catch (Exception ex)
            {
                uow.Rollback();
                _logger.LogError(ex, ErrorInesperadoLog, nameof(ConfirmarSolicitudAltaAsync));
                return Task.FromResult(OperationResult<object?>.IsFailed(
                    "REG_SOLICITUD_99",
                    nameof(ConfirmarSolicitudAltaAsync),
                    "Error al crear la solicitud de alta.",
                    500));
            }
        }

        private void RegistrarAdmisionPorPersona(IUnitOfWork uow, long codigoPersona)
        {
            uow.RegistroAdmisiones.Add(RegistroEntityFactory.CrearRegistroAdmisione(
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_REGISTRO_ADMISIONES),
                codigoPersona,
                null));
        }

        private void GuardarImagenesDocumentoReconocido(
            IUnitOfWork uow,
            Persona persona,
            DtoRegistroDocumentoImagenesTemporales? imagenes)
        {
            if (imagenes is null)
            {
                return;
            }

            DocumentoIdentidadPersonaService.GuardarImagenesDocumentoReconocido(
                uow,
                _dbConnectionContext,
                persona,
                imagenes);

            uow.Personas.Update(persona);
        }

        private void RegistrarAdmisionPorSolicitudAlta(IUnitOfWork uow, long idSolicitudAlta)
        {
            uow.RegistroAdmisiones.Add(RegistroEntityFactory.CrearRegistroAdmisione(
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_REGISTRO_ADMISIONES),
                null,
                idSolicitudAlta));
        }

        private async Task<OperationResult<DtoRegistroConfirmacionResponse?>> EnviarMailLinkPasswordAsync(Persona persona, string originMethod)
        {
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
                // SRV-05: éxito parcial estructurado, no solo en el mensaje — el front decide qué mostrar.
                _logger.LogWarning(
                    "No se pudo enviar el mail de activación para la persona {CodigoPersona} en {Metodo}: {ErrorCode}.",
                    persona.CodigoPersona,
                    originMethod,
                    mail.ErrorCode);

                return OperationResult<DtoRegistroConfirmacionResponse?>.IsSuccess(
                    new DtoRegistroConfirmacionResponse { MailEnviado = false },
                    originMethod,
                    "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de contraseña.");
            }

            return OperationResult<DtoRegistroConfirmacionResponse?>.IsSuccess(
                new DtoRegistroConfirmacionResponse { MailEnviado = true },
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
            return DocumentoIdentidadPersonaService.ResolverCodigoValidacionDocumento(
                error,
                "REG_DOC_01",
                "REG_DOC_02");
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
            DtoRegistroPendingPersona data,
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
            persona.UsuarioUltModifPassword = Constantes.kUSERNAME_USUARIO_ADMISIONES;
            uow.Personas.Update(persona);
        }

    }
}
