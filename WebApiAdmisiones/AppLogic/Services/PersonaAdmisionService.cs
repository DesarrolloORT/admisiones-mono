using AppLogic.DevartDTOs;
using AppLogic.Helpers;
using AppLogic.Interfaces;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Services
{
    public class PersonaAdmisionService : IPersonaAdmisionService
    {
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IDbConnectionContext _dbConnectionContext;

        public PersonaAdmisionService(IUnitOfWorkFactory uowFactory, IDbConnectionContext dbConnectionContext)
        {
            _uowFactory = uowFactory;
            _dbConnectionContext = dbConnectionContext;
        }

        public OperationResult<DtoPersonaDevart> ObtenerPersona(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetPersonaWithRelated(codigoPersona);
            if (persona == null)
                return OperationResult<DtoPersonaDevart>.IsFailed("GEN_PER_01", nameof(ObtenerPersona), "Persona no encontrada.", 404);

            return OperationResult<DtoPersonaDevart>.Ok(persona.ToDto(), nameof(ObtenerPersona));
        }

        public OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerEncuestaInicialAdmision(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
                return OperationResult<DtoEncuestaIniAdmisionDevart>.IsFailed("GEN_DPI_01", nameof(ObtenerEncuestaInicialAdmision), "No se encontraron datos de pre-inscripción para la persona.", 204);

            return OperationResult<DtoEncuestaIniAdmisionDevart>.Ok(encuesta.ToDtoWithRelated(1), nameof(ObtenerEncuestaInicialAdmision));
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
    }
}
