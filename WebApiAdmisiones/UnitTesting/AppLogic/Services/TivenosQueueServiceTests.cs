using AppLogic.Integrations.Tivenos.Dtos;
using AppLogic.Integrations.Tivenos.Interfaces;
using AppLogic.Integrations.Tivenos.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class TivenosQueueServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IEnvioParaTivenoRepository> _envioParaTivenosRepoMock = new();
        private readonly TivenosQueueService _service = new();

        public TivenosQueueServiceTests()
        {
            _uowMock.Setup(u => u.EnvioParaTivenos).Returns(_envioParaTivenosRepoMock.Object);
        }

        [Fact]
        public void EncolarAltaInteresXSeleccionEnSitio_EncolaPayload()
        {
            var result = _service.EnqueueProductInterestFromSiteSelection(
                _uowMock.Object,
                RequestBase(TivenosAltaInteresOperacion.CreateProductInterest()),
                777);

            Assert.True(result);
            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.IdEnvioParaTivenos == 777 &&
                e.Origen == "ADMISIONES" &&
                e.TipoProcesoLlamador == "Alta" &&
                e.Disparador == "CreateProductInterest" &&
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
        public void EncolarAltaInteresXSeleccionEnSitio_ParaProductoExistenteActualizado_EncolaModificar()
        {
            _service.EnqueueProductInterestFromSiteSelection(
                _uowMock.Object,
                RequestBase(TivenosAltaInteresOperacion.UpdateInterest()),
                777);

            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.TipoProcesoLlamador == "Modificar" &&
                e.Disparador == "ActualizarInteres" &&
                e.OrigenLlamador == null)), Times.Once);
        }

        [Fact]
        public void EncolarAltaInteresXSeleccionEnSitio_ParaProductoNuevoEnInteresExistente_EncolaAltaConOrigenSis()
        {
            _service.EnqueueProductInterestFromSiteSelection(
                _uowMock.Object,
                RequestBase(TivenosAltaInteresOperacion.CreateOrUpdateInterest()),
                777);

            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.TipoProcesoLlamador == "Alta" &&
                e.Disparador == "ActualizarInteres" &&
                e.OrigenLlamador == "SIS")), Times.Once);
        }

        [Fact]
        public void EncolarRegistroDesdeSitioAdmisiones_EncolaPayload()
        {
            var result = _service.EnqueueSiteRegistration(
                _uowMock.Object,
                RequestBase(TivenosAltaInteresOperacion.SiteRegistration()),
                999);

            Assert.True(result);
            _envioParaTivenosRepoMock.Verify(r => r.Add(It.Is<EnvioParaTiveno>(e =>
                e.IdEnvioParaTivenos == 999 &&
                e.Origen == "ADMISIONES" &&
                e.OrigenLlamador == null &&
                e.TipoProcesoLlamador == "Alta" &&
                e.Disparador == "Registro" &&
                e.Modulo == "InteresPersona" &&
                e.Metodo == "RegistroDesdeSitioAdmisiones" &&
                e.Status == "Nuevo" &&
                e.CodigoSape == 123 &&
                e.ProcesoId == 20 &&
                e.ProductoId == 10 &&
                e.InteresProdGradoInteresId == null)), Times.Once);
        }

        [Fact]
        public void EncolarAltaDatosBachillerato_EncolaPayload()
        {
            var result = _service.EnqueueHighSchoolDataCreation(
                _uowMock.Object,
                new DtoTivenosBachilleratoRequest
                {
                    CodigoPersona = 123,
                    CodigoOrientacion = 1304
                },
                888);

            Assert.True(result);
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
        public void EncolarModificacionDatosBachillerato_EncolaPayload()
        {
            var result = _service.EnqueueHighSchoolDataUpdate(
                _uowMock.Object,
                new DtoTivenosBachilleratoRequest
                {
                    CodigoPersona = 123,
                    CodigoOrientacion = null
                },
                889);

            Assert.True(result);
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

        private static DtoTivenosAltaInteresRequest RequestBase(TivenosAltaInteresOperacion operacion) => new()
        {
            CodigoPersona = 123,
            IdProducto = 10,
            IdProceso = 20,
            Operacion = operacion,
        };
    }
}
