using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.IServices;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Utilities;

namespace AppLogic.Services
{
    public class RegistroService : IRegistroService
    {
        private readonly ICatalogosService _catalogosService;
        private readonly IPreinscripcionService _preinscripcionService;
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly ILdap _ldap;
        private readonly IRecaptchaService _recaptchaService;

        public RegistroService(
            ICatalogosService catalogosService,
            IPreinscripcionService preinscripcionService,
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            ILdap ldap,
            IRecaptchaService recaptchaService)
        {
            _catalogosService = catalogosService;
            _preinscripcionService = preinscripcionService;
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _ldap = ldap;
            _recaptchaService = recaptchaService;
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

            var validacion = RegistroValidationHelper.ValidarDocumentoBase(request.TipoDocumento, request.Documento, nameof(EvaluarDocumentoAsync));
            if (!validacion.Success)
            {
                return OperationResult<RegistroEvaluacionResponse>.IsFailed(
                    validacion.ErrorCode,
                    nameof(EvaluarDocumentoAsync),
                    validacion.Message,
                    validacion.HttpCode);
            }

            var tipoDocumento = RegistroNormalizationHelper.Normalizar(request.TipoDocumento);
            var documento = RegistroNormalizationHelper.Normalizar(request.Documento);

            if (tipoDocumento != "CI")
            {
                return OperationResult<RegistroEvaluacionResponse>.IsFailed(
                    "REG_DOC_04",
                    nameof(EvaluarDocumentoAsync),
                    "EvaluarDocumento solo aplica para cédula de identidad.",
                    400);
            }

            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetByDocumento(documento);
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

            var existeUsuario = await _ldap.ExisteUsuarioLDAP(persona.CodigoPersona.ToString(CultureInfo.InvariantCulture));
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

        public async Task<OperationResult<object?>> ConfirmarRegistroAsync(RegistroConfirmarRequest request)
        {
            if (request == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(ConfirmarRegistroAsync),
                    "La solicitud es obligatoria.",
                    400);
            }

            var captcha = await _recaptchaService.ValidarAsync(request.CaptchaToken);
            if (!captcha.Success)
            {
                return OperationResult<object?>.IsFailed(
                    captcha.ErrorCode,
                    nameof(ConfirmarRegistroAsync),
                    captcha.Message,
                    captcha.HttpCode);
            }

            var documentoValidation = RegistroValidationHelper.ValidarDocumentoBase(request.TipoDocumento, request.Documento, nameof(ConfirmarRegistroAsync));
            if (!documentoValidation.Success)
            {
                return OperationResult<object?>.IsFailed(
                    documentoValidation.ErrorCode,
                    nameof(ConfirmarRegistroAsync),
                    documentoValidation.Message,
                    documentoValidation.HttpCode);
            }

            var commonValidation = RegistroValidationHelper.ValidarProductoYProceso(_uowFactory, request, nameof(ConfirmarRegistroAsync));
            if (!commonValidation.Success)
            {
                return commonValidation;
            }

            var tipoDocumento = RegistroNormalizationHelper.Normalizar(request.TipoDocumento);
            var documento = RegistroNormalizationHelper.Normalizar(request.Documento);

            using var uow = _uowFactory.Create();
            var persona = tipoDocumento == "CI"
                ? uow.Personas.GetByDocumento(documento)
                : null;

            if (tipoDocumento != "CI")
            {
                return await CrearSolicitudAltaAsync(uow, request);
            }

            if (persona == null)
            {
                return await CrearPersonaInteresAsync(uow, request);
            }

            var existeUsuario = await _ldap.ExisteUsuarioLDAP(persona.CodigoPersona.ToString(CultureInfo.InvariantCulture));
            if (existeUsuario)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_USUARIO_EXISTENTE",
                    nameof(ConfirmarRegistroAsync),
                    "Ya estás registrado. Para acceder, ingresá con tu número de usuario y tu contraseña.",
                    409);
            }

            var verificacion = RegistroValidationHelper.ValidarVerificacionPersonaExistente(persona, request, nameof(ConfirmarRegistroAsync));
            if (!verificacion.Success)
            {
                return verificacion;
            }

