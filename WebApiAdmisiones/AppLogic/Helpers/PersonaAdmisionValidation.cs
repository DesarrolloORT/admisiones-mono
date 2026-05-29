using AppLogic.Constants;
using AppLogic.Requests;
using Utilities;

namespace AppLogic.Helpers
{
    public static class PersonaAdmisionValidation
    {
        public static OperationResult<bool> ValidarActualizarPersonaRequest(
            ActualizarPersonaRequest request,
            string callingMethod,
            DateTime fechaMinimaNacimiento)
        {
            if (request.FechaNacimiento <= fechaMinimaNacimiento)
            {
                return OperationResult<bool>.IsFailed("PER_AP_03", callingMethod, "Fecha de nacimiento incorrecta.", 400);
            }

            if (string.IsNullOrWhiteSpace(request.PrimerApellido)
                || string.IsNullOrWhiteSpace(request.PrimerNombre)
                || string.IsNullOrWhiteSpace(request.Mail)
                || string.IsNullOrWhiteSpace(request.VerificacionMail)
                || string.IsNullOrWhiteSpace(request.Direccion))
            {
                return OperationResult<bool>.IsFailed("PER_AP_04", callingMethod, "Faltan par\u00e1metros obligatorios.", 400);
            }

            if (!string.Equals(request.Mail, request.VerificacionMail, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<bool>.IsFailed("PER_AP_05", callingMethod, "El mail y su verificaci\u00f3n no coinciden.", 400);
            }

            if (!string.IsNullOrWhiteSpace(request.Sexo)
                && request.Sexo.Trim() is not (CommonConstants.Sexo.Masculino or CommonConstants.Sexo.Femenino))
            {
                return OperationResult<bool>.IsFailed("PER_AP_06", callingMethod, "Sexo inv\u00e1lido.", 400);
            }

            if (request.PrimerNombre.Trim().Length <= 1)
            {
                return OperationResult<bool>.IsFailed("PER_AP_07", callingMethod, "Primer nombre inv\u00e1lido.", 400);
            }

            if (request.PrimerApellido.Trim().Length <= 1)
            {
                return OperationResult<bool>.IsFailed("PER_AP_08", callingMethod, "Primer apellido inv\u00e1lido.", 400);
            }

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        public static OperationResult<bool> ValidarDatosPersonaEncuestaRequest(
            GuardarDatosPersonaEncuestaRequest request,
            string tipoPersona,
            string callingMethod,
            DateTime fechaMinimaNacimiento)
        {
            var personaRequest = CrearActualizarPersonaRequestDesdeEncuesta(request);
            var validacionPersona = ValidarActualizarPersonaRequest(personaRequest, callingMethod, fechaMinimaNacimiento);
            if (!validacionPersona.Success)
                return validacionPersona;

            var validacionSexo = ValidarSexoEncuesta(request, callingMethod);
            if (!validacionSexo.Success)
                return validacionSexo;

            var validacionSgi = ValidarCondicionesSgi(request, tipoPersona, callingMethod);
            if (!validacionSgi.Success)
                return validacionSgi;

            var validacionReglas = ValidarReglasGeneralesEncuesta(request, callingMethod);
            if (!validacionReglas.Success)
                return validacionReglas;

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        public static ActualizarPersonaRequest CrearActualizarPersonaRequestDesdeEncuesta(GuardarDatosPersonaEncuestaRequest request)
        {
            return new ActualizarPersonaRequest
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
            };
        }

        private static OperationResult<bool> ValidarSexoEncuesta(
            GuardarDatosPersonaEncuestaRequest request,
            string callingMethod)
        {
            if (!string.Equals(request.Sexo?.Trim(), CommonConstants.Sexo.Masculino, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(request.Sexo?.Trim(), CommonConstants.Sexo.Femenino, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<bool>.IsFailed("PER_DPE_14", callingMethod, "Sexo inv\u00e1lido.", 400);
            }

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        private static OperationResult<bool> ValidarCondicionesSgi(
            GuardarDatosPersonaEncuestaRequest request,
            string tipoPersona,
            string callingMethod)
        {
            if (tipoPersona != PersonaConstants.TipoPersonaSgi)
            {
                return OperationResult<bool>.Ok(true, callingMethod);
            }

            if (string.IsNullOrWhiteSpace(request.TrabajaActualmente)
                || request.TrabajaActualmente is not ("S" or "N"))
            {
                return OperationResult<bool>.IsFailed("PER_DPE_15", callingMethod, "Debe indicar si trabaja actualmente.", 400);
            }

            if (request.TrabajaActualmente == "S" && request.TipoJornada is not (1 or 2))
            {
                return OperationResult<bool>.IsFailed("PER_DPE_16", callingMethod, "Debe indicar el tipo de jornada.", 400);
            }

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        private static OperationResult<bool> ValidarReglasGeneralesEncuesta(
            GuardarDatosPersonaEncuestaRequest request,
            string callingMethod)
        {
            var validacionBase = ValidarReglasBaseEncuesta(request, callingMethod);
            if (!validacionBase.Success)
                return validacionBase;

            var validacionDecision = ValidarReglasDecisionEncuesta(request, callingMethod);
            if (!validacionDecision.Success)
                return validacionDecision;

            var validacionAsesoramiento = ValidarReglasAsesoramientoEncuesta(request, callingMethod);
            if (!validacionAsesoramiento.Success)
                return validacionAsesoramiento;

            var validacionEducacion = ValidarReglasEducacionEncuesta(request, callingMethod);
            if (!validacionEducacion.Success)
                return validacionEducacion;

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        private static OperationResult<bool> ValidarReglasBaseEncuesta(
            GuardarDatosPersonaEncuestaRequest request,
            string callingMethod)
        {
            if (request.UltimoAnioSexto <= 0)
                return OperationResult<bool>.IsFailed("PER_DPE_17", callingMethod, "Debe indicar la \u00faltima vez que curs\u00f3 sexto.", 400);
            if (request.InstruccionMadre < 1 || request.InstruccionMadre > 7)
                return OperationResult<bool>.IsFailed("PER_DPE_18", callingMethod, "Error en el nivel de formaci\u00f3n de madre o tutor indicado.", 400);
            if (request.InstruccionPadre < 1 || request.InstruccionPadre > 7)
                return OperationResult<bool>.IsFailed("PER_DPE_19", callingMethod, "Error en el nivel de formaci\u00f3n de padre o tutor indicado.", 400);
            if (request.UltimoAnioSexto < 4 || request.UltimoAnioSexto > 6)
                return OperationResult<bool>.IsFailed("PER_DPE_25", callingMethod, "\u00daltimo a\u00f1o de bachillerato inv\u00e1lido.", 400);
            if (request.InformarEncuesta is not (CommonConstants.Booleanos.Si or CommonConstants.Booleanos.No))
                return OperationResult<bool>.IsFailed("PER_DPE_26", callingMethod, "Debe indicar si informa encuesta.", 400);
            if (request.UltimoAnioSecundaria is not (1 or 2))
                return OperationResult<bool>.IsFailed("PER_DPE_27", callingMethod, "\u00daltimo a\u00f1o de secundaria inv\u00e1lido.", 400);
            if (request.NivelDecision is not (1 or 2))
                return OperationResult<bool>.IsFailed("PER_DPE_28", callingMethod, "Nivel de decisi\u00f3n inv\u00e1lido.", 400);

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        private static OperationResult<bool> ValidarReglasDecisionEncuesta(
            GuardarDatosPersonaEncuestaRequest request,
            string callingMethod)
        {
            if (request.DecisionCarrera is not (0 or 2 or 3 or 4))
                return OperationResult<bool>.IsFailed("PER_DPE_20", callingMethod, "Debe indicar la decisi\u00f3n de carrera.", 400);
            if (request.DecisionUniversidad is not (0 or 2 or 3 or 4))
                return OperationResult<bool>.IsFailed("PER_DPE_21", callingMethod, "Debe indicar la decisi\u00f3n de universidad.", 400);
            if (request.CompartidoCon < PersonaConstants.CompartidoCon.Padres || request.CompartidoCon > PersonaConstants.CompartidoCon.Nadie)
                return OperationResult<bool>.IsFailed("PER_DPE_22", callingMethod, "Debe indicar con qui\u00e9n comparti\u00f3 la decisi\u00f3n.", 400);
            if (request.InfoOtrasUniversidadesAntes is not (CommonConstants.Booleanos.Si or CommonConstants.Booleanos.No))
                return OperationResult<bool>.IsFailed("PER_DPE_23", callingMethod, "Debe indicar si se inform\u00f3 en alguna universidad.", 400);
            if (request.InfoOtrasUniversidadesAntes == CommonConstants.Booleanos.Si && request.UniversidadesConsideradas.Count == 0)
                return OperationResult<bool>.IsFailed("PER_DPE_24", callingMethod, "Debe indicar en qu\u00e9 otras universidades se inform\u00f3.", 400);
            if (request.OpcionesMotivosSeleccionados.Count == 0)
                return OperationResult<bool>.IsFailed("PER_DPE_37", callingMethod, "Debe indicar los motivos de elecci\u00f3n.", 400);

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        private static OperationResult<bool> ValidarReglasAsesoramientoEncuesta(
            GuardarDatosPersonaEncuestaRequest request,
            string callingMethod)
        {
            if (request.AsesoramientoOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_29", callingMethod, "Debe indicar si hubo reuni\u00f3n de asesoramiento.", 400);
            if (request.AsesoramientoOrt.GetValueOrDefault() && (request.ValoracionAsesoramientoOrt < 1 || request.ValoracionAsesoramientoOrt > 5))
                return OperationResult<bool>.IsFailed("PER_DPE_30", callingMethod, "Valoraci\u00f3n de asesoramiento inv\u00e1lida.", 400);
            if (request.VistaSitioWebOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_31", callingMethod, "Debe indicar si visit\u00f3 el sitio de ORT.", 400);
            if (request.VistaSitioWebOrt.GetValueOrDefault() && (request.ValoracionSitioWeb < 1 || request.ValoracionSitioWeb > 5))
                return OperationResult<bool>.IsFailed("PER_DPE_32", callingMethod, "Valoraci\u00f3n del sitio web inv\u00e1lida.", 400);
            if (request.VistaInstalacionesOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_33", callingMethod, "Debe indicar si visit\u00f3 las instalaciones de ORT.", 400);
            if (request.VistaInstalacionesOrt.GetValueOrDefault() && (request.ValoracionInstalacionesOrt < 1 || request.ValoracionInstalacionesOrt > 5))
                return OperationResult<bool>.IsFailed("PER_DPE_34", callingMethod, "Valoraci\u00f3n de instalaciones inv\u00e1lida.", 400);
            if (request.PublicidadOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_35", callingMethod, "Debe indicar si recuerda alguna publicidad de ORT.", 400);
            if (request.PublicidadOrt.GetValueOrDefault() && request.OpcionesPublicidadSeleccionadas.Count == 0)
                return OperationResult<bool>.IsFailed("PER_DPE_36", callingMethod, "Debe indicar publicidades seleccionadas.", 400);

            return OperationResult<bool>.Ok(true, callingMethod);
        }

        private static OperationResult<bool> ValidarReglasEducacionEncuesta(
            GuardarDatosPersonaEncuestaRequest request,
            string callingMethod)
        {
            if (request.UltimoAnioSexto == 6 && !request.CodigoTitulo.HasValue)
                return OperationResult<bool>.IsFailed("PER_DPE_38", callingMethod, "Debe indicar orientaci\u00f3n del bachillerato.", 400);
            if (request.InstruccionMadre is 5 or 6 && request.InstruccionMadreOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_39", callingMethod, "Debe indicar si la madre o tutor obtuvo el t\u00edtulo en ORT.", 400);
            if (request.InstruccionPadre is 5 or 6 && request.InstruccionPadreOrt == null)
                return OperationResult<bool>.IsFailed("PER_DPE_40", callingMethod, "Debe indicar si el padre o tutor obtuvo el t\u00edtulo en ORT.", 400);
            if (request.TieneEducacionSuperior == null && request.UltimoAnioSexto == 6)
                return OperationResult<bool>.IsFailed("PER_DPE_41", callingMethod, "Debe indicar si tiene educaci\u00f3n superior.", 400);
            if (request.TieneEducacionSuperior.GetValueOrDefault() && request.UniversidadesEducacionSuperior.Count == 0)
                return OperationResult<bool>.IsFailed("PER_DPE_42", callingMethod, "Debe seleccionar universidades si tiene educaci\u00f3n superior.", 400);

            return OperationResult<bool>.Ok(true, callingMethod);
        }
    }
}
