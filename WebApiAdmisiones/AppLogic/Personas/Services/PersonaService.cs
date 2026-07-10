using AppLogic.Dtos.Autenticacion;
using AppLogic.Personas.Constants;
using AppLogic.Personas.Helpers;
using AppLogic.Personas.Interfaces;
using AppLogic.Personas.Requests;
using AppLogic.Personas.Responses;
using AppLogic.Personas.Validators;
using AppLogic.DevartDTOs;
using AppLogic.Helpers.ValidationHelpers;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Utilities;

namespace AppLogic.Personas.Services
{
    public class PersonaService(
        IUnitOfWorkFactory uowFactory,
        ILdap ldap,
        IDbConnectionContext dbConnectionContext,
        ILogger<PersonaService>? logger = null)
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

        public OperationResult<bool> ActualizarDatosPersona(long codigoPersona, DtoActualizarDatosPersonaRequest request)
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
            persona.Direccion = DocumentUtils.FormatearTextoCapitalizado(request.Direccion);
            persona.Telefono1 = DocumentUtils.NormalizarOpcional(request.Telefono1);
            persona.Email = DocumentUtils.NormalizarOpcional(request.Mail);

            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Personas.Update(persona);
            uow.Save();

            return OperationResult<bool>.Ok(true, nameof(ActualizarDatosPersona));
        }

        //Valida un telefono para front
        public OperationResult<bool> EsTelefonoValidoFront(DtoTelefono telefonoValidar, bool telefono1)
        {
            // Retorno temprano si el teléfono está vacío o nulo
            if (string.IsNullOrWhiteSpace(telefonoValidar.TelefonoSimple))
            {
                return OperationResult<bool>.Ok(false, nameof(EsTelefonoValidoFront));
            }

            // Validar el teléfono
            var telefonoValido = PhoneVerification.Validar(telefonoValidar.TelefonoSimple, telefonoValidar.Iso2, telefono1);
            bool telValido = telefonoValido != null && telefonoValido.TelefonoValido;

            return OperationResult<bool>.Ok(telValido, nameof(EsTelefonoValidoFront));
        }

        public OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripciones(long codigoPersona)
        {

            using var uow = uowFactory.Create();
            var dtos = uow.VdInscripcionesFresco1y2s
                .GetInscripcionesFrescoHabilitadas(codigoPersona)
                .ToDtos();

            var inscripciones3y4 = uow.VdInscripcionesFresco3y4s
                .GetInscripcionesFrescoHabilitadas(codigoPersona)
                .Select(MapearInscripcionFresco3y4);

            dtos.AddRange(inscripciones3y4);

            return OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>.Ok(dtos, nameof(ObtenerMisInscripciones));
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
                logger?.LogError(ex, "Error inesperado en {Metodo}", nameof(CambiarPasswordAsync));
                return OperationResult<object>.IsFailed(
                    "CAM_PAS_99",
                    nameof(CambiarPasswordAsync),
                    "Error al cambiar contraseña.",
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

        public OperationResult<DtoDocumentoPersonaResponse> ObtenerDocumentoPersona(long codigoPersona)
        {
            using var uow = uowFactory.Create();
            var persona = uow.Personas.GetByKey(codigoPersona);
            var fechaVencimientoDocumentoDefinitivo = persona?.FechaVtoDocumentoPersona;
            var frente = DocumentoIdentidadPersonaService.ObtenerDocumentoOpcionalParaConsulta(
                uow,
                codigoPersona,
                PersonaConstants.DocumentoPersona.Frente,
                fechaVencimientoDocumentoDefinitivo,
                nameof(ObtenerDocumentoPersona));
            if (!frente.Success)
            {
                return OperationResult<DtoDocumentoPersonaResponse>.IsFailed(
                    frente.ErrorCode,
                    nameof(ObtenerDocumentoPersona),
                    frente.Message,
                    frente.HttpCode);
            }

            var dorso = DocumentoIdentidadPersonaService.ObtenerDocumentoOpcionalParaConsulta(
                uow,
                codigoPersona,
                PersonaConstants.DocumentoPersona.Dorso,
                fechaVencimientoDocumentoDefinitivo,
                nameof(ObtenerDocumentoPersona));
            if (!dorso.Success)
            {
                return OperationResult<DtoDocumentoPersonaResponse>.IsFailed(
                    dorso.ErrorCode,
                    nameof(ObtenerDocumentoPersona),
                    dorso.Message,
                    dorso.HttpCode);
            }

            if (frente.Data is null && dorso.Data is null)
            {
                return OperationResult<DtoDocumentoPersonaResponse>.IsFailed(
                    "GEN_DA_02",
                    nameof(ObtenerDocumentoPersona),
                    "Documento no encontrado.",
                    404);
            }

            return OperationResult<DtoDocumentoPersonaResponse>.Ok(
                new DtoDocumentoPersonaResponse
                {
                    Frente = frente.Data?.Archivo,
                    Dorso = dorso.Data?.Archivo,
                    FechaVencimiento = frente.Data?.FechaVencimiento ?? dorso.Data?.FechaVencimiento
                },
                nameof(ObtenerDocumentoPersona));

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

        public OperationResult<bool> SubirDocumentoPersona(
            long codigoPersona,
            DateTime fecha,
            DtoDocumentoPersonaArchivo frente,
            DtoDocumentoPersonaArchivo dorso)
        {
            var validacionFrente = ValidarArchivoDocumentoPersona(frente, "frente");
            if (!validacionFrente.Success)
            {
                return validacionFrente;
            }

            var validacionDorso = ValidarArchivoDocumentoPersona(dorso, "dorso");
            if (!validacionDorso.Success)
            {
                return validacionDorso;
            }

            var validacionFecha = DocumentoIdentidadPersonaService.ValidarFechaVencimientoDocumento(
                fecha,
                nameof(SubirDocumentoPersona),
                "GEN_SDA_05");
            if (!validacionFecha.Success)
            {
                return validacionFecha;
            }

            using var uow = uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona is null)
            {
                return OperationResult<bool>.IsFailed("GEN_SDA_02", nameof(SubirDocumentoPersona), PersonaConstants.PersonaNoEncontradaMessage, 404);
            }

            var resultadoFrente = GuardarOActualizarDocumentoTemporal(
                uow,
                codigoPersona,
                PersonaConstants.DocumentoPersona.Frente,
                fecha,
                frente);
            if (!resultadoFrente.Success)
            {
                return resultadoFrente;
            }

            var resultadoDorso = GuardarOActualizarDocumentoTemporal(
                uow,
                codigoPersona,
                PersonaConstants.DocumentoPersona.Dorso,
                fecha,
                dorso);
            if (!resultadoDorso.Success)
            {
                return resultadoDorso;
            }

            persona.FechaVtoDocumentoPersona = fecha;
            PersonaValidation.AuditarPersona(persona, codigoPersona, uow, false);
            uow.Save();
            return OperationResult<bool>.Ok(true, nameof(SubirDocumentoPersona));
        }

        private OperationResult<bool> GuardarOActualizarDocumentoTemporal(
            IUnitOfWork uow,
            long codigoPersona,
            int tipo,
            DateTime fecha,
            DtoDocumentoPersonaArchivo documento)
        {
            var fileContent = documento.Archivo ?? Array.Empty<byte>();
            var fileName = documento.NombreArchivo ?? string.Empty;
            var documentoExistente = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(codigoPersona, tipo);

            if (documentoExistente is null)
            {
                var resultadoGuardado = DocumentoIdentidadPersonaService.CrearDocumentoTemporal(
                    codigoPersona,
                    dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL),
                    tipo,
                    fecha,
                    fileContent,
                    fileName,
                    nameof(SubirDocumentoPersona));

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
                var resultadoModificacion = DocumentoIdentidadPersonaService.ActualizarDocumentoTemporal(
                    documentoExistente,
                    tipo,
                    fecha,
                    fileContent,
                    fileName,
                    nameof(SubirDocumentoPersona));
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

            return OperationResult<bool>.Ok(true, nameof(SubirDocumentoPersona));
        }

        private static OperationResult<bool> ValidarArchivoDocumentoPersona(DtoDocumentoPersonaArchivo? documento, string lado)
        {
            if (documento?.Archivo == null || documento.Archivo.Length == 0)
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_SDA_03",
                    nameof(SubirDocumentoPersona),
                    $"Debe enviar el documento de identidad ({lado}).",
                    400);
            }

            if (string.IsNullOrWhiteSpace(documento.NombreArchivo))
            {
                return OperationResult<bool>.IsFailed(
                    "GEN_SDA_04",
                    nameof(SubirDocumentoPersona),
                    $"Debe enviar el nombre del archivo del documento de identidad ({lado}).",
                    400);
            }

            var validacion = FileValidationHelper.ValidateIdentityDocumentFile(
                documento.Archivo,
                documento.NombreArchivo,
                nameof(SubirDocumentoPersona));
            if (!validacion.Success)
            {
                return OperationResult<bool>.IsFailed(
                    validacion.ErrorCode,
                    nameof(SubirDocumentoPersona),
                    validacion.Message,
                    validacion.HttpCode);
            }

            return OperationResult<bool>.Ok(true, nameof(SubirDocumentoPersona));
        }

        private static OperationResult<bool> ValidarActualizarDatosPersona(DtoActualizarDatosPersonaRequest request)
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

            var imageValidation = FileValidator.ValidateImageFile(
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

            var imageValidation = FileValidator.ValidateImageFile(
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
                TipoDocumento = DocumentUtils.Normalizar(persona.TipoDocumento),
                Documento = DocumentUtils.Normalizar(persona.Documento),
                PrimerNombre = DocumentUtils.Normalizar(persona.PrimerNombre),
                SegundoNombre = DocumentUtils.Normalizar(persona.SegundoNombre),
                PrimerApellido = DocumentUtils.Normalizar(persona.PrimerApellido),
                SegundoApellido = DocumentUtils.Normalizar(persona.SegundoApellido),
                FechaNacimiento = persona.FechaNacimiento ?? default,
                Sexo = DocumentUtils.Normalizar(persona.Sexo),
                CodigoPais = persona.CodigoPais ?? 0,
                CodigoEstado = persona.CodigoEstado ?? 0,
                CodigoCiudad = persona.CodigoCiudad ?? 0,
                Direccion = DocumentUtils.Normalizar(persona.Direccion),
                Telefono1 = DocumentUtils.Normalizar(persona.Telefono1),
                Mail = mail,
                VerificacionMail = mail,
                IdentidadRestringida = identidadRestringida
            };
        }

        private static DtoVdInscripcionesFresco1y2Devart MapearInscripcionFresco3y4(VdInscripcionesFresco3y4 source)
        {
            return new DtoVdInscripcionesFresco1y2Devart
            {
                IdTurno = source.IdTurno,
                IdProducto = source.IdProducto,
                IdComienzo = source.IdComienzo,
                FechaInicioComienzo = source.FechaInicioComienzo,
                NombreExtensoProducto = source.NombreExtensoProducto,
                IdNivelProducto = source.IdNivelProducto,
                NombreComienzo = source.NombreComienzo,
                NombreTurno = source.NombreTurno,
                IdProceso = source.IdProceso,
                FechaReferencia = source.FechaReferencia,
                IdInscripto = source.IdInscripto,
                EstadoInscripcion = source.EstadoInscripcion,
            };
        }
    }
}
