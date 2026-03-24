using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AppLogic.DevartDTOs;
using AppLogic.Helpers;
using AppLogic.Interfaces;
using AppLogic.Requests;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Configuration;
using Utilities;

namespace AppLogic.Services
{
    public class PersonaAdmisionService : IPersonaAdmisionService
    {
        private readonly DateTime _fechaMinimaNacimiento;
        private readonly long _uruguayCodigoPais;
        private readonly long _exteriorInstitucionOrt;
        private readonly long _tituloGenericoSextoExterior;

        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;
        private readonly IGeneralService _generalService;

        public PersonaAdmisionService(
            IUnitOfWorkFactory uowFactory,
            IDbConnectionContext dbConnectionContext,
            IGeneralService generalService,
            IConfiguration configuration)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
            _generalService = generalService;
            _fechaMinimaNacimiento = configuration.GetValue("Admisiones:PersonaAdmision:FechaMinimaNacimiento", new DateTime(1900, 1, 1));
            _uruguayCodigoPais = configuration.GetValue("Admisiones:PersonaAdmision:UruguayCodigoPais", 1L);
            _exteriorInstitucionOrt = configuration.GetValue("Admisiones:PersonaAdmision:ExteriorInstitucionOrt", 2898L);
            _tituloGenericoSextoExterior = configuration.GetValue("Admisiones:PersonaAdmision:TituloGenericoSextoExterior", 5L);
        }

        public OperationResult<DtoPersonaDevart> ObtenerPersona(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetPersonaWithRelated(codigoPersona);
            if (persona == null)
                return OperationResult<DtoPersonaDevart>.IsFailed("GEN_PER_01", nameof(ObtenerPersona), "Persona no encontrada.", 404);

            return OperationResult<DtoPersonaDevart>.Ok(persona.ToDto(), nameof(ObtenerPersona));
        }

        public OperationResult<bool> ActualizarPersona(long codigoPersona, ActualizarPersonaRequest request)
        {
            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetPersonaWithRelated(codigoPersona);
            if (persona == null)
                return OperationResult<bool>.IsFailed("PER_AP_01", nameof(ActualizarPersona), "Persona no encontrada.", 404);

            var validacion = ValidarActualizarPersonaRequest(request, nameof(ActualizarPersona));
            if (!validacion.Success)
                return validacion;

            var ciudad = uow.Ciudads.GetByKey(request.CodigoPais, request.CodigoEstado, request.CodigoCiudad);
            if (ciudad == null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_AP_02",
                    nameof(ActualizarPersona),
                    "No existe la ciudad indicada para el país y estado enviados.",
                    400);
            }

