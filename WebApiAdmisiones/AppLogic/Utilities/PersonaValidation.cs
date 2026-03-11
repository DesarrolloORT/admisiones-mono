using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Utilities
{
    /// <summary>
    /// Servicios estáticos para validación y auditoría de datos de persona
    /// </summary>
    public static class PersonaValidation
    {
        public static void AuditarPersona(Persona persona, long codigoPersona, IUnitOfWork uow, bool esConfirmacion)
        {
            persona.UsuarioModifFdp = codigoPersona.ToString();
            persona.UsuarioUltimaActualizacion = uow.ObtenerDbUserId();
            persona.FechaUltimaActualizacion = DateTime.Now;
            persona.HoraUltimaActualizacion = DateTime.Now.ToString("HH:mm:ss");
            if (esConfirmacion)
            {
                // Confirma los datos personales
                persona.FechaConfDatosPersona = DateTime.Now;
                persona.HoraConfDatosPersona = DateTime.Now.ToString("HH:mm:ss");
                persona.UsuarioConfDatosPersona = uow.ObtenerDbUserId();
            }
        }

        public static OperationResult<bool> ValidarDatosObligatoriosPersona(Persona persona, string callingMethod)
        {
            var vBasicos = ValidarBasicosCompletos(persona);
            if (!vBasicos.Success) return OperationResult<bool>.IsFailed(vBasicos.ErrorCode, callingMethod, vBasicos.Message, 400);

            var vDoc = ValidarDocumentoCompleto(persona);
            if (!vDoc.Success) return OperationResult<bool>.IsFailed(vDoc.ErrorCode, callingMethod, vDoc.Message, 400);

            var vDir = ValidarDireccionCompleta(persona);
            if (!vDir.Success) return OperationResult<bool>.IsFailed(vDir.ErrorCode, callingMethod, vDir.Message, 400);

            var vContacto = ValidarContactoCompleto(persona);
            if (!vContacto.Success) return OperationResult<bool>.IsFailed(vContacto.ErrorCode, callingMethod, vContacto.Message, 400);

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        // Validaciones para confirmación de datos personales
        private static OperationResult<bool> ValidarBasicosCompletos(Persona persona)
        {
            if (persona.CodigoPaisNacimiento == null)
                return OperationResult<bool>.IsFailed("DP_ACDP_BAS_01", nameof(ValidarBasicosCompletos), "Falta país de nacimiento.", 400);
            if (string.IsNullOrWhiteSpace(persona.NacionalidadPersona) && string.Equals(persona.FuncionarioActivoPersona, "SI", StringComparison.OrdinalIgnoreCase))
                return OperationResult<bool>.IsFailed("DP_ACDP_BAS_02", nameof(ValidarBasicosCompletos), "Falta nacionalidad.", 400);
            return OperationResult<bool>.Ok(true, nameof(ValidarBasicosCompletos));
        }
        
        private static OperationResult<bool> ValidarDocumentoCompleto(Persona persona)
        {
            if (!persona.FechaVtoDocumentoPersona.HasValue || persona.FechaVtoDocumentoPersona.Value <= DateTime.MinValue)
                return OperationResult<bool>.IsFailed("DP_ACDP_DOC_01", nameof(ValidarDocumentoCompleto), "Falta fecha de vencimiento de documento.", 400);
            return OperationResult<bool>.Ok(true, nameof(ValidarDocumentoCompleto));
        }
        
        private static OperationResult<bool> ValidarDireccionCompleta(Persona persona)
        {
            if (persona.CodigoPais == null || persona.CodigoPais <= 0)
                return OperationResult<bool>.IsFailed("DP_ACDP_DIR_01", nameof(ValidarDireccionCompleta), "Falta país de residencia.", 400);
            if (persona.CodigoEstado == null || persona.CodigoEstado <= 0)
                return OperationResult<bool>.IsFailed("DP_ACDP_DIR_02", nameof(ValidarDireccionCompleta), "Falta estado/provincia.", 400);
            if (persona.CodigoCiudad == null || persona.CodigoCiudad <= 0)
                return OperationResult<bool>.IsFailed("DP_ACDP_DIR_03", nameof(ValidarDireccionCompleta), "Falta ciudad.", 400);
            if (string.IsNullOrWhiteSpace(persona.Direccion))
                return OperationResult<bool>.IsFailed("DP_ACDP_DIR_04", nameof(ValidarDireccionCompleta), "Falta domicilio.", 400);
            return OperationResult<bool>.Ok(true, nameof(ValidarDireccionCompleta));
        }
        
        private static OperationResult<bool> ValidarContactoCompleto(Persona persona)
        {
            if (string.IsNullOrWhiteSpace(persona.Email))
                return OperationResult<bool>.IsFailed("DP_ACDP_CON_01", nameof(ValidarContactoCompleto), "Falta email.", 400);
            if (string.IsNullOrWhiteSpace(persona.Telefono1))
                return OperationResult<bool>.IsFailed("DP_ACDP_CON_02", nameof(ValidarContactoCompleto), "Falta teléfono principal.", 400);
            if (persona.IdCaracteristicaPaisTel1 <= 0)
                return OperationResult<bool>.IsFailed("DP_ACDP_CON_03", nameof(ValidarContactoCompleto), "Falta característica país teléfono 1.", 400);
            // Tel2 opcional, pero si existe característica sin teléfono podría ser inconsistente: no se valida estrictamente
            return OperationResult<bool>.Ok(true, nameof(ValidarContactoCompleto));
        }
    }
}