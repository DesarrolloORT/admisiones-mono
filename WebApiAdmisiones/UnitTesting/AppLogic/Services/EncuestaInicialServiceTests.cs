using AppLogic.Inscripciones.Encuesta.Dtos;
using AppLogic.Catalogos.Interfaces;
using AppLogic.Inscripciones.Interfaces;
using AppLogic.Inscripciones.Encuesta.Services;
using AppLogic.Tivenos.Dtos;
using AppLogic.Tivenos.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    /// <summary>
    /// Tests dedicados de <see cref="EncuestaInicialService"/> resuelto vía <see cref="IEncuestaInicialService"/>.
    /// Creado junto al refactor que sacó el <c>new EncuestaInicialService(...)</c> de <c>InscripcionesService</c>
    /// (rama refactor/encuesta-inicial-di), demostrando que la clase ahora es testeable de forma independiente.
    /// Los escenarios profundos (definitiva, bachillerato legacy 1304, Tivenos) siguen cubiertos por
    /// <c>InscripcionesServiceTests</c>, que ahora ejercitan la implementación real inyectada por DI.
    /// </summary>
    public class EncuestaInicialServiceTests
    {
        private static readonly DateTime FechaBase = new(2026, 5, 27, 10, 30, 0);

        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock = new();
        private readonly Mock<IGeneralService> _generalServiceMock = new();
        private readonly Mock<ITivenosEnvioService> _tivenosEnvioServiceMock = new();
        private readonly IEncuestaInicialService _service;

        public EncuestaInicialServiceTests()
        {
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _dbConnectionContextMock.Setup(d => d.CurrentDateTime()).Returns(FechaBase);

            _service = new EncuestaInicialService(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _generalServiceMock.Object,
                _tivenosEnvioServiceMock.Object);
        }

        [Fact]
        public void ObtenerEncuestaInicial_PersonaNoEncontrada_DevuelveNotFound()
        {
            SetupPersona(null);

            var result = _service.ObtenerEncuestaInicial(123);

            Assert.False(result.Success);
            Assert.Equal(404, result.HttpCode);
            Assert.Equal("GEN_OEI_01", result.ErrorCode);
        }

        [Fact]
        public void GuardarEncuestaInicial_PersonaNoEncontrada_DevuelveNotFound()
        {
            SetupPersona(null);

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest());

            Assert.False(result.Success);
            Assert.Equal(404, result.HttpCode);
            Assert.Equal("INS_EI_01", result.ErrorCode);
        }

        [Fact]
        public void ObtenerEncuestaInicial_ConDerechoSinEncuesta_DevuelveTieneDerechoSinEncuesta()
        {
            SetupPersonaValida();
            SetupReposDerecho(encuestaExistente: null);

            var result = _service.ObtenerEncuestaInicial(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data!.TieneDerechoEncuesta);
            Assert.Null(result.Data.Encuesta);
        }

        [Fact]
        public void GuardarEncuestaInicial_ParcialMinimo_CreaEncuestaTemporal()
        {
            SetupPersonaValida();

            EncuestaIniAdmision? encuestaAgregada = null;
            var encuestaRepo = SetupReposDerecho(encuestaExistente: null);
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(900);

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest());

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(900, encuestaAgregada!.IdEncuestaIni);
            Assert.Equal("TEMPORAL", encuestaAgregada.EstadoEncuestaIniAdmision);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
            // Encuesta incompleta: no debe finalizar como definitiva ni encolar bachillerato.
            _tivenosEnvioServiceMock.Verify(
                s => s.EncolarAltaDatosBachillerato(It.IsAny<IUnitOfWork>(), It.IsAny<DtoTivenosBachilleratoRequest>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        // ---- Helpers de setup (harness compacto, solo lo necesario) ----

        private void SetupPersona(Persona? persona)
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
        }

        private void SetupPersonaValida() =>
            SetupPersona(new Persona { CodigoPersona = 123, TipoDocumento = "DE", Documento = "123" });

        /// <summary>
        /// Configura los repos que determinan el derecho a encuesta (todos "no existe" =&gt; tiene derecho)
        /// y devuelve el mock de EncuestaIniAdmisions para setups adicionales.
        /// </summary>
        private Mock<IEncuestaIniAdmisionRepository> SetupReposDerecho(EncuestaIniAdmision? encuestaExistente)
        {
            var frescoRepo = new Mock<IVdEsFrescoAdmisionRepository>();
            frescoRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(false);
            _uowMock.Setup(u => u.VdEsFrescoAdmisions).Returns(frescoRepo.Object);

            var encuestaIniRepo = new Mock<IEncuestaIniRepository>();
            encuestaIniRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(false);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaIniRepo.Object);

            var encuestaAdmRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaAdmRepo.Setup(r => r.ExisteCompletaPorDocumento("DE", "123")).Returns(false);
            encuestaAdmRepo.Setup(r => r.GetByPersona(123)).Returns(encuestaExistente);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaAdmRepo.Object);

            return encuestaAdmRepo;
        }
    }
}
