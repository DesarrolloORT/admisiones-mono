using AppLogic.Identity.Dtos;
using AppLogic.Registration.Constants;
using AppLogic.Contracts.Constants;
using AppLogic.Contracts.Text;
using AppLogic.Registration.Dtos;
using BusinessLogic.Entities;
using LdapService.DTOs;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Utilities;

namespace AppLogic.Registration.Mapping;

[ExcludeFromCodeCoverage]
public static class RegistrationMapper
{
    /// <summary>
    /// Request de alta a persona pendiente, para guardarla en Redis hasta que fije su contraseña.
    /// </summary>
    public static PendingPerson ToPendingPerson(
        RegisterPersonRequest request,
        string flowIdPending,
        string tokenHash)
    {
        return new PendingPerson
        {
            FlowId = flowIdPending,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber,
            FirstSurname = request.FirstSurname,
            SecondSurname = request.SecondSurname,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            BirthDate = request.BirthDate,
            Sex = request.Sex,
            Address = request.Address,
            PrimaryPhone = request.PrimaryPhone,
            Email = request.Email,
            CountryId = request.CountryId,
            StateId = request.StateId,
            CityId = request.CityId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static SolicitudAlta CreateRegistrationRequest(long idSolicitudAlta, RegisterPersonRequest request)
    {
        return new SolicitudAlta
        {
            IdSolicitudAlta = idSolicitudAlta,
            DocumentoSolicitudAlta = TextNormalization.Trim(request.DocumentNumber),
            SexoSolicitudAlta = TextNormalization.Trim(request.Sex),
            IdEstadoSolicitudAlta = SgiPersonRecordConstants.PendingRequestStatus,
            PrimerApellidoSolicitudAlta = TextNormalization.ToTitleCaseInvariant(request.FirstSurname),
            SegundoApellidoSolicituAlta = TextNormalization.ToTitleCaseInvariant(request.SecondSurname),
            PrimerNombreSolicitudAlta = TextNormalization.ToTitleCaseInvariant(request.FirstName),
            SegundoNombreSolicitudAlta = TextNormalization.ToTitleCaseInvariant(request.MiddleName),
            UsuarioSolicitudAlta = Constantes.kUSERNAME_USUARIO_ADMISIONES,
            DireccionSolicitudAlta = TextNormalization.Trim(request.Address),
            CodigoPais = request.CountryId,
            CodigoEstado = request.StateId,
            CodigoCiudad = request.CityId,
            Telefono1SolicitudAlta = TextNormalization.Trim(request.PrimaryPhone),
            EmailSolicitudAlta = TextNormalization.Trim(request.Email),
            FechaNacimientoSolicituAlta = request.BirthDate.Date,
            TipoDocumentoSolicitudAlta = TextNormalization.Trim(request.DocumentType),
            IdTipoAccion = SgiPersonRecordConstants.AdmissionsSiteRegistrationActionType,
        };
    }

    public static RegistroAdmisione CreateAdmissionRecord(
        long idRegistroAdmisiones,
        long? personId,
        long? idSolicitudAlta)
    {
        return new RegistroAdmisione
        {
            IdRegistroAdmisiones = idRegistroAdmisiones,
            CodigoPersona = personId,
            IdSolicitudAlta = idSolicitudAlta
        };
    }

    public static Persona CreatePerson(long personId, RegisterPersonRequest request, Ciudad city, DateTime now)
    {
        var firstName = TextNormalization.ToTitleCaseInvariant(request.FirstName);
        var middleName = TextNormalization.ToTitleCaseInvariant(request.MiddleName);
        var firstSurname = TextNormalization.ToTitleCaseInvariant(request.FirstSurname);
        var secondSurname = TextNormalization.ToTitleCaseInvariant(request.SecondSurname);

        return new Persona
        {
            CodigoPersona = personId,
            CodigoVigencia = SgiPersonRecordConstants.ActiveValidityCode,
            TipoPersona = SgiPersonRecordConstants.SgiPersonType,
            PrimerNombre = firstName,
            SegundoNombre = middleName,
            PrimerApellido = firstSurname,
            SegundoApellido = secondSurname,
            PrimerNombreMay = TextNormalization.ToUpperWithoutAccents(firstName),
            SegundoNombreMay = TextNormalization.ToUpperWithoutAccents(middleName),
            PrimerApellidoMay = TextNormalization.ToUpperWithoutAccents(firstSurname),
            SegundoApellidoMay = TextNormalization.ToUpperWithoutAccents(secondSurname),
            FechaNacimiento = request.BirthDate.Date,
            Sexo = TextNormalization.Trim(request.Sex),
            Direccion = TextNormalization.Trim(request.Address),
            Telefono1 = TextNormalization.Trim(request.PrimaryPhone),
            Email = TextNormalization.Trim(request.Email),
            Documento = TextNormalization.Trim(request.DocumentNumber),
            TipoDocumento = TextNormalization.Trim(request.DocumentType),
            CodigoPais = request.CountryId,
            CodigoEstado = request.StateId,
            CodigoCiudad = request.CityId,
            CodigoFuenteDatos = SgiPersonRecordConstants.AdmissionsDataSourceCode,
            RecibeCartasPersona = "SI",
            RecibeEmailsPersona = "SI",
            UsuarioUltimaActualizacion = Constantes.kUSERNAME_USUARIO_ADMISIONES,
            FechaUltimaActualizacion = now.Date,
            HoraUltimaActualizacion = now.ToString(SchemaConstants.LegacyTimeFormat, CultureInfo.InvariantCulture),
            Ciudad = city
        };
    }

    /// <summary>
    /// Crea una entidad Persona a partir de los datos de registro pendiente en Redis.
    /// </summary>
    public static Persona CreatePerson(long personId, PendingPerson data, Ciudad city, DateTime now)
    {
        var firstName = TextNormalization.ToTitleCaseInvariant(data.FirstName);
        var middleName = TextNormalization.ToTitleCaseInvariant(data.MiddleName);
        var firstSurname = TextNormalization.ToTitleCaseInvariant(data.FirstSurname);
        var secondSurname = TextNormalization.ToTitleCaseInvariant(data.SecondSurname);

        return new Persona
        {
            CodigoPersona = personId,
            CodigoVigencia = SgiPersonRecordConstants.ActiveValidityCode,
            TipoPersona = SgiPersonRecordConstants.SgiPersonType,
            PrimerNombre = firstName,
            SegundoNombre = middleName,
            PrimerApellido = firstSurname,
            SegundoApellido = secondSurname,
            PrimerNombreMay = TextNormalization.ToUpperWithoutAccents(firstName),
            SegundoNombreMay = TextNormalization.ToUpperWithoutAccents(middleName),
            PrimerApellidoMay = TextNormalization.ToUpperWithoutAccents(firstSurname),
            SegundoApellidoMay = TextNormalization.ToUpperWithoutAccents(secondSurname),
            FechaNacimiento = data.BirthDate.Date,
            Sexo = TextNormalization.Trim(data.Sex),
            Direccion = TextNormalization.Trim(data.Address),
            Telefono1 = TextNormalization.Trim(data.PrimaryPhone),
            Email = TextNormalization.Trim(data.Email),
            Documento = TextNormalization.Trim(data.DocumentNumber),
            TipoDocumento = TextNormalization.Trim(data.DocumentType),
            CodigoPais = data.CountryId,
            CodigoEstado = data.StateId,
            CodigoCiudad = data.CityId,
            CodigoFuenteDatos = SgiPersonRecordConstants.AdmissionsDataSourceCode,
            RecibeCartasPersona = "SI",
            RecibeEmailsPersona = "SI",
            UsuarioUltimaActualizacion = Constantes.kUSERNAME_USUARIO_ADMISIONES,
            FechaUltimaActualizacion = now.Date,
            HoraUltimaActualizacion = now.ToString(SchemaConstants.LegacyTimeFormat, CultureInfo.InvariantCulture),
            Ciudad = city
        };
    }

    public static ParamCrearUsuarioLdap BuildLdapUserRequest(Persona person)
    {
        return new ParamCrearUsuarioLdap
        {
            CodigoPersona = person.CodigoPersona.ToString(CultureInfo.InvariantCulture),
            IdFuncionario = string.Empty,
            FuncionarioActivo = "NO",
            NumeroAlumnoViejo = string.Empty,
            PrimerNombre = person.PrimerNombre,
            SegundoNombre = person.SegundoNombre ?? string.Empty,
            PrimerApellido = person.PrimerApellido,
            SegundoApellido = person.SegundoApellido ?? string.Empty,
            Email = person.Email ?? string.Empty,
            Direccion = person.Direccion ?? string.Empty,
            CodigoPostal = person.CodigoPostal ?? string.Empty,
            Telefono1 = person.Telefono1 ?? string.Empty,
            Telefono2 = person.Telefono2 ?? string.Empty,
            Documento = person.Documento ?? string.Empty,
            TipoDocumento = person.TipoDocumento ?? string.Empty,
            CodigoVigencia = person.CodigoVigencia,
            NombreCiudad = person.Ciudad?.Nombre ?? string.Empty,
            UsuarioModificacion = Constantes.kUSERNAME_USUARIO_ADMISIONES,
            NuevoUsuario = person.CodigoPersona.ToString(CultureInfo.InvariantCulture)
        };
    }
}
