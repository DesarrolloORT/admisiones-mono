using System;
using System.Globalization;
using AppLogic.Constants;
using AppLogic.DTOs;
using BusinessLogic.Entities;
using LdapService.DTOs;

namespace AppLogic.Helpers
{
    public static class RegistroEntityFactoryHelper
    {
        public static SolicitudAlta CrearSolicitudAlta(long idSolicitudAlta, RegistroConfirmarSolicitudAltaRequest request)
        {
            return new SolicitudAlta
            {
                IdSolicitudAlta = idSolicitudAlta,
                DocumentoSolicitudAlta = RegistroNormalizationHelper.Normalizar(request.Documento),
                SexoSolicitudAlta = RegistroNormalizationHelper.Normalizar(request.Sexo),
                IdEstadoSolicitudAlta = InscripcionesConstants.InteresProducto.EstadoSolicitudPendiente,
                PrimerApellidoSolicitudAlta = RegistroNormalizationHelper.FormatoCapital(request.PrimerApellido),
                SegundoApellidoSolicituAlta = RegistroNormalizationHelper.FormatoCapital(request.SegundoApellido),
                PrimerNombreSolicitudAlta = RegistroNormalizationHelper.FormatoCapital(request.PrimerNombre),
                SegundoNombreSolicitudAlta = RegistroNormalizationHelper.FormatoCapital(request.SegundoNombre),
                UsuarioSolicitudAlta = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                DireccionSolicitudAlta = RegistroNormalizationHelper.Normalizar(request.Direccion),
                CodigoPais = null,
                CodigoEstado = null,
                CodigoCiudad = null,
                Telefono1SolicitudAlta = RegistroNormalizationHelper.Normalizar(request.Telefono1),
                Telefono2SolicitudAlta = RegistroNormalizationHelper.Normalizar(request.Telefono2),
                EmailSolicitudAlta = RegistroNormalizationHelper.Normalizar(request.Mail),
                FechaNacimientoSolicituAlta = request.FechaNacimiento.Date,
                TipoDocumentoSolicitudAlta = RegistroNormalizationHelper.Normalizar(request.TipoDocumento),
                IdProducto = request.IdProducto,
                IdTipoAccion = InscripcionesConstants.InteresProducto.TipoAccionRegistroSitioAdmisiones,
                IdProceso = request.IdProceso
            };
        }

        public static Intere CrearInteres(decimal idInteres, long codigoPersona, long idProceso, DateTime now)
        {
            return new Intere
            {
                IdInteres = idInteres,
                CodigoPersona = codigoPersona,
                IdProceso = idProceso,
                IdFormaContacto = InscripcionesConstants.InteresProducto.FormaContactoWeb,
                ContactadorInteres = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                ObservacionesInteres = InscripcionesConstants.InteresProducto.ObservacionesWeb,
                IdLugar = InscripcionesConstants.InteresProducto.LugarInteresWeb,
                IdGradoPureza = InscripcionesConstants.InteresProducto.GradoPurezaPuro,
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaIngreso = now.Date,
                HoraIngreso = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture)
            };
        }

