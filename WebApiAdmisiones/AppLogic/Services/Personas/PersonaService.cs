using System.Globalization;
using AppLogic.Constants;
using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.IServices.Personas;
using AppLogic.Requests;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Utilities;

namespace AppLogic.Services.Personas
{
    public class PersonaService(
        IUnitOfWorkFactory uowFactory,
        ILdap ldap,
        IDbConnectionContext dbConnectionContext)
        : IPersonaService
    {
        public OperationResult<DtoDatosPersona> ObtenerDatosPersona(long codigoPersona)
        {
            using var uow = uowFactory.Create();
            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<DtoDatosPersona>.IsFailed(
                    "PER_DAT_01",
                    nameof(ObtenerDatosPersona),
                    "No se encontró la persona autenticada.",
                    404);
            }

            var identidadRestringida = PersonaIdentityHelper.TieneIdentidadRestringida(
                persona,
                uow.Inscriptos.TieneInscripcionActiva(codigoPersona));
            return OperationResult<DtoDatosPersona>.Ok(
                MapearDatosPersona(persona, identidadRestringida),
                nameof(ObtenerDatosPersona));
        }

        public OperationResult<bool> ActualizarDatosPersona(long codigoPersona, ActualizarDatosPersonaRequest request)
        {
            using var uow = uowFactory.Create();
            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_01",
                    nameof(ActualizarDatosPersona),
                    "No se encontró la persona autenticada.",
                    404);
            }

            var validacion = ValidarActualizarDatosPersona(request);
            if (!validacion.Success)
            {
                return validacion;
            }

            var identidadRestringida = PersonaIdentityHelper.TieneIdentidadRestringida(
                persona,
                uow.Inscriptos.TieneInscripcionActiva(codigoPersona));
            var validacionIdentidad = PersonaIdentityHelper.ValidarCambiosIdentidad(
                persona,
                request,
                identidadRestringida,
                nameof(ActualizarDatosPersona));
            if (!validacionIdentidad.Success)
            {
                return validacionIdentidad;
            }

            var ciudad = uow.Ciudads.GetByKey(request.CodigoPais, request.CodigoEstado, request.CodigoCiudad);
            if (ciudad is null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_05",
                    nameof(ActualizarDatosPersona),
                    "No existe la ciudad indicada para el país y estado enviados.",
                    400);
            }

            PersonaIdentityHelper.AplicarCambiosIdentidad(persona, request, identidadRestringida);
            persona.CodigoPais = request.CodigoPais;
            persona.CodigoEstado = request.CodigoEstado;
            persona.CodigoCiudad = request.CodigoCiudad;
            persona.Direccion = FormatearTextoCapitalizado(request.Direccion);
            persona.Telefono1 = request.Telefono1?.Trim();
            persona.Email = request.Mail?.Trim();

            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Personas.Update(persona);
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(ActualizarDatosPersona));
        }

        public async Task<OperationResult<object>> CambiarPasswordAsync(long codigoPersona, DtoCambiarPasswordRequest request)
        {
            try
            {
                if (request == null)
                {
                    return OperationResult<object>.IsFailed(
                        "CAM_PAS_01",
                        nameof(CambiarPasswordAsync),
                        "La solicitud es obligatoria.",
                        400);
                }

                var validacionPassword = Util.ValidarPassword(request.PasswordActual, request.PasswordNueva);
                if (!string.IsNullOrWhiteSpace(validacionPassword))
                {
                    return OperationResult<object>.IsFailed(
                        "CAM_PAS_02",
                        nameof(CambiarPasswordAsync),
                        validacionPassword,
                        400);
                }

                var cambioPassword = await ldap.CambiarPasswordAsync(
                    codigoPersona.ToString(CultureInfo.InvariantCulture),
                    request.PasswordActual,
                    request.PasswordNueva);

                if (!cambioPassword.Success)
                {
                    return OperationResult<object>.IsFailed(
                        cambioPassword.ErrorCode,
                        nameof(CambiarPasswordAsync),
                        cambioPassword.Message,
                        cambioPassword.HttpCode);
                }

                return OperationResult<object>.Ok(
                    "Se actualizó tu contraseña",
                    nameof(CambiarPasswordAsync));
            }
            catch (Exception ex)
            {
                return OperationResult<object>.IsFailed(
                    "CAM_PAS_99",
                    nameof(CambiarPasswordAsync),
                    $"Error al cambiar contraseña: {ex.Message}",
                    500,
                    default!);
            }
        }

        public OperationResult<byte[]> ObtenerFotoPersona(long codigoPersona)
        {
            using var uow = uowFactory.Create();
            var imagen = uow.Imagens.GetFotoByPersona(codigoPersona);

            if (imagen == null)
                return OperationResult<byte[]>.IsFailed("GEN_FA_01", nameof(ObtenerFotoPersona), "Foto no encontrada.", 404);

            if (imagen.BlobImagen == null || imagen.BlobImagen.Length == 0)
                return OperationResult<byte[]>.IsFailed("GEN_FA_02", nameof(ObtenerFotoPersona), "La foto no contiene imagen.", 404);

            return OperationResult<byte[]>.Ok(imagen.BlobImagen, nameof(ObtenerFotoPersona));
        }

        public OperationResult<byte[]> ObtenerDocumentoPersona(long codigoPersona, int tipo)
        {
            if (tipo != 1 && tipo != 2)
                return OperationResult<byte[]>.IsFailed("GEN_DA_01", nameof(ObtenerDocumentoPersona), "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).", 400);

            using var uow = uowFactory.Create();
            var imagenTemporal = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);

            if (imagenTemporal == null)
                return OperationResult<byte[]>.IsFailed("GEN_DA_02", nameof(ObtenerDocumentoPersona), "Documento no encontrado.", 404);

            if (imagenTemporal.FechaVtoDocumentoPersona.HasValue && imagenTemporal.FechaVtoDocumentoPersona.Value < DateTime.Now)
                return OperationResult<byte[]>.IsFailed("GEN_DA_03", nameof(ObtenerDocumentoPersona), "El documento se encuentra vencido.", 409);

            if (imagenTemporal.BlobImagen == null || imagenTemporal.BlobImagen.Length == 0)
                return OperationResult<byte[]>.IsFailed("GEN_DA_04", nameof(ObtenerDocumentoPersona), "El documento no contiene imagen.", 404);

            return OperationResult<byte[]>.Ok(imagenTemporal.BlobImagen, nameof(ObtenerDocumentoPersona));
        }

        public OperationResult<bool> SubirFotoPersona(long codigoPersona, byte[] fileContent, string fileName)
        {
            using var uow = uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed("GEN_SFA_01", nameof(SubirFotoPersona), PersonaConstants.PersonaNoEncontradaMessage, 404);
            }

            var imagenExistente = uow.Imagens.GetFotoByPersona(codigoPersona);

            if (imagenExistente is null)
            {
                var resultadoGuardado = GuardarFotoPersona(
                    persona,
                    dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN),
                    fileContent,
                    fileName);

                if (!resultadoGuardado.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoGuardado.ErrorCode,
                        nameof(SubirFotoPersona),
                        resultadoGuardado.Message,
                        resultadoGuardado.HttpCode);
                }

                uow.Imagens.Add(resultadoGuardado.Data!);
            }
            else
            {
                var resultadoModificacion = ModificarFotoPersona(imagenExistente, fileContent, fileName);
                if (!resultadoModificacion.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoModificacion.ErrorCode,
                        nameof(SubirFotoPersona),
                        resultadoModificacion.Message,
                        resultadoModificacion.HttpCode);
                }

                uow.Imagens.Update(imagenExistente);
            }

            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(SubirFotoPersona));
        }

        public OperationResult<bool> SubirDocumentoPersona(long codigoPersona, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            if (tipo != 1 && tipo != 2)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_SDA_01",
                    nameof(SubirDocumentoPersona),
                    "Tipo de documento inválido. Los valores admitidos son 1 (frente) y 2 (dorso).",
                    400);
            }

            using var uow = uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed("GEN_SDA_02", nameof(SubirDocumentoPersona), PersonaConstants.PersonaNoEncontradaMessage, 404);
            }

            var documentoExistente = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);

            if (documentoExistente is null)
            {
                var resultadoGuardado = GuardarDocumentoPersona(
                    codigoPersona,
                    dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL),
                    tipo,
                    fecha,
                    fileContent,
                    fileName);

                if (!resultadoGuardado.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoGuardado.ErrorCode,
                        nameof(SubirDocumentoPersona),
                        resultadoGuardado.Message,
                        resultadoGuardado.HttpCode);
                }

                uow.ImagenTemporals.Add(resultadoGuardado.Data!);
            }
            else
            {
                var resultadoModificacion = ModificarDocumentoPersona(documentoExistente, tipo, fecha, fileContent, fileName);
                if (!resultadoModificacion.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        resultadoModificacion.ErrorCode,
                        nameof(SubirDocumentoPersona),
                        resultadoModificacion.Message,
                        resultadoModificacion.HttpCode);
                }

                uow.ImagenTemporals.Update(documentoExistente);
            }

            persona.FechaVtoDocumentoPersona = fecha;
            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(SubirDocumentoPersona));
        }

        private static OperationResult<bool> ValidarActualizarDatosPersona(ActualizarDatosPersonaRequest request)
        {
            if (request is null)
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_02",
                    nameof(ActualizarDatosPersona),
                    "La solicitud es obligatoria.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(request.Direccion)
                || string.IsNullOrWhiteSpace(request.Mail)
                || string.IsNullOrWhiteSpace(request.VerificacionMail))
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_03",
                    nameof(ActualizarDatosPersona),
                    "Faltan parámetros obligatorios.",
                    400);
            }

            if (!string.Equals(request.Mail, request.VerificacionMail, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<bool>.IsFailed(
                    "PER_ADP_04",
                    nameof(ActualizarDatosPersona),
                    "El mail y su verificación no coinciden.",
                    400);
            }

            return OperationResult<bool>.Ok(true, nameof(ActualizarDatosPersona));
        }

        private static OperationResult<Imagen> GuardarFotoPersona(Persona persona, int idImagen, byte[] fileContent, string fileName)
        {
            if (fileContent == null || fileContent.Length == 0)
                return OperationResult<Imagen>.IsFailed("GEN_SFA_03", nameof(GuardarFotoPersona), "La imagen no puede estar vacía.", 400);

            var imageValidation = FileValidationHelper.ValidateImageFile(
                fileContent,
                fileName,
                nameof(GuardarFotoPersona));

            if (!imageValidation.Success)
            {
                return OperationResult<Imagen>.IsFailed(
                    "GEN_SFA_02",
                    nameof(GuardarFotoPersona),
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
                nameof(GuardarFotoPersona));
        }

        private static OperationResult<bool> ModificarFotoPersona(Imagen existing, byte[] fileContent, string fileName)
        {
            if (fileContent == null || fileContent.Length == 0)
                return OperationResult<bool>.IsFailed("GEN_SFA_04", nameof(ModificarFotoPersona), "La imagen no puede estar vacía.", 400);

            var imageValidation = FileValidationHelper.ValidateImageFile(
                fileContent,
                fileName,
                nameof(ModificarFotoPersona));

            if (!imageValidation.Success)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_SFA_05",
                    nameof(ModificarFotoPersona),
                    $"La imagen no es válida. Solo se permiten imágenes válidas en formato JPG, JPEG o PNG. Detalle: {imageValidation.Message}",
                    400);
            }

            var extension = ResolverExtensionPersistida(fileName, ".jpg");

            existing.NombreImagen = ConstruirNombrePersistido(existing.CodigoPersona ?? 0, 3, extension);
            existing.TipoImagen = "3";
            existing.BlobImagen = fileContent;
            return OperationResult<bool>.Ok(true, nameof(ModificarFotoPersona));
        }

        private static OperationResult<ImagenTemporal> GuardarDocumentoPersona(long codigoPersona, int idImagenTemporal, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            var validacion = FileValidationHelper.ValidateDocumentFile(fileContent, fileName, nameof(GuardarDocumentoPersona));
            if (!validacion.Success)
            {
                return OperationResult<ImagenTemporal>.IsFailed(validacion.ErrorCode, nameof(GuardarDocumentoPersona), validacion.Message, validacion.HttpCode);
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
                nameof(GuardarDocumentoPersona));
        }

        private static OperationResult<bool> ModificarDocumentoPersona(ImagenTemporal existing, int tipo, DateTime fecha, byte[] fileContent, string fileName)
        {
            var validacion = FileValidationHelper.ValidateDocumentFile(fileContent, fileName, nameof(ModificarDocumentoPersona));
            if (!validacion.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validacion.ErrorCode,
                    nameof(ModificarDocumentoPersona),
                    validacion.Message,
                    validacion.HttpCode);
            }

            var extension = ResolverExtensionPersistida(fileName, ".pdf");
            existing.NombreImagen = ConstruirNombrePersistido(existing.CodigoPersona ?? 0, tipo, extension);
            existing.TipoImagen = tipo.ToString();
            existing.BlobImagen = fileContent;
            existing.FechaVtoDocumentoPersona = fecha;
            return OperationResult<bool>.Ok(true, nameof(ModificarDocumentoPersona));
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

        private static DtoDatosPersona MapearDatosPersona(Persona persona, bool identidadRestringida)
        {
            var mail = persona.Email ?? string.Empty;
            return new DtoDatosPersona
            {
                TipoDocumento = persona.TipoDocumento?.Trim() ?? string.Empty,
                Documento = persona.Documento?.Trim() ?? string.Empty,
                PrimerNombre = persona.PrimerNombre?.Trim() ?? string.Empty,
                SegundoNombre = persona.SegundoNombre?.Trim() ?? string.Empty,
                PrimerApellido = persona.PrimerApellido?.Trim() ?? string.Empty,
                SegundoApellido = persona.SegundoApellido?.Trim() ?? string.Empty,
                FechaNacimiento = persona.FechaNacimiento ?? default,
                Sexo = persona.Sexo?.Trim() ?? string.Empty,
                CodigoPais = persona.CodigoPais ?? 0,
                CodigoEstado = persona.CodigoEstado ?? 0,
                CodigoCiudad = persona.CodigoCiudad ?? 0,
                Direccion = persona.Direccion?.Trim() ?? string.Empty,
                Telefono1 = persona.Telefono1?.Trim() ?? string.Empty,
                Mail = mail,
                VerificacionMail = mail,
                IdentidadRestringida = identidadRestringida
            };
        }

        private static string FormatearTextoCapitalizado(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            var texto = string.Join(" ", valor.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(texto.ToLower(CultureInfo.CurrentCulture));
        }
    }
}
