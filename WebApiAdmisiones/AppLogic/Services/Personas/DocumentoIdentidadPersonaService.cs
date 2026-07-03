using AppLogic.Dtos.Personas;
using AppLogic.Dtos.Registro;
using AppLogic.Constants;
using AppLogic.Helpers.ValidationHelpers;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Services.Personas
{
    public static class DocumentoIdentidadPersonaService
    {
        private const string TipoImagenDocumentoIdentidadPersistido = "1";

        public static bool EsTipoDocumentoValido(int tipo)
        {
            return tipo == PersonaConstants.DocumentoPersona.Frente
                || tipo == PersonaConstants.DocumentoPersona.Dorso;
        }

        public static OperationResult<byte[]> ObtenerDocumentoParaConsulta(
            IUnitOfWork uow,
            long codigoPersona,
            int tipo,
            string methodName)
        {
            if (!EsTipoDocumentoValido(tipo))
            {
                return OperationResult<byte[]>.IsFailed(
                    "GEN_DA_01",
                    methodName,
                    "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).",
                    400);
            }

            var temporal = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);
            if (temporal is not null)
            {
                return ValidarDocumentoTemporalParaConsulta(temporal, methodName);
            }

            var definitivo = uow.Imagens.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);
            if (definitivo is null)
            {
                return OperationResult<byte[]>.IsFailed(
                    "GEN_DA_02",
                    methodName,
                    "Documento no encontrado.",
                    404);
            }

            var persona = uow.Personas.GetByKey(codigoPersona);
            return ValidarDocumentoDefinitivoParaConsulta(definitivo, persona?.FechaVtoDocumentoPersona, methodName);
        }

        public static OperationResult<DtoDocumentoPersonaConsulta?> ObtenerDocumentoOpcionalParaConsulta(
            IUnitOfWork uow,
            long codigoPersona,
            int tipo,
            DateTime? fechaVencimientoDocumentoDefinitivo,
            string methodName)
        {
            if (!EsTipoDocumentoValido(tipo))
            {
                return OperationResult<DtoDocumentoPersonaConsulta?>.IsFailed(
                    "GEN_DA_01",
                    methodName,
                    "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).",
                    400);
            }

            var temporal = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);
            if (temporal is not null)
            {
                var validacionTemporal = ValidarDocumentoTemporalParaConsulta(temporal, methodName);
                if (!validacionTemporal.Success)
                {
                    return OperationResult<DtoDocumentoPersonaConsulta?>.IsFailed(
                        validacionTemporal.ErrorCode,
                        methodName,
                        validacionTemporal.Message,
                        validacionTemporal.HttpCode);
                }

                return OperationResult<DtoDocumentoPersonaConsulta?>.Ok(
                    new DtoDocumentoPersonaConsulta
                    {
                        Archivo = new DtoDocumentoPersonaArchivo
                        {
                            NombreArchivo = temporal.NombreImagen,
                            Archivo = validacionTemporal.Data
                        },
                        FechaVencimiento = temporal.FechaVtoDocumentoPersona
                    },
                    methodName);
            }

            var definitivo = uow.Imagens.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);
            if (definitivo is null)
            {
                return OperationResult<DtoDocumentoPersonaConsulta?>.Ok(null, methodName);
            }

            var validacionDefinitivo = ValidarDocumentoDefinitivoParaConsulta(
                definitivo,
                fechaVencimientoDocumentoDefinitivo,
                methodName);
            if (!validacionDefinitivo.Success)
            {
                return OperationResult<DtoDocumentoPersonaConsulta?>.IsFailed(
                    validacionDefinitivo.ErrorCode,
                    methodName,
                    validacionDefinitivo.Message,
                    validacionDefinitivo.HttpCode);
            }

            return OperationResult<DtoDocumentoPersonaConsulta?>.Ok(
                new DtoDocumentoPersonaConsulta
                {
                    Archivo = new DtoDocumentoPersonaArchivo
                    {
                        NombreArchivo = definitivo.NombreImagen,
                        Archivo = validacionDefinitivo.Data
                    },
                    FechaVencimiento = fechaVencimientoDocumentoDefinitivo
                },
                methodName);
        }

        public static OperationResult<bool> ValidarDocumentosIdentidadParaConfirmacion(
            IUnitOfWork uow,
            Persona persona,
            string methodName)
        {
            var temporales = ValidarParTemporalParaConfirmacion(uow, persona.CodigoPersona, methodName);
            if (temporales.Success)
            {
                return temporales;
            }

            var definitivos = ValidarParDefinitivoParaConfirmacion(uow, persona, methodName);
            if (definitivos.Success)
            {
                return definitivos;
            }

            if (temporales.ErrorCode != "INS_CPI_09")
            {
                return temporales;
            }

            if (definitivos.ErrorCode != "INS_CPI_09" || EsErrorDorso(definitivos))
            {
                return definitivos;
            }

            return temporales;
        }

        public static OperationResult<ImagenTemporal> CrearDocumentoTemporal(
            long codigoPersona,
            int idImagenTemporal,
            int tipo,
            DateTime fecha,
            byte[] fileContent,
            string fileName,
            string methodName)
        {
            var validacion = FileValidationHelper.ValidateIdentityDocumentFile(fileContent, fileName, methodName);
            if (!validacion.Success)
            {
                return OperationResult<ImagenTemporal>.IsFailed(
                    validacion.ErrorCode,
                    methodName,
                    validacion.Message,
                    validacion.HttpCode);
            }

            return OperationResult<ImagenTemporal>.Ok(
                new ImagenTemporal
                {
                    IdImagenTemporal = idImagenTemporal,
                    CodigoPersona = codigoPersona,
                    NombreImagen = ConstruirNombrePersistido(
                        codigoPersona,
                        tipo,
                        ".jpg"),
                    TipoImagen = TipoImagenDocumentoIdentidadPersistido,
                    BlobImagen = fileContent,
                    FechaVtoDocumentoPersona = fecha
                },
                methodName);
        }

        public static OperationResult<bool> ActualizarDocumentoTemporal(
            ImagenTemporal existente,
            int tipo,
            DateTime fecha,
            byte[] fileContent,
            string fileName,
            string methodName)
        {
            var validacion = FileValidationHelper.ValidateIdentityDocumentFile(fileContent, fileName, methodName);
            if (!validacion.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validacion.ErrorCode,
                    methodName,
                    validacion.Message,
                    validacion.HttpCode);
            }

            existente.NombreImagen = ConstruirNombrePersistido(
                existente.CodigoPersona ?? 0,
                tipo,
                ".jpg");
            existente.TipoImagen = TipoImagenDocumentoIdentidadPersistido;
            existente.BlobImagen = fileContent;
            existente.FechaVtoDocumentoPersona = fecha;

            return OperationResult<bool>.Ok(true, methodName);
        }

        public static OperationResult<bool> ValidarImagenesDocumentoReconocido(
            DtoRegistroDocumentoImagenesTemporales? imagenes,
            string methodName)
        {
            if (imagenes is null)
            {
                return OperationResult<bool>.Ok(true, methodName);
            }

            var documento = imagenes.DocumentoFrente;
            var documentValidation = FileValidationHelper.ValidateIdentityDocumentFile(
                documento.Archivo,
                ResolverNombreArchivo(documento.NombreArchivo, "documento.pdf"),
                methodName);
            if (!documentValidation.Success)
            {
                return documentValidation;
            }

            if (imagenes.CaraPersona is null)
            {
                return OperationResult<bool>.Ok(true, methodName);
            }

            var cara = imagenes.CaraPersona;
            return FileValidationHelper.ValidateImageFile(
                cara.Archivo,
                ResolverNombreArchivo(cara.NombreArchivo, "cara.jpg"),
                methodName);
        }

        public static void GuardarImagenesDocumentoReconocido(
            IUnitOfWork uow,
            IDbConnectionContext dbConnectionContext,
            Persona persona,
            DtoRegistroDocumentoImagenesTemporales? imagenes)
        {
            if (imagenes is null)
            {
                return;
            }

            var documento = imagenes.DocumentoFrente;
            var fechaVencimiento = imagenes.FechaVencimiento ?? DateTime.Today.AddYears(1);
            var documentoExistente = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(
                persona.CodigoPersona,
                PersonaConstants.DocumentoPersona.Frente);

            if (documentoExistente is null)
            {
                uow.ImagenTemporals.Add(new ImagenTemporal
                {
                    IdImagenTemporal = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL),
                    CodigoPersona = persona.CodigoPersona,
                    NombreImagen = ConstruirNombrePersistido(
                        persona.CodigoPersona,
                        PersonaConstants.DocumentoPersona.Frente,
                        ".jpg"),
                    TipoImagen = TipoImagenDocumentoIdentidadPersistido,
                    BlobImagen = documento.Archivo,
                    FechaVtoDocumentoPersona = fechaVencimiento
                });
            }
            else
            {
                documentoExistente.NombreImagen = ConstruirNombrePersistido(
                    persona.CodigoPersona,
                    PersonaConstants.DocumentoPersona.Frente,
                    ".jpg");
                documentoExistente.TipoImagen = TipoImagenDocumentoIdentidadPersistido;
                documentoExistente.BlobImagen = documento.Archivo;
                documentoExistente.FechaVtoDocumentoPersona = fechaVencimiento;
                uow.ImagenTemporals.Update(documentoExistente);
            }

            persona.FechaVtoDocumentoPersona = fechaVencimiento;

            if (imagenes.CaraPersona is null)
            {
                return;
            }

            var cara = imagenes.CaraPersona;
            var fotoExistente = uow.Imagens.GetFotoByPersona(persona.CodigoPersona);
            if (fotoExistente is null)
            {
                uow.Imagens.Add(new Imagen
                {
                    IdImagen = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN),
                    CodigoPersona = persona.CodigoPersona,
                    NombreImagen = ConstruirNombrePersistido(
                        persona.CodigoPersona,
                        PersonaConstants.TipoImagenFoto,
                        ResolverExtensionPersistida(cara.NombreArchivo, ".jpg")),
                    TipoImagen = PersonaConstants.TipoImagenFoto.ToString(),
                    BlobImagen = cara.Archivo
                });
            }
            else
            {
                fotoExistente.NombreImagen = ConstruirNombrePersistido(
                    persona.CodigoPersona,
                    PersonaConstants.TipoImagenFoto,
                    ResolverExtensionPersistida(cara.NombreArchivo, ".jpg"));
                fotoExistente.TipoImagen = PersonaConstants.TipoImagenFoto.ToString();
                fotoExistente.BlobImagen = cara.Archivo;
                uow.Imagens.Update(fotoExistente);
            }
        }

        public static string ResolverExtensionPersistida(string? fileName, string defaultExtension)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return string.IsNullOrWhiteSpace(extension) ? defaultExtension : extension;
        }

        public static string ConstruirNombrePersistido(long codigoPersona, int tipoImagen, string extension)
        {
            return $"{codigoPersona}_{tipoImagen}{extension}";
        }

        private static OperationResult<bool> ValidarParTemporalParaConfirmacion(
            IUnitOfWork uow,
            long codigoPersona,
            string methodName)
        {
            var frente = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(
                codigoPersona,
                PersonaConstants.DocumentoPersona.Frente);
            var validacionFrente = ValidarDocumentoTemporalParaConfirmacion(frente, "frente", methodName);
            if (!validacionFrente.Success)
            {
                return validacionFrente;
            }

            var dorso = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(
                codigoPersona,
                PersonaConstants.DocumentoPersona.Dorso);
            var validacionDorso = ValidarDocumentoTemporalParaConfirmacion(dorso, "dorso", methodName);
            if (!validacionDorso.Success)
            {
                return validacionDorso;
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarParDefinitivoParaConfirmacion(
            IUnitOfWork uow,
            Persona persona,
            string methodName)
        {
            var frente = uow.Imagens.GetDocumentoByPersonaAndTipo(
                persona.CodigoPersona,
                PersonaConstants.DocumentoPersona.Frente);
            var validacionFrente = ValidarDocumentoDefinitivoParaConfirmacion(
                frente,
                persona.FechaVtoDocumentoPersona,
                "frente",
                methodName);
            if (!validacionFrente.Success)
            {
                return validacionFrente;
            }

            var dorso = uow.Imagens.GetDocumentoByPersonaAndTipo(
                persona.CodigoPersona,
                PersonaConstants.DocumentoPersona.Dorso);
            var validacionDorso = ValidarDocumentoDefinitivoParaConfirmacion(
                dorso,
                persona.FechaVtoDocumentoPersona,
                "dorso",
                methodName);
            if (!validacionDorso.Success)
            {
                return validacionDorso;
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<byte[]> ValidarDocumentoTemporalParaConsulta(
            ImagenTemporal documento,
            string methodName)
        {
            if (documento.FechaVtoDocumentoPersona.HasValue
                && documento.FechaVtoDocumentoPersona.Value.Date < DateTime.Today)
            {
                return OperationResult<byte[]>.IsFailed(
                    "GEN_DA_03",
                    methodName,
                    "El documento se encuentra vencido.",
                    409);
            }

            if (documento.BlobImagen == null || documento.BlobImagen.Length == 0)
            {
                return OperationResult<byte[]>.IsFailed(
                    "GEN_DA_04",
                    methodName,
                    "El documento no contiene imagen.",
                    404);
            }

            return OperationResult<byte[]>.Ok(documento.BlobImagen, methodName);
        }

        private static OperationResult<byte[]> ValidarDocumentoDefinitivoParaConsulta(
            Imagen documento,
            DateTime? fechaVencimiento,
            string methodName)
        {
            if (fechaVencimiento.HasValue && fechaVencimiento.Value.Date < DateTime.Today)
            {
                return OperationResult<byte[]>.IsFailed(
                    "GEN_DA_03",
                    methodName,
                    "El documento se encuentra vencido.",
                    409);
            }

            if (documento.BlobImagen == null || documento.BlobImagen.Length == 0)
            {
                return OperationResult<byte[]>.IsFailed(
                    "GEN_DA_04",
                    methodName,
                    "El documento no contiene imagen.",
                    404);
            }

            return OperationResult<byte[]>.Ok(documento.BlobImagen, methodName);
        }

        private static OperationResult<bool> ValidarDocumentoTemporalParaConfirmacion(
            ImagenTemporal? documento,
            string lado,
            string methodName)
        {
            if (documento == null)
            {
                return DocumentoFaltante(lado, methodName);
            }

            if (documento.FechaVtoDocumentoPersona.HasValue
                && documento.FechaVtoDocumentoPersona.Value.Date < DateTime.Today)
            {
                return DocumentoVencido(methodName);
            }

            if (documento.BlobImagen == null || documento.BlobImagen.Length == 0)
            {
                return DocumentoSinImagen(lado, methodName);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarDocumentoDefinitivoParaConfirmacion(
            Imagen? documento,
            DateTime? fechaVencimiento,
            string lado,
            string methodName)
        {
            if (documento == null)
            {
                return DocumentoFaltante(lado, methodName);
            }

            if (fechaVencimiento.HasValue && fechaVencimiento.Value.Date < DateTime.Today)
            {
                return DocumentoVencido(methodName);
            }

            if (documento.BlobImagen == null || documento.BlobImagen.Length == 0)
            {
                return DocumentoSinImagen(lado, methodName);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> DocumentoFaltante(string lado, string methodName)
        {
            return OperationResult<bool>.IsFailed(
                "INS_CPI_09",
                methodName,
                $"Debe subir el documento de identidad ({lado}).",
                404);
        }

        private static OperationResult<bool> DocumentoVencido(string methodName)
        {
            return OperationResult<bool>.IsFailed(
                "INS_CPI_10",
                methodName,
                "El documento de identidad se encuentra vencido.",
                409);
        }

        private static OperationResult<bool> DocumentoSinImagen(string lado, string methodName)
        {
            return OperationResult<bool>.IsFailed(
                "INS_CPI_11",
                methodName,
                $"El documento de identidad ({lado}) no contiene imagen.",
                404);
        }

        private static bool EsErrorDorso(OperationResult<bool> result)
        {
            return result.Message?.Contains("(dorso)", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static string ResolverNombreArchivo(string? fileName, string defaultFileName)
        {
            return string.IsNullOrWhiteSpace(fileName) ? defaultFileName : fileName;
        }
    }
}