            return await RegistrarInteresYUsuarioAsync(uow, persona, request);
        }

        public OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos()
        {
            return _catalogosService.ObtenerTipoDocumentos();
        }

        public OperationResult<IEnumerable<RegistroComienzoResponse>> ObtenerComienzos(long idCarrera)
        {
            var result = _preinscripcionService.ObtenerProcesosHabilitadosPorProducto(idCarrera);
            if (!result.Success)
            {
                return OperationResult<IEnumerable<RegistroComienzoResponse>>.IsFailed(
                    result.ErrorCode,
                    nameof(ObtenerComienzos),
                    result.Message,
                    result.HttpCode);
            }

            var comienzos = result.Data?.Select(proceso => new RegistroComienzoResponse
            {
                IdProceso = proceso.IdProceso,
                NombreProceso = proceso.NombreProceso
            });

            return OperationResult<IEnumerable<RegistroComienzoResponse>>.Ok(comienzos, nameof(ObtenerComienzos));
        }

        public OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaisesEstadosCiudades()
        {
            using var uow = _uowFactory.Create();

            var paises = uow.Paises.GetPaisesConEstadosYCiudades().ToList();

            return OperationResult<IEnumerable<DtoPaisDevart>>.Ok(paises.ToDtosWithRelated(2), nameof(ObtenerPaisesEstadosCiudades));
        }

        public OperationResult<IEnumerable<RegistroCarreraResponse>> ObtenerCarreras()
        {
            using var uow = _uowFactory.Create();

            var entidades = uow.Productos.GetProductosVigentesParaRegistro();
            var dtos = entidades.Select(MapCarrera);

            return OperationResult<IEnumerable<RegistroCarreraResponse>>.Ok(
                dtos,
                nameof(ObtenerCarreras));
        }

        private static RegistroCarreraResponse MapCarrera(Producto producto)
        {
            return new RegistroCarreraResponse
            {
                IdProducto = producto.IdProducto,
                NombreProducto = producto.NombreProducto,
                IdNivelProducto = producto.IdNivelProducto,
                NombreNivelProducto = producto.NivelProducto?.NombreNivelProducto
            };
        }

        private async Task<OperationResult<object?>> RegistrarInteresYUsuarioAsync(
            IUnitOfWork uow,
            Persona persona,
            RegistroConfirmarRequest request)
        {
            var inscripcion = uow.Inscriptos.GetUltimaInscripcionActiva(persona.CodigoPersona);
            if (inscripcion != null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_INSCRIPCION_ACTIVA",
                    nameof(ConfirmarRegistroAsync),
                    "Ya estás inscripto a un producto activo.",
                    409);
            }

            try
            {
                uow.BeginTransaction();
                UpsertInteres(uow, persona.CodigoPersona, request.IdProducto, request.IdProceso);
                AsegurarPersonaAdmite(uow, persona.CodigoPersona);
                RegistrarActividadYAccion(uow, persona.CodigoPersona, request.IdProceso);

                var crearUsuario = await _ldap.CrearUsuarioAsync(RegistroEntityFactoryHelper.CrearUsuarioLdapRequest(persona));
                if (!crearUsuario.Success)
                {
                    uow.Rollback();
                    return OperationResult<object?>.IsFailed(
                        crearUsuario.ErrorCode,
                        nameof(ConfirmarRegistroAsync),
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
                    nameof(ConfirmarRegistroAsync),
                    $"Error al registrar el interés: {ex.Message}",
                    500);
            }

            return await EnviarContraseniaAsync(persona);
        }

        private async Task<OperationResult<object?>> CrearPersonaInteresAsync(
            IUnitOfWork uow,
            RegistroConfirmarRequest request)
        {
            var validacion = RegistroValidationHelper.ValidarDatosPersonaCompleta(
                request,
                requiereDireccionYCiudad: true,
                nameof(ConfirmarRegistroAsync));
            if (!validacion.Success)
            {
                return validacion;
            }

            var ciudad = uow.Ciudads.GetByKey(request.CodigoPais, request.CodigoEstado, request.CodigoCiudad);
            if (ciudad == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REG_CIUDAD_01",
                    nameof(ConfirmarRegistroAsync),
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

                var crearUsuario = await _ldap.CrearUsuarioAsync(RegistroEntityFactoryHelper.CrearUsuarioLdapRequest(persona));
                if (!crearUsuario.Success)
                {
                    uow.Rollback();
                    return OperationResult<object?>.IsFailed(
                        crearUsuario.ErrorCode,
                        nameof(ConfirmarRegistroAsync),
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
                    nameof(ConfirmarRegistroAsync),
                    $"Error al crear la persona: {ex.Message}",
                    500);
            }

            return await EnviarContraseniaAsync(persona);
        }

        private async Task<OperationResult<object?>> CrearSolicitudAltaAsync(
            IUnitOfWork uow,
            RegistroConfirmarRequest request)
        {
            var validacion = RegistroValidationHelper.ValidarDatosPersonaCompleta(
                request,
                requiereDireccionYCiudad: false,
                nameof(ConfirmarRegistroAsync));
            if (!validacion.Success)
            {
                return validacion;
            }

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
                    nameof(ConfirmarRegistroAsync),
                    "La solicitud de alta quedó registrada.");
            }
            catch (Exception ex)
            {
                uow.Rollback();
                return OperationResult<object?>.IsFailed(
                    "REG_SOLICITUD_99",
                    nameof(ConfirmarRegistroAsync),
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

        private async Task<OperationResult<object?>> EnviarContraseniaAsync(Persona persona)
        {
            var mail = await _ldap.EnviarContrasenia(
                persona.CodigoPersona.ToString(CultureInfo.InvariantCulture),
                persona.TipoDocumento ?? string.Empty,
                persona.Documento ?? string.Empty,
                persona.PrimerApellido,
                "ADMISIONES",
                "REGISTRO");

            if (!mail.Success)
            {
                return OperationResult<object?>.IsSuccess(
                    null,
                    nameof(ConfirmarRegistroAsync),
                    "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de usuario o contraseña.");
            }

            return OperationResult<object?>.IsSuccess(
                null,
                nameof(ConfirmarRegistroAsync),
                "Registro realizado correctamente.");
        }

    }
}
