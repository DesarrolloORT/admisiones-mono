using AppLogic.Registro.Requests;
using AppLogic.Registro.Dtos;
using AppLogic.Constants;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using LdapService.DTOs;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Utilities;

namespace AppLogic.Registro.Factories
{
    [ExcludeFromCodeCoverage]
    public static class RegistroEntityFactoryHelper
    {
        public static SolicitudAlta CrearSolicitudAlta(long idSolicitudAlta, DtoRegistroPersonaRequest request)
        {
            return new SolicitudAlta
            {
                IdSolicitudAlta = idSolicitudAlta,
                DocumentoSolicitudAlta = DocumentUtils.Normalizar(request.Documento),
                SexoSolicitudAlta = DocumentUtils.Normalizar(request.Sexo),
                IdEstadoSolicitudAlta = InscripcionesConstants.InteresProducto.EstadoSolicitudPendiente,
                PrimerApellidoSolicitudAlta = DocumentUtils.FormatoCapital(request.PrimerApellido),
                SegundoApellidoSolicituAlta = DocumentUtils.FormatoCapital(request.SegundoApellido),
                PrimerNombreSolicitudAlta = DocumentUtils.FormatoCapital(request.PrimerNombre),
                SegundoNombreSolicitudAlta = DocumentUtils.FormatoCapital(request.SegundoNombre),
                UsuarioSolicitudAlta = Constantes.kUSERNAME_USUARIO_ADMISIONES,
                DireccionSolicitudAlta = DocumentUtils.Normalizar(request.Direccion),
                CodigoPais = request.CodigoPais,
                CodigoEstado = request.CodigoEstado,
                CodigoCiudad = request.CodigoCiudad,
                Telefono1SolicitudAlta = DocumentUtils.Normalizar(request.Telefono1),
                EmailSolicitudAlta = DocumentUtils.Normalizar(request.Mail),
                FechaNacimientoSolicituAlta = request.FechaNacimiento.Date,
                TipoDocumentoSolicitudAlta = DocumentUtils.Normalizar(request.TipoDocumento),
                IdTipoAccion = InscripcionesConstants.InteresProducto.TipoAccionRegistroSitioAdmisiones,
            };
        }

        public static RegistroAdmisione CrearRegistroAdmisione(
            long idRegistroAdmisiones,
            long? codigoPersona,
            long? idSolicitudAlta)
        {
            return new RegistroAdmisione
            {
                IdRegistroAdmisiones = idRegistroAdmisiones,
                CodigoPersona = codigoPersona,
                IdSolicitudAlta = idSolicitudAlta
            };
        }

        public static Persona CrearPersona(long codigoPersona, DtoRegistroPersonaRequest request, Ciudad ciudad, DateTime now)
        {
            var primerNombre = DocumentUtils.FormatoCapital(request.PrimerNombre);
            var segundoNombre = DocumentUtils.FormatoCapital(request.SegundoNombre);
            var primerApellido = DocumentUtils.FormatoCapital(request.PrimerApellido);
            var segundoApellido = DocumentUtils.FormatoCapital(request.SegundoApellido);

            return new Persona
            {
                CodigoPersona = codigoPersona,
                CodigoVigencia = InscripcionesConstants.InteresProducto.CodigoVigenciaActiva,
                TipoPersona = InscripcionesConstants.InteresProducto.TipoPersonaSgi,
                PrimerNombre = primerNombre,
                SegundoNombre = segundoNombre,
                PrimerApellido = primerApellido,
                SegundoApellido = segundoApellido,
                PrimerNombreMay = DocumentUtils.NormalizarMayusculas(primerNombre),
                SegundoNombreMay = DocumentUtils.NormalizarMayusculas(segundoNombre),
                PrimerApellidoMay = DocumentUtils.NormalizarMayusculas(primerApellido),
                SegundoApellidoMay = DocumentUtils.NormalizarMayusculas(segundoApellido),
                FechaNacimiento = request.FechaNacimiento.Date,
                Sexo = DocumentUtils.Normalizar(request.Sexo),
                Direccion = DocumentUtils.Normalizar(request.Direccion),
                Telefono1 = DocumentUtils.Normalizar(request.Telefono1),
                Email = DocumentUtils.Normalizar(request.Mail),
                Documento = DocumentUtils.Normalizar(request.Documento),
                TipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento),
                CodigoPais = request.CodigoPais,
                CodigoEstado = request.CodigoEstado,
                CodigoCiudad = request.CodigoCiudad,
                CodigoFuenteDatos = InscripcionesConstants.InteresProducto.CodigoFuenteDatosAdmisiones,
                RecibeCartasPersona = "SI",
                RecibeEmailsPersona = "SI",
                UsuarioUltimaActualizacion = Constantes.kUSERNAME_USUARIO_ADMISIONES,
                FechaUltimaActualizacion = now.Date,
                HoraUltimaActualizacion = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture),
                Ciudad = ciudad
            };
        }

        /// <summary>
        /// Crea una entidad Persona a partir de los datos de registro pendiente en Redis.
        /// </summary>
        public static Persona CrearPersona(long codigoPersona, DtoRegistroPendingPersona data, Ciudad ciudad, DateTime now)
        {
            var primerNombre = DocumentUtils.FormatoCapital(data.PrimerNombre);
            var segundoNombre = DocumentUtils.FormatoCapital(data.SegundoNombre);
            var primerApellido = DocumentUtils.FormatoCapital(data.PrimerApellido);
            var segundoApellido = DocumentUtils.FormatoCapital(data.SegundoApellido);

            return new Persona
            {
                CodigoPersona = codigoPersona,
                CodigoVigencia = InscripcionesConstants.InteresProducto.CodigoVigenciaActiva,
                TipoPersona = InscripcionesConstants.InteresProducto.TipoPersonaSgi,
                PrimerNombre = primerNombre,
                SegundoNombre = segundoNombre,
                PrimerApellido = primerApellido,
                SegundoApellido = segundoApellido,
                PrimerNombreMay = DocumentUtils.NormalizarMayusculas(primerNombre),
                SegundoNombreMay = DocumentUtils.NormalizarMayusculas(segundoNombre),
                PrimerApellidoMay = DocumentUtils.NormalizarMayusculas(primerApellido),
                SegundoApellidoMay = DocumentUtils.NormalizarMayusculas(segundoApellido),
                FechaNacimiento = data.FechaNacimiento.Date,
                Sexo = DocumentUtils.Normalizar(data.Sexo),
                Direccion = DocumentUtils.Normalizar(data.Direccion),
                Telefono1 = DocumentUtils.Normalizar(data.Telefono1),
                Email = DocumentUtils.Normalizar(data.Email),
                Documento = DocumentUtils.Normalizar(data.Documento),
                TipoDocumento = DocumentUtils.Normalizar(data.TipoDocumento),
                CodigoPais = data.CodigoPais,
                CodigoEstado = data.CodigoEstado,
                CodigoCiudad = data.CodigoCiudad,
                CodigoFuenteDatos = InscripcionesConstants.InteresProducto.CodigoFuenteDatosAdmisiones,
                RecibeCartasPersona = "SI",
                RecibeEmailsPersona = "SI",
                UsuarioUltimaActualizacion = Constantes.kUSERNAME_USUARIO_ADMISIONES,
                FechaUltimaActualizacion = now.Date,
                HoraUltimaActualizacion = now.ToString(InscripcionesConstants.InteresProducto.FormatoHora, CultureInfo.InvariantCulture),
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
                UsuarioModificacion = Constantes.kUSERNAME_USUARIO_ADMISIONES,
                NuevoUsuario = persona.CodigoPersona.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}
