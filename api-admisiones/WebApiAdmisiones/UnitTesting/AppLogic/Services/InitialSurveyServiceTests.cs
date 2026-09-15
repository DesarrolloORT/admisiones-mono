using AppLogic.Enrollments.Survey.Dtos;
using AppLogic.Catalogs.Interfaces;
using AppLogic.Enrollments.Interfaces;
using AppLogic.Enrollments.Survey.Services;
using AppLogic.Integrations.Tivenos.Dtos;
using AppLogic.Integrations.Tivenos.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    /// <summary>
    /// Tests dedicados de <see cref="InitialSurveyService"/> resuelto vía <see cref="IInitialSurveyService"/>.
    /// Creado junto al refactor que sacó el <c>new InitialSurveyService(...)</c> de <c>InscripcionesService</c>
    /// (rama refactor/encuesta-inicial-di), demostrando que la clase ahora es testeable de forma independiente.
    /// Los escenarios profundos (definitiva, bachillerato legacy 1304, Tivenos) siguen cubiertos por
    /// <c>EnrollmentUseCasesTests</c>, que ahora ejercitan la implementación real inyectada por DI.
    /// </summary>
    public class InitialSurveyServiceTests
    {
        private static readonly DateTime FechaBase = new(2026, 5, 27, 10, 30, 0);

        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock = new();
        private readonly Mock<IAdmissionDueDateCalculator> _generalServiceMock = new();
        private readonly Mock<ITivenosQueueService> _tivenosEnvioServiceMock = new();
        private readonly IInitialSurveyService _service;

        public InitialSurveyServiceTests()
        {
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _dbConnectionContextMock.Setup(d => d.CurrentDateTime()).Returns(FechaBase);

            _service = new InitialSurveyService(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _generalServiceMock.Object,
                _tivenosEnvioServiceMock.Object);
        }

        [Fact]
        public void ObtenerEncuestaInicial_PersonaNoEncontrada_DevuelveNotFound()
        {
            SetupPersona(null);

            var result = _service.GetInitialSurvey(123);

            Assert.False(result.Success);
            Assert.Equal(404, result.HttpCode);
            Assert.Equal("GEN_OEI_01", result.ErrorCode);
        }

        [Fact]
        public void GuardarEncuestaInicial_PersonaNoEncontrada_DevuelveNotFound()
        {
            SetupPersona(null);

            var result = _service.SaveInitialSurvey(123, new SaveInitialSurveyRequest());

            Assert.False(result.Success);
            Assert.Equal(404, result.HttpCode);
            Assert.Equal("INS_EI_01", result.ErrorCode);
        }

        [Fact]
        public void ObtenerEncuestaInicial_ConDerechoSinEncuesta_DevuelveTieneDerechoSinEncuesta()
        {
            SetupPersonaValida();
            SetupReposDerecho(encuestaExistente: null);

            var result = _service.GetInitialSurvey(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data!.CanAnswerSurvey);
            Assert.Null(result.Data.Survey);
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

            var result = _service.SaveInitialSurvey(123, new SaveInitialSurveyRequest());

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(900, encuestaAgregada!.IdEncuestaIni);
            Assert.Equal("TEMPORAL", encuestaAgregada.EstadoEncuestaIniAdmision);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
            // Encuesta incompleta: no debe finalizar como definitiva ni encolar bachillerato.
            _tivenosEnvioServiceMock.Verify(
                s => s.EnqueueHighSchoolDataCreation(It.IsAny<IUnitOfWork>(), It.IsAny<DtoTivenosBachilleratoRequest>(), It.IsAny<int>()),
                Times.Never);
        }

        /// <summary>
        /// Una encuesta CONFIRMADO (completa) sigue siendo editable: el postulante puede corregir una
        /// sección mientras no confirme la preinscripción. Antes devolvía 403 INS_EI_56 porque el
        /// derecho a encuesta consultaba ExisteCompletaPorDocumento.
        /// </summary>
        [Fact]
        public void GuardarEncuestaInicial_CompletaSinConfirmar_PermiteCorregir()
        {
            SetupPersonaValida();
            var encuestaCompleta = new EncuestaIniAdmision
            {
                IdEncuestaIni = 900,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "CONFIRMADO",
                FechaProcesadoEncuestaIni = null
            };
            var encuestaRepo = SetupReposDerecho(encuestaCompleta);

            var result = _service.SaveInitialSurvey(123, new SaveInitialSurveyRequest());

            Assert.True(result.Success);
            encuestaRepo.Verify(r => r.Update(encuestaCompleta), Times.AtLeastOnce);
            encuestaRepo.Verify(r => r.Add(It.IsAny<EncuestaIniAdmision>()), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        /// <summary>
        /// El estado DEFINITIVO cierra la encuesta apenas se confirma la preinscripción, incluso si
        /// LogicaORT todavía no la migró porque la inscripción quedó "a la espera" en bandeja.
        /// </summary>
        [Fact]
        public void GuardarEncuestaInicial_ConfirmadaSinProcesar_DevuelveConflicto()
        {
            SetupPersonaValida();
            var encuestaSellada = new EncuestaIniAdmision
            {
                IdEncuestaIni = 900,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "DEFINITIVO",
                FechaProcesadoEncuestaIni = null
            };
            var encuestaRepo = SetupReposDerecho(encuestaSellada);

            var result = _service.SaveInitialSurvey(123, new SaveInitialSurveyRequest());

            Assert.False(result.Success);
            Assert.Equal(409, result.HttpCode);
            Assert.Equal("INS_EI_57", result.ErrorCode);
            encuestaRepo.Verify(r => r.Update(It.IsAny<EncuestaIniAdmision>()), Times.Never);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
        }

        /// <summary>
        /// Con FECHA_PROCESADO_ENCUESTA_INI sellada, LogicaORT ya copió la encuesta a T_ENCUESTA_INI:
        /// desde ahí no admite cambios.
        /// </summary>
        [Fact]
        public void GuardarEncuestaInicial_YaProcesada_DevuelveConflicto()
        {
            SetupPersonaValida();
            var encuestaProcesada = new EncuestaIniAdmision
            {
                IdEncuestaIni = 900,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "DEFINITIVO",
                FechaProcesadoEncuestaIni = FechaBase
            };
            var encuestaRepo = SetupReposDerecho(encuestaProcesada);

            var result = _service.SaveInitialSurvey(123, new SaveInitialSurveyRequest());

            Assert.False(result.Success);
            Assert.Equal(409, result.HttpCode);
            Assert.Equal("INS_EI_57", result.ErrorCode);
            encuestaRepo.Verify(r => r.Update(It.IsAny<EncuestaIniAdmision>()), Times.Never);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
        }

        /// <summary>
        /// Una encuesta CONFIRMADO (completa, sin confirmar la preinscripción) se sigue devolviendo: el
        /// postulante que salió del flujo y vuelve tiene que poder ver y corregir lo que cargó.
        /// </summary>
        [Fact]
        public void ObtenerEncuestaInicial_CompletaSinConfirmar_DevuelveEncuesta()
        {
            SetupPersonaValida();
            SetupReposDerecho(new EncuestaIniAdmision
            {
                IdEncuestaIni = 900,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "CONFIRMADO",
                FechaProcesadoEncuestaIni = null
            });

            var result = _service.GetInitialSurvey(123);

            Assert.True(result.Success);
            Assert.True(result.Data!.CanAnswerSurvey);
            Assert.NotNull(result.Data.Survey);
            Assert.Equal(900, result.Data.Survey.SurveyId);
        }

        /// <summary>
        /// Preinscripción confirmada que quedó "a la espera": FECHA_PROCESADO_ENCUESTA_INI sigue en NULL
        /// porque LogicaORT no llegó a migrar, pero la encuesta ya está completa y no se vuelve a pedir.
        /// Es el caso que reportaba "todavía no completó la encuesta".
        /// </summary>
        [Fact]
        public void ObtenerEncuestaInicial_ConfirmadaSinProcesar_NoDevuelveEncuesta()
        {
            SetupPersonaValida();
            SetupReposDerecho(new EncuestaIniAdmision
            {
                IdEncuestaIni = 900,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "DEFINITIVO",
                FechaProcesadoEncuestaIni = null
            });

            var result = _service.GetInitialSurvey(123);

            Assert.True(result.Success);
            Assert.False(result.Data!.CanAnswerSurvey);
            Assert.Null(result.Data.Survey);
        }

        /// <summary>
        /// Procesada por LogicaORT (migrada a T_ENCUESTA_INI): la encuesta deja de mostrarse.
        /// </summary>
        [Fact]
        public void ObtenerEncuestaInicial_YaProcesada_NoDevuelveEncuesta()
        {
            SetupPersonaValida();
            SetupReposDerecho(new EncuestaIniAdmision
            {
                IdEncuestaIni = 900,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "DEFINITIVO",
                FechaProcesadoEncuestaIni = FechaBase
            });

            var result = _service.GetInitialSurvey(123);

            Assert.True(result.Success);
            Assert.False(result.Data!.CanAnswerSurvey);
            Assert.Null(result.Data.Survey);
        }

        /// <summary>
        /// El derecho a encuesta también corta por la tabla final: una vez que LogicaORT insertó en
        /// T_ENCUESTA_INI al confirmar la preinscripción, el GET no devuelve nada.
        /// </summary>
        [Fact]
        public void ObtenerEncuestaInicial_ConEncuestaHistorica_NoDevuelveEncuesta()
        {
            SetupPersonaValida();
            SetupReposDerecho(encuestaExistente: null, existeEncuestaIni: true);

            var result = _service.GetInitialSurvey(123);

            Assert.True(result.Success);
            Assert.False(result.Data!.CanAnswerSurvey);
            Assert.Null(result.Data.Survey);
        }

        /// <summary>
        /// El departamento no se guarda en T_ENCUESTA_INI_ADMISION: se deriva del CODIGO_ESTADO de la
        /// institución elegida para que el front pueda precargar el combo al reabrir la encuesta.
        /// </summary>
        [Fact]
        public void ObtenerEncuestaInicial_InstitucionUruguaya_DevuelveDepartamentoDeLaInstitucion()
        {
            SetupPersonaValida();
            SetupReposDerecho(SurveyConInstitucion(ubicacionUltimoAnio: 1));
            SetupInstitucion(codigoEmpresa: 4821, codigoEstado: 10);

            var result = _service.GetInitialSurvey(123);

            Assert.True(result.Success);
            Assert.Equal(10, result.Data!.Survey!.SecondaryInstitutionStateId);
        }

        /// <summary>
        /// Cursó en el exterior: CODIGO_INSTITUCION_BAC queda en la institución legacy (ORT Uruguay),
        /// así que devolver su departamento sería un dato falso.
        /// </summary>
        [Fact]
        public void ObtenerEncuestaInicial_UltimoAnioEnElExterior_NoDevuelveDepartamento()
        {
            SetupPersonaValida();
            SetupReposDerecho(SurveyConInstitucion(ubicacionUltimoAnio: 2));
            SetupInstitucion(codigoEmpresa: 4821, codigoEstado: 10);

            var result = _service.GetInitialSurvey(123);

            Assert.True(result.Success);
            Assert.Null(result.Data!.Survey!.SecondaryInstitutionStateId);
        }

        // ---- Helpers de setup (harness compacto, solo lo necesario) ----

        private static EncuestaIniAdmision SurveyConInstitucion(short ubicacionUltimoAnio) => new()
        {
            IdEncuestaIni = 900,
            CodigoPersona = 123,
            EstadoEncuestaIniAdmision = "TEMPORAL",
            CodigoInstitucionBac = 4821,
            UltimoanioSecundariaEncuestaIni = ubicacionUltimoAnio
        };

        private void SetupInstitucion(long codigoEmpresa, long codigoEstado)
        {
            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(codigoEmpresa))
                .Returns(new Empresa { CodigoEmpresa = codigoEmpresa, CodigoEstado = codigoEstado });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);
        }

        private void SetupPersona(Persona? person)
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(person);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
        }

        private void SetupPersonaValida() =>
            SetupPersona(new Persona { CodigoPersona = 123, TipoDocumento = "DE", Documento = "123" });

        /// <summary>
        /// Configura los repos que determinan el derecho a encuesta (todos "no existe" =&gt; tiene derecho)
        /// y devuelve el mock de EncuestaIniAdmisions para setups adicionales.
        /// </summary>
        private Mock<IEncuestaIniAdmisionRepository> SetupReposDerecho(
            EncuestaIniAdmision? encuestaExistente,
            bool existeEncuestaIni = false)
        {
            var frescoRepo = new Mock<IVdEsFrescoAdmisionRepository>();
            frescoRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(false);
            _uowMock.Setup(u => u.VdEsFrescoAdmisions).Returns(frescoRepo.Object);

            var encuestaIniRepo = new Mock<IEncuestaIniRepository>();
            encuestaIniRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(existeEncuestaIni);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaIniRepo.Object);

            var encuestaAdmRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaAdmRepo.Setup(r => r.GetByPersona(123)).Returns(encuestaExistente);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaAdmRepo.Object);

            return encuestaAdmRepo;
        }
    }
}
