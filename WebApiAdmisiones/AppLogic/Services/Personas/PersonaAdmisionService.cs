using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AppLogic.Constants;
using AppLogic.DevartDTOs;
using AppLogic.Helpers;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Personas;
using AppLogic.Requests;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Services.Personas
{
    public class PersonaAdmisionService : IPersonaAdmisionService
    {
        private sealed record DatosAcademicosEncuesta(
            long? CodigoInstitucionBac,
            string NombreInstitucion,
            long? CodigoTitulo,
            long UltimoAnioSexto);

        private sealed record DatosTituloEncuesta(
            long? CodigoTitulo,
            long UltimoAnioSexto);

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly IGeneralService _generalService;

        public PersonaAdmisionService(
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            IGeneralService generalService)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _generalService = generalService;
        }

        public OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerEncuestaInicialAdmision(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
                return OperationResult<DtoEncuestaIniAdmisionDevart>.IsFailed("GEN_DPI_01", nameof(ObtenerEncuestaInicialAdmision), "No se encontraron datos de pre-inscripción para la persona.", 204);

            return OperationResult<DtoEncuestaIniAdmisionDevart>.Ok(encuesta.ToDtoWithRelated(1), nameof(ObtenerEncuestaInicialAdmision));
        }

        public OperationResult<bool> GuardarDatosPersonaEncuesta(long codigoPersona, GuardarDatosPersonaEncuestaRequest request)
        {
            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetPersonaWithRelated(codigoPersona);
            if (persona == null)
                return OperationResult<bool>.IsFailed("PER_DPE_01", nameof(GuardarDatosPersonaEncuesta), PersonaConstants.PersonaNoEncontradaMessage, 404);

            var validacion = PersonaAdmisionValidation.ValidarDatosPersonaEncuestaRequest(
                request,
                persona.TipoPersona,
                nameof(GuardarDatosPersonaEncuesta),
                PersonaConstants.Parametros.FechaMinimaNacimiento);
            if (!validacion.Success)
                return validacion;

            var validacionCiudad = ValidarCiudadParaEncuesta(uow, request, nameof(GuardarDatosPersonaEncuesta));
            if (!validacionCiudad.Success)
                return validacionCiudad;

            var productoResult = ObtenerProductoValidoParaEncuesta(uow, request, nameof(GuardarDatosPersonaEncuesta));
            if (!productoResult.Success)
                return OperationResult<bool>.IsFailed(
                    productoResult.ErrorCode,
                    nameof(GuardarDatosPersonaEncuesta),
                    productoResult.Message,
                    productoResult.HttpCode);

            var idComienzoResult = ObtenerIdComienzoValidoParaEncuesta(uow, request, nameof(GuardarDatosPersonaEncuesta));
            if (!idComienzoResult.Success)
                return OperationResult<bool>.IsFailed(
                    idComienzoResult.ErrorCode,
                    nameof(GuardarDatosPersonaEncuesta),
                    idComienzoResult.Message,
                    idComienzoResult.HttpCode);

            var datosAcademicosResult = ResolverDatosAcademicosEncuesta(uow, request, nameof(GuardarDatosPersonaEncuesta));
            if (!datosAcademicosResult.Success)
                return OperationResult<bool>.IsFailed(
                    datosAcademicosResult.ErrorCode,
                    nameof(GuardarDatosPersonaEncuesta),
                    datosAcademicosResult.Message,
                    datosAcademicosResult.HttpCode);

            var idComienzo = idComienzoResult.Data;
            var datosAcademicos = datosAcademicosResult.Data!;

            var existente = uow.EncuestaIniAdmisions.GetByPersonaProductoComienzo(codigoPersona, request.IdProducto, idComienzo);
            if (existente != null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_DPE_13",
                    nameof(GuardarDatosPersonaEncuesta),
                    "Ya existe un registro para dicha encuesta.",
                    409);
            }

            var esSgi = persona.TipoPersona == PersonaConstants.TipoPersonaSgi;
            var esUsoExclusivo = EsUsoExclusivo(persona);
            AplicarCambiosPersona(
                persona,
                PersonaAdmisionValidation.CrearActualizarPersonaRequestDesdeEncuesta(request),
                esUsoExclusivo,
                esSgi);
            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Personas.Update(persona);

            var fechaVencimientoResult = _generalService.CalcularFechaVencimientoAdmisiones(codigoPersona, request.IdProceso);
            if (!fechaVencimientoResult.Success)
            {
                return OperationResult<bool>.IsFailed(
                    fechaVencimientoResult.ErrorCode,
                    nameof(GuardarDatosPersonaEncuesta),
                    fechaVencimientoResult.Message,
                    fechaVencimientoResult.HttpCode);
            }

            var fechaVencimiento = fechaVencimientoResult.Data;
            var encuesta = CrearEncuestaInicialAdmision(
                uow,
                request,
                persona,
                codigoPersona,
                idComienzo,
                fechaVencimiento,
                datosAcademicos);

            CargarConQuienCompartioDecision(encuesta, request.CompartidoCon);
            uow.EncuestaIniAdmisions.Add(encuesta);
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(GuardarDatosPersonaEncuesta));
        }

        private static OperationResult<bool> ValidarCiudadParaEncuesta(
            IUnitOfWork uow,
            GuardarDatosPersonaEncuestaRequest request,
            string methodName)
        {
            var ciudad = uow.Ciudads.GetByKey(request.CodigoPais, request.CodigoEstado, request.CodigoCiudad);
            if (ciudad is null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_DPE_02",
                    methodName,
                    "No existe la ciudad indicada para el país y estado enviados.",
                    400);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<Producto> ObtenerProductoValidoParaEncuesta(
            IUnitOfWork uow,
            GuardarDatosPersonaEncuestaRequest request,
            string methodName)
        {
            var producto = uow.Productos.GetByKey(request.IdProducto);
            if (producto is null)
            {
                return OperationResult<Producto>.IsFailed(
                    "PER_DPE_03",
                    methodName,
                    "El producto indicado es inválido.",
                    400);
            }

            if (producto.IdNivelProducto == 1 && request.UltimoAnioSexto == 4)
            {
                return OperationResult<Producto>.IsFailed(
                    "PER_DPE_04",
                    methodName,
                    "Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto año.",
                    400);
            }

            return OperationResult<Producto>.Ok(producto, methodName);
        }

        private static OperationResult<long> ObtenerIdComienzoValidoParaEncuesta(
            IUnitOfWork uow,
            GuardarDatosPersonaEncuestaRequest request,
            string methodName)
        {
            var proceso = uow.Procesos.GetProcesosHabilitadosPorProducto(request.IdProducto)
                .FirstOrDefault(p => p.IdProceso == request.IdProceso);
            if (proceso is null)
            {
                return OperationResult<long>.IsFailed(
                    "PER_DPE_05",
                    methodName,
                    "No existe un proceso habilitado para el producto indicado.",
                    404);
            }

            var idComienzo = uow.ProcesoComienzos.GetAllWithRelated()
                .Where(pc => pc.IdProceso == request.IdProceso)
                .Select(pc => (long?)pc.IdComienzo)
                .FirstOrDefault();
            if (!idComienzo.HasValue)
            {
                return OperationResult<long>.IsFailed(
                    "PER_DPE_06",
                    methodName,
                    "No se encontró un comienzo activo para el proceso indicado.",
                    404);
            }

            return OperationResult<long>.Ok(idComienzo.Value, methodName);
        }

        private static OperationResult<DatosAcademicosEncuesta> ResolverDatosAcademicosEncuesta(
            IUnitOfWork uow,
            GuardarDatosPersonaEncuestaRequest request,
            string methodName)
        {
            long? codigoInstitucionBac = request.CodigoInstitucionBac;
            var nombreInstitucion = request.NombreInstitucion;
            long? codigoTitulo = request.CodigoTitulo;
            long ultimoAnioSexto = request.UltimoAnioSexto;

            if (request.UltimoAnioSecundaria == PersonaConstants.Parametros.UruguayCodigoPais)
            {
                if (!codigoInstitucionBac.HasValue || codigoInstitucionBac.Value <= 0)
                {
                    return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                        "PER_DPE_07",
                        methodName,
                        "Debe indicar el código de la institución.",
                        400);
                }

                var institucion = uow.Empresas.GetByKey(codigoInstitucionBac.Value);
                if (institucion is null)
                {
                    return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                        "PER_DPE_08",
                        methodName,
                        "La institución indicada es inválida.",
                        400);
                }

                nombreInstitucion = institucion.Nombre ?? nombreInstitucion;
            }
            else
            {
                codigoInstitucionBac = PersonaConstants.Parametros.ExteriorInstitucionOrt;
                if (ultimoAnioSexto == 6)
                {
                    codigoTitulo = PersonaConstants.Parametros.TituloGenericoSextoExterior;
                }
            }

            if (ultimoAnioSexto == 6)
            {
                var resultadoTitulo = ResolverTituloParaSextoAnio(uow, codigoTitulo, ultimoAnioSexto, methodName);
                if (!resultadoTitulo.Success)
                {
                    return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                        resultadoTitulo.ErrorCode,
                        methodName,
                        resultadoTitulo.Message,
                        resultadoTitulo.HttpCode);
                }

                codigoTitulo = resultadoTitulo.Data!.CodigoTitulo;
                ultimoAnioSexto = resultadoTitulo.Data.UltimoAnioSexto;
            }
            else
            {
                codigoTitulo = null;
                var resultadoAnio = ResolverAnioBachillerSinTitulo(uow, request.UltimoAnioSexto, methodName);
                if (!resultadoAnio.Success)
                {
                    return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                        resultadoAnio.ErrorCode,
                        methodName,
                        resultadoAnio.Message,
                        resultadoAnio.HttpCode);
                }

                ultimoAnioSexto = resultadoAnio.Data;
            }

            return OperationResult<DatosAcademicosEncuesta>.Ok(
                new DatosAcademicosEncuesta(codigoInstitucionBac, nombreInstitucion, codigoTitulo, ultimoAnioSexto),
                methodName);
        }

        private static OperationResult<DatosTituloEncuesta> ResolverTituloParaSextoAnio(
            IUnitOfWork uow,
            long? codigoTitulo,
            long ultimoAnioSexto,
            string methodName)
        {
            if (!codigoTitulo.HasValue || codigoTitulo.Value <= 0)
            {
                return OperationResult<DatosTituloEncuesta>.IsFailed(
                    "PER_DPE_09",
                    methodName,
                    "Debe indicar el código del título.",
                    400);
            }

            var titulo = uow.Titulos.GetByKey(codigoTitulo.Value);
            if (titulo is null)
            {
                return OperationResult<DatosTituloEncuesta>.IsFailed(
                    "PER_DPE_10",
                    methodName,
                    "El título indicado es inválido.",
                    400);
            }

            if (titulo.IdAnioBachiller.HasValue)
            {
                var anioTitulo = uow.AnioBachillers.GetByKey((long)titulo.IdAnioBachiller.Value);
                if (anioTitulo is null)
                {
                    return OperationResult<DatosTituloEncuesta>.IsFailed(
                        "PER_DPE_11",
                        methodName,
                        "El bachillerato indicado es inválido.",
                        400);
                }

                ultimoAnioSexto = (long?)(anioTitulo.CantAniosAnioBachiller) ?? ultimoAnioSexto;
            }

            return OperationResult<DatosTituloEncuesta>.Ok(
                new DatosTituloEncuesta(codigoTitulo, ultimoAnioSexto),
                methodName);
        }

        private static OperationResult<long> ResolverAnioBachillerSinTitulo(
            IUnitOfWork uow,
            long ultimoAnioSextoRequest,
            string methodName)
        {
            var anioBachiller = uow.AnioBachillers.GetAll()
                .FirstOrDefault(a => a.CantAniosAnioBachiller == ultimoAnioSextoRequest);
            if (anioBachiller is null)
            {
                return OperationResult<long>.IsFailed(
                    "PER_DPE_12",
                    methodName,
                    "El bachillerato indicado es inválido.",
                    400);
            }

            return OperationResult<long>.Ok(
                (long)(anioBachiller.CantAniosAnioBachiller ?? ultimoAnioSextoRequest),
                methodName);
        }

        private EncuestaIniAdmision CrearEncuestaInicialAdmision(
            IUnitOfWork uow,
            GuardarDatosPersonaEncuestaRequest request,
            Persona persona,
            long codigoPersona,
            long idComienzo,
            DateTime fechaVencimiento,
            DatosAcademicosEncuesta datosAcademicos)
        {
            var ahora = DateTime.Now;

            return new EncuestaIniAdmision
            {
                IdEncuestaIni = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION),
                IdProducto = request.IdProducto,
                IdComienzo = idComienzo,
                FechaEncuestaIni = ahora,
                ClaveEncuestaIni = GenerarClaveEncuesta(request.IdProducto, persona.Documento),
                NombreInstSecEncuestaIni = datosAcademicos.NombreInstitucion,
                CodigoTitulo = datosAcademicos.CodigoTitulo,
                UltimoAnioSextoEncuestaIni = datosAcademicos.UltimoAnioSexto.ToString(),
                VecesSextoEncuestaIni = request.VecesSextoBool ? request.VecesSexto.ToString() : null,
                InstruccionPadreEncuestaIni = request.InstruccionPadre.ToString(),
                InstruccionMadreEncuestaIni = request.InstruccionMadre.ToString(),
                DecisionCarreraEncuestaIni = request.DecisionCarrera?.ToString(),
                DecisionUniverEncuestaIni = request.DecisionUniversidad?.ToString(),
                InforOtrasAntesEncuestaIni = request.InfoOtrasUniversidadesAntes,
                InforOtrasLinea1Ini = request.InfoOtrasLinea1,
                InforOtrasLinea2Ini = request.InfoOtrasLinea2,
                TipoDocumento = persona.TipoDocumento,
                Documento = persona.Documento,
                CodigoPersona = codigoPersona,
                CodigoInstitucionBac = datosAcademicos.CodigoInstitucionBac,
                FechaVtoAdmision = fechaVencimiento,
                InformarEncuestaIni = request.InformarEncuesta,
                IdProceso = request.IdProceso,
                TipoInscripcion = "SOLO_ENCUESTA_INI",
                NuevaversionEncuestaIni = "SI",
                UltimoanioSecundariaEncuestaIni = request.UltimoAnioSecundaria == PersonaConstants.Parametros.UruguayCodigoPais,
                TieneEducacionSuperiorEncuestaIni = ConvertirBoolASiNo(request.TieneEducacionSuperior),
                NivelDecisionEncuestaIni = request.NivelDecision == 1,
                AsesoramientoOrtEncuestaIni = ConvertirBoolASiNo(request.AsesoramientoOrt),
                ValoracionAsesoramientoOrtEncuestaIni = request.AsesoramientoOrt == true
                    ? request.ValoracionAsesoramientoOrt > 3
                    : null,
                VistaSitioWebOrtEncuestaIni = ConvertirBoolASiNo(request.VistaSitioWebOrt),
                ValoracionSitioWebOrtEncuestaIni = request.VistaSitioWebOrt == true
                    ? request.ValoracionSitioWeb > 3
                    : null,
                VistaInstalacionesOrtEncuestaIni = ConvertirBoolASiNo(request.VistaInstalacionesOrt),
                ValoracionInstalacionesOrtEncuestaIni = request.VistaInstalacionesOrt == true
                    ? request.ValoracionInstalacionesOrt > 3
                    : null,
                PublicidadOrtEncuestaIni = ConvertirBoolASiNo(request.PublicidadOrt),
                InstruccionMadreOrtEncuestaIni = request.InstruccionMadre is 5 or 6
                    ? ConvertirBoolASiNo(request.InstruccionMadreOrt)
                    : null,
                InstruccionPadreOrtEncuestaIni = request.InstruccionPadre is 5 or 6
                    ? ConvertirBoolASiNo(request.InstruccionPadreOrt)
                    : null,
                UsuarioIngreso = uow.ObtenerDbUserId(),
                FechaIngreso = ahora,
                HoraIngreso = ahora.ToString("HH:mm:ss")
            };
        }

        private static string FormatearTextoCapitalizado(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            var texto = string.Join(" ", valor.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(texto.ToLower(CultureInfo.CurrentCulture));
        }

        private static string? FormatearTextoCapitalizadoNullable(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : FormatearTextoCapitalizado(valor);
        }

        private static string ConvertirBoolASiNo(bool valor)
        {
            return valor ? "SI" : "NO";
        }

        private static string? ConvertirBoolASiNo(bool? valor)
        {
            if (!valor.HasValue)
            {
                return null;
            }

            return ConvertirBoolASiNo(valor.Value);
        }

        private static string GenerarClaveEncuesta(long idProducto, string? documento)
        {
            var input = $"{idProducto}/{documento?.Trim().ToUpperInvariant()}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash)[..30];
        }

        private static bool EsUsoExclusivo(Persona persona)
        {
            var funcionarioActivo = string.Equals(persona.FuncionarioActivoPersona, "SI", StringComparison.OrdinalIgnoreCase);
            var usoExclusivoDba = string.Equals(persona.UsoexclusivodbaPersona, "SI", StringComparison.OrdinalIgnoreCase);
            var tieneInscripcion = persona.Inscriptos?.Any() == true;

            return funcionarioActivo || usoExclusivoDba || (!usoExclusivoDba && tieneInscripcion);
        }

        private static void AplicarCambiosPersona(Persona persona, ActualizarPersonaRequest request, bool esUsoExclusivo, bool esSgi)
        {
            if (!esUsoExclusivo)
            {
                persona.PrimerNombre = FormatearTextoCapitalizado(request.PrimerNombre);
                persona.SegundoNombre = FormatearTextoCapitalizadoNullable(request.SegundoNombre);
                persona.PrimerApellido = FormatearTextoCapitalizado(request.PrimerApellido);
                persona.SegundoApellido = FormatearTextoCapitalizadoNullable(request.SegundoApellido);
                persona.PrimerNombreMay = persona.PrimerNombre.ToUpperInvariant();
                persona.SegundoNombreMay = persona.SegundoNombre?.ToUpperInvariant();
                persona.PrimerApellidoMay = persona.PrimerApellido.ToUpperInvariant();
                persona.SegundoApellidoMay = persona.SegundoApellido?.ToUpperInvariant();
                persona.FechaNacimiento = request.FechaNacimiento;
            }

            persona.CodigoPais = request.CodigoPais;
            persona.CodigoEstado = request.CodigoEstado;
            persona.CodigoCiudad = request.CodigoCiudad;
            persona.Sexo = string.IsNullOrWhiteSpace(request.Sexo) ? null : request.Sexo.Trim().ToUpperInvariant();
            persona.Direccion = FormatearTextoCapitalizado(request.Direccion);
            persona.Telefono1 = request.Telefono1?.Trim();
            persona.Email = request.Mail?.Trim();

            if (esSgi)
            {
                persona.TrabajaActualmente = string.IsNullOrWhiteSpace(request.TrabajaActualmente)
                    ? null
                    : request.TrabajaActualmente.Trim().ToUpperInvariant();
                persona.TipoJornada = request.TipoJornada > 0 ? (byte?)request.TipoJornada : null;
            }
        }

        private static void CargarConQuienCompartioDecision(EncuestaIniAdmision encuesta, int valor)
        {
            encuesta.ComparPadresEncuestaIni = ConvertirBoolASiNo(valor == 1);
            encuesta.ComparOtrosEncuestaIni = ConvertirBoolASiNo(valor == 4);
            encuesta.ComparAmigoFamEncuestaIni = ConvertirBoolASiNo(valor == 2);
            encuesta.ComparNadieEncuestaIni = ConvertirBoolASiNo(valor == 5);
            encuesta.ComparAmigoPropEncuestaIni = ConvertirBoolASiNo(valor == 3);
        }

    }
}
