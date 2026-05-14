using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.IServices;
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
        private readonly ICatalogosService _catalogosService;
        private readonly IPreinscripcionService _preinscripcionService;
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly ILdap _ldap;
        private readonly IServiceScopeFactory? _serviceScopeFactory;

        public RegistroService(
            ICatalogosService catalogosService,
            IPreinscripcionService preinscripcionService,
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            ILdap ldap,
            IServiceScopeFactory? serviceScopeFactory = null)
        {
            _catalogosService = catalogosService;
            _preinscripcionService = preinscripcionService;
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _ldap = ldap;
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
                    "REG_DOC_03",
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

            var documentoValidation = RegistroValidationHelper.ValidarDocumentoBase(request.TipoDocumento, request.Documento, nameof(VerificarIdentidadAsync));
            if (!documentoValidation.Success)
            {
                return OperationResult<object?>.IsFailed(
                    documentoValidation.ErrorCode,
                    nameof(VerificarIdentidadAsync),
                    documentoValidation.Message,
                    documentoValidation.HttpCode);
            }

            var tipoDocumento = RegistroNormalizationHelper.Normalizar(request.TipoDocumento);
            var documento = RegistroNormalizationHelper.Normalizar(request.Documento);
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

            var documentoValidation = RegistroValidationHelper.ValidarDocumentoBase(request.TipoDocumento, request.Documento, nameof(ConfirmarPersonaExistenteAsync));
            if (!documentoValidation.Success)
            {
                return OperationResult<object?>.IsFailed(
                    documentoValidation.ErrorCode,
                    nameof(ConfirmarPersonaExistenteAsync),
                    documentoValidation.Message,
                    documentoValidation.HttpCode);
            }

            var tipoDocumento = RegistroNormalizationHelper.Normalizar(request.TipoDocumento);
            if (tipoDocumento != "CI")
            {
                return OperationResult<object?>.IsFailed(
                    "REG_DOC_03",
                    nameof(ConfirmarPersonaExistenteAsync),
                    "ConfirmarPersonaExistente solo aplica para cédula de identidad.",
                    400);
            }

            using var uow = _uowFactory.Create();
            var commonValidation = RegistroValidationHelper.ValidarProductoYProceso(
                uow,
                request.IdProducto,
                request.IdProceso,
                nameof(ConfirmarPersonaExistenteAsync));
            if (!commonValidation.Success)
            {
                return commonValidation;
            }

            var documento = RegistroNormalizationHelper.Normalizar(request.Documento);
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

            var documentoValidation = RegistroValidationHelper.ValidarDocumentoBase(request.TipoDocumento, request.Documento, nameof(ConfirmarNuevaPersonaAsync));
            if (!documentoValidation.Success)
            {
                return OperationResult<object?>.IsFailed(
                    documentoValidation.ErrorCode,
                    nameof(ConfirmarNuevaPersonaAsync),
                    documentoValidation.Message,
                    documentoValidation.HttpCode);
            }

            var tipoDocumento = RegistroNormalizationHelper.Normalizar(request.TipoDocumento);
            if (tipoDocumento != "CI")
            {
                return OperationResult<object?>.IsFailed(
                    "REG_DOC_03",
                    nameof(ConfirmarNuevaPersonaAsync),
                    "ConfirmarNuevaPersona solo aplica para cédula de identidad.",
                    400);
            }

            using var uow = _uowFactory.Create();
            var commonValidation = RegistroValidationHelper.ValidarProductoYProceso(
                uow,
                request.IdProducto,
                request.IdProceso,
                nameof(ConfirmarNuevaPersonaAsync));
            if (!commonValidation.Success)
            {
                return commonValidation;
            }

            var documento = RegistroNormalizationHelper.Normalizar(request.Documento);
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

            var documentoValidation = RegistroValidationHelper.ValidarDocumentoBase(request.TipoDocumento, request.Documento, nameof(ConfirmarSolicitudAltaAsync));
            if (!documentoValidation.Success)
            {
                return OperationResult<object?>.IsFailed(
                    documentoValidation.ErrorCode,
                    nameof(ConfirmarSolicitudAltaAsync),
                    documentoValidation.Message,
                    documentoValidation.HttpCode);
            }

            var tipoDocumento = RegistroNormalizationHelper.Normalizar(request.TipoDocumento);
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
            var dtos = entidades.Select(MapCarrera).ToList();

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

            return await EnviarContraseniaAsync(persona, originMethod);
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

            return await EnviarContraseniaAsync(persona, nameof(ConfirmarNuevaPersonaAsync));
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

        private async Task<OperationResult<object?>> EnviarContraseniaAsync(Persona persona, string originMethod)
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
