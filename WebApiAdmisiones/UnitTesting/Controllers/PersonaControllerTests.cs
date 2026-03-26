using AppLogic.DevartDTOs;
using AppLogic.IServices;
using AppLogic.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Controllers
{
    public class PersonaControllerTests
    {
        [Fact]
        public void ObtenerPersona_ReturnsOk()
        {
            var serviceMock = new Mock<IPersonaAdmisionService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<PersonaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            var controller = new PersonaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerPersona(1))
                .Returns(OperationResult<DtoPersonaDevart>.Ok(new DtoPersonaDevart(), nameof(IPersonaAdmisionService.ObtenerPersona)));

            var response = controller.ObtenerPersona();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ActualizarPersona_ReturnsOk()
        {
            var serviceMock = new Mock<IPersonaAdmisionService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<PersonaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            var controller = new PersonaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ActualizarPersona(1, It.IsAny<ActualizarPersonaRequest>()))
                .Returns(OperationResult<bool>.Ok(true, nameof(IPersonaAdmisionService.ActualizarPersona)));

            var response = controller.ActualizarPersona(new ActualizarPersonaRequest
            {
                PrimerApellido = "Perez",
                PrimerNombre = "Ana",
                Mail = "ana@test.com",
                VerificacionMail = "ana@test.com",
                Direccion = "18 de julio 1234",
                Sexo = "F",
                FechaNacimiento = new DateTime(2000, 1, 1),
                CodigoPais = 1,
                CodigoEstado = 1,
                CodigoCiudad = 1
            });

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void GuardarDatosPersonaEncuesta_ReturnsOk()
        {
            var serviceMock = new Mock<IPersonaAdmisionService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<PersonaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            var controller = new PersonaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.GuardarDatosPersonaEncuesta(1, It.IsAny<GuardarDatosPersonaEncuestaRequest>()))
                .Returns(OperationResult<bool>.Ok(true, nameof(IPersonaAdmisionService.GuardarDatosPersonaEncuesta)));

            var response = controller.GuardarDatosPersonaEncuesta(new GuardarDatosPersonaEncuestaRequest
            {
                PrimerApellido = "Perez",
                PrimerNombre = "Ana",
                Mail = "ana@test.com",
                VerificacionMail = "ana@test.com",
                Direccion = "18 de julio 1234",
                Sexo = "F",
                FechaNacimiento = new DateTime(2000, 1, 1),
                Telefono1 = "24001234",
                CodigoPais = 1,
                CodigoEstado = 1,
                CodigoCiudad = 1,
                Documento = "12345678",
                TipoDocumento = "CI",
                IdProducto = 10,
                IdProceso = 20,
                UltimoAnioSexto = 5,
                VecesSexto = 0,
                InstruccionPadre = 3,
                InstruccionMadre = 3,
                DecisionCarrera = 2,
                DecisionUniversidad = 2,
                InfoOtrasUniversidadesAntes = "NO",
                CompartidoCon = 1,
                CodigoInstitucionBac = 100,
                InformarEncuesta = "SI",
                UltimoAnioSecundaria = 1,
                TieneEducacionSuperior = false,
                NivelDecision = 1,
                AsesoramientoOrt = true,
                ValoracionAsesoramientoOrt = 4,
                VistaSitioWebOrt = true,
                ValoracionSitioWeb = 4,
                VistaInstalacionesOrt = true,
                ValoracionInstalacionesOrt = 4,
                PublicidadOrt = true,
                OpcionesPublicidadSeleccionadas = [new PublicidadEncuestaRequest { IdPublicidad = 1, NombrePublicidad = "Web" }],
                OpcionesMotivosSeleccionados = [new MotivoEncuestaRequest { IdMotivo = 1, NombreMotivo = "Prestigio" }]
            });

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
