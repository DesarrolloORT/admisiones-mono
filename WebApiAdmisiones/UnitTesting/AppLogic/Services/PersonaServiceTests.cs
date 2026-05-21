using AppLogic.DTOs;
using AppLogic.Requests;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using LdapService.Interfaces;
using Moq;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class PersonaServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IPersonaRepository> _personaRepositoryMock;
        private readonly Mock<BusinessLogic.IDevartRepositories.ICiudadRepository> _ciudadRepositoryMock;
        private readonly Mock<ILdap> _ldapMock;
        private readonly PersonaService _service;

        public PersonaServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _personaRepositoryMock = new Mock<IPersonaRepository>();
            _ciudadRepositoryMock = new Mock<BusinessLogic.IDevartRepositories.ICiudadRepository>();
            _ldapMock = new Mock<ILdap>();

            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _uowMock.Setup(u => u.Personas).Returns(_personaRepositoryMock.Object);
            _uowMock.Setup(u => u.Ciudads).Returns(_ciudadRepositoryMock.Object);
            _uowMock.Setup(u => u.ObtenerDbUserId()).Returns("ADMISIONES");

            _service = new PersonaService(_uowFactoryMock.Object, _ldapMock.Object);
        }

        [Fact]
        public void ObtenerDatosPersona_NotFound_ReturnsFailed()
        {
            _personaRepositoryMock
                .Setup(r => r.GetPersonaWithRelated(123))
                .Returns((Persona)null!);

            var result = _service.ObtenerDatosPersona(123);

            Assert.False(result.Success);
            Assert.Equal("PER_DAT_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void ObtenerDatosPersona_Found_MapsOnlyRequiredData()
        {
            var fechaNacimiento = new DateTime(2000, 1, 2);
            _personaRepositoryMock
                .Setup(r => r.GetPersonaWithRelated(123))
                .Returns(new Persona
                {
                    CodigoPersona = 123,
                    TipoDocumento = "CI",
                    Documento = "12345678",
                    PrimerNombre = "Ana",
                    SegundoNombre = "Maria",
                    PrimerApellido = "Perez",
                    SegundoApellido = "Gomez",
                    FechaNacimiento = fechaNacimiento,
                    Sexo = "F",
                    CodigoPais = 1,
                    CodigoEstado = 2,
                    CodigoCiudad = 3,
                    Direccion = "18 de julio 1234",
                    Telefono1 = "24001234",
                    Email = "ana@test.com"
                });

            var result = _service.ObtenerDatosPersona(123);

            Assert.True(result.Success);
            Assert.IsType<DtoDatosPersona>(result.Data);
            Assert.Equal("CI", result.Data.TipoDocumento);
            Assert.Equal("12345678", result.Data.Documento);
            Assert.Equal("Ana", result.Data.PrimerNombre);
            Assert.Equal("Maria", result.Data.SegundoNombre);
            Assert.Equal("Perez", result.Data.PrimerApellido);
            Assert.Equal("Gomez", result.Data.SegundoApellido);
            Assert.Equal(fechaNacimiento, result.Data.FechaNacimiento);
            Assert.Equal("F", result.Data.Sexo);
            Assert.Equal(1, result.Data.CodigoPais);
            Assert.Equal(2, result.Data.CodigoEstado);
            Assert.Equal(3, result.Data.CodigoCiudad);
            Assert.Equal("18 de julio 1234", result.Data.Direccion);
            Assert.Equal("24001234", result.Data.Telefono1);
            Assert.Equal("ana@test.com", result.Data.Mail);
            Assert.Equal("ana@test.com", result.Data.VerificacionMail);
        }

        [Fact]
        public void ActualizarDatosPersona_UpdatesOnlyAllowedFields()
        {
            var persona = new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "CI",
                Documento = "12345678",
                PrimerNombre = "Ana",
                SegundoNombre = "Maria",
                PrimerApellido = "Perez",
                SegundoApellido = "Gomez",
                FechaNacimiento = new DateTime(2000, 1, 2),
                Sexo = "F",
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "Vieja",
                Telefono1 = "111",
                Email = "viejo@test.com"
            };
            var request = new ActualizarDatosPersonaRequest
            {
                CodigoPais = 4,
                CodigoEstado = 5,
                CodigoCiudad = 6,
                Direccion = "  nueva direccion  ",
                Telefono1 = " 222 ",
                Mail = "nuevo@test.com",
                VerificacionMail = "nuevo@test.com"
            };

            _personaRepositoryMock.Setup(r => r.GetPersonaWithRelated(123)).Returns(persona);
            _ciudadRepositoryMock.Setup(r => r.GetByKey(4, 5, 6)).Returns(new Ciudad());

            var result = _service.ActualizarDatosPersona(123, request);

            Assert.True(result.Success);
            Assert.Equal("CI", persona.TipoDocumento);
            Assert.Equal("12345678", persona.Documento);
            Assert.Equal("Ana", persona.PrimerNombre);
            Assert.Equal("Maria", persona.SegundoNombre);
            Assert.Equal("Perez", persona.PrimerApellido);
            Assert.Equal("Gomez", persona.SegundoApellido);
            Assert.Equal(new DateTime(2000, 1, 2), persona.FechaNacimiento);
            Assert.Equal("F", persona.Sexo);
            Assert.Equal(4, persona.CodigoPais);
            Assert.Equal(5, persona.CodigoEstado);
            Assert.Equal(6, persona.CodigoCiudad);
            Assert.Equal("Nueva Direccion", persona.Direccion);
            Assert.Equal("222", persona.Telefono1);
            Assert.Equal("nuevo@test.com", persona.Email);
            _personaRepositoryMock.Verify(r => r.Update(persona), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void ActualizarDatosPersona_MailMismatch_ReturnsFailed()
        {
            _personaRepositoryMock
                .Setup(r => r.GetPersonaWithRelated(123))
                .Returns(new Persona { CodigoPersona = 123 });

            var result = _service.ActualizarDatosPersona(123, new ActualizarDatosPersonaRequest
            {
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "18 de julio 1234",
                Mail = "uno@test.com",
                VerificacionMail = "dos@test.com"
            });

            Assert.False(result.Success);
            Assert.Equal("PER_ADP_04", result.ErrorCode);
            _ciudadRepositoryMock.Verify(r => r.GetByKey(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public void ActualizarDatosPersona_InvalidCity_ReturnsFailed()
        {
            _personaRepositoryMock
                .Setup(r => r.GetPersonaWithRelated(123))
                .Returns(new Persona { CodigoPersona = 123 });
            _ciudadRepositoryMock
                .Setup(r => r.GetByKey(1, 2, 3))
                .Returns((Ciudad)null!);

            var result = _service.ActualizarDatosPersona(123, new ActualizarDatosPersonaRequest
            {
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "18 de julio 1234",
                Mail = "uno@test.com",
                VerificacionMail = "uno@test.com"
            });

            Assert.False(result.Success);
            Assert.Equal("PER_ADP_05", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_NullRequest_ReturnsFailed()
        {
            var result = await _service.CambiarPasswordAsync(12345, null!);

            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_InvalidPassword_ReturnsFailedWithoutCallingLdap()
        {
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "short1A!"
            };

            var result = await _service.CambiarPasswordAsync(12345, request);

            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_02", result.ErrorCode);
            Assert.Equal("La contraseña debe tener entre 12 y 20 caracteres", result.Message);
            Assert.Equal(400, result.HttpCode);
            _ldapMock.Verify(
                x => x.CambiarPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CambiarPasswordAsync_LdapSuccess_ReturnsOk()
        {
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(ILdap.CambiarPasswordAsync)));

            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            Assert.True(result.Success);
            Assert.Equal("Se actualizó tu contraseña", result.Data);
            Assert.Equal(200, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_LdapFailure_PropagatesFailure()
        {
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "AUTH_LDAP_22",
                    nameof(ILdap.CambiarPasswordAsync),
                    "El servicio LDAP no pudo cambiar la password.",
                    400,
                    false));

            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            Assert.False(result.Success);
            Assert.Equal("AUTH_LDAP_22", result.ErrorCode);
            Assert.Equal("El servicio LDAP no pudo cambiar la password.", result.Message);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task CambiarPasswordAsync_ExceptionThrown_ReturnsFailedWithErrorCode()
        {
            long codigoPersona = 12345;
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            _ldapMock.Setup(x => x.CambiarPasswordAsync(
                    codigoPersona.ToString(),
                    request.PasswordActual,
                    request.PasswordNueva))
                .ThrowsAsync(new InvalidOperationException("LDAP service unavailable"));

            var result = await _service.CambiarPasswordAsync(codigoPersona, request);

            Assert.False(result.Success);
            Assert.Equal("CAM_PAS_99", result.ErrorCode);
            Assert.Contains("Error al cambiar contraseña", result.Message);
            Assert.Contains("LDAP service unavailable", result.Message);
            Assert.Equal(500, result.HttpCode);
        }
    }
}