        public static InteresProducto CrearInteresProducto(decimal idInteres, long idProducto, DateTime now)
        {
            return new InteresProducto
            {
                IdInteres = idInteres,
                IdProducto = idProducto,
                IdTipoInteres = InscripcionesConstants.InteresProducto.TipoInteresComun,
                IdGradoInteres = InscripcionesConstants.InteresProducto.GradoInteresRegistro,
                FechaInteresProd = now.Date,
                FechaIngreso = now.Date,
                FechaAltaInteresProd = now.Date,
                HoraIngreso = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture),
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                ObservacionesInteresProd = InscripcionesConstants.InteresProducto.ObservacionesWeb
            };
        }

        public static PersonaAdmite CrearPersonaAdmite(long codigoPersona, DateTime now)
        {
            return new PersonaAdmite
            {
                CodigoPersona = codigoPersona,
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaIngreso = now.Date,
                HoraIngreso = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture),
                FechaFrescoPersonaAdmite = now.Date
            };
        }

        public static Actividad CrearActividad(decimal idActividad, long idProceso, DateTime now)
        {
            return new Actividad
            {
                IdActividad = idActividad,
                IdFormaContacto = InscripcionesConstants.InteresProducto.FormaContactoWeb,
                IdTipoActividad = InscripcionesConstants.InteresProducto.TipoActividadMonoAccion,
                IdTipoAccion = InscripcionesConstants.InteresProducto.TipoAccionRegistroSitioAdmisiones,
                IdEstadoAccion = InscripcionesConstants.InteresProducto.EstadoAccionRealizada,
                UsernameGeneradorActividad = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                UsernameRealizadoActividad = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaGeneradorActividad = now.Date,
                FechaRealizadoActividad = now.Date,
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaIngreso = now.Date,
                HoraIngreso = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture),
                IdProceso = idProceso
            };
        }

        public static Accion CrearAccion(decimal idAccion, decimal idActividad, long codigoPersona, DateTime now)
        {
            return new Accion
            {
                IdAccion = idAccion,
                IdActividad = idActividad,
                CodigoPersona = codigoPersona,
                FechaRealizadoAccion = now.Date,
                UsuarioRealizadoAccion = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                IdEstadoAccion = InscripcionesConstants.InteresProducto.EstadoAccionRealizada,
                IdAccionResultado = InscripcionesConstants.InteresProducto.ResultadoAccionRealizada,
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaIngreso = now.Date,
                HoraIngreso = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture)
            };
        }

        public static Persona CrearPersona(long codigoPersona, RegistroConfirmarNuevaPersonaRequest request, Ciudad ciudad, DateTime now)
        {
            var primerNombre = RegistroNormalizationHelper.FormatoCapital(request.PrimerNombre);
            var segundoNombre = RegistroNormalizationHelper.FormatoCapital(request.SegundoNombre);
            var primerApellido = RegistroNormalizationHelper.FormatoCapital(request.PrimerApellido);
            var segundoApellido = RegistroNormalizationHelper.FormatoCapital(request.SegundoApellido);

            return new Persona
            {
                CodigoPersona = codigoPersona,
                CodigoVigencia = InscripcionesConstants.InteresProducto.CodigoVigenciaActiva,
                TipoPersona = InscripcionesConstants.InteresProducto.TipoPersonaSgi,
                PrimerNombre = primerNombre,
                SegundoNombre = segundoNombre,
                PrimerApellido = primerApellido,
                SegundoApellido = segundoApellido,
                PrimerNombreMay = RegistroNormalizationHelper.NormalizarMayusculas(primerNombre),
                SegundoNombreMay = RegistroNormalizationHelper.NormalizarMayusculas(segundoNombre),
                PrimerApellidoMay = RegistroNormalizationHelper.NormalizarMayusculas(primerApellido),
                SegundoApellidoMay = RegistroNormalizationHelper.NormalizarMayusculas(segundoApellido),
                FechaNacimiento = request.FechaNacimiento.Date,
                Sexo = RegistroNormalizationHelper.Normalizar(request.Sexo),
                Direccion = RegistroNormalizationHelper.Normalizar(request.Direccion),
                Telefono1 = RegistroNormalizationHelper.Normalizar(request.Telefono1),
                Telefono2 = RegistroNormalizationHelper.Normalizar(request.Telefono2),
                Email = RegistroNormalizationHelper.Normalizar(request.Mail),
                Documento = RegistroNormalizationHelper.Normalizar(request.Documento),
                TipoDocumento = RegistroNormalizationHelper.Normalizar(request.TipoDocumento),
                CodigoPais = request.CodigoPais,
                CodigoEstado = request.CodigoEstado,
                CodigoCiudad = request.CodigoCiudad,
                CodigoFuenteDatos = InscripcionesConstants.InteresProducto.CodigoFuenteDatosAdmisiones,
                RecibeCartasPersona = "SI",
                RecibeEmailsPersona = "SI",
                UsuarioUltimaActualizacion = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaUltimaActualizacion = now.Date,
                HoraUltimaActualizacion = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture),
                UsuarioIngreso = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                FechaIngreso = now.Date,
                HoraIngreso = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture),
                Ciudad = ciudad
            };
        }

        public static ParamCrearUsuarioLdap CrearUsuarioLdapRequest(Persona persona)
        {
            return new ParamCrearUsuarioLdap
            {
                CodigoPersona = persona.CodigoPersona.ToString(CultureInfo.InvariantCulture),
                IdFuncionario = string.Empty,
                FuncionarioActivo = "NO",
                NumeroAlumnoViejo = string.Empty,
                PrimerNombre = persona.PrimerNombre,
                SegundoNombre = persona.SegundoNombre ?? string.Empty,
                PrimerApellido = persona.PrimerApellido,
                SegundoApellido = persona.SegundoApellido ?? string.Empty,
                Email = persona.Email ?? string.Empty,
                Direccion = persona.Direccion ?? string.Empty,
                CodigoPostal = persona.CodigoPostal ?? string.Empty,
                Telefono1 = persona.Telefono1 ?? string.Empty,
                Telefono2 = persona.Telefono2 ?? string.Empty,
                Documento = persona.Documento ?? string.Empty,
                TipoDocumento = persona.TipoDocumento ?? string.Empty,
                CodigoVigencia = persona.CodigoVigencia,
                NombreCiudad = persona.Ciudad?.Nombre ?? string.Empty,
                UsuarioModificacion = InscripcionesConstants.InteresProducto.UsuarioAdmisiones,
                NuevoUsuario = persona.CodigoPersona.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}
