using AppLogic.DTOs;
using AppLogic.IServices.Tivenos;
using AppLogic.Services.Tivenos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class TivenosEnvioServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IEnvioParaTivenoRepository> _envioParaTivenosRepoMock = new();
        private readonly Mock<IParametroRepository> _parametroRepoMock = new();
        private readonly TivenosEnvioService _service = new();

        public TivenosEnvioServiceTests()
        {
            _uowMock.Setup(u => u.EnvioParaTivenos).Returns(_envioParaTivenosRepoMock.Object);
            _uowMock.Setup(u => u.Parametros).Returns(_parametroRepoMock.Object);
        }

        [Fact]
        public void EncolarAltaInteresXSeleccionEnSitio_ConTivenosNoLiberadoYPersonaComun_NoEncola()
        {
            _parametroRepoMock
                .Setup(r => r.ObtenerSeLiberoTivenos())
                .Returns("NO");

            var result = _service.EncolarAltaInteresXSeleccionEnSitio(
                _uowMock.Object,
                RequestBase(TivenosAltaInteresOperacion.AltaInteresProducto()),
                777,
                "Test");

            Assert.True(result.Success);
            Assert.False(result.Data);
            _envioParaTivenosRepoMock.Verify(r => r.Add(It.IsAny<EnvioParaTiveno>()), Times.Never);
        }

        [Fact]
        public void EncolarAltaInteresXSeleccionEnSitio_ConTivenosLiberado_EncolaPayloadLegacy()
        {
            _parametroRepoMock
                .Setup(r => r.ObtenerSeLiberoTivenos())
                .Returns("SI");

            var result = _service.EncolarAltaInteresXSeleccionEnSitio(
                _uowMock.Object,
                RequestBase(TivenosAltaInteresOperacion.AltaInteresProducto()),
                777,
                "Test");

            Assert.True(result.Success);
            Assert.True(result.Data);
            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.IdEnvioParaTivenos == 777 &&
                e.Origen == "ADMISIONES" &&
                e.TipoProcesoLlamador == "Alta" &&
                e.Disparador == "AltaInteresProducto" &&
                e.Modulo == "InteresProducto" &&
                e.Metodo == "AltaInteresXSeleccionEnSitio" &&
                e.Status == "Nuevo" &&
                e.CodigoSape == 123 &&
                e.ProcesoId == 20 &&
                e.ProductoId == 10 &&
                e.InteresProdGradoInteresId == 4 &&
                e.MotivodesinteresId == null &&
                e.MotivodesinteresNombre == string.Empty)), Times.Once);
        }

        [Fact]
        public void EncolarAltaInteresXSeleccionEnSitio_ConTivenosNoLiberadoYPersonaLegacy_Encola()
        {
            _parametroRepoMock
                .Setup(r => r.ObtenerSeLiberoTivenos())
                .Returns("NO");

            var result = _service.EncolarAltaInteresXSeleccionEnSitio(
                _uowMock.Object,
                new TivenosAltaInteresRequest
                {
                    CodigoPersona = 129329,
                    IdProducto = 10,
                    IdProceso = 20,
                    Operacion = TivenosAltaInteresOperacion.AltaInteresProducto(),
                },
                777,
                "Test");

            Assert.True(result.Success);
            Assert.True(result.Data);
            _envioParaTivenosRepoMock.Verify(r => r.Add(It.IsAny<EnvioParaTiveno>()), Times.Once);
        }

        [Fact]
        public void EncolarAltaInteresXSeleccionEnSitio_ParaProductoExistenteActualizado_EncolaModificar()
        {
            _parametroRepoMock
                .Setup(r => r.ObtenerSeLiberoTivenos())
                .Returns("SI");

            _service.EncolarAltaInteresXSeleccionEnSitio(
                _uowMock.Object,
                RequestBase(TivenosAltaInteresOperacion.ModificarActualizarInteres()),
                777,
                "Test");

            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.TipoProcesoLlamador == "Modificar" &&
                e.Disparador == "ActualizarInteres" &&
                e.OrigenLlamador == null)), Times.Once);
        }

        [Fact]
        public void EncolarAltaInteresXSeleccionEnSitio_ParaProductoNuevoEnInteresExistente_EncolaAltaConOrigenSis()
        {
            _parametroRepoMock
                .Setup(r => r.ObtenerSeLiberoTivenos())
                .Returns("SI");

            _service.EncolarAltaInteresXSeleccionEnSitio(
                _uowMock.Object,
                RequestBase(TivenosAltaInteresOperacion.AltaActualizarInteres()),
                777,
                "Test");

            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.TipoProcesoLlamador == "Alta" &&
                e.Disparador == "ActualizarInteres" &&
                e.OrigenLlamador == "SIS")), Times.Once);
        }

        [Fact]
        public void EncolarAltaDatosBachillerato_ConTivenosLiberado_EncolaPayloadLegacy()
        {
            _parametroRepoMock
                .Setup(r => r.ObtenerSeLiberoTivenos())
                .Returns("SI");

            var result = _service.EncolarAltaDatosBachillerato(
                _uowMock.Object,
                new TivenosBachilleratoRequest
                {
                    CodigoPersona = 123,
                    CodigoOrientacion = 1304
                },
                888,
                "Test");

            Assert.True(result.Success);
            Assert.True(result.Data);
            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.IdEnvioParaTivenos == 888 &&
                e.Origen == "ADMISIONES" &&
                e.TipoProcesoLlamador == "Alta" &&
                e.Disparador == "AltaBachilleratoPersona" &&
                e.Modulo == "Bachillerato" &&
                e.Metodo == "AltaDatosBachillerato" &&
                e.Status == "Nuevo" &&
                e.CodigoSape == 123 &&
                e.BachilleratoOrientacionId == 1304)), Times.Once);
        }

        [Fact]
        public void EncolarModificacionDatosBachillerato_ConTivenosLiberado_EncolaPayloadLegacy()
        {
            _parametroRepoMock
                .Setup(r => r.ObtenerSeLiberoTivenos())
                .Returns("SI");

            var result = _service.EncolarModificacionDatosBachillerato(
                _uowMock.Object,
                new TivenosBachilleratoRequest
                {
                    CodigoPersona = 123,
                    CodigoOrientacion = null
                },
                889,
                "Test");

            Assert.True(result.Success);
            Assert.True(result.Data);
            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.IdEnvioParaTivenos == 889 &&
                e.Origen == "ADMISIONES" &&
                e.TipoProcesoLlamador == "Modificacion" &&
                e.Disparador == "ModificacionBachilleratoPersona" &&
                e.Modulo == "Bachillerato" &&
                e.Metodo == "ModificacionDatosBachillerato" &&
                e.Status == "Nuevo" &&
                e.CodigoSape == 123 &&
                e.BachilleratoOrientacionId == null)), Times.Once);
        }

        private static TivenosAltaInteresRequest RequestBase(TivenosAltaInteresOperacion operacion) => new()
        {
            CodigoPersona = 123,
            IdProducto = 10,
            IdProceso = 20,
            Operacion = operacion,
        };
    }
}
