using AppLogic.Enrollments.UseCases;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.UseCases.Payments;
using AppLogic.Contracts.Constants;
using AppLogic.Enrollments.Survey.Dtos;
using AppLogic.Enrollments.Dtos;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using AppLogic.Integrations.EnrollmentsAndPayments.Services;
using AppLogic.DevartDTOs;
using AppLogic.Catalogs.Interfaces;
using AppLogic.Enrollments.Interfaces;
using AppLogic.Enrollments.Services;
using AppLogic.Enrollments.Survey.Services;
using AppLogic.Integrations.Tivenos.Dtos;
using AppLogic.Integrations.Tivenos.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModBandejaAppLogic.DevartDTOs;
using ModBandejaAppLogic.Interfaces;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class EnrollmentUseCasesTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<IAdmissionDueDateCalculator> _generalServiceMock;
        private readonly Mock<ITivenosQueueService> _tivenosEnvioServiceMock;
        private readonly Mock<IBandejaService> _bandejaServiceMock;
        private readonly Mock<IServiceScopeFactory> _bandejaServiceScopeFactoryMock;
        private readonly InitialSurveyService _encuesta;
        private readonly EnrollmentUseCases _service;
        private readonly IStartEnrollmentPayment _payments;

        public EnrollmentUseCasesTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _generalServiceMock = new Mock<IAdmissionDueDateCalculator>();
            _tivenosEnvioServiceMock = new Mock<ITivenosQueueService>();
            _bandejaServiceMock = new Mock<IBandejaService>();
            _bandejaServiceScopeFactoryMock = CrearBandejaServiceScopeFactoryMock(_bandejaServiceMock.Object);
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _dbConnectionContextMock
                .Setup(d => d.CurrentDateTime())
                .Returns(FechaBase);
            _generalServiceMock
                .Setup(s => s.CalculateAdmissionDueDate(It.IsAny<IUnitOfWork>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(global::Utilities.OperationResult<DateTime>.Ok(FechaBase.AddDays(5), nameof(IAdmissionDueDateCalculator.CalculateAdmissionDueDate)));
            var encuestaIniRepo = new Mock<IEncuestaIniRepository>();
            encuestaIniRepo
                .Setup(r => r.GetByPersona(It.IsAny<long>()))
                .Returns((EncuestaIni)null);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaIniRepo.Object);
            var ofertasDisponibles3y4Repo = new Mock<IVdOfertasDisponibles3y4Repository>();
            ofertasDisponibles3y4Repo
                .Setup(r => r.GetOfertasDisponibles(It.IsAny<long>()))
                .Returns(new List<VdOfertasDisponibles3y4>());
            _uowMock.Setup(u => u.VdOfertasDisponibles3y4s).Returns(ofertasDisponibles3y4Repo.Object);
            _tivenosEnvioServiceMock
                .Setup(s => s.EnqueueProductInterestFromSiteSelection(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosAltaInteresRequest>(),
                    It.IsAny<int>()))
                .Returns(true);
            _tivenosEnvioServiceMock
                .Setup(s => s.EnqueueSiteRegistration(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosAltaInteresRequest>(),
                    It.IsAny<int>()))
                .Returns(true);
            _tivenosEnvioServiceMock
                .Setup(s => s.EnqueueHighSchoolDataCreation(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosBachilleratoRequest>(),
                    It.IsAny<int>()))
                .Returns(true);
            _tivenosEnvioServiceMock
                .Setup(s => s.EnqueueHighSchoolDataUpdate(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosBachilleratoRequest>(),
                    It.IsAny<int>()))
                .Returns(true);
            var apiClient = new EnrollmentsAndPaymentsApiClient(
                new HttpClient { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<EnrollmentsAndPaymentsApiClient>.Instance);
            _service = CrearCasosDeUso(apiClient);
            _payments = CrearPagos(apiClient);
            _encuesta = CrearEncuestaInicialServiceReal();
        }

        // InscripcionesService resuelve IBandejaService desde un scope de DI propio por cada
        // oferta (ver ConfirmarInscripcionCorporativa), asi que el mock del scope factory siempre
        // devuelve el mismo IBandejaService mockeado, sin importar cuantas veces se llame.
        private static Mock<IServiceScopeFactory> CrearBandejaServiceScopeFactoryMock(IBandejaService bandejaService)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(p => p.GetService(typeof(IBandejaService))).Returns(bandejaService);

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
            return scopeFactoryMock;
        }

        // Construye la implementación real de InitialSurveyService con los mismos mocks,
        // para preservar la cobertura profunda de encuesta que ya ejercitan estos tests
        // (ahora vía la dependencia inyectada en lugar del antiguo `new` interno).
        private InitialSurveyService CrearEncuestaInicialServiceReal() =>
            new(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _generalServiceMock.Object,
                _tivenosEnvioServiceMock.Object);

        private static readonly DateTime FechaBase = new(2026, 5, 27, 10, 30, 0);


        [Fact]
        public void ObtenerAceptacionReglamentoEstudiantil_WhenNoAcceptance_ReturnsFalseWithoutDate()
        {
            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetPrimeraByPersona(123))
                .Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = _service.Regulations.Execute(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.False(result.Data!.AcceptedStudentRegulations);
            Assert.Null(result.Data.AcceptanceDate);
            aceptacionRepo.Verify(r => r.GetPrimeraByPersona(123), Times.Once);
        }

        [Fact]
        public void ObtenerAceptacionReglamentoEstudiantil_WhenAcceptanceExists_ReturnsTrueWithFirstDate()
        {
            var primeraFecha = new DateTime(2024, 3, 15);
            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetPrimeraByPersona(123))
                .Returns(new AceptacionReglamentoEst
                {
                    IdAceptacionReglamentoEst = 10,
                    CodigoPersona = 123,
                    FechaIngreso = primeraFecha
                });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = _service.Regulations.Execute(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data!.AcceptedStudentRegulations);
            Assert.Equal(primeraFecha, result.Data.AcceptanceDate);
            aceptacionRepo.Verify(r => r.GetPrimeraByPersona(123), Times.Once);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenReglamentoNotAcceptedAndNoPriorAcceptance_ReturnsBadRequest()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns((AceptacionReglamentoEst)null);
            aceptacionRepo
                .Setup(r => r.GetByPersona(123))
                .Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = false,
                SelectedOfferingIds = [10]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_EncuestaCompletaVencidaSinConfirmacionHistorica_DevuelveEncuestaVencida()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompletaVencida(123));

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_12", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_EncuestaCompletaVencidaPeroYaConfirmadaEnEncuestaIni_NoDevuelveEncuestaVencida()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompletaVencida(123));
            SetupEncuestaIni(123, new EncuestaIni { CodigoPersona = 123 });
            SetupDocumentosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns((AceptacionReglamentoEst)null);
            aceptacionRepo
                .Setup(r => r.GetByPersona(123))
                .Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = false,
                SelectedOfferingIds = [10]
            });

            // Llega a la validacion de reglamento (INS_CPI_02): prueba que no corto antes por INS_CPI_12.
            Assert.False(result.Success);
            Assert.Equal("INS_CPI_02", result.ErrorCode);
        }

        /// <summary>
        /// La inscripcion quedo "a la espera" (LogicaORT la derivo a bandeja, p.ej. por semestre 2). Es un
        /// resultado exitoso: la encuesta queda sellada en DEFINITIVO aunque LogicaORT no llegue a migrarla
        /// ni a sellar FECHA_PROCESADO_ENCUESTA_INI, que es la puerta de esa migracion diferida.
        /// </summary>
        [Fact]
        public async Task ConfirmarPreInscripcion_QuedaALaEspera_SellaEncuestaComoDefinitivo()
        {
            var service = CrearServiceConApi(new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """
                {
                  "respuesta": true,
                  "confirmada": true,
                  "inscripcionPendiente": true,
                  "ofertas": []
                }
                """)));

            var encuesta = EncuestaCompleta(123);
            SetupConfirmacionSimple(encuesta);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Waiting);
            Assert.Equal("DEFINITIVO", encuesta.EstadoEncuestaIniAdmision);
            Assert.Null(encuesta.FechaProcesadoEncuestaIni);
        }

        /// <summary>
        /// El sellado no se revierte cuando la confirmacion falla: un rechazo de LogicaORT (oferta cerrada,
        /// plan de pago) no se arregla editando la encuesta. Lo que si tiene que seguir funcionando es el
        /// reintento, y por eso las reglas de confirmacion aceptan DEFINITIVO como encuesta completa.
        /// </summary>
        [Fact]
        public async Task ConfirmarPreInscripcion_EncuestaYaSellada_PermiteReintentar()
        {
            var service = CrearServiceConApi(new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """
                {
                  "respuesta": true,
                  "confirmada": true,
                  "inscripcionPendiente": true,
                  "ofertas": []
                }
                """)));

            var encuesta = EncuestaCompleta(123);
            encuesta.EstadoEncuestaIniAdmision = "DEFINITIVO";
            SetupConfirmacionSimple(encuesta);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Waiting);
            Assert.Equal("DEFINITIVO", encuesta.EstadoEncuestaIniAdmision);
        }

        /// <summary>
        /// Nivel 3 y 4 no requieren encuesta: sin fila, el sellado es no-op y la confirmacion sigue igual.
        /// </summary>
        [Fact]
        public async Task ConfirmarPreInscripcion_SinEncuesta_NoRompeElSellado()
        {
            var service = CrearServiceConApi(new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """
                {
                  "respuesta": true,
                  "confirmada": true,
                  "inscripcionPendiente": true,
                  "ofertas": []
                }
                """)));

            SetupConfirmacionSimple(null, productLevelId: 3);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Waiting);
        }

        /// <summary>Escenario minimo de confirmacion online de una sola oferta.</summary>
        private void SetupConfirmacionSimple(EncuestaIniAdmision? encuesta, long productLevelId = 1)
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1, productLevelId: productLevelId);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, encuesta!);
            SetupDocumentosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns(new AceptacionReglamentoEst { IdAceptacionReglamentoEst = 999, CodigoPersona = 123 });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenReglamentoNotAcceptedButHasPriorAcceptance_Confirms()
        {
            AceptacionReglamentoEst? aceptacionAgregada = null;
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "idInscripcion": 80
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST))
                .Returns(1000);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns((AceptacionReglamentoEst)null);
            aceptacionRepo
                .Setup(r => r.GetByPersona(123))
                .Returns(new AceptacionReglamentoEst { IdAceptacionReglamentoEst = 500, CodigoPersona = 123 });
            aceptacionRepo
                .Setup(r => r.Add(It.IsAny<AceptacionReglamentoEst>()))
                .Callback<AceptacionReglamentoEst>(a => aceptacionAgregada = a);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(80)).Returns(new Inscripto { IdInscripto = 80 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = false,
                SelectedOfferingIds = [10]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.NotNull(aceptacionAgregada);
            Assert.Equal(1000, aceptacionAgregada!.IdAceptacionReglamentoEst);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_EsInscripcionCorporativaNivel3_AltaEnBandejaSinLlamarApi()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1, productLevelId: 3);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns(new AceptacionReglamentoEst { IdAceptacionReglamentoEst = 999, CodigoPersona = 123 });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            DtoTramiteBandejaDevartModBandeja? dtoTramite = null;
            DtoInstanciaWorkflowDevartModBandeja? dtoInstancia = null;
            IEnumerable<DtoBandejaDevartModBandeja>? dtosBandeja = null;
            _bandejaServiceMock
                .Setup(s => s.AltaTramiteWorkflow(
                    It.IsAny<DtoTramiteBandejaDevartModBandeja>(),
                    It.IsAny<DtoInstanciaWorkflowDevartModBandeja>(),
                    It.IsAny<IEnumerable<DtoBandejaDevartModBandeja>>()))
                .Callback<DtoTramiteBandejaDevartModBandeja, DtoInstanciaWorkflowDevartModBandeja, IEnumerable<DtoBandejaDevartModBandeja>>(
                    (t, i, b) => { dtoTramite = t; dtoInstancia = i; dtosBandeja = b; })
                .Returns(global::Utilities.OperationResult<long>.Ok(555, "AltaTramiteWorkflow"));

            var instInscripciones = new List<InstWorkflowInscripcion>();
            var instInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instInscripcionRepo.Setup(r => r.Add(It.IsAny<InstWorkflowInscripcion>()))
                .Callback<InstWorkflowInscripcion>(x => instInscripciones.Add(x));
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instInscripcionRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = new() { 10 },
                IsCorporateEnrollment = true
            });

            Assert.True(result.Success);
            Assert.False(result.Data!.Confirmed);
            Assert.True(result.Data.Waiting);
            // "A la espera" es una pantalla generica: el response no trae detalle de ofertas.
            Assert.Empty(result.Data.Enrollments);
            // Se carga una fila en T_INST_WORKFLOW_INSCRIPCION (1:1 con la instancia).
            var fila = Assert.Single(instInscripciones);
            Assert.Equal(555m, fila.IdInstanciaWorkflow);
            Assert.Equal(10L, fila.IdOferta!.Value);
            Assert.Equal(40m, fila.IdComienzo!.Value);
            Assert.Equal(1m, fila.IdTurno!.Value);
            Assert.Equal(20m, fila.IdProducto!.Value);
            Assert.NotNull(dtoTramite);
            Assert.Equal(89, dtoTramite!.IdProceso);
            Assert.Equal(48, dtoTramite.IdGrupoResponsable);
            Assert.NotNull(dtoInstancia);
            Assert.Equal(89, dtoInstancia!.IdProceso);
            Assert.Contains("AP", dtoInstancia.XmlInstanciaWorkflow);
            Assert.NotNull(dtosBandeja);
            var bandejas = dtosBandeja!.ToList();
            Assert.Equal(2, bandejas.Count);
            // Ids de estado de proceso configurados por ambiente: los de CorporateInboxConfiguration.
            var inicio = Assert.Single(bandejas, b => b.IdEstadoProceso == 11111);
            Assert.Equal(48, inicio.IdGrupoResponsable);
            Assert.NotNull(inicio.FechaTomadoBandeja);
            Assert.Equal("SIGUIENTE", inicio.AccionMenu);
            var request = Assert.Single(bandejas, b => b.IdEstadoProceso == 22222);
            Assert.Equal(48, request.IdGrupoResponsable);
            Assert.Equal(DateTime.MinValue, request.FechaTomadoBandeja);
            _bandejaServiceMock.Verify(
                s => s.AltaTramiteWorkflow(
                    It.IsAny<DtoTramiteBandejaDevartModBandeja>(),
                    It.IsAny<DtoInstanciaWorkflowDevartModBandeja>(),
                    It.IsAny<IEnumerable<DtoBandejaDevartModBandeja>>()),
                Times.Once);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_EsInscripcionCorporativaMultiplesOfertas_CreaUnTramiteYFilaPorOferta()
        {
            SetupPersona(123);

            // Dos ofertas del mismo producto (20) y turno (1) con comienzos distintos: valido en nivel 3/4.
            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(10))
                .Returns(OfertaValida(10, 20, 40, 1, nombreComienzo: "Marzo 2026", productLevelId: 3));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(11))
                .Returns(OfertaValida(11, 20, 41, 1, nombreComienzo: "Agosto 2026", productLevelId: 3));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var interesRepo = new Mock<IInteresProductoOfertaRepository>();
            interesRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10))
                .Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            interesRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 11))
                .Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesRepo.Object);

            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns(new AceptacionReglamentoEst { IdAceptacionReglamentoEst = 999, CodigoPersona = 123 });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            // Un XML por trámite (una oferta cada uno, forma legacy).
            var xmls = new List<string>();
            _bandejaServiceMock
                .Setup(s => s.AltaTramiteWorkflow(
                    It.IsAny<DtoTramiteBandejaDevartModBandeja>(),
                    It.IsAny<DtoInstanciaWorkflowDevartModBandeja>(),
                    It.IsAny<IEnumerable<DtoBandejaDevartModBandeja>>()))
                .Callback<DtoTramiteBandejaDevartModBandeja, DtoInstanciaWorkflowDevartModBandeja, IEnumerable<DtoBandejaDevartModBandeja>>(
                    (t, i, b) => xmls.Add(i.XmlInstanciaWorkflow))
                .Returns(global::Utilities.OperationResult<long>.Ok(555, "AltaTramiteWorkflow"));

            var instInscripciones = new List<InstWorkflowInscripcion>();
            var instInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instInscripcionRepo.Setup(r => r.Add(It.IsAny<InstWorkflowInscripcion>()))
                .Callback<InstWorkflowInscripcion>(x => instInscripciones.Add(x));
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instInscripcionRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = new() { 10, 11 },
                IsCorporateEnrollment = true
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Waiting);
            // "A la espera": el response son solo flags.
            Assert.Empty(result.Data.Enrollments);

            // Un trámite (instancia + XML) por oferta.
            _bandejaServiceMock.Verify(
                s => s.AltaTramiteWorkflow(
                    It.IsAny<DtoTramiteBandejaDevartModBandeja>(),
                    It.IsAny<DtoInstanciaWorkflowDevartModBandeja>(),
                    It.IsAny<IEnumerable<DtoBandejaDevartModBandeja>>()),
                Times.Exactly(2));
            Assert.Equal(2, xmls.Count);
            Assert.Contains(xmls, x => x.Contains("(10)") && x.Contains("Marzo 2026"));
            Assert.Contains(xmls, x => x.Contains("(11)") && x.Contains("Agosto 2026"));

            // Una fila en T_INST_WORKFLOW_INSCRIPCION por oferta, con su comienzo.
            Assert.Equal(2, instInscripciones.Count);
            Assert.Contains(instInscripciones, f => f.IdOferta == 10 && f.IdComienzo == 40);
            Assert.Contains(instInscripciones, f => f.IdOferta == 11 && f.IdComienzo == 41);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_EsInscripcionCorporativaNivel1_ReturnsBadRequestSinTocarBandeja()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1, productLevelId: 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns(new AceptacionReglamentoEst { IdAceptacionReglamentoEst = 999, CodigoPersona = 123 });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = new() { 10 },
                IsCorporateEnrollment = true
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_17", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _bandejaServiceMock.Verify(
                s => s.AltaTramiteWorkflow(
                    It.IsAny<DtoTramiteBandejaDevartModBandeja>(),
                    It.IsAny<DtoInstanciaWorkflowDevartModBandeja>(),
                    It.IsAny<IEnumerable<DtoBandejaDevartModBandeja>>()),
                Times.Never);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenEncuestaIsNotDefinitivo_ReturnsConflict()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupEncuesta(123, new EncuestaIniAdmision
            {
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "TEMPORAL",
                IdProducto = 20,
                IdProceso = 30,
                IdComienzo = 40
            });

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_07", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_Nivel1SinEncuestaInicial_DevuelveNotFound()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1, productLevelId: 1);
            SetupEncuesta(123, null);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_06", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_Nivel3SinEncuestaInicial_NoRequiereEncuesta()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1, productLevelId: 3);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupDocumentosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns((AceptacionReglamentoEst)null);
            aceptacionRepo
                .Setup(r => r.GetByPersona(123))
                .Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            // No se llama a SetupEncuesta: nivel 3 y 4 no deben ni consultar T_ENCUESTA_INI_ADMISION.
            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = false,
                SelectedOfferingIds = [10]
            });

            // Llega a la validacion de reglamento (INS_CPI_02): prueba que nivel 3 no exige encuesta.
            Assert.False(result.Success);
            Assert.Equal("INS_CPI_02", result.ErrorCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenDocumentoFrenteMissing_ReturnsNotFound()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));

            var imagenRepo = new Mock<IImagenTemporalRepository>();
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, 1))
                .Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenRepo.Object);
            var imagenDefinitivaRepo = new Mock<IImagenRepository>();
            imagenDefinitivaRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(123, It.IsAny<int>()))
                .Returns((Imagen)null);
            _uowMock.Setup(u => u.Imagens).Returns(imagenDefinitivaRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_09", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithValidData_CreatesAcceptanceAndReturnsApiResponse()
        {
            AceptacionReglamentoEst? aceptacionAgregada = null;
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "estadoCuenta": {
                    "saldoActual": 3210.50
                  },
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Analista Programador",
                    "idComienzo": 40,
                    "comienzo": "Marzo 2026",
                    "idTurno": 1,
                    "turno": "Nocturno"
                  },
                  "ofertas": [
                    { "idOferta": 10, "idInscripcion": 77, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorCuota": 5000, "valorSeniaMinima": 2500 }
                  ]
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST))
                .Returns(999);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns((AceptacionReglamentoEst)null);
            aceptacionRepo
                .Setup(r => r.Add(It.IsAny<AceptacionReglamentoEst>()))
                .Callback<AceptacionReglamentoEst>(a => aceptacionAgregada = a);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.Equal(77, result.Data.Enrollments[0].EnrollmentId);
            Assert.Equal(10, result.Data.Enrollments[0].OfferingId);
            Assert.Equal(2500, result.Data.DepositAmount);
            Assert.Equal(20, result.Data.Summary.ProductId);
            Assert.Equal("Analista Programador", result.Data.Summary.DegreeProgram);
            Assert.Equal(new DateTime(2026, 7, 1), result.Data.Summary.PaymentDueDate);
            Assert.NotNull(result.Data.CurrentAccount);
            Assert.Equal(3210.50m, result.Data.CurrentAccount!.CurrentBalance);
            Assert.NotNull(aceptacionAgregada);
            Assert.Equal(999, aceptacionAgregada!.IdAceptacionReglamentoEst);
            var requestApi = Assert.Single(handler.Requests);
            Assert.Contains("tipoInscripcion=ONLINE", requestApi.RequestUri);
            Assert.Contains("idProducto=20", requestApi.RequestUri);
            Assert.Contains("idProceso=30", requestApi.RequestUri);
            Assert.Contains("idsOfertasSeleccionadas=10", requestApi.RequestUri);
            Assert.Contains("\"idTurno\":1", requestApi.Body);
            Assert.DoesNotContain("Pagos/CtaCte", requestApi.RequestUri);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithDefinitiveDocuments_Confirms()
        {
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "estadoCuenta": {
                    "saldoActual": 3210.50
                  },
                  "ofertas": [
                    { "idOferta": 10, "idInscripcion": 78 }
                  ]
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosDefinitivosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns(new AceptacionReglamentoEst
                {
                    IdAceptacionReglamentoEst = 999,
                    CodigoPersona = 123,
                    IdProducto = 20,
                    IdComienzo = 40
                });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(78)).Returns(new Inscripto { IdInscripto = 78 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.Equal(78, result.Data.Enrollments[0].EnrollmentId);
            Assert.NotNull(result.Data.CurrentAccount);
            Assert.Equal(3210.50m, result.Data.CurrentAccount!.CurrentBalance);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenEstadoCuentaMissing_ReturnsConfirmationWithoutEstadoCuenta()
        {
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "ofertas": [
                    { "idOferta": 10, "idInscripcion": 79 }
                  ]
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosDefinitivosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns(new AceptacionReglamentoEst
                {
                    IdAceptacionReglamentoEst = 999,
                    CodigoPersona = 123,
                    IdProducto = 20,
                    IdComienzo = 40
                });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(79)).Returns(new Inscripto { IdInscripto = 79 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.Equal(79, result.Data.Enrollments[0].EnrollmentId);
            Assert.Null(result.Data.CurrentAccount);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("ConfirmarPreInscripcionMultiple", request.RequestUri);
            Assert.DoesNotContain("Pagos/CtaCte", request.RequestUri);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithEncuestaIniHistorica_ConfirmsWithoutDefinitiveEncuestaAdmision()
        {
            AceptacionReglamentoEst? aceptacionAgregada = null;
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "estadoCuenta": {
                    "saldoActual": 3210.50
                  }
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupEncuesta(123, new EncuestaIniAdmision
            {
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "TEMPORAL"
            });
            SetupEncuestaIni(123, new EncuestaIni
            {
                CodigoPersona = 123,
                IdProducto = 99,
                IdComienzo = 88
            });
            SetupDocumentosValidos(123);

            SetupOfertaConfirmacion(10, 20, 40, 1, "Analista en TI", "ATI", "Marzo 2026");
            SetupInteresActivoOferta(123, 20, 10, 30);

            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST))
                .Returns(999);
            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns((AceptacionReglamentoEst)null);
            aceptacionRepo
                .Setup(r => r.Add(It.IsAny<AceptacionReglamentoEst>()))
                .Callback<AceptacionReglamentoEst>(a => aceptacionAgregada = a);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.Equal(20, result.Data.Summary.ProductId);
            Assert.Equal("Analista en TI", result.Data.Summary.DegreeProgram);
            Assert.NotNull(aceptacionAgregada);
            Assert.Equal(20, aceptacionAgregada!.IdProducto);
            Assert.Equal(40, aceptacionAgregada.IdComienzo);
            Assert.NotNull(result.Data.CurrentAccount);
            Assert.Equal(3210.50m, result.Data.CurrentAccount!.CurrentBalance);

            var requestApi = Assert.Single(handler.Requests);
            Assert.Contains("idProducto=20", requestApi.RequestUri);
            Assert.Contains("idProceso=30", requestApi.RequestUri);
            Assert.Contains("idsOfertasSeleccionadas=10", requestApi.RequestUri);
            Assert.Contains("tipoInscripcion=ONLINE", requestApi.RequestUri);
            Assert.Contains("\"idTurno\":1", requestApi.Body);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenInteresOfertaMissing_ReturnsConflict()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupEncuesta(123, EncuestaCompleta(123));

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo
                .Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10))
                .Returns((Proceso)null);
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_14", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenRequestHasNoOfertas_ReturnsBadRequest()
        {
            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = []
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenOfertasBelongToDifferentProductos_ReturnsBadRequest()
        {
            SetupPersona(123);
            SetupEncuesta(123, EncuestaCompleta(123));

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(10)).Returns(OfertaValida(10, 20, 40, 1));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(11)).Returns(OfertaValida(11, 21, 40, 1));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 21, 11)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10, 11]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_17", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithMultipleOfertas_ReturnsResultPerOferta()
        {
            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """
                {
                  "respuesta": true,
                  "confirmada": true,
                  "inscripcionPendiente": false,
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Analista Programador",
                    "idComienzo": 40,
                    "comienzo": "Marzo 2026",
                    "idTurno": 1,
                    "turno": "Nocturno"
                  },
                  "ofertas": [
                    { "idOferta": 10, "idInscripcion": 77, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorCuota": 1000, "valorSeniaMinima": 250 },
                    { "idOferta": 11, "idInscripcion": 78, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorCuota": 800, "valorSeniaMinima": 200 }
                  ]
                }
                """));
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(10)).Returns(OfertaValida(10, 20, 40, 1));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(11)).Returns(OfertaValida(11, 20, 40, 1));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 11)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo.Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40)).Returns(new AceptacionReglamentoEst
            {
                IdAceptacionReglamentoEst = 999,
                CodigoPersona = 123,
                IdProducto = 20,
                IdComienzo = 40
            });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10, 11]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.False(result.Data.Waiting);
            Assert.Equal("Analista Programador", result.Data.Summary.DegreeProgram);
            Assert.Equal(2, result.Data.Enrollments.Count);
            Assert.Equal(10, result.Data.Enrollments[0].OfferingId);
            Assert.Equal(77, result.Data.Enrollments[0].EnrollmentId);
            Assert.Equal(11, result.Data.Enrollments[1].OfferingId);
            Assert.Equal(78, result.Data.Enrollments[1].EnrollmentId);
            Assert.Equal(450, result.Data.DepositAmount);
            var requestApi = Assert.Single(handler.Requests);
            Assert.Contains("ConfirmarPreInscripcionMultiple", requestApi.RequestUri);
            Assert.Contains("idsOfertasSeleccionadas=10", requestApi.RequestUri);
            Assert.Contains("idsOfertasSeleccionadas=11", requestApi.RequestUri);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_ConProductoNivel3y4YComienzosDistintos_ConfirmaConExito()
        {
            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """
                {
                  "respuesta": true,
                  "confirmada": true,
                  "inscripcionPendiente": false,
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Analista Programador",
                    "idComienzo": 40,
                    "comienzo": "Marzo 2026",
                    "idTurno": 1,
                    "turno": "Nocturno"
                  },
                  "ofertas": [
                    { "idOferta": 10, "idInscripcion": 77, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorCuota": 1000, "valorSeniaMinima": 250 },
                    { "idOferta": 11, "idInscripcion": 78, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorCuota": 800, "valorSeniaMinima": 200 }
                  ]
                }
                """));
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(10)).Returns(OfertaValida(10, 20, 40, 1, productLevelId: 3, nombreComienzo: "Marzo 2026"));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(11)).Returns(OfertaValida(11, 20, 41, 1, productLevelId: 3, nombreComienzo: "Agosto 2026"));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 11)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo.Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40)).Returns(new AceptacionReglamentoEst
            {
                IdAceptacionReglamentoEst = 999,
                CodigoPersona = 123,
                IdProducto = 20,
                IdComienzo = 40
            });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10, 11]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.Equal(2, result.Data.Enrollments.Count);
            // El comienzo es por oferta: en nivel 3 y 4 cada oferta conserva el suyo.
            Assert.Equal("Marzo 2026", result.Data.Enrollments[0].Intake);
            Assert.Equal("Agosto 2026", result.Data.Enrollments[1].Intake);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_ConProductoNivel1y2YComienzosDistintos_DevuelveBadRequest()
        {
            SetupPersona(123);
            SetupEncuesta(123, EncuestaCompleta(123));

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(10)).Returns(OfertaValida(10, 20, 40, 1, productLevelId: 1));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(11)).Returns(OfertaValida(11, 20, 41, 1, productLevelId: 1));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 11)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10, 11]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_17", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_ConProductoNivel3y4YTurnosDistintos_ConfirmaConExito()
        {
            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, """
                {
                  "respuesta": true,
                  "confirmada": true,
                  "inscripcionPendiente": false,
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Analista Programador",
                    "idComienzo": 40,
                    "comienzo": "Marzo 2026",
                    "idTurno": 1,
                    "turno": "Nocturno"
                  },
                  "ofertas": [
                    { "idOferta": 10, "idInscripcion": 77, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorCuota": 1000, "valorSeniaMinima": 250 },
                    { "idOferta": 11, "idInscripcion": 78, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorCuota": 800, "valorSeniaMinima": 200 }
                  ]
                }
                """));
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(10)).Returns(OfertaValida(10, 20, 40, 1, productLevelId: 3, nombreTurno: "Nocturno"));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(11)).Returns(OfertaValida(11, 20, 40, 3, productLevelId: 3, nombreTurno: "Matutino"));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 11)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo.Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40)).Returns(new AceptacionReglamentoEst
            {
                IdAceptacionReglamentoEst = 999,
                CodigoPersona = 123,
                IdProducto = 20,
                IdComienzo = 40
            });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10, 11]
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.Equal(2, result.Data.Enrollments.Count);
            // El turno es por oferta: en nivel 3 y 4 cada oferta conserva el suyo.
            Assert.Equal("Nocturno", result.Data.Enrollments[0].Shift);
            Assert.Equal("Matutino", result.Data.Enrollments[1].Shift);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_ConProductoNivel1y2YTurnosDistintos_DevuelveBadRequest()
        {
            SetupPersona(123);
            SetupEncuesta(123, EncuestaCompleta(123));

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(10)).Returns(OfertaValida(10, 20, 40, 1, productLevelId: 1));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(11)).Returns(OfertaValida(11, 20, 40, 3, productLevelId: 1));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 11)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var result = await _service.Confirm.ExecuteAsync(123, new ConfirmPreEnrollmentRequest
            {
                AcceptedRegulations = true,
                SelectedOfferingIds = [10, 11]
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_17", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ReactivarInscripcion_WhenListaVacia_ReturnsBadRequest()
        {
            var result = await _service.Reactivate.ExecuteAsync(123, new ReactivateEnrollmentRequest { EnrollmentIds = [] });

            Assert.False(result.Success);
            Assert.Equal("INS_REA_00", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ReactivarInscripcion_WhenAlgunIdNoEsPositivo_ReturnsBadRequest()
        {
            var result = await _service.Reactivate.ExecuteAsync(123, new ReactivateEnrollmentRequest { EnrollmentIds = [555, 0] });

            Assert.False(result.Success);
            Assert.Equal("INS_REA_00", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ReactivarInscripcion_WhenInscripcionNoEncontrada_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await _service.Reactivate.ExecuteAsync(123, new ReactivateEnrollmentRequest { EnrollmentIds = [555] });

            Assert.False(result.Success);
            Assert.Equal("INS_REA_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task ReactivarInscripcion_WhenInscripcionNoEstaDeBaja_ReturnsConflict()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123, IdOferta = 10, BajaInscr = null });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await _service.Reactivate.ExecuteAsync(123, new ReactivateEnrollmentRequest { EnrollmentIds = [555] });

            Assert.False(result.Success);
            Assert.Equal("INS_REA_02", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public async Task ReactivarInscripcion_WithBaja_ResuelveOfertaYDelegaEnConfirmarPreInscripcion()
        {
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Analista Programador",
                    "idComienzo": 40,
                    "comienzo": "Marzo 2026",
                    "idTurno": 1,
                    "turno": "Nocturno"
                  },
                  "ofertas": [
                    { "idOferta": 10, "idInscripcion": 88 }
                  ]
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ACEPTACION_REGLAMENTO_EST))
                .Returns(999);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123, IdOferta = 10, BajaInscr = FechaBase });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await service.Reactivate.ExecuteAsync(123, new ReactivateEnrollmentRequest { EnrollmentIds = [555] });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.Equal(88, result.Data.Enrollments[0].EnrollmentId);
            Assert.Equal(10, result.Data.Enrollments[0].OfferingId);
            var requestApi = Assert.Single(handler.Requests);
            Assert.Contains("idsOfertasSeleccionadas=10", requestApi.RequestUri);
        }

        [Fact]
        public async Task ReactivarInscripcion_ConVariasInscripcionesNivel3y4_ConfirmaTodasLasOfertas()
        {
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Diploma en Gestion",
                    "idComienzo": 40,
                    "comienzo": "Marzo 2026",
                    "idTurno": 1,
                    "turno": "Nocturno"
                  },
                  "ofertas": [
                    { "idOferta": 10, "idInscripcion": 88, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorSeniaMinima": 250 },
                    { "idOferta": 11, "idInscripcion": 89, "fechaVencimientoPago": "2026-07-01T00:00:00", "valorSeniaMinima": 200 }
                  ]
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupEncuesta(123, EncuestaCompleta(123));
            SetupDocumentosValidos(123);

            // Dos seminarios del mismo producto: en nivel 3 y 4 pueden diferir en turno y comienzo.
            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(10)).Returns(OfertaValida(10, 20, 40, 1, productLevelId: 3, nombreTurno: "Nocturno"));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(11)).Returns(OfertaValida(11, 20, 41, 3, productLevelId: 3, nombreTurno: "Matutino"));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            interesProductoOfertaRepo.Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 11)).Returns(new Proceso { IdProceso = 30, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123, IdOferta = 10, BajaInscr = FechaBase });
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(556, 123))
                .Returns(new Inscripto { IdInscripto = 556, CodigoPersona = 123, IdOferta = 11, BajaInscr = FechaBase });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo.Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40)).Returns(new AceptacionReglamentoEst
            {
                IdAceptacionReglamentoEst = 999,
                CodigoPersona = 123,
                IdProducto = 20,
                IdComienzo = 40
            });
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await service.Reactivate.ExecuteAsync(123, new ReactivateEnrollmentRequest { EnrollmentIds = [555, 556] });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmed);
            Assert.Equal(2, result.Data.Enrollments.Count);
            Assert.Equal(88, result.Data.Enrollments[0].EnrollmentId);
            Assert.Equal(89, result.Data.Enrollments[1].EnrollmentId);
            // La seña es el total de las dos ofertas: el alumno paga todo junto.
            Assert.Equal(450m, result.Data.DepositAmount);
            var requestApi = Assert.Single(handler.Requests);
            Assert.Contains("idsOfertasSeleccionadas=10", requestApi.RequestUri);
            Assert.Contains("idsOfertasSeleccionadas=11", requestApi.RequestUri);
        }

        [Fact]
        public async Task ReactivarInscripcion_CuandoUnaDeLasInscripcionesNoEstaDeBaja_NoReactivaNinguna()
        {
            var handler = ConfirmacionConEstadoCuentaHandler("""{ "confirmada": true, "ofertas": [] }""");
            var service = CrearServiceConApi(handler);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123, IdOferta = 10, BajaInscr = FechaBase });
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(556, 123))
                .Returns(new Inscripto { IdInscripto = 556, CodigoPersona = 123, IdOferta = 11, BajaInscr = null });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await service.Reactivate.ExecuteAsync(123, new ReactivateEnrollmentRequest { EnrollmentIds = [555, 556] });

            Assert.False(result.Success);
            Assert.Equal("INS_REA_02", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
            // Todo o nada: no se llamó a la API interna, así que la oferta 10 tampoco se reactivó.
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public void RegistrarInteresProducto_CreaInteresNuevoYPersistenciaRelacionada()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);
            SetupOfertaValidaParaRegistro();

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere>());
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES))
                .Returns(500);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns((PersonaAdmite)null);
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            var survey = new EncuestaIniAdmision { IdEncuestaIni = 77, CodigoPersona = 123 };
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(survey);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var fechaAntes = DateTime.Now;
            var result = _service.Interest.Execute(123, new ProductInterestRequest { ProductId = 10, AdmissionProcessId = 20, OfferingIds = new List<long> { 30 } });
            var fechaDespues = DateTime.Now;

            Assert.True(result.Success);
            intereRepo.Verify(r => r.Add(It.Is<Intere>(i =>
                i.CodigoPersona == 123 &&
                i.IdProceso == 20 &&
                i.IdFormaContacto == 7m)), Times.Once);
            interesProductoRepo.Verify(r => r.Add(It.Is<InteresProducto>(i =>
                i.IdInteres == 500m &&
                i.IdProducto == 10 &&
                i.IdGradoInteres == 4m &&
                i.FechaInteresProd.HasValue &&
                i.FechaInteresProd.Value >= fechaAntes &&
                i.FechaInteresProd.Value <= fechaDespues)), Times.Once);
            personaAdmiteRepo.Verify(r => r.Add(It.Is<PersonaAdmite>(p => p.CodigoPersona == 123)), Times.Once);
            interesProductoOfertaRepo.Verify(r => r.Add(It.Is<InteresProductoOferta>(x =>
                x.IdInteres == 500 &&
                x.IdProducto == 10 &&
                x.IdOferta == 30)), Times.Once);
            Assert.Equal(20, survey.IdProceso);
            Assert.Equal(40, survey.IdComienzo);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueProductInterestFromSiteSelection(
                _uowMock.Object,
                It.Is<DtoTivenosAltaInteresRequest>(r =>
                    r.CodigoPersona == 123 &&
                    r.IdProducto == 10 &&
                    r.IdProceso == 20 &&
                    r.Operacion.TipoProcesoLlamador == "Alta" &&
                    r.Operacion.Disparador == "CreateProductInterest" &&
                    r.Operacion.OrigenLlamador == null),
                It.IsAny<int>()), Times.Once);
            // Primer ingreso de la persona a admisiones (sin fila en T_PERSONA_ADMITE): tambien va el registro.
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueSiteRegistration(
                _uowMock.Object,
                It.Is<DtoTivenosAltaInteresRequest>(r =>
                    r.CodigoPersona == 123 &&
                    r.IdProducto == 10 &&
                    r.IdProceso == 20 &&
                    r.Operacion.TipoProcesoLlamador == "Alta" &&
                    r.Operacion.Disparador == "Registro" &&
                    r.Operacion.OrigenLlamador == null),
                It.IsAny<int>()), Times.Once);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public void RegistrarInteresProducto_CuandoFallaEncolarTivenos_HaceRollbackYNoCommit()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);
            SetupOfertaValidaParaRegistro();

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere>());
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES))
                .Returns(500);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns((PersonaAdmite)null);
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            _tivenosEnvioServiceMock
                .Setup(s => s.EnqueueProductInterestFromSiteSelection(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosAltaInteresRequest>(),
                    It.IsAny<int>()))
                .Throws(new InvalidOperationException("No se pudo encolar Tivenos."));

            Assert.Throws<InvalidOperationException>(() =>
                _service.Interest.Execute(123, new ProductInterestRequest { ProductId = 10, AdmissionProcessId = 20, OfferingIds = new List<long> { 30 } }));

            _uowMock.Verify(u => u.Rollback(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public void RegistrarInteresProducto_ConOfertaNoCompatibleConProceso_DevuelveError()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);
            SetupOfertaParaRegistro(30, 10, 40);

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere>());
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES))
                .Returns(500);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns((PersonaAdmite)null);
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision { IdEncuestaIni = 77, CodigoPersona = 123 });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetByKeyWithRelated(20, 40)).Returns((ProcesoComienzo)null);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var result = _service.Interest.Execute(123, new ProductInterestRequest { ProductId = 10, AdmissionProcessId = 20, OfferingIds = new List<long> { 30 } });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("GEN_IP_10", result.ErrorCode);
            _uowMock.Verify(u => u.Rollback(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public void RegistrarInteresProducto_ConInteresExistente_MantieneOtrosInteresesYActualizaSeleccionado()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);
            SetupOfertaValidaParaRegistro();

            var interesExistente = new Intere
            {
                IdInteres = 700,
                CodigoPersona = 123,
                IdProceso = 20,
                InteresProductos = new List<InteresProducto>
                {
                    new InteresProducto { IdInteres = 700, IdProducto = 10, IdGradoInteres = 2 },
                    new InteresProducto { IdInteres = 700, IdProducto = 11, IdGradoInteres = 4 },
                    new InteresProducto { IdInteres = 700, IdProducto = 12, IdGradoInteres = 5 }
                }
            };

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere> { interesExistente });
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            var interesProductoProducto10 = new InteresProducto { IdInteres = 700, IdProducto = 10, IdGradoInteres = 2 };
            var interesProductoProducto11 = new InteresProducto { IdInteres = 700, IdProducto = 11, IdGradoInteres = 4 };
            var interesProductoProducto12 = new InteresProducto { IdInteres = 700, IdProducto = 12, IdGradoInteres = 5 };
            interesProductoRepo
                .Setup(r => r.GetByKey(700, 10))
                .Returns(interesProductoProducto10);
            interesProductoRepo
                .Setup(r => r.GetByKey(700, 11))
                .Returns(interesProductoProducto11);
            interesProductoRepo
                .Setup(r => r.GetByKey(700, 12))
                .Returns(interesProductoProducto12);
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns(new PersonaAdmite { CodigoPersona = 123, FechaFrescoPersonaAdmite = DateTime.Today });
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo
                .Setup(r => r.GetByKey(700, 10, 30))
                .Returns(new InteresProductoOferta
                {
                    IdInteres = 700,
                    IdProducto = 10,
                    IdOferta = 30
                });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.Interest.Execute(123, new ProductInterestRequest { ProductId = 10, AdmissionProcessId = 20, OfferingIds = new List<long> { 30 } });

            Assert.True(result.Success);
            Assert.Equal(4m, interesProductoProducto10.IdGradoInteres);
            Assert.Equal(2m, interesProductoProducto10.IdGradoInteresAnt);
            Assert.Equal(4m, interesProductoProducto11.IdGradoInteres);
            Assert.Equal(0m, interesProductoProducto11.IdGradoInteresAnt);
            Assert.Equal(5m, interesProductoProducto12.IdGradoInteres);
            interesProductoRepo.Verify(r => r.Update(interesProductoProducto10), Times.Once);
            interesProductoRepo.Verify(r => r.Update(interesProductoProducto11), Times.Never);
            interesProductoRepo.Verify(r => r.Update(interesProductoProducto12), Times.Never);
            interesProductoRepo.Verify(r => r.Update(It.IsAny<InteresProducto>()), Times.Once);
            interesProductoOfertaRepo.Verify(r => r.Add(It.IsAny<InteresProductoOferta>()), Times.Never);
            intereRepo.Verify(r => r.Add(It.IsAny<Intere>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueProductInterestFromSiteSelection(
                _uowMock.Object,
                It.Is<DtoTivenosAltaInteresRequest>(r =>
                    r.CodigoPersona == 123 &&
                    r.IdProducto == 10 &&
                    r.IdProceso == 20 &&
                    r.Operacion.TipoProcesoLlamador == "Modificar" &&
                    r.Operacion.Disparador == "ActualizarInteres" &&
                    r.Operacion.OrigenLlamador == null),
                It.IsAny<int>()), Times.Once);
            // La persona ya tenia FechaFrescoPersonaAdmite: no se le manda un registro duplicado.
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueSiteRegistration(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosAltaInteresRequest>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void RegistrarInteresProducto_ConInscripcionPrevia_DevuelveConflict()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(true);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = _service.Interest.Execute(123, new ProductInterestRequest { ProductId = 10, AdmissionProcessId = 20, OfferingIds = new List<long> { 30 } });

            Assert.False(result.Success);
            Assert.Equal(409, result.HttpCode);
            Assert.Equal("GEN_IP_04", result.ErrorCode);
        }

        [Fact]
        public void RegistrarInteresProducto_ConPendiente_DevuelveConflict()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(true);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var result = _service.Interest.Execute(123, new ProductInterestRequest { ProductId = 10, AdmissionProcessId = 20, OfferingIds = new List<long> { 30 } });

            Assert.False(result.Success);
            Assert.Equal(409, result.HttpCode);
            Assert.Equal("GEN_IP_05", result.ErrorCode);
        }

        [Fact]
        public void RegistrarInteresProducto_SinOfertasSeleccionadas_DevuelveError()
        {
            var result = _service.Interest.Execute(123, new ProductInterestRequest { ProductId = 10, AdmissionProcessId = 20, OfferingIds = new List<long>() });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("GEN_IP_11", result.ErrorCode);
        }

        [Fact]
        public void RegistrarInteresProducto_ConVariasOfertasNivel3y4_PersisteUnaFilaPorOferta()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 3 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.TieneInscripcionPendienteParaProducto(123, 10)).Returns(false);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(30)).Returns(OfertaValida(30, 10, 40, 1, productLevelId: 3));
            ofertaRepo.Setup(r => r.GetByKeyWithRelated(31)).Returns(OfertaValida(31, 10, 40, 1, productLevelId: 3));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetByKeyWithRelated(20, 40)).Returns(new ProcesoComienzo { IdProceso = 20, IdComienzo = 40 });
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo.Setup(r => r.GetInteresesPersonaProcesosHabilitados(123)).Returns(new List<Intere>());
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_INTERES))
                .Returns(500);

            var interesProductoRepo = new Mock<BusinessLogic.IDevartRepositories.IInteresProductoRepository>();
            _uowMock.Setup(u => u.InteresProductos).Returns(interesProductoRepo.Object);

            var personaAdmiteRepo = new Mock<IPersonaAdmiteRepository>();
            personaAdmiteRepo.Setup(r => r.GetByKey(123)).Returns((PersonaAdmite)null);
            _uowMock.Setup(u => u.PersonaAdmites).Returns(personaAdmiteRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.Interest.Execute(123, new ProductInterestRequest { ProductId = 10, AdmissionProcessId = 20, OfferingIds = new List<long> { 30, 31 } });

            Assert.True(result.Success);
            interesProductoOfertaRepo.Verify(r => r.Add(It.Is<InteresProductoOferta>(x =>
                x.IdInteres == 500 && x.IdProducto == 10 && x.IdOferta == 30)), Times.Once);
            interesProductoOfertaRepo.Verify(r => r.Add(It.Is<InteresProductoOferta>(x =>
                x.IdInteres == 500 && x.IdProducto == 10 && x.IdOferta == 31)), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public void GuardarEncuestaInicial_ParcialMinimo_CreaTemporalSinActualizarPersona()
        {
            SetupPersonaValida();

            EncuestaIniAdmision? encuestaAgregada = null;
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(900);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest());

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(900, encuestaAgregada!.IdEncuestaIni);
            Assert.Equal("TEMPORAL", encuestaAgregada.EstadoEncuestaIniAdmision);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
            _uowMock.Verify(u => u.Personas.Update(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public void ObtenerEncuestaInicial_ConDerechoSinEncuesta_DevuelveEncuestaNullReadOnly()
        {
            SetupPersonaValida();
            SetupReposDerechoEncuesta();

            var result = _encuesta.GetInitialSurvey(123);

            Assert.True(result.Success);
            Assert.True(result.Data!.CanAnswerSurvey);
            Assert.Null(result.Data.Survey);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public void ObtenerEncuestaInicial_ConEncuesta_MapeaDtoFuncionalYEstadoPersistido()
        {
            SetupPersona(new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "DE",
                Documento = "123"
            });

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                IdEncuestaIni = 900,
                CodigoPersona = 123,
                IdProducto = 1981,
                IdProceso = 110,
                EstadoEncuestaIniAdmision = "CONFIRMADO",
                CursaSecundariaActualmenteEncuestaIni = "SI",
                VecesSextoEncuestaIni = "2",
                TieneEducacionSuperiorEncuestaIni = "SI", // Exterior: "SI" sin universidades -> id 2
                ComparAmigoFamEncuestaIni = "SI",
                InforOtrasAntesEncuestaIni = "NO"
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            SetupReposDerechoEncuesta();
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAllWithRelated()).Returns(new List<AnioBachiller>());
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var empresaRepo = new Mock<IEmpresaConsideradaAdmisionRepository>();
            empresaRepo.Setup(r => r.GetByPersona(123)).Returns(new List<EmpresaConsideradaAdmision>
            {
                new() { CodigoEmpresa = 55 }
            });
            _uowMock.Setup(u => u.EmpresaConsideradaAdmisions).Returns(empresaRepo.Object);

            var educacionRepo = new Mock<IEducacionSuperiorAdmisionRepository>();
            educacionRepo.Setup(r => r.GetByPersona(123)).Returns(new List<EducacionSuperiorAdmision>());
            _uowMock.Setup(u => u.EducacionSuperiorAdmisions).Returns(educacionRepo.Object);

            var motivoRepo = new Mock<IMotivoEleccionAdmisionRepository>();
            motivoRepo.Setup(r => r.GetByPersona(123)).Returns(new List<MotivoEleccionAdmision>
            {
                new() { IdMotivo = 7 }
            });
            _uowMock.Setup(u => u.MotivoEleccionAdmisions).Returns(motivoRepo.Object);

            var publicidadRepo = new Mock<IPublicidadEleccionAdmisionRepository>();
            publicidadRepo.Setup(r => r.GetByPersona(123)).Returns(new List<PublicidadEleccionAdmision>
            {
                new() { IdPublicidad = 8 }
            });
            _uowMock.Setup(u => u.PublicidadEleccionAdmisions).Returns(publicidadRepo.Object);

            var result = _encuesta.GetInitialSurvey(123);

            Assert.True(result.Success);
            var survey = result.Data!.Survey!;
            Assert.Equal(900, survey.SurveyId);
            Assert.Equal("CONFIRMADO", survey.Status);
            Assert.Equal(1981, survey.DegreeProgramId);
            Assert.Equal(110, survey.AdmissionProcessId);
            Assert.True(survey.CurrentlyInSecondary);
            Assert.True(survey.RepeatsHighSchoolYear);
            Assert.Equal(2, survey.HighSchoolYearRepeatCount);
            Assert.Equal(2, survey.PreviousHigherEducationId);
            Assert.Equal(2, survey.DecisionSupportId);
            Assert.Equal([55], survey.ConsideredUniversityIds);
            Assert.Equal([7], survey.OrtChoiceReasonIds);
            Assert.Equal([8], survey.OrtAdvertisingIds);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public void EncuestaInicial_RecursoBachilleratoNo_SobreviveElRoundTrip()
        {
            SetupPersonaValida();
            SetupReposDerechoEncuesta();

            EncuestaIniAdmision? guardada = null;
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(() => guardada);
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => guardada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(900);

            var guardado = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                RepeatsHighSchoolYear = false
            });

            Assert.True(guardado.Success);
            Assert.Equal("0", guardada!.VecesSextoEncuestaIni);

            var leido = _encuesta.GetInitialSurvey(123);

            Assert.True(leido.Success);
            Assert.False(leido.Data!.Survey!.RepeatsHighSchoolYear);
            Assert.Null(leido.Data.Survey.HighSchoolYearRepeatCount);
        }

        [Fact]
        public void GuardarEncuestaInicial_SinDerecho_CortaAntesDeValidarRequest()
        {
            SetupPersonaValida();
            SetupReposDerechoEncuesta(existeFresco: true);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                DecisionLevelId = 99
            });

            Assert.False(result.Success);
            Assert.Equal(403, result.HttpCode);
            Assert.Equal("INS_EI_56", result.ErrorCode);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_EducacionSuperiorUruguaySinUniversidades_Falla()
        {
            SetupPersonaValida();

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                PreviousHigherEducationId = 1,
                HigherEducationUniversityIds = []
            });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("INS_EI_63", result.ErrorCode);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_CarreraUniversitariaConCuarto_Falla()
        {
            SetupPersonaValida();

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto { IdProducto = 10, IdNivelProducto = 1 });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAllWithRelated()).Returns(new List<AnioBachiller> { AnioBachiller(4, 10) });
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                DegreeProgramId = 10,
                CurrentlyInSecondary = true,
                HighSchoolYear = 10
            });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("INS_EI_64", result.ErrorCode);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_SecundariaExterior_AplicaDefaultsLegacy()
        {
            SetupPersonaValida();

            EncuestaIniAdmision? encuestaAgregada = null;
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(902);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAllWithRelated()).Returns(new List<AnioBachiller> { AnioBachiller(6, 12) });
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                LastSecondaryYearLocationId = 2,
                CurrentlyInSecondary = true,
                HighSchoolYear = 12
            });

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(2898, encuestaAgregada!.CodigoInstitucionBac);
            Assert.Equal(5, encuestaAgregada.CodigoTitulo);
        }

        [Fact]
        public void GuardarEncuestaInicial_UniversidadOtro_GuardaNombreEnTablasHijas()
        {
            SetupPersonaValida();

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                IdEncuestaIni = 10,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "TEMPORAL"
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetUniversidades()).Returns(new List<Empresa>());
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var consideradaRepo = new Mock<IEmpresaConsideradaAdmisionRepository>();
            _uowMock.Setup(u => u.EmpresaConsideradaAdmisions).Returns(consideradaRepo.Object);

            var superiorRepo = new Mock<IEducacionSuperiorAdmisionRepository>();
            _uowMock.Setup(u => u.EducacionSuperiorAdmisions).Returns(superiorRepo.Object);

            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_EMPRESA_CONSIDERADA_ADMISION))
                .Returns(501);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_EDUCACION_SUPERIOR_ADMISION))
                .Returns(601);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                ResearchedOtherUniversities = true,
                ConsideredUniversityIds = [0],
                ConsideredUniversityOthers = [" Otra universidad "],
                PreviousHigherEducationId = 1,
                HigherEducationUniversityIds = [0],
                HigherEducationUniversityOthers = [" Otra superior "]
            });

            Assert.True(result.Success);
            consideradaRepo.Verify(r => r.Add(It.Is<EmpresaConsideradaAdmision>(e =>
                e.IdEmpresaConsiderada == 501 &&
                e.CodigoPersona == 123 &&
                e.CodigoEmpresa == null &&
                e.NombreOtraEmpresa == "Otra universidad")), Times.Once);
            superiorRepo.Verify(r => r.Add(It.Is<EducacionSuperiorAdmision>(e =>
                e.IdEducacionSuperior == 601 &&
                e.CodigoPersona == 123 &&
                e.CodigoEmpresa == null &&
                e.NombreOtraEmpresa == "Otra superior")), Times.Once);
        }

        [Fact]
        public void GuardarEncuestaInicial_EducacionSuperiorExteriorConUniversidad_Falla()
        {
            SetupPersonaValida();

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                PreviousHigherEducationId = 2,
                HigherEducationUniversityIds = [0],
                HigherEducationUniversityOthers = ["Otra superior"]
            });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("INS_EI_25", result.ErrorCode);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_EducacionSuperiorExterior_PersisteSiSinUniversidades()
        {
            SetupPersonaValida();

            EncuestaIniAdmision? encuestaAgregada = null;
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(903);

            var superiorRepo = new Mock<IEducacionSuperiorAdmisionRepository>();
            _uowMock.Setup(u => u.EducacionSuperiorAdmisions).Returns(superiorRepo.Object);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                PreviousHigherEducationId = 2
            });

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal("SI", encuestaAgregada!.TieneEducacionSuperiorEncuestaIni);
            superiorRepo.Verify(r => r.RemoveByPersona(123), Times.Once);
        }

        [Fact]
        public void GuardarEncuestaInicial_UniversidadOtroSinSeleccionOtra_Falla()
        {
            SetupPersonaValida();

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetUniversidades()).Returns(new List<Empresa>
            {
                new() { CodigoEmpresa = 55, Nombre = "Universidad" }
            });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                ResearchedOtherUniversities = true,
                ConsideredUniversityIds = [55],
                ConsideredUniversityOthers = ["Otra universidad"]
            });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("INS_EI_25", result.ErrorCode);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_AnioBachilleratoPorId_GuardaCantAnios()
        {
            SetupPersonaValida();

            EncuestaIniAdmision? encuestaAgregada = null;
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(901);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAllWithRelated()).Returns(new List<AnioBachiller>
            {
                AnioBachiller(6, 12)
            });
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                CurrentlyInSecondary = true,
                HighSchoolYear = 6
            });

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal("12", encuestaAgregada!.UltimoAnioSextoEncuestaIni);
        }

        [Fact]
        public void GuardarEncuestaInicial_NoCursaSecundaria_LimpiaBachillerato()
        {
            SetupPersonaValida();

            var survey = new EncuestaIniAdmision
            {
                IdEncuestaIni = 10,
                CodigoPersona = 123,
                AniosInstruccionEncuestaIni = "12",
                UltimoAnioSextoEncuestaIni = "12",
                CodigoTitulo = 1300
            };
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(survey);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                CurrentlyInSecondary = false,
                HighSchoolYear = 12,
                HighSchoolTrackId = 1300
            });

            Assert.True(result.Success);
            Assert.Equal("NO", survey.CursaSecundariaActualmenteEncuestaIni);
            Assert.Null(survey.AniosInstruccionEncuestaIni);
            Assert.Null(survey.UltimoAnioSextoEncuestaIni);
            Assert.Null(survey.CodigoTitulo);
        }

        [Fact]
        public void GuardarEncuestaInicial_MotivosVacio_ReemplazaBorrandoSeleccion()
        {
            SetupPersonaValida();

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                IdEncuestaIni = 10,
                CodigoPersona = 123,
                EstadoEncuestaIniAdmision = "TEMPORAL"
            });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var motivoRepo = new Mock<IMotivoEleccionAdmisionRepository>();
            _uowMock.Setup(u => u.MotivoEleccionAdmisions).Returns(motivoRepo.Object);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest
            {
                OrtChoiceReasonIds = []
            });

            Assert.True(result.Success);
            motivoRepo.Verify(r => r.RemoveByPersona(123), Times.Once);
            motivoRepo.Verify(r => r.Add(It.IsAny<MotivoEleccionAdmision>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_Parcial_NoSincronizaBachillerato()
        {
            SetupPersonaValida();

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(901);

            var bachilleratoRepo = new Mock<BusinessLogic.IDevartRepositories.IBachilleratoPersonaRepository>();
            _uowMock.Setup(u => u.BachilleratoPersonas).Returns(bachilleratoRepo.Object);

            var result = _encuesta.SaveInitialSurvey(123, new SaveInitialSurveyRequest());

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.GetByKey(It.IsAny<long>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueHighSchoolDataCreation(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueHighSchoolDataUpdate(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaSinBachillerato_InsertaYEncolaAlta()
        {
            SetupEncuestaCompletaParaGuardar(null, out var bachilleratoRepo);

            var result = _encuesta.SaveInitialSurvey(123, RequestEncuestaCompleta(ultimoAnioSexto: 11, codigoTitulo: null));

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.Add(It.Is<BachilleratoPersona>(b =>
                b.CodigoPersona == 123 &&
                b.CodigoInstitucion == 50 &&
                b.AnioBachillerPer == "5" &&
                b.CodigoOrientacion == 1304 &&
                b.ActualizacionBachillerPer == FechaBase)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueHighSchoolDataCreation(
                _uowMock.Object,
                It.Is<DtoTivenosBachilleratoRequest>(r => r.CodigoPersona == 123 && r.CodigoOrientacion == 1304),
                777), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueHighSchoolDataUpdate(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaSinCursaSecundaria_QuedaTemporal()
        {
            SetupEncuestaCompletaParaGuardar(null, out var bachilleratoRepo);

            var request = RequestEncuestaCompleta();
            request.CurrentlyInSecondary = null;

            var result = _encuesta.SaveInitialSurvey(123, request);

            Assert.True(result.Success);
            Assert.Equal("TEMPORAL", result.Data!.Status);
            Assert.Contains("cursaSecundariaActualmente", result.Data.PendingFields);
            bachilleratoRepo.Verify(r => r.GetByKey(123), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaNoCursaSecundaria_NoExigeBachillerato()
        {
            SetupEncuestaCompletaParaGuardar(null, out var bachilleratoRepo);

            var request = RequestEncuestaCompleta();
            request.CurrentlyInSecondary = false;
            request.HighSchoolYear = null;
            request.HighSchoolTrackId = null;

            var result = _encuesta.SaveInitialSurvey(123, request);

            Assert.True(result.Success);
            Assert.Equal("CONFIRMADO", result.Data!.Status);
            Assert.DoesNotContain("anioBachillerato", result.Data.PendingFields);
            Assert.DoesNotContain("orientacionBachilleratoId", result.Data.PendingFields);
            bachilleratoRepo.Verify(r => r.GetByKey(123), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaConBachilleratoDistinto_ActualizaYEncolaModificacion()
        {
            var existing = new BachilleratoPersona
            {
                CodigoPersona = 123,
                CodigoInstitucion = 40,
                AnioBachillerPer = "5",
                CodigoOrientacion = 1304,
                UsuarioIngreso = string.Empty,
                FechaIngreso = FechaBase.AddDays(-1)
            };
            SetupEncuestaCompletaParaGuardar(existing, out var bachilleratoRepo);

            var result = _encuesta.SaveInitialSurvey(123, RequestEncuestaCompleta());

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.Update(It.Is<BachilleratoPersona>(b =>
                b.CodigoPersona == 123 &&
                b.CodigoInstitucion == 50 &&
                b.AnioBachillerPer == "6" &&
                b.CodigoOrientacion == 1300 &&
                b.ActualizacionBachillerPer == FechaBase)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueHighSchoolDataUpdate(
                _uowMock.Object,
                It.Is<DtoTivenosBachilleratoRequest>(r => r.CodigoPersona == 123 && r.CodigoOrientacion == 1300),
                777), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueHighSchoolDataCreation(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaConBachilleratoIgual_NoDuplicaEnvio()
        {
            var existing = new BachilleratoPersona
            {
                CodigoPersona = 123,
                CodigoInstitucion = 50,
                AnioBachillerPer = "6",
                CodigoOrientacion = 1300,
                UsuarioIngreso = string.Empty,
                FechaIngreso = FechaBase.AddDays(-1)
            };
            SetupEncuestaCompletaParaGuardar(existing, out var bachilleratoRepo);

            var result = _encuesta.SaveInitialSurvey(123, RequestEncuestaCompleta());

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.Update(It.IsAny<BachilleratoPersona>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueHighSchoolDataCreation(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EnqueueHighSchoolDataUpdate(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>()), Times.Never);
        }

        private void SetupOfertaConfirmacion(
            long idOferta,
            long productId,
            long idComienzo,
            long idTurno,
            string nombreExtenso = "Analista Programador",
            string name = "AP",
            string nombreComienzo = "Marzo 2026",
            long productLevelId = 0)
        {
            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo
                .Setup(r => r.GetByKeyWithRelated(idOferta))
                .Returns(OfertaValida(idOferta, productId, idComienzo, idTurno, nombreExtenso, name, nombreComienzo, productLevelId));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);
        }

        private void SetupOfertaParaRegistro(long idOferta, long productId, long idComienzo, long idTurno = 1)
        {
            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo
                .Setup(r => r.GetByKeyWithRelated(idOferta))
                .Returns(OfertaValida(idOferta, productId, idComienzo, idTurno));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);
        }

        private void SetupOfertaValidaParaRegistro(
            long idOferta = 30,
            long productId = 10,
            long admissionProcessId = 20,
            long idComienzo = 40,
            long idTurno = 1)
        {
            SetupOfertaParaRegistro(idOferta, productId, idComienzo, idTurno);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo
                .Setup(r => r.GetByKeyWithRelated(admissionProcessId, idComienzo))
                .Returns(new ProcesoComienzo { IdProceso = admissionProcessId, IdComienzo = idComienzo });
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);
        }

        private void SetupInteresActivoOferta(long personId, long productId, long idOferta, long admissionProcessId)
        {
            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo
                .Setup(r => r.GetProcesoPorInteresActivoOferta(personId, productId, idOferta))
                .Returns(new Proceso { IdProceso = admissionProcessId, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);
        }

        private void SetupEncuestaCompletaParaGuardar(
            BachilleratoPersona? bachilleratoExistente,
            out Mock<BusinessLogic.IDevartRepositories.IBachilleratoPersonaRepository> bachilleratoRepo)
        {
            SetupPersonaValida();

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            encuestaRepo.Setup(r => r.GetByPersonaProductoComienzo(123, 10, 30)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto
            {
                IdProducto = 10,
                IdNivelProducto = 2
            });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo
                .Setup(r => r.GetOfertasSeleccionadas(123, 10, 20))
                .Returns(new List<Oferta> { OfertaValida(99, 10, 30, 1) });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetByKey(50)).Returns(new Empresa { CodigoEmpresa = 50, Nombre = "Liceo" });
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var tituloRepo = new Mock<ITituloRepository>();
            tituloRepo.Setup(r => r.GetByKey(1300)).Returns(new Titulo
            {
                CodigoTitulo = 1300,
                Nombre = "Sexto",
                IdAnioBachiller = 6,
                UsuarioIngreso = string.Empty,
                FechaIngreso = FechaBase,
                HoraIngreso = "10:30:00",
                Bachillerato = "SI"
            });
            _uowMock.Setup(u => u.Titulos).Returns(tituloRepo.Object);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAll()).Returns(new List<AnioBachiller>
            {
                AnioBachiller(4, 10),
                AnioBachiller(5, 11),
                AnioBachiller(6, 12)
            });
            anioRepo.Setup(r => r.GetAllWithRelated()).Returns(new List<AnioBachiller>
            {
                AnioBachiller(4, 10),
                AnioBachiller(5, 11),
                new()
                {
                    IdAnioBachiller = 6,
                    CantAniosAnioBachiller = 12,
                    NombreAnioBachiller = "6",
                    UsuarioIngreso = string.Empty,
                    FechaIngreso = FechaBase,
                    HoraIngreso = "10:30:00",
                    Titulos =
                    [
                        new Titulo
                        {
                            CodigoTitulo = 1300,
                            Nombre = "Sexto",
                            IdAnioBachiller = 6,
                            UsuarioIngreso = string.Empty,
                            FechaIngreso = FechaBase,
                            HoraIngreso = "10:30:00",
                            Bachillerato = "SI"
                        }
                    ]
                }
            });
            anioRepo.Setup(r => r.GetByKey(6)).Returns(AnioBachiller(6, 12));
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var motivoOpcionesRepo = new Mock<BusinessLogic.IDevartRepositories.IMotivoOpcionesAdmisionRepository>();
            motivoOpcionesRepo.Setup(r => r.GetByKey(1)).Returns(new MotivoOpcionesAdmision { IdMotivo = 1 });
            motivoOpcionesRepo.Setup(r => r.GetAll()).Returns(new List<MotivoOpcionesAdmision> { new() { IdMotivo = 1 } });
            _uowMock.Setup(u => u.MotivoOpcionesAdmisions).Returns(motivoOpcionesRepo.Object);

            var empresaConsideradaRepo = new Mock<IEmpresaConsideradaAdmisionRepository>();
            _uowMock.Setup(u => u.EmpresaConsideradaAdmisions).Returns(empresaConsideradaRepo.Object);

            var educacionSuperiorRepo = new Mock<IEducacionSuperiorAdmisionRepository>();
            _uowMock.Setup(u => u.EducacionSuperiorAdmisions).Returns(educacionSuperiorRepo.Object);

            var motivoRepo = new Mock<IMotivoEleccionAdmisionRepository>();
            motivoRepo.Setup(r => r.GetByPersona(123)).Returns(new List<MotivoEleccionAdmision>
            {
                new() { CodigoPersona = 123, IdMotivo = 1 }
            });
            _uowMock.Setup(u => u.MotivoEleccionAdmisions).Returns(motivoRepo.Object);

            var publicidadRepo = new Mock<IPublicidadEleccionAdmisionRepository>();
            _uowMock.Setup(u => u.PublicidadEleccionAdmisions).Returns(publicidadRepo.Object);

            bachilleratoRepo = new Mock<BusinessLogic.IDevartRepositories.IBachilleratoPersonaRepository>();
            bachilleratoRepo.Setup(r => r.GetByKey(123)).Returns(bachilleratoExistente);
            _uowMock.Setup(u => u.BachilleratoPersonas).Returns(bachilleratoRepo.Object);

            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(900);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS))
                .Returns(777);
        }

        private static SaveInitialSurveyRequest RequestEncuestaCompleta(
            long ultimoAnioSexto = 12,
            long? codigoTitulo = 1300)
        {
            return new SaveInitialSurveyRequest
            {
                DegreeProgramId = 10,
                AdmissionProcessId = 20,
                CurrentlyInSecondary = true,
                HighSchoolTrackId = codigoTitulo,
                HighSchoolYear = ultimoAnioSexto,
                FatherEducationLevelId = 1,
                MotherEducationLevelId = 1,
                CareerDecisionYearId = 2,
                OrtDecisionYearId = 2,
                ResearchedOtherUniversities = false,
                DecisionSupportId = 1,
                SecondaryInstitutionId = 50,
                LastSecondaryYearLocationId = 1,
                PreviousHigherEducationId = 3,
                DecisionLevelId = 1,
                HadOrtAdvisory = false,
                VisitedOrtWebsite = false,
                VisitedOrtFacilities = false,
                RecallsOrtAdvertising = false,
                OrtChoiceReasonIds = [1]
            };
        }

        private static AnioBachiller AnioBachiller(long idAnio, long cantAnios)
        {
            return new AnioBachiller
            {
                IdAnioBachiller = idAnio,
                CantAniosAnioBachiller = cantAnios,
                NombreAnioBachiller = cantAnios.ToString(),
                UsuarioIngreso = string.Empty,
                FechaIngreso = FechaBase,
                HoraIngreso = "10:30:00"
            };
        }

        private static Oferta OfertaValida(
            long idOferta,
            long productId,
            long idComienzo,
            long idTurno,
            string nombreExtenso = "Analista Programador",
            string name = "AP",
            string nombreComienzo = "Marzo 2026",
            long productLevelId = 1,
            string nombreTurno = "Nocturno")
        {
            return new Oferta
            {
                IdOferta = idOferta,
                IdTurno = idTurno,
                InscripcionesAbiertasOferta = "SI",
                Turno = new Turno { IdTurno = idTurno, NombreTurno = nombreTurno },
                Supraoferta = new Supraoferta
                {
                    IdComienzo = idComienzo,
                    EstadoSupraoferta = "D",
                    Comienzo = new Comienzo { IdComienzo = idComienzo, NombreComienzo = nombreComienzo },
                    Paquete = new Paquete
                    {
                        IdProducto = productId,
                        Producto = new Producto
                        {
                            IdProducto = productId,
                            NombreProducto = name,
                            NombreExtensoProducto = nombreExtenso,
                            IdNivelProducto = productLevelId
                        }
                    }
                }
            };
        }

        private void SetupPersonaValida()
        {
            SetupPersona(new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "DE",
                Documento = "123"
            });
        }

        private void SetupPersona(Persona? person)
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(person);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
        }

        private void SetupPersona(long personId)
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(personId)).Returns(new Persona
            {
                CodigoPersona = personId,
                FechaVtoDocumentoPersona = DateTime.Today.AddYears(1)
            });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
        }

        private void SetupEncuesta(long personId, EncuestaIniAdmision survey)
        {
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(personId)).Returns(survey);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
        }

        private void SetupEncuestaIni(long personId, EncuestaIni survey)
        {
            var encuestaRepo = new Mock<IEncuestaIniRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(personId)).Returns(survey);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaRepo.Object);
        }

        private void SetupDocumentosValidos(long personId)
        {
            var imagenRepo = new Mock<IImagenTemporalRepository>();
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(personId, 1))
                .Returns(new ImagenTemporal
                {
                    CodigoPersona = personId,
                    TipoImagen = "1",
                    BlobImagen = [1],
                    FechaVtoDocumentoPersona = DateTime.Today.AddYears(1)
                });
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(personId, 2))
                .Returns(new ImagenTemporal
                {
                    CodigoPersona = personId,
                    TipoImagen = "1",
                    BlobImagen = [1],
                    FechaVtoDocumentoPersona = DateTime.Today.AddYears(1)
                });
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenRepo.Object);
        }

        private void SetupDocumentosDefinitivosValidos(long personId)
        {
            var temporalRepo = new Mock<IImagenTemporalRepository>();
            temporalRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(personId, It.IsAny<int>()))
                .Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(temporalRepo.Object);

            var imagenRepo = new Mock<IImagenRepository>();
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(personId, 1))
                .Returns(new Imagen
                {
                    CodigoPersona = personId,
                    TipoImagen = "1",
                    BlobImagen = [1]
                });
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(personId, 2))
                .Returns(new Imagen
                {
                    CodigoPersona = personId,
                    TipoImagen = "1",
                    BlobImagen = [1]
                });
            _uowMock.Setup(u => u.Imagens).Returns(imagenRepo.Object);
        }

        private static EncuestaIniAdmision EncuestaCompleta(long personId)
        {
            return new EncuestaIniAdmision
            {
                CodigoPersona = personId,
                EstadoEncuestaIniAdmision = "CONFIRMADO",
                IdProducto = 20,
                IdProceso = 30,
                IdComienzo = 40,
                FechaVtoAdmision = DateTime.Today.AddDays(10)
            };
        }

        private static EncuestaIniAdmision EncuestaCompletaVencida(long personId)
        {
            var survey = EncuestaCompleta(personId);
            survey.FechaVtoAdmision = DateTime.Today.AddDays(-1);
            return survey;
        }

        private static StubHttpMessageHandler ConfirmacionConEstadoCuentaHandler(string confirmacionBody)
        {
            return ConfirmacionConEstadoCuentaHandler(
                confirmacionBody,
                """
                {
                  "saldoActual": 3210.50,
                  "saldoVencido": 100,
                  "saldoAVencer": 200,
                  "movimientos": []
                }
                """);
        }

        private static StubHttpMessageHandler ConfirmacionConEstadoCuentaHandler(
            string confirmacionBody,
            string estadoCuentaBody,
            HttpStatusCode estadoCuentaStatusCode = HttpStatusCode.OK)
        {
            return new StubHttpMessageHandler(request =>
            {
                if (request.RequestUri?.ToString().Contains("Pagos/CtaCte", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return JsonResponse(estadoCuentaStatusCode, estadoCuentaBody);
                }

                return JsonResponse(HttpStatusCode.OK, confirmacionBody);
            });
        }

        /// <summary>
        /// Composition root de los casos de uso de inscripciones para los tests: reemplaza al
        /// antiguo InscripcionesService, que concentraba los cuatro procesos en una clase.
        /// </summary>
        private sealed record EnrollmentUseCases(
            IRegisterProductInterest Interest,
            IConfirmPreEnrollment Confirm,
            IReactivateEnrollment Reactivate,
            IGetEnrollmentDetails Details,
            IGetStudentRegulationsAcceptance Regulations);

        /// <summary>Ids distintos a los de cualquier ambiente real: si el codigo volviera a hardcodearlos, los asserts fallan.</summary>
        private static IConfiguration CorporateInboxConfiguration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CorporateInbox:IdEstadoProcesoInicio"] = "11111",
                    ["CorporateInbox:IdEstadoProcesoSolicitud"] = "22222"
                })
                .Build();

        private EnrollmentUseCases CrearCasosDeUso(EnrollmentsAndPaymentsApiClient apiClient)
        {
            var confirm = new ConfirmPreEnrollment(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                apiClient,
                new ConfirmCorporatePreEnrollment(_bandejaServiceScopeFactoryMock.Object, CorporateInboxConfiguration()));

            return new EnrollmentUseCases(
                new RegisterProductInterest(
                    _uowFactoryMock.Object, _dbConnectionContextMock.Object, _tivenosEnvioServiceMock.Object),
                confirm,
                new ReactivateEnrollment(_uowFactoryMock.Object, confirm),
                new GetEnrollmentDetails(_uowFactoryMock.Object, apiClient),
                new GetStudentRegulationsAcceptance(_uowFactoryMock.Object));
        }

        /// <summary>
        /// Arma el caso de uso de pago con sus tres casos de uso concretos, contra la API interna
        /// simulada por <paramref name="handler"/>.
        /// </summary>
        private IStartEnrollmentPayment CrearPagosConApi(HttpMessageHandler handler)
        {
            var apiClient = new EnrollmentsAndPaymentsApiClient(
                new HttpClient(handler) { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<EnrollmentsAndPaymentsApiClient>.Instance);

            return CrearPagos(apiClient);
        }

        private IStartEnrollmentPayment CrearPagos(EnrollmentsAndPaymentsApiClient apiClient) =>
            new StartEnrollmentPayment(
                _uowFactoryMock.Object,
                new PayWithPersonalAccount(_uowFactoryMock.Object, apiClient),
                new RegisterExternalPaymentMethod(_uowFactoryMock.Object),
                new GenerateInvoicePaymentUrl(_uowFactoryMock.Object, apiClient));

        private EnrollmentUseCases CrearServiceConApi(HttpMessageHandler handler)
        {
            var apiClient = new EnrollmentsAndPaymentsApiClient(
                new HttpClient(handler) { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<EnrollmentsAndPaymentsApiClient>.Instance);

            return CrearCasosDeUso(apiClient);
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : HttpMessageHandler
        {
            public List<CapturedRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new CapturedRequest(
                    request.Method,
                    request.RequestUri?.ToString() ?? string.Empty,
                    request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));

                return handler(request);
            }
        }

        private sealed record CapturedRequest(HttpMethod Method, string RequestUri, string Body);

        private void SetupReposDerechoEncuesta(
            bool existeFresco = false,
            bool existeEncuestaIni = false)
        {
            var frescoRepo = new Mock<IVdEsFrescoAdmisionRepository>();
            frescoRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(existeFresco);
            _uowMock.Setup(u => u.VdEsFrescoAdmisions).Returns(frescoRepo.Object);

            var encuestaIniRepo = new Mock<IEncuestaIniRepository>();
            encuestaIniRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(existeEncuestaIni);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaIniRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
        }

        private void SetupFresco(string? estado, decimal productId = 10m, decimal admissionProcessId = 20m, decimal idInscripto = 0m, string? productFullName = null, long? idOferta = null)
        {
            var fresco1y2Repo = new Mock<IVdInscripcionesFresco1y2Repository>();
            fresco1y2Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(estado == null
                    ? new List<VdInscripcionesFresco1y2>()
                    : new List<VdInscripcionesFresco1y2> { new() { IdProducto = productId, IdProceso = admissionProcessId, IdInscripto = idInscripto, EstadoInscripcion = estado, NombreExtensoProducto = productFullName, IdOferta = idOferta } });
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(fresco1y2Repo.Object);

            var fresco3y4Repo = new Mock<IVdInscripcionesFresco3y4Repository>();
            fresco3y4Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco3y4>());
            _uowMock.Setup(u => u.VdInscripcionesFresco3y4s).Returns(fresco3y4Repo.Object);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenEnProceso_ReturnsOfertasSeleccionadas()
        {
            SetupFresco(global::AppLogic.Contracts.Constants.EnrollmentStatus.InProgress);
            // Nivel 3 y 4: el interés puede abarcar varias ofertas con distinto comienzo.
            var offerings = new List<Oferta>
            {
                OfertaValida(99, 10, 7, 5, name: "AP", nombreExtenso: "Analista Programador", nombreComienzo: "Marzo 2026"),
                OfertaValida(100, 10, 8, 5, name: "AP", nombreExtenso: "Analista Programador", nombreComienzo: "Agosto 2026")
            };
            var interesRepo = new Mock<IInteresProductoOfertaRepository>();
            interesRepo.Setup(r => r.GetOfertasSeleccionadas(123, 10, 20)).Returns(offerings);
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesRepo.Object);

            var result = await _service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("En proceso", result.Data!.Status);
            Assert.NotNull(result.Data.InProgress);
            Assert.Equal(10, result.Data.InProgress!.Summary.ProductId);
            Assert.Equal("Analista Programador", result.Data.InProgress!.Summary.DegreeProgram);
            Assert.Null(result.Data.InProgress!.Summary.PaymentDueDate);
            Assert.Equal(2, result.Data.InProgress.Interests.Count);
            Assert.Equal(99, result.Data.InProgress.Interests[0].OfferingId);
            Assert.Equal("Marzo 2026", result.Data.InProgress.Interests[0].Intake);
            Assert.Equal("Nocturno", result.Data.InProgress.Interests[0].Shift);
            Assert.Null(result.Data.InProgress.Interests[0].EnrollmentId);
            Assert.Equal(100, result.Data.InProgress.Interests[1].OfferingId);
            Assert.Equal("Agosto 2026", result.Data.InProgress.Interests[1].Intake);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenEnProcesoNivel3y4SinIdInscripto_ReturnsOfertasSeleccionadas()
        {
            // "En proceso" todavia no tiene fila en T_INSCRIPTO: la vista fresco trae ID_INSCRIPTO null.
            var fresco1y2Repo = new Mock<IVdInscripcionesFresco1y2Repository>();
            fresco1y2Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco1y2>());
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(fresco1y2Repo.Object);

            var fresco3y4Repo = new Mock<IVdInscripcionesFresco3y4Repository>();
            fresco3y4Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco3y4>
                {
                    new()
                    {
                        IdProducto = 10m,
                        IdProceso = 20m,
                        IdInscripto = null,
                        IdOferta = 99L,
                        EstadoInscripcion = global::AppLogic.Contracts.Constants.EnrollmentStatus.InProgress
                    }
                });
            _uowMock.Setup(u => u.VdInscripcionesFresco3y4s).Returns(fresco3y4Repo.Object);

            var interesRepo = new Mock<IInteresProductoOfertaRepository>();
            interesRepo
                .Setup(r => r.GetOfertasSeleccionadas(123, 10, 20))
                .Returns(new List<Oferta> { OfertaValida(99, 10, 7, 5, productLevelId: 4) });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesRepo.Object);

            var result = await _service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("En proceso", result.Data!.Status);
            Assert.NotNull(result.Data.InProgress);
            Assert.Equal(99, result.Data.InProgress!.Interests[0].OfferingId);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenALaEspera_ReturnsEstadoSinDetalle()
        {
            SetupFresco(global::AppLogic.Contracts.Constants.EnrollmentStatus.Waiting);

            var result = await _service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("A la espera", result.Data!.Status);
            Assert.Null(result.Data.InProgress);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenPagoPendiente_ReturnsDetallePago()
        {
            SetupFresco(
                global::AppLogic.Contracts.Constants.EnrollmentStatus.PaymentPending,
                idInscripto: 555m,
                productFullName: "Analista programador",
                idOferta: 99L);

            var enrollment = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 99,
                FechaVtoInscr = new DateTime(2026, 7, 1)
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(enrollment);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(555)).Returns((InscriptoSeniaMinimum)null);
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
                """
                {
                  "carritos": [
                    { "idCarrito": "123|10|1|7|555", "senia": 1500.50 }
                  ],
                  "estadoCuenta": { "saldoActual": 3210.50 }
                }
                """));
            var service = CrearServiceConApi(handler);

            var result = await service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("Pago pendiente", result.Data!.Status);
            Assert.NotNull(result.Data.PendingPayment);
            Assert.True(result.Data.PendingPayment!.Confirmed);
            Assert.Equal(555, result.Data.PendingPayment!.Enrollments[0].EnrollmentId);
            Assert.Equal(99, result.Data.PendingPayment!.Enrollments[0].OfferingId);
            Assert.Equal(1500.50m, result.Data.PendingPayment.DepositAmount);
            Assert.Equal(3210.50m, result.Data.PendingPayment.CurrentAccount!.CurrentBalance);
            Assert.Equal(new DateTime(2026, 7, 1), result.Data.PendingPayment.Summary.PaymentDueDate);
            Assert.Equal("Analista programador", result.Data.PendingPayment.Summary.DegreeProgram);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("Pagos/Carritos?idsInscripcion=555", request.RequestUri);
            inscriptoRepo.Verify(r => r.GetDetalleByKey(555, 123), Times.Once);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenPagoPendienteNivel1y2_UsaIdOfertaDeLaVistaFresco()
        {
            var fresco1y2Repo = new Mock<IVdInscripcionesFresco1y2Repository>();
            fresco1y2Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco1y2>
                {
                    new()
                    {
                        IdProducto = 10, IdProceso = 20, IdInscripto = 555, IdOferta = 77,
                        EstadoInscripcion = global::AppLogic.Contracts.Constants.EnrollmentStatus.PaymentPending,
                        NombreExtensoProducto = "Analista programador"
                    }
                });
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(fresco1y2Repo.Object);

            var fresco3y4Repo = new Mock<IVdInscripcionesFresco3y4Repository>();
            fresco3y4Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco3y4>());
            _uowMock.Setup(u => u.VdInscripcionesFresco3y4s).Returns(fresco3y4Repo.Object);

            // IdOferta distinto del que trae la vista, para probar que gana el de la vista (99 no deberia aparecer).
            var enrollment = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 99,
                FechaVtoInscr = new DateTime(2026, 7, 1)
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(enrollment);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(555)).Returns((InscriptoSeniaMinimum)null);
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
                """
                {
                  "carritos": [],
                  "estadoCuenta": { "saldoActual": 0 }
                }
                """));
            var service = CrearServiceConApi(handler);

            var result = await service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal(77, result.Data!.PendingPayment!.Enrollments[0].OfferingId);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenPagoPendienteConSeminarios_AgrupaSeniasDeVariasOfertas()
        {
            var fresco1y2Repo = new Mock<IVdInscripcionesFresco1y2Repository>();
            fresco1y2Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco1y2>());
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(fresco1y2Repo.Object);

            var fresco3y4Repo = new Mock<IVdInscripcionesFresco3y4Repository>();
            fresco3y4Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123, 10, 20))
                .Returns(new List<VdInscripcionesFresco3y4>
                {
                    new()
                    {
                        IdProducto = 10, IdProceso = 20, IdInscripto = 555, IdOferta = 99,
                        EstadoInscripcion = global::AppLogic.Contracts.Constants.EnrollmentStatus.PaymentPending,
                        ProgConSeminariosProducto = "SI",
                        DescripcionOferta = "Seminario de introduccion",
                        FechaInicioComienzo = new DateTime(2026, 3, 1)
                    },
                    new()
                    {
                        IdProducto = 10, IdProceso = 20, IdInscripto = 556, IdOferta = 100,
                        EstadoInscripcion = global::AppLogic.Contracts.Constants.EnrollmentStatus.PaymentPending,
                        ProgConSeminariosProducto = "SI",
                        DescripcionOferta = "Seminario avanzado",
                        FechaInicioComienzo = new DateTime(2026, 3, 2)
                    }
                });
            _uowMock.Setup(u => u.VdInscripcionesFresco3y4s).Returns(fresco3y4Repo.Object);

            var inscripto555 = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 99,
                FechaVtoInscr = new DateTime(2026, 7, 1)
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(inscripto555);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(555)).Returns((InscriptoSeniaMinimum)null);
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
                """
                {
                  "carritos": [
                    { "idCarrito": "123|10|1|7|555", "senia": 1500.50 },
                    { "idCarrito": "123|10|1|8|556", "senia": 800.25 }
                  ],
                  "estadoCuenta": { "saldoActual": 3210.50 }
                }
                """));
            var service = CrearServiceConApi(handler);

            var result = await service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("Pago pendiente", result.Data!.Status);
            Assert.NotNull(result.Data.PendingPayment);
            Assert.Equal(2, result.Data.PendingPayment!.Enrollments.Count);
            Assert.Equal(555, result.Data.PendingPayment!.Enrollments[0].EnrollmentId);
            Assert.Equal("Seminario de introduccion", result.Data.PendingPayment!.Enrollments[0].OfferingDescription);
            Assert.Equal(556, result.Data.PendingPayment!.Enrollments[1].EnrollmentId);
            Assert.Equal("Seminario avanzado", result.Data.PendingPayment!.Enrollments[1].OfferingDescription);
            Assert.Equal(2300.75m, result.Data.PendingPayment.DepositAmount);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("idsInscripcion=555", request.RequestUri);
            Assert.Contains("idsInscripcion=556", request.RequestUri);
            inscriptoRepo.Verify(r => r.GetDetalleByKey(555, 123), Times.Once);
            inscriptoRepo.Verify(r => r.GetDetalleByKey(556, It.IsAny<long>()), Times.Never);
        }

        /// <summary>
        /// Producto con seminarios en dos estados: los ya pagos no tienen que entrar al carrito de
        /// la tarjeta de pago pendiente. El estado llega desde la tarjeta que abrió la persona.
        /// </summary>
        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenSeminariosEnDosEstados_UsaSoloLosDelEstadoPedido()
        {
            var fresco1y2Repo = new Mock<IVdInscripcionesFresco1y2Repository>();
            fresco1y2Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco1y2>());
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(fresco1y2Repo.Object);

            var fresco3y4Repo = new Mock<IVdInscripcionesFresco3y4Repository>();
            fresco3y4Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123, 10, 20))
                .Returns(new List<VdInscripcionesFresco3y4>
                {
                    // Comienzo más temprano: sin filtro por estado, esta fila decidiría el detalle.
                    new()
                    {
                        IdProducto = 10, IdProceso = 20, IdInscripto = 555, IdOferta = 99,
                        IdNivelProducto = 4,
                        EstadoInscripcion = global::AppLogic.Contracts.Constants.EnrollmentStatus.Confirmed,
                        ProgConSeminariosProducto = "SI",
                        DescripcionOferta = "Seminario ya pago",
                        FechaInicioComienzo = new DateTime(2026, 3, 1)
                    },
                    new()
                    {
                        IdProducto = 10, IdProceso = 20, IdInscripto = 556, IdOferta = 100,
                        IdNivelProducto = 4,
                        EstadoInscripcion = global::AppLogic.Contracts.Constants.EnrollmentStatus.PaymentPending,
                        ProgConSeminariosProducto = "SI",
                        DescripcionOferta = "Seminario impago",
                        FechaInicioComienzo = new DateTime(2026, 3, 2)
                    }
                });
            _uowMock.Setup(u => u.VdInscripcionesFresco3y4s).Returns(fresco3y4Repo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(556, 123))
                .Returns(new Inscripto { IdInscripto = 556, CodigoPersona = 123, IdOferta = 100, FechaVtoInscr = new DateTime(2026, 7, 1) });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(556)).Returns((InscriptoSeniaMinimum)null);
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
                """
                {
                  "carritos": [
                    { "idCarrito": "123|10|1|8|556", "senia": 800.25 }
                  ],
                  "estadoCuenta": { "saldoActual": 3210.50 }
                }
                """));
            var service = CrearServiceConApi(handler);

            var result = await service.Details.ExecuteAsync(
                123, 10, 20, global::AppLogic.Contracts.Constants.EnrollmentStatus.PaymentPending);

            Assert.True(result.Success);
            Assert.Equal("Pago pendiente", result.Data!.Status);
            var pendiente = Assert.Single(result.Data.PendingPayment!.Enrollments);
            Assert.Equal(556, pendiente.EnrollmentId);
            Assert.Equal("Seminario impago", pendiente.OfferingDescription);
            Assert.Equal(800.25m, result.Data.PendingPayment.DepositAmount);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("idsInscripcion=556", request.RequestUri);
            Assert.DoesNotContain("idsInscripcion=555", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenConfirmada_ReturnsDatosYMaterias()
        {
            SetupFresco(global::AppLogic.Contracts.Constants.EnrollmentStatus.Confirmed, idInscripto: 555m);

            var enrollment = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 99,
                Oferta = new Oferta
                {
                    IdOferta = 99,
                    IdTurno = 5,
                    Turno = new Turno { IdTurno = 5, NombreTurno = "Matutino" },
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 7, NombreComienzo = "Marzo 2026" },
                        Paquete = new Paquete
                        {
                            Producto = new Producto
                            {
                                IdProducto = 10,
                                NombreWebProducto = "Licenciatura en DiseÃ±o GrÃ¡fico",
                                NombreCoordAcadProducto = "No usar",
                                EmailCoordAcadProducto = "no.usar@ort.edu.uy"
                            }
                        }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(enrollment);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var ofertaRepo = new Mock<IOfertaRepository>();
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var coordinadoresRepo = new Mock<IVdInscriptoCoordinadoreRepository>();
            coordinadoresRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCoordinadore>
            {
                new()
                {
                    IdInscripto = 555,
                    CooacadCodigo = 1,
                    CooacadPrimerNombre = "MarÃ­a ",
                    CooacadPrimerApellido = " RodrÃ­guez",
                    MailAcad = " maria.rodriguez@ort.edu.uy ",
                    CoorespCodigo = 2,
                    CoorespPrimerNombre = "Juan",
                    CoorespPrimerApellido = "PÃ©rez",
                    MailResp = "juan.perez@ort.edu.uy"
                }
            });
            _uowMock.Setup(u => u.VdInscriptoCoordinadores).Returns(coordinadoresRepo.Object);

            var creditosRepo = new Mock<IVdInscriptoCreditoAlumnoRepository>();
            creditosRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCreditoAlumno>
            {
                new() { IdInscripto = 555, IdMateria = 1, DescripcionMateria = "Arte y estÃ©tica I" },
                new() { IdInscripto = 555, IdMateria = 2, DescripcionMateria = "FotografÃ­a y ediciÃ³n de video" },
                new() { IdInscripto = 555, IdMateria = 1, DescripcionMateria = "Arte y estÃ©tica I duplicada" },
                new() { IdInscripto = 555, IdMateria = null, DescripcionMateria = "Sin materia" }
            });
            _uowMock.Setup(u => u.VdInscriptoCreditoAlumnos).Returns(creditosRepo.Object);

            var result = await _service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("Confirmada", result.Data!.Status);
            Assert.NotNull(result.Data.Confirmed);
            Assert.Equal(123, result.Data.Confirmed!.PersonId);
            Assert.Equal("Licenciatura en DiseÃ±o GrÃ¡fico", result.Data.Confirmed!.DegreeProgram);
            Assert.Equal("MarÃ­a RodrÃ­guez", result.Data.Confirmed.AcademicCoordinator!.Name);
            Assert.Equal("maria.rodriguez@ort.edu.uy", result.Data.Confirmed.AcademicCoordinator.Email);
            Assert.Equal("Juan PÃ©rez", result.Data.Confirmed.CourseCoordinator!.Name);
            Assert.Equal("juan.perez@ort.edu.uy", result.Data.Confirmed.CourseCoordinator.Email);
            Assert.Single(result.Data.Confirmed.Enrollments);
            Assert.Equal(2, result.Data.Confirmed.Enrollments[0].FirstSemesterSubjects.Count);
            Assert.Contains(result.Data.Confirmed.Enrollments[0].FirstSemesterSubjects, m => m.Name == "Arte y estÃ©tica I");
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenConfirmadaAndCoordinadoresIguales_MuestraSoloAcademico()
        {
            SetupFresco(global::AppLogic.Contracts.Constants.EnrollmentStatus.Confirmed, idInscripto: 555m);

            var enrollment = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 99,
                Oferta = new Oferta
                {
                    IdOferta = 99,
                    IdTurno = 5,
                    Turno = new Turno { IdTurno = 5, NombreTurno = "Matutino" },
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 7, NombreComienzo = "Marzo 2026" },
                        Paquete = new Paquete
                        {
                            Producto = new Producto { IdProducto = 10, NombreWebProducto = "Licenciatura en DiseÃ±o GrÃ¡fico" }
                        }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(enrollment);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var coordinadoresRepo = new Mock<IVdInscriptoCoordinadoreRepository>();
            coordinadoresRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCoordinadore>
            {
                new()
                {
                    IdInscripto = 555,
                    CooacadCodigo = 10,
                    CooacadPrimerNombre = "MarÃ­a",
                    CooacadPrimerApellido = "RodrÃ­guez",
                    MailAcad = "maria.rodriguez@ort.edu.uy",
                    CoorespCodigo = 10,
                    CoorespPrimerNombre = "MarÃ­a",
                    CoorespPrimerApellido = "RodrÃ­guez",
                    MailResp = "maria.rodriguez@ort.edu.uy"
                }
            });
            _uowMock.Setup(u => u.VdInscriptoCoordinadores).Returns(coordinadoresRepo.Object);

            var creditosRepo = new Mock<IVdInscriptoCreditoAlumnoRepository>();
            creditosRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCreditoAlumno>());
            _uowMock.Setup(u => u.VdInscriptoCreditoAlumnos).Returns(creditosRepo.Object);

            var result = await _service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("MarÃ­a RodrÃ­guez", result.Data!.Confirmed!.AcademicCoordinator!.Name);
            Assert.Null(result.Data.Confirmed.CourseCoordinator);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenConfirmadaConSeminarios_MuestraTodasLasOfertas()
        {
            var fresco1y2Repo = new Mock<IVdInscripcionesFresco1y2Repository>();
            fresco1y2Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco1y2>());
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(fresco1y2Repo.Object);

            var fresco3y4Repo = new Mock<IVdInscripcionesFresco3y4Repository>();
            fresco3y4Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(123, 10, 20))
                .Returns(new List<VdInscripcionesFresco3y4>
                {
                    new()
                    {
                        IdProducto = 10, IdProceso = 20, IdInscripto = 555, IdOferta = 57018,
                        EstadoInscripcion = global::AppLogic.Contracts.Constants.EnrollmentStatus.Confirmed,
                        ProgConSeminariosProducto = "SI",
                        DescripcionOferta = "Seminario de introduccion"
                    },
                    new()
                    {
                        IdProducto = 10, IdProceso = 20, IdInscripto = 556, IdOferta = 57019,
                        EstadoInscripcion = global::AppLogic.Contracts.Constants.EnrollmentStatus.Confirmed,
                        ProgConSeminariosProducto = "SI",
                        DescripcionOferta = "Seminario avanzado"
                    }
                });
            _uowMock.Setup(u => u.VdInscripcionesFresco3y4s).Returns(fresco3y4Repo.Object);

            var product = new Producto { IdProducto = 10, NombreWebProducto = "Certificado en Gerencia" };
            var inscripto555 = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 57018,
                Oferta = new Oferta
                {
                    IdOferta = 57018,
                    Turno = new Turno { IdTurno = 1, NombreTurno = "Matutino" },
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 1501, NombreComienzo = "Marzo 2026" },
                        Paquete = new Paquete { Producto = product }
                    }
                }
            };
            var inscripto556 = new Inscripto
            {
                IdInscripto = 556,
                CodigoPersona = 123,
                IdOferta = 57019,
                Oferta = new Oferta
                {
                    IdOferta = 57019,
                    Turno = new Turno { IdTurno = 3, NombreTurno = "Nocturno" },
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 1497, NombreComienzo = "Setiembre 2026" },
                        Paquete = new Paquete { Producto = product }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(inscripto555);
            inscriptoRepo.Setup(r => r.GetDetalleByKey(556, 123)).Returns(inscripto556);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var coordinadoresRepo = new Mock<IVdInscriptoCoordinadoreRepository>();
            coordinadoresRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCoordinadore>());
            _uowMock.Setup(u => u.VdInscriptoCoordinadores).Returns(coordinadoresRepo.Object);

            var creditosRepo = new Mock<IVdInscriptoCreditoAlumnoRepository>();
            creditosRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCreditoAlumno>
            {
                new() { IdInscripto = 555, IdMateria = 1, DescripcionMateria = "Decisiones financieras para ejecutivos" }
            });
            creditosRepo.Setup(r => r.GetByInscripto(556)).Returns(new List<VdInscriptoCreditoAlumno>
            {
                new() { IdInscripto = 556, IdMateria = 2, DescripcionMateria = "Liderazgo de equipos" }
            });
            _uowMock.Setup(u => u.VdInscriptoCreditoAlumnos).Returns(creditosRepo.Object);

            var result = await _service.Details.ExecuteAsync(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("Confirmada", result.Data!.Status);
            Assert.NotNull(result.Data.Confirmed);
            Assert.Equal("Certificado en Gerencia", result.Data.Confirmed!.DegreeProgram);
            Assert.Equal(2, result.Data.Confirmed.Enrollments.Count);
            var oferta555 = result.Data.Confirmed.Enrollments.Single(o => o.EnrollmentId == 555);
            Assert.Equal(57018, oferta555.OfferingId);
            Assert.Equal("Marzo 2026", oferta555.Intake);
            Assert.Equal("Decisiones financieras para ejecutivos", Assert.Single(oferta555.FirstSemesterSubjects).Name);
            var oferta556 = result.Data.Confirmed.Enrollments.Single(o => o.EnrollmentId == 556);
            Assert.Equal(57019, oferta556.OfferingId);
            Assert.Equal("Setiembre 2026", oferta556.Intake);
            Assert.Equal("Liderazgo de equipos", Assert.Single(oferta556.FirstSemesterSubjects).Name);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenNoInscripcion_ReturnsNotFound()
        {
            SetupFresco(null);

            var result = await _service.Details.ExecuteAsync(123, 10, 20);

            Assert.False(result.Success);
            Assert.Equal("INS_DET_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task Pagar_WithSistarbanc_PostsAllCarritosAndReturnsUrl()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, "\"https://pagos.test/factura\""));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = "SISTARBANC",
                SistarbancBankId = "001"
            });

            Assert.True(result.Success);
            Assert.Equal("https://pagos.test/factura", result.Data!.PaymentUrl);
            Assert.Null(result.Data.EncryptedParameters);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("UrlCrearFactura?tipoPago=SISTARBANC&banco=001&idsInscripcion=555", request.RequestUri);
            Assert.Equal(string.Empty, request.Body);
        }

        [Fact]
        public async Task Pagar_WithParametrosEncriptados_SeparatesUrlFromParam()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, "\"https://pagos.test/factura?parametrosEncriptados=abc123\""));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = "BANRED"
            });

            Assert.True(result.Success);
            Assert.Equal("https://pagos.test/factura", result.Data!.PaymentUrl);
            Assert.Equal("abc123", result.Data.EncryptedParameters);
        }

        [Fact]
        public async Task Pagar_WhenSistarbancWithoutBank_ReturnsBadRequest()
        {
            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = "SISTARBANC"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_UF_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task Pagar_WithUrl_WhenInscriptoDoesNotBelongToPersona_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = "BANRED"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_UF_04", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task Pagar_WithUrl_WhenApiRejects_ReturnsFailure()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.BadRequest, "No hay carritos para la inscripcion."));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = "BANRED"
            });

            Assert.False(result.Success);
            Assert.Equal("URL_CREAR_FACTURA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task Pagar_WithCuentaPersonal_ReturnsPagoConfirmadoConDetalle()
        {
            var enrollment = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 99,
                Oferta = new Oferta
                {
                    IdOferta = 99,
                    IdTurno = 5,
                    Turno = new Turno { IdTurno = 5, NombreTurno = "Matutino" },
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 7, NombreComienzo = "Marzo 2026" },
                        Paquete = new Paquete
                        {
                            Producto = new Producto { IdProducto = 10, NombreWebProducto = "Licenciatura en DiseÃ±o GrÃ¡fico" }
                        }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(enrollment);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var coordinadoresRepo = new Mock<IVdInscriptoCoordinadoreRepository>();
            coordinadoresRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCoordinadore>
            {
                new()
                {
                    IdInscripto = 555,
                    CooacadCodigo = 1,
                    CooacadPrimerNombre = "MarÃ­a",
                    CooacadPrimerApellido = "RodrÃ­guez",
                    MailAcad = "maria.rodriguez@ort.edu.uy"
                }
            });
            _uowMock.Setup(u => u.VdInscriptoCoordinadores).Returns(coordinadoresRepo.Object);

            var creditosRepo = new Mock<IVdInscriptoCreditoAlumnoRepository>();
            creditosRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCreditoAlumno>
            {
                new() { IdInscripto = 555, IdMateria = 1, DescripcionMateria = "Arte y estÃ©tica I" }
            });
            _uowMock.Setup(u => u.VdInscriptoCreditoAlumnos).Returns(creditosRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """[{ "clave": "123|10|1|7|555", "valor": "ok" }]"""));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "CUENTA_PERSONAL" });

            Assert.True(result.Success);
            Assert.Equal("PAGO_CONFIRMADO", result.Data!.Result);
            Assert.Single(result.Data.Messages);
            Assert.NotNull(result.Data.Confirmed);
            Assert.Equal(123, result.Data.Confirmed!.PersonId);
            Assert.Equal("Licenciatura en DiseÃ±o GrÃ¡fico", result.Data.Confirmed!.DegreeProgram);
            Assert.Equal("MarÃ­a RodrÃ­guez", result.Data.Confirmed.AcademicCoordinator!.Name);
            Assert.Single(result.Data.Confirmed.Enrollments);
            Assert.Single(result.Data.Confirmed.Enrollments[0].FirstSemesterSubjects);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("Pagos/Carritos/Pagar?tipoPago=PAGO_CUENTA_CORRIENTE&idsInscripcion=555", request.RequestUri);
        }

        [Fact]
        public async Task Pagar_WithCuentaPersonal_DetalleBestEffortCuandoVistasVacias()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var coordinadoresRepo = new Mock<IVdInscriptoCoordinadoreRepository>();
            coordinadoresRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCoordinadore>());
            _uowMock.Setup(u => u.VdInscriptoCoordinadores).Returns(coordinadoresRepo.Object);

            var creditosRepo = new Mock<IVdInscriptoCreditoAlumnoRepository>();
            creditosRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCreditoAlumno>());
            _uowMock.Setup(u => u.VdInscriptoCreditoAlumnos).Returns(creditosRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """[{ "clave": "123|10|1|7|555", "valor": "ok" }]"""));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "CUENTA_PERSONAL" });

            Assert.True(result.Success);
            Assert.Equal("PAGO_CONFIRMADO", result.Data!.Result);
            Assert.NotNull(result.Data.Confirmed);
            Assert.Equal(123, result.Data.Confirmed!.PersonId);
            Assert.Null(result.Data.Confirmed.AcademicCoordinator);
            Assert.Single(result.Data.Confirmed.Enrollments);
            Assert.Empty(result.Data.Confirmed.Enrollments[0].FirstSemesterSubjects);
        }

        [Fact]
        public async Task Pagar_WithAbitab_GuardaMetodoPago()
        {
            InscriptoSeniaMinimum? agregado = null;
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(555)).Returns((InscriptoSeniaMinimum)null);
            seniaRepo.Setup(r => r.Add(It.IsAny<InscriptoSeniaMinimum>()))
                .Callback<InscriptoSeniaMinimum>(x => agregado = x);
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = " abitab " });

            Assert.True(result.Success);
            Assert.Equal("METODO_GUARDADO", result.Data!.Result);
            Assert.Equal("ABITAB", agregado!.MetodoPagoSeniaMinima);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public async Task Pagar_WithBanred_ReturnsUrlGenerada()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, "\"https://pagos.test/factura\""));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "BANRED" });

            Assert.True(result.Success);
            Assert.Equal("URL_GENERADA", result.Data!.Result);
            Assert.Equal("https://pagos.test/factura", result.Data.PaymentUrl);
            Assert.Null(result.Data.EncryptedParameters);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("UrlCrearFactura?tipoPago=BANRED&banco=&idsInscripcion=555", request.RequestUri);
        }

        [Fact]
        public async Task Pagar_WithInvalidTipoPago_ReturnsBadRequest()
        {
            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "OTRO" });

            Assert.False(result.Success);
            Assert.Equal("INS_PAG_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _uowFactoryMock.Verify(f => f.Create(), Times.Never);
        }

        [Fact]
        public async Task Pagar_WithCuentaPersonal_PostsAllCarritosAndReturnsMessages()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .SetupSequence(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 })
                .Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK,
                """
                [
                  { "clave": "123|10|1|7|555", "valor": "Tu pago con Cuenta Personal se realizó exitosamente." },
                  { "clave": "123|10|1|7|556", "valor": "Tu pago con Cuenta Personal se realizó exitosamente." }
                ]
                """));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "CUENTA_PERSONAL" });

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Messages.Count);
            Assert.Equal("123|10|1|7|555", result.Data.Messages[0].Key);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("Pagos/Carritos/Pagar?tipoPago=PAGO_CUENTA_CORRIENTE&idsInscripcion=555", request.RequestUri);
            Assert.Equal(string.Empty, request.Body);
        }

        [Fact]
        public async Task Pagar_WithCuentaPersonal_WhenInscriptoDoesNotBelongToPersona_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "CUENTA_PERSONAL" });

            Assert.False(result.Success);
            Assert.Equal("INS_PC_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task Pagar_WithCuentaPersonal_WhenNoCarritos_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.BadRequest, "No hay carritos para la inscripcion."));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "CUENTA_PERSONAL" });

            Assert.False(result.Success);
            Assert.Equal("PAGAR_CARRITOS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task Pagar_WithCuentaPersonal_WhenLegacyRejects_ReturnsFailure()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, "saldo insuficiente"));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555], PaymentType = "CUENTA_PERSONAL" });

            Assert.False(result.Success);
            Assert.Equal("PAGAR_CARRITOS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Contains("saldo insuficiente", result.Message);
        }

        [Fact]
        public async Task Pagar_WithCuentaPersonal_MultiplesOfertas_PagaTodoJunto()
        {
            var product = new Producto { IdProducto = 10, NombreWebProducto = "Certificado en Gerencia" };
            var inscripto555 = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 57018,
                Oferta = new Oferta
                {
                    IdOferta = 57018,
                    IdTurno = 1,
                    Turno = new Turno { IdTurno = 1, NombreTurno = "Matutino" },
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 1501, NombreComienzo = "Marzo 2026" },
                        Paquete = new Paquete { Producto = product }
                    }
                }
            };
            var inscripto556 = new Inscripto
            {
                IdInscripto = 556,
                CodigoPersona = 123,
                IdOferta = 57019,
                Oferta = new Oferta
                {
                    IdOferta = 57019,
                    IdTurno = 3,
                    Turno = new Turno { IdTurno = 3, NombreTurno = "Nocturno" },
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 1497, NombreComienzo = "Setiembre 2026" },
                        Paquete = new Paquete { Producto = product }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(inscripto555);
            inscriptoRepo.Setup(r => r.GetDetalleByKey(556, 123)).Returns(inscripto556);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var coordinadoresRepo = new Mock<IVdInscriptoCoordinadoreRepository>();
            coordinadoresRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCoordinadore>());
            _uowMock.Setup(u => u.VdInscriptoCoordinadores).Returns(coordinadoresRepo.Object);

            var creditosRepo = new Mock<IVdInscriptoCreditoAlumnoRepository>();
            creditosRepo.Setup(r => r.GetByInscripto(555)).Returns(new List<VdInscriptoCreditoAlumno>
            {
                new() { IdInscripto = 555, IdMateria = 1, DescripcionMateria = "Decisiones financieras para ejecutivos" }
            });
            creditosRepo.Setup(r => r.GetByInscripto(556)).Returns(new List<VdInscriptoCreditoAlumno>
            {
                new() { IdInscripto = 556, IdMateria = 2, DescripcionMateria = "Liderazgo de equipos" }
            });
            _uowMock.Setup(u => u.VdInscriptoCreditoAlumnos).Returns(creditosRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK,
                """
                [
                  { "clave": "123|10|1|7|555", "valor": "ok" },
                  { "clave": "123|10|1|8|556", "valor": "ok" }
                ]
                """));
            var service = CrearPagosConApi(handler);

            var result = await service.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555, 556], PaymentType = "CUENTA_PERSONAL" });

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Messages.Count);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("Pagos/Carritos/Pagar?tipoPago=PAGO_CUENTA_CORRIENTE", request.RequestUri);
            Assert.Contains("idsInscripcion=555", request.RequestUri);
            Assert.Contains("idsInscripcion=556", request.RequestUri);

            Assert.NotNull(result.Data.Confirmed);
            Assert.Equal("Certificado en Gerencia", result.Data.Confirmed!.DegreeProgram);
            Assert.Equal(2, result.Data.Confirmed.Enrollments.Count);
            var oferta555 = result.Data.Confirmed.Enrollments.Single(o => o.EnrollmentId == 555);
            Assert.Equal(57018, oferta555.OfferingId);
            Assert.Equal("Marzo 2026", oferta555.Intake);
            Assert.Equal("Decisiones financieras para ejecutivos", Assert.Single(oferta555.FirstSemesterSubjects).Name);
            var oferta556 = result.Data.Confirmed.Enrollments.Single(o => o.EnrollmentId == 556);
            Assert.Equal(57019, oferta556.OfferingId);
            Assert.Equal("Setiembre 2026", oferta556.Intake);
            Assert.Equal("Liderazgo de equipos", Assert.Single(oferta556.FirstSemesterSubjects).Name);
        }

        [Fact]
        public async Task Pagar_WithAbitab_MultiplesOfertas_GuardaTodasLasReservas()
        {
            var agregados = new List<InscriptoSeniaMinimum>();
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            inscriptoRepo.Setup(r => r.GetDetalleByKey(556, 123)).Returns(new Inscripto { IdInscripto = 556, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((InscriptoSeniaMinimum)null);
            seniaRepo.Setup(r => r.Add(It.IsAny<InscriptoSeniaMinimum>()))
                .Callback<InscriptoSeniaMinimum>(x => agregados.Add(x));
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [555, 556], PaymentType = "ABITAB" });

            Assert.True(result.Success);
            Assert.Equal(2, agregados.Count);
            Assert.Contains(agregados, a => a.IdInscripto == 555);
            Assert.Contains(agregados, a => a.IdInscripto == 556);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public async Task Pagar_WithAbitab_MultiplesOfertas_UnaNoPerteneceALaPersona_NoGuardaNinguna()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            inscriptoRepo.Setup(r => r.GetDetalleByKey(556, 123)).Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(It.IsAny<long>())).Returns((InscriptoSeniaMinimum)null);
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest { EnrollmentIds = [556, 555], PaymentType = "ABITAB" });

            Assert.False(result.Success);
            Assert.Equal("INS_MP_03", result.ErrorCode);
            _uowMock.Verify(u => u.Rollback(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
            seniaRepo.Verify(r => r.Add(It.IsAny<InscriptoSeniaMinimum>()), Times.Never);
        }

        [Fact]
        public async Task Pagar_WithPaganza_NormalizesMetodoPago()
        {
            InscriptoSeniaMinimum? agregado = null;
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(555)).Returns((InscriptoSeniaMinimum)null);
            seniaRepo.Setup(r => r.Add(It.IsAny<InscriptoSeniaMinimum>()))
                .Callback<InscriptoSeniaMinimum>(x => agregado = x);
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = " paganza "
            });

            Assert.True(result.Success);
            Assert.Equal("PAGANZA", agregado!.MetodoPagoSeniaMinima);
        }

        [Fact]
        public async Task Pagar_WhenMetodoPagoInvalid_ReturnsBadRequest()
        {
            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = "TARJETA"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_PAG_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _uowFactoryMock.Verify(f => f.Create(), Times.Never);
        }

        [Fact]
        public async Task Pagar_WithAbitab_WhenInscriptoDoesNotBelongToPersona_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = "ABITAB"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_MP_03", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public async Task Pagar_WithPaganza_WhenAlreadyExists_ReturnsConflict()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(555)).Returns(new InscriptoSeniaMinimum { IdInscripto = 555, MetodoPagoSeniaMinima = "ABITAB" });
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var result = await _payments.ExecuteAsync(123, new StartPaymentRequest
            {
                EnrollmentIds = [555],
                PaymentType = "PAGANZA"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_MP_04", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
            seniaRepo.Verify(r => r.Add(It.IsAny<InscriptoSeniaMinimum>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }
    }
}