            var esUsoExclusivo = EsUsoExclusivo(persona);
            AplicarCambiosPersona(persona, request, esUsoExclusivo, persona.TipoPersona == "SGI");
            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Personas.Update(persona);
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(ActualizarPersona));
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
                return OperationResult<bool>.IsFailed("PER_DPE_01", nameof(GuardarDatosPersonaEncuesta), "Persona no encontrada.", 404);

            var validacion = ValidarDatosPersonaEncuestaRequest(request, persona.TipoPersona, nameof(GuardarDatosPersonaEncuesta));
            if (!validacion.Success)
                return validacion;

            var ciudad = uow.Ciudads.GetByKey(request.CodigoPais, request.CodigoEstado, request.CodigoCiudad);
            if (ciudad == null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_DPE_02",
                    nameof(GuardarDatosPersonaEncuesta),
                    "No existe la ciudad indicada para el país y estado enviados.",
                    400);
            }

            var producto = uow.Productos.GetByKey(request.IdProducto);
            if (producto == null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_DPE_03",
                    nameof(GuardarDatosPersonaEncuesta),
                    "El producto indicado es inválido.",
                    400);
            }

            if (producto.IdNivelProducto == 1 && request.UltimoAnioSexto == 4)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_DPE_04",
                    nameof(GuardarDatosPersonaEncuesta),
                    "Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto año.",
                    400);
            }

            var proceso = uow.Procesos.GetProcesosHabilitadosPorProducto(request.IdProducto)
                .FirstOrDefault(p => p.IdProceso == request.IdProceso);
            if (proceso == null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_DPE_05",
                    nameof(GuardarDatosPersonaEncuesta),
                    "No existe un proceso habilitado para el producto indicado.",
                    404);
            }

            var idComienzo = uow.ProcesoComienzos.GetAllWithRelated()
                .Where(pc => pc.IdProceso == request.IdProceso)
                .Select(pc => (long?)pc.IdComienzo)
                .FirstOrDefault();
            if (!idComienzo.HasValue)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_DPE_06",
                    nameof(GuardarDatosPersonaEncuesta),
                    "No se encontró un comienzo activo para el proceso indicado.",
                    404);
            }

            long? codigoInstitucionBac = request.CodigoInstitucionBac;
            string nombreInstitucion = request.NombreInstitucion;
            long? codigoTitulo = request.CodigoTitulo;
            long ultimoAnioSexto = request.UltimoAnioSexto;

            if (request.UltimoAnioSecundaria == _uruguayCodigoPais)
            {
                if (!codigoInstitucionBac.HasValue || codigoInstitucionBac.Value <= 0)
                {
                    return OperationResult<bool>.IsFailed(
                        "PER_DPE_07",
                        nameof(GuardarDatosPersonaEncuesta),
                        "Debe indicar el código de la institución.",
                        400);
                }

                var institucion = uow.Empresas.GetByKey(codigoInstitucionBac.Value);
                if (institucion == null)
                {
                    return OperationResult<bool>.IsFailed(
                        "PER_DPE_08",
                        nameof(GuardarDatosPersonaEncuesta),
                        "La institución indicada es inválida.",
                        400);
                }

                nombreInstitucion = institucion.Nombre ?? nombreInstitucion;
            }
            else
            {
                codigoInstitucionBac = _exteriorInstitucionOrt;
                if (ultimoAnioSexto == 6)
                    codigoTitulo = _tituloGenericoSextoExterior;
            }

            if (ultimoAnioSexto == 6)
            {
                if (!codigoTitulo.HasValue || codigoTitulo.Value <= 0)
                {
                    return OperationResult<bool>.IsFailed(
                        "PER_DPE_09",
                        nameof(GuardarDatosPersonaEncuesta),
                        "Debe indicar el código del título.",
                        400);
                }

                var titulo = uow.Titulos.GetByKey(codigoTitulo.Value);
                if (titulo == null)
                {
                    return OperationResult<bool>.IsFailed(
                        "PER_DPE_10",
                        nameof(GuardarDatosPersonaEncuesta),
                        "El título indicado es inválido.",
                        400);
                }

                if (titulo.IdAnioBachiller.HasValue)
                {
                    var anioTitulo = uow.AnioBachillers.GetByKey((long)titulo.IdAnioBachiller.Value);
                    if (anioTitulo == null)
                    {
                        return OperationResult<bool>.IsFailed(
                            "PER_DPE_11",
                            nameof(GuardarDatosPersonaEncuesta),
                            "El bachillerato indicado es inválido.",
                            400);
                    }

                    ultimoAnioSexto = (long?)(anioTitulo.CantAniosAnioBachiller) ?? ultimoAnioSexto;
                }
            }
            else
            {
                codigoTitulo = null;
                var anioBachiller = uow.AnioBachillers.GetAll()
                    .FirstOrDefault(a => a.CantAniosAnioBachiller == request.UltimoAnioSexto);
                if (anioBachiller == null)
                {
                    return OperationResult<bool>.IsFailed(
                        "PER_DPE_12",
                        nameof(GuardarDatosPersonaEncuesta),
                        "El bachillerato indicado es inválido.",
                        400);
                }

                ultimoAnioSexto = (long)(anioBachiller.CantAniosAnioBachiller ?? request.UltimoAnioSexto);
            }

            var existente = uow.EncuestaIniAdmisions.GetByPersonaProductoComienzo(codigoPersona, request.IdProducto, idComienzo.Value);
            if (existente != null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_DPE_13",
                    nameof(GuardarDatosPersonaEncuesta),
                    "Ya existe un registro para dicha encuesta.",
                    409);
            }

            var esSgi = persona.TipoPersona == "SGI";
            var esUsoExclusivo = EsUsoExclusivo(persona);
            AplicarCambiosPersona(
                persona,
                new ActualizarPersonaRequest
                {
                    PrimerApellido = request.PrimerApellido,
                    SegundoApellido = request.SegundoApellido,
                    PrimerNombre = request.PrimerNombre,
                    SegundoNombre = request.SegundoNombre,
                    Mail = request.Mail,
                    VerificacionMail = request.VerificacionMail,
                    Direccion = request.Direccion,
                    Sexo = request.Sexo,
                    FechaNacimiento = request.FechaNacimiento,
                    Telefono1 = request.Telefono1,
                    Telefono2 = request.Telefono2,
                    CodigoPais = request.CodigoPais,
                    CodigoEstado = request.CodigoEstado,
                    CodigoCiudad = request.CodigoCiudad,
                    Documento = request.Documento,
                    TipoDocumento = request.TipoDocumento,
                    TrabajaActualmente = request.TrabajaActualmente,
                    TipoJornada = request.TipoJornada
                },
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
            var ahora = DateTime.Now;
            var encuesta = new EncuestaIniAdmision
            {
                IdEncuestaIni = _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION),
                IdProducto = request.IdProducto,
                IdComienzo = idComienzo.Value,
                FechaEncuestaIni = ahora,
                ClaveEncuestaIni = GenerarClaveEncuesta(request.IdProducto, persona.Documento),
                NombreInstSecEncuestaIni = nombreInstitucion,
                CodigoTitulo = codigoTitulo,
                UltimoAnioSextoEncuestaIni = ultimoAnioSexto.ToString(),
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
                CodigoInstitucionBac = codigoInstitucionBac,
                FechaVtoAdmision = fechaVencimiento,
                InformarEncuestaIni = request.InformarEncuesta,
                IdProceso = request.IdProceso,
                TipoInscripcion = "SOLO_ENCUESTA_INI",
                NuevaversionEncuestaIni = "SI",
                UltimoanioSecundariaEncuestaIni = request.UltimoAnioSecundaria == _uruguayCodigoPais,
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

            CargarConQuienCompartioDecision(encuesta, request.CompartidoCon);
            uow.EncuestaIniAdmisions.Add(encuesta);
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(GuardarDatosPersonaEncuesta));
        }

        public OperationResult<byte[]> ObtenerDocumentoAlumno(long codigoPersona, int tipo)
        {
            if (tipo != 1 && tipo != 2)
                return OperationResult<byte[]>.IsFailed("GEN_DA_01", nameof(ObtenerDocumentoAlumno), "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).", 400);

            using var uow = _uowFactory.Create();
            var imagenTemporal = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);

            if (imagenTemporal == null)
                return OperationResult<byte[]>.IsFailed("GEN_DA_02", nameof(ObtenerDocumentoAlumno), "Documento no encontrado.", 404);

            if (imagenTemporal.FechaVtoDocumentoPersona.HasValue && imagenTemporal.FechaVtoDocumentoPersona.Value < DateTime.Now)
                return OperationResult<byte[]>.IsFailed("GEN_DA_03", nameof(ObtenerDocumentoAlumno), "El documento se encuentra vencido.", 409);

            if (imagenTemporal.BlobImagen == null || imagenTemporal.BlobImagen.Length == 0)
                return OperationResult<byte[]>.IsFailed("GEN_DA_04", nameof(ObtenerDocumentoAlumno), "El documento no contiene imagen.", 404);

            return OperationResult<byte[]>.Ok(imagenTemporal.BlobImagen, nameof(ObtenerDocumentoAlumno));
        }

        public OperationResult<byte[]> ObtenerFotoAlumno(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var imagen = uow.Imagens.GetFotoByPersona(codigoPersona);

            if (imagen == null)
                return OperationResult<byte[]>.IsFailed("GEN_FA_01", nameof(ObtenerFotoAlumno), "Foto no encontrada.", 404);

            if (imagen.BlobImagen == null || imagen.BlobImagen.Length == 0)
                return OperationResult<byte[]>.IsFailed("GEN_FA_02", nameof(ObtenerFotoAlumno), "La foto no contiene imagen.", 404);

            return OperationResult<byte[]>.Ok(imagen.BlobImagen, nameof(ObtenerFotoAlumno));
        }

        public OperationResult<bool> SubirFotoAlumno(long codigoPersona, byte[] fileContent, string fileName)
        {
            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed("GEN_SFA_01", nameof(SubirFotoAlumno), "Persona no encontrada.", 404);
            }

            var imagenExistente = uow.Imagens.GetFotoByPersona(codigoPersona);

            if (imagenExistente is null)
            {
                var resultadoGuardado = GuardarFotoAlumno(
                    persona,
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN),
                    fileContent,
                    fileName);

                if (!resultadoGuardado.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoGuardado.ErrorCode,
                        nameof(SubirFotoAlumno),
                        resultadoGuardado.Message,
                        resultadoGuardado.HttpCode);
                }

                uow.Imagens.Add(resultadoGuardado.Data!);
            }
            else
            {
                var resultadoModificacion = ModificarFotoAlumno(imagenExistente, fileContent, fileName);
                if (!resultadoModificacion.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoModificacion.ErrorCode,
                        nameof(SubirFotoAlumno),
                        resultadoModificacion.Message,
                        resultadoModificacion.HttpCode);
                }

                uow.Imagens.Update(imagenExistente);
            }

            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(SubirFotoAlumno));
        }

        public OperationResult<bool> SubirDocumentoAlumno(long codigoPersona, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            if (tipo != 1 && tipo != 2)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_SDA_01",
                    nameof(SubirDocumentoAlumno),
                    "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).",
                    400);
            }

            using var uow = _uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed("GEN_SDA_02", nameof(SubirDocumentoAlumno), "Persona no encontrada.", 404);
            }

            var documentoExistente = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);

            if (documentoExistente is null)
            {
                var resultadoGuardado = GuardarDocumentoAlumno(
                    codigoPersona,
                    _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL),
                    tipo,
                    fecha,
                    fileContent,
                    fileName);

                if (!resultadoGuardado.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoGuardado.ErrorCode,
                        nameof(SubirDocumentoAlumno),
                        resultadoGuardado.Message,
                        resultadoGuardado.HttpCode);
                }

                uow.ImagenTemporals.Add(resultadoGuardado.Data!);
            }
            else
            {
                var resultadoModificacion = ModificarDocumentoAlumno(documentoExistente, tipo, fecha, fileContent, fileName);
                if (!resultadoModificacion.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoModificacion.ErrorCode,
                        nameof(SubirDocumentoAlumno),
                        resultadoModificacion.Message,
                        resultadoModificacion.HttpCode);
                }

                uow.ImagenTemporals.Update(documentoExistente);
            }

            persona.FechaVtoDocumentoPersona = fecha;
            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(SubirDocumentoAlumno));
        }

        private static OperationResult<Imagen> GuardarFotoAlumno(Persona persona, int idImagen, byte[] fileContent, string fileName)
        {
            if (fileContent == null || fileContent.Length == 0)
                return OperationResult<Imagen>.IsFailed("GEN_SFA_03", nameof(GuardarFotoAlumno), "La imagen no puede estar vacía.", 400);

            var imageValidation = FileValidationHelper.ValidateImageFile(
                fileContent,
                fileName,
                nameof(GuardarFotoAlumno));

            if (!imageValidation.Success)
            {
                return OperationResult<Imagen>.IsFailed(
                    "GEN_SFA_02",
                    nameof(GuardarFotoAlumno),
                    $"La imagen no es válida. Solo se permiten imágenes válidas en formato JPG, JPEG o PNG. Detalle: {imageValidation.Message}",
                    400);
            }

            var extension = ResolverExtensionPersistida(fileName, ".jpg");

            return OperationResult<Imagen>.Ok(
                new Imagen
                {
                    IdImagen = idImagen,
                    CodigoPersona = persona.CodigoPersona,
                    NombreImagen = ConstruirNombrePersistido(persona.CodigoPersona, 3, extension),
                    TipoImagen = "3",
                    BlobImagen = fileContent
                },
                nameof(GuardarFotoAlumno));
        }

        private static OperationResult<bool> ModificarFotoAlumno(Imagen existing, byte[] fileContent, string fileName)
        {
            if (fileContent == null || fileContent.Length == 0)
                return OperationResult<bool>.IsFailed("GEN_SFA_04", nameof(ModificarFotoAlumno), "La imagen no puede estar vacía.", 400);

            var imageValidation = FileValidationHelper.ValidateImageFile(
                fileContent,
                fileName,
                nameof(ModificarFotoAlumno));

            if (!imageValidation.Success)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_SFA_05",
                    nameof(ModificarFotoAlumno),
                    $"La imagen no es válida. Solo se permiten imágenes válidas en formato JPG, JPEG o PNG. Detalle: {imageValidation.Message}",
                    400);
            }

            var extension = ResolverExtensionPersistida(fileName, ".jpg");

            existing.NombreImagen = ConstruirNombrePersistido(existing.CodigoPersona ?? 0, 3, extension);
            existing.TipoImagen = "3";
            existing.BlobImagen = fileContent;
            return OperationResult<bool>.Ok(true, nameof(ModificarFotoAlumno));
        }

        private static OperationResult<ImagenTemporal> GuardarDocumentoAlumno(long codigoPersona, int idImagenTemporal, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            var validacion = FileValidationHelper.ValidateDocumentFile(fileContent, fileName, nameof(GuardarDocumentoAlumno));
            if (!validacion.Success)
            {
                return OperationResult<ImagenTemporal>.IsFailed(validacion.ErrorCode, nameof(GuardarDocumentoAlumno), validacion.Message, validacion.HttpCode);
            }

            var extension = ResolverExtensionPersistida(fileName, ".pdf");
            var nombrePersistencia = ConstruirNombrePersistido(codigoPersona, tipo, extension);

            return OperationResult<ImagenTemporal>.Ok(
                new ImagenTemporal
                {
                    IdImagenTemporal = idImagenTemporal,
                    CodigoPersona = codigoPersona,
                    NombreImagen = nombrePersistencia,
                    TipoImagen = tipo.ToString(),
                    BlobImagen = fileContent,
                    FechaVtoDocumentoPersona = fecha
                },
                nameof(GuardarDocumentoAlumno));
        }

        private static OperationResult<bool> ModificarDocumentoAlumno(ImagenTemporal existing, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            var validacion = FileValidationHelper.ValidateDocumentFile(fileContent, fileName, nameof(ModificarDocumentoAlumno));
            if (!validacion.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validacion.ErrorCode,
                    nameof(ModificarDocumentoAlumno),
                    validacion.Message,
                    validacion.HttpCode);
            }

            var extension = ResolverExtensionPersistida(fileName, ".pdf");
            existing.NombreImagen = ConstruirNombrePersistido(existing.CodigoPersona ?? 0, tipo, extension);
            existing.TipoImagen = tipo.ToString();
            existing.BlobImagen = fileContent;
            existing.FechaVtoDocumentoPersona = fecha;
            return OperationResult<bool>.Ok(true, nameof(ModificarDocumentoAlumno));
        }

        private static string ResolverExtensionPersistida(string fileName, string defaultExtension)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return string.IsNullOrWhiteSpace(extension) ? defaultExtension : extension;
        }

        private static string ConstruirNombrePersistido(long codigoPersona, int tipoImagen, string extension)
        {
            return $"{codigoPersona}_{tipoImagen}{extension}";
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

        private static string? ConvertirBoolASiNo(bool? valor)
        {
            return valor.HasValue ? (valor.Value ? "SI" : "NO") : null;
        }

        private static string GenerarClaveEncuesta(long idProducto, string? documento)
        {
            var input = $"{idProducto}/{documento?.Trim().ToUpperInvariant()}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash)[..30];
        }

        private OperationResult<bool> ValidarActualizarPersonaRequest(ActualizarPersonaRequest request, string callingMethod)
        {
            if (request.FechaNacimiento <= _fechaMinimaNacimiento)
            {
                return OperationResult<bool>.IsFailed("PER_AP_03", callingMethod, "Fecha de nacimiento incorrecta.", 400);
            }

            if (string.IsNullOrWhiteSpace(request.PrimerApellido)
                || string.IsNullOrWhiteSpace(request.PrimerNombre)
                || string.IsNullOrWhiteSpace(request.Mail)
                || string.IsNullOrWhiteSpace(request.VerificacionMail)
                || string.IsNullOrWhiteSpace(request.Direccion))
            {
                return OperationResult<bool>.IsFailed("PER_AP_04", callingMethod, "Faltan parámetros obligatorios.", 400);
            }

            if (!string.Equals(request.Mail, request.VerificacionMail, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<bool>.IsFailed("PER_AP_05", callingMethod, "El mail y su verificación no coinciden.", 400);
            }

            if (!string.IsNullOrWhiteSpace(request.Sexo)
                && request.Sexo.Trim() is not ("M" or "F"))
            {
                return OperationResult<bool>.IsFailed("PER_AP_06", callingMethod, "Sexo inválido.", 400);
            }

            if (request.PrimerNombre.Trim().Length <= 1)
            {
                return OperationResult<bool>.IsFailed("PER_AP_07", callingMethod, "Primer nombre inválido.", 400);
            }

            if (request.PrimerApellido.Trim().Length <= 1)
            {
                return OperationResult<bool>.IsFailed("PER_AP_08", callingMethod, "Primer apellido inválido.", 400);
            }

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        private OperationResult<bool> ValidarDatosPersonaEncuestaRequest(
            GuardarDatosPersonaEncuestaRequest request,
            string tipoPersona,
            string callingMethod)
        {
            var validacionPersona = ValidarActualizarPersonaRequest(
                new ActualizarPersonaRequest
                {
                    PrimerApellido = request.PrimerApellido,
                    SegundoApellido = request.SegundoApellido,
                    PrimerNombre = request.PrimerNombre,
                    SegundoNombre = request.SegundoNombre,
                    Mail = request.Mail,
                    VerificacionMail = request.VerificacionMail,
                    Direccion = request.Direccion,
                    Sexo = request.Sexo,
                    FechaNacimiento = request.FechaNacimiento,
                    Telefono1 = request.Telefono1,
                    Telefono2 = request.Telefono2,
                    CodigoPais = request.CodigoPais,
                    CodigoEstado = request.CodigoEstado,
                    CodigoCiudad = request.CodigoCiudad,
                    Documento = request.Documento,
                    TipoDocumento = request.TipoDocumento,
                    TrabajaActualmente = request.TrabajaActualmente,
                    TipoJornada = request.TipoJornada
                },
                callingMethod);
            if (!validacionPersona.Success)
                return validacionPersona;

            if (!string.Equals(request.Sexo?.Trim(), "M", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(request.Sexo?.Trim(), "F", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<bool>.IsFailed("PER_DPE_14", callingMethod, "Sexo inválido.", 400);
            }

            if (tipoPersona == "SGI")
            {
                if (string.IsNullOrWhiteSpace(request.TrabajaActualmente)
                    || request.TrabajaActualmente is not ("S" or "N"))
                {
                    return OperationResult<bool>.IsFailed("PER_DPE_15", callingMethod, "Debe indicar si trabaja actualmente.", 400);
                }

                if (request.TrabajaActualmente == "S" && request.TipoJornada is not (1 or 2))
                {
                    return OperationResult<bool>.IsFailed("PER_DPE_16", callingMethod, "Debe indicar el tipo de jornada.", 400);
                }
            }

            if (request.UltimoAnioSexto <= 0)
                return OperationResult<bool>.IsFailed("PER_DPE_17", callingMethod, "Debe indicar la última vez que cursó sexto.", 400);
            if (request.InstruccionMadre < 1 || request.InstruccionMadre > 7)
                return OperationResult<bool>.IsFailed("PER_DPE_18", callingMethod, "Error en el nivel de formación de madre o tutor indicado.", 400);
            if (request.InstruccionPadre < 1 || request.InstruccionPadre > 7)
                return OperationResult<bool>.IsFailed("PER_DPE_19", callingMethod, "Error en el nivel de formación de padre o tutor indicado.", 400);
            if (request.DecisionCarrera is not (0 or 2 or 3 or 4))
                return OperationResult<bool>.IsFailed("PER_DPE_20", callingMethod, "Debe indicar la decisión de carrera.", 400);
            if (request.DecisionUniversidad is not (0 or 2 or 3 or 4))
                return OperationResult<bool>.IsFailed("PER_DPE_21", callingMethod, "Debe indicar la decisión de universidad.", 400);
            if (request.CompartidoCon < 1 || request.CompartidoCon > 5)
                return OperationResult<bool>.IsFailed("PER_DPE_22", callingMethod, "Debe indicar con quién compartió la decisión.", 400);
            if (request.InfoOtrasUniversidadesAntes is not ("SI" or "NO"))
                return OperationResult<bool>.IsFailed("PER_DPE_23", callingMethod, "Debe indicar si se informó en alguna universidad.", 400);
            if (request.InfoOtrasUniversidadesAntes == "SI" && request.UniversidadesConsideradas.Count == 0)
                return OperationResult<bool>.IsFailed("PER_DPE_24", callingMethod, "Debe indicar en qué otras universidades se informó.", 400);
            if (request.UltimoAnioSexto < 4 || request.UltimoAnioSexto > 6)
                return OperationResult<bool>.IsFailed("PER_DPE_25", callingMethod, "Último año de bachillerato inválido.", 400);
            if (request.InformarEncuesta is not ("SI" or "NO"))
                return OperationResult<bool>.IsFailed("PER_DPE_26", callingMethod, "Debe indicar si informa encuesta.", 400);
            if (request.UltimoAnioSecundaria is not (1 or 2))
                return OperationResult<bool>.IsFailed("PER_DPE_27", callingMethod, "Último año de secundaria inválido.", 400);
            if (request.NivelDecision is not (1 or 2))
                return OperationResult<bool>.IsFailed("PER_DPE_28", callingMethod, "Nivel de decisión inválido.", 400);
            if (request.AsesoramientoOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_29", callingMethod, "Debe indicar si hubo reunión de asesoramiento.", 400);
            if (request.AsesoramientoOrt == true && (request.ValoracionAsesoramientoOrt < 1 || request.ValoracionAsesoramientoOrt > 5))
                return OperationResult<bool>.IsFailed("PER_DPE_30", callingMethod, "Valoración de asesoramiento inválida.", 400);
            if (request.VistaSitioWebOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_31", callingMethod, "Debe indicar si visitó el sitio de ORT.", 400);
            if (request.VistaSitioWebOrt == true && (request.ValoracionSitioWeb < 1 || request.ValoracionSitioWeb > 5))
                return OperationResult<bool>.IsFailed("PER_DPE_32", callingMethod, "Valoración del sitio web inválida.", 400);
            if (request.VistaInstalacionesOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_33", callingMethod, "Debe indicar si visitó las instalaciones de ORT.", 400);
            if (request.VistaInstalacionesOrt == true && (request.ValoracionInstalacionesOrt < 1 || request.ValoracionInstalacionesOrt > 5))
                return OperationResult<bool>.IsFailed("PER_DPE_34", callingMethod, "Valoración de instalaciones inválida.", 400);
            if (request.PublicidadOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_35", callingMethod, "Debe indicar si recuerda alguna publicidad de ORT.", 400);
            if (request.PublicidadOrt == true && request.OpcionesPublicidadSeleccionadas.Count == 0)
                return OperationResult<bool>.IsFailed("PER_DPE_36", callingMethod, "Debe indicar publicidades seleccionadas.", 400);
            if (request.OpcionesMotivosSeleccionados.Count == 0)
                return OperationResult<bool>.IsFailed("PER_DPE_37", callingMethod, "Debe indicar los motivos de elección.", 400);
            if (request.UltimoAnioSexto == 6 && !request.CodigoTitulo.HasValue)
                return OperationResult<bool>.IsFailed("PER_DPE_38", callingMethod, "Debe indicar orientación del bachillerato.", 400);
            if (request.InstruccionMadre is 5 or 6 && request.InstruccionMadreOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_39", callingMethod, "Debe indicar si la madre o tutor obtuvo el título en ORT.", 400);
            if (request.InstruccionPadre is 5 or 6 && request.InstruccionPadreOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_40", callingMethod, "Debe indicar si el padre o tutor obtuvo el título en ORT.", 400);
            if (request.TieneEducacionSuperior == null && request.UltimoAnioSexto == 6)
                return OperationResult<bool>.IsFailed("PER_DPE_41", callingMethod, "Debe indicar si tiene educación superior.", 400);
            if (request.TieneEducacionSuperior == true && request.UniversidadesEducacionSuperior.Count == 0)
                return OperationResult<bool>.IsFailed("PER_DPE_42", callingMethod, "Debe seleccionar universidades si tiene educación superior.", 400);

            return OperationResult<bool>.Ok(true, callingMethod);
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
            encuesta.ComparPadresEncuestaIni = "NO";
            encuesta.ComparOtrosEncuestaIni = "NO";
            encuesta.ComparAmigoFamEncuestaIni = "NO";
            encuesta.ComparNadieEncuestaIni = "NO";
            encuesta.ComparAmigoPropEncuestaIni = "NO";

            switch (valor)
            {
                case 1:
                    encuesta.ComparPadresEncuestaIni = "SI";
                    break;
                case 2:
                    encuesta.ComparAmigoFamEncuestaIni = "SI";
                    break;
                case 3:
                    encuesta.ComparAmigoPropEncuestaIni = "SI";
                    break;
                case 4:
                    encuesta.ComparOtrosEncuestaIni = "SI";
                    break;
                case 5:
                    encuesta.ComparNadieEncuestaIni = "SI";
                    break;
            }
        }

    }
}
