using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Tivenos;
using AppLogic.Services.Inscripciones;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class InscripcionesServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly Mock<IGeneralService> _generalServiceMock;
        private readonly Mock<ITivenosEnvioService> _tivenosEnvioServiceMock;
        private readonly InscripcionesService _service;

        public InscripcionesServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _generalServiceMock = new Mock<IGeneralService>();
            _tivenosEnvioServiceMock = new Mock<ITivenosEnvioService>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _dbConnectionContextMock
                .Setup(d => d.CurrentDateTime())
                .Returns(FechaBase);
            _generalServiceMock
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(global::Utilities.OperationResult<DateTime>.Ok(FechaBase.AddDays(5), nameof(IGeneralService.CalcularFechaVencimientoAdmisiones)));
            var encuestaIniRepo = new Mock<IEncuestaIniRepository>();
            encuestaIniRepo
                .Setup(r => r.GetByPersona(It.IsAny<long>()))
                .Returns((EncuestaIni)null);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaIniRepo.Object);
            _tivenosEnvioServiceMock
                .Setup(s => s.EncolarAltaInteresXSeleccionEnSitio(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<TivenosAltaInteresRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Returns(global::Utilities.OperationResult<bool>.Ok(true, nameof(ITivenosEnvioService.EncolarAltaInteresXSeleccionEnSitio)));
            _tivenosEnvioServiceMock
                .Setup(s => s.EncolarAltaDatosBachillerato(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<TivenosBachilleratoRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Returns(global::Utilities.OperationResult<bool>.Ok(true, nameof(ITivenosEnvioService.EncolarAltaDatosBachillerato)));
            _tivenosEnvioServiceMock
                .Setup(s => s.EncolarModificacionDatosBachillerato(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<TivenosBachilleratoRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Returns(global::Utilities.OperationResult<bool>.Ok(true, nameof(ITivenosEnvioService.EncolarModificacionDatosBachillerato)));
            var apiClient = new InscripcionesyPagosApiClient(
                new HttpClient { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<InscripcionesyPagosApiClient>.Instance);
            _service = new InscripcionesService(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _generalServiceMock.Object,
                _tivenosEnvioServiceMock.Object,
                apiClient);
        }

        private static readonly DateTime FechaBase = new(2026, 5, 27, 10, 30, 0);

        [Fact]
        public void ObtenerAceptacionReglamentoEstudiantil_WhenNoAcceptance_ReturnsFalseWithoutDate()
        {
            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetPrimeraByPersona(123))
                .Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = _service.ObtenerAceptacionReglamentoEstudiantil(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.False(result.Data!.AceptoReglamentoEstudiantil);
            Assert.Null(result.Data.FechaAceptacion);
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

            var result = _service.ObtenerAceptacionReglamentoEstudiantil(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data!.AceptoReglamentoEstudiantil);
            Assert.Equal(primeraFecha, result.Data.FechaAceptacion);
            aceptacionRepo.Verify(r => r.GetPrimeraByPersona(123), Times.Once);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenReglamentoNotAccepted_ReturnsBadRequest()
        {
            var result = await _service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = false,
                IdOfertaSeleccionada = 10
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
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

            var result = await _service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_07", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenDocumentoFrenteMissing_ReturnsNotFound()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaDefinitiva(123));

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

            var result = await _service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
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
                  "idInscripcion": 77,
                  "seniaInscripcion": 2500,
                  "fechaVencimientoPago": "2026-07-01T00:00:00",
                  "resumen": {
                    "idProducto": 20,
                    "carrera": "Analista Programador",
                    "idComienzo": 40,
                    "comienzo": "Marzo 2026",
                    "idTurno": 1,
                    "turno": "Nocturno"
                  }
                }
                """,
                """
                {
                  "saldoActual": 3210.50,
                  "saldoVencido": 100,
                  "saldoAVencer": 200,
                  "movimientos": [
                    {
                      "fecha": "2026-07-01T00:00:00",
                      "concepto": "Inscripcion",
                      "debe": 3210.50,
                      "haber": 0,
                      "saldo": 3210.50
                    }
                  ]
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaDefinitiva(123));
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

            var result = await service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.Equal(77, result.Data.IdInscripcion);
            Assert.Equal(2500, result.Data.SeniaInscripcion);
            Assert.Equal("Analista Programador", result.Data.Resumen.Carrera);
            Assert.NotNull(result.Data.EstadoCuenta);
            Assert.Equal(3210.50m, result.Data.EstadoCuenta!.SaldoActual);
            Assert.NotNull(aceptacionAgregada);
            Assert.Equal(999, aceptacionAgregada!.IdAceptacionReglamentoEst);
            Assert.Equal(2, handler.Requests.Count);
            var requestApi = handler.Requests[0];
            Assert.Contains("tipoInscripcion=ONLINE", requestApi.RequestUri);
            Assert.Contains("idProducto=20", requestApi.RequestUri);
            Assert.Contains("idProceso=30", requestApi.RequestUri);
            Assert.Contains("idOfertaSeleccionada=10", requestApi.RequestUri);
            Assert.Contains("\"idTurno\":1", requestApi.Body);
            var requestEstadoCuenta = handler.Requests[1];
            Assert.Equal(HttpMethod.Get, requestEstadoCuenta.Method);
            Assert.Contains("Pagos/CtaCte", requestEstadoCuenta.RequestUri);
            Assert.Contains("estado=SALDO_ACTUAL_Y_MOVIMIENTOS", requestEstadoCuenta.RequestUri);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithDefinitiveDocuments_Confirms()
        {
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "idInscripcion": 78
                }
                """);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaDefinitiva(123));
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

            var result = await service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.Equal(78, result.Data.IdInscripcion);
            Assert.NotNull(result.Data.EstadoCuenta);
            Assert.Equal(3210.50m, result.Data.EstadoCuenta!.SaldoActual);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenEstadoCuentaFails_ReturnsConfirmationWithoutEstadoCuenta()
        {
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "idInscripcion": 79
                }
                """,
                "error",
                HttpStatusCode.InternalServerError);
            var service = CrearServiceConApi(handler);

            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaDefinitiva(123));
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

            var result = await service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.Equal(79, result.Data.IdInscripcion);
            Assert.Null(result.Data.EstadoCuenta);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Contains("ConfirmarPreInscripcion", handler.Requests[0].RequestUri);
            Assert.Contains("Pagos/CtaCte", handler.Requests[1].RequestUri);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithEncuestaIniHistorica_ConfirmsWithoutDefinitiveEncuestaAdmision()
        {
            AceptacionReglamentoEst? aceptacionAgregada = null;
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "success": true,
                  "seniaInscripcion": 1500
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

            var result = await service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.Equal(20, result.Data.Resumen.IdProducto);
            Assert.Equal(40, result.Data.Resumen.IdComienzo);
            Assert.Equal("Analista en TI", result.Data.Resumen.Carrera);
            Assert.Equal("Marzo 2026", result.Data.Resumen.Comienzo);
            Assert.NotNull(aceptacionAgregada);
            Assert.Equal(20, aceptacionAgregada!.IdProducto);
            Assert.Equal(40, aceptacionAgregada.IdComienzo);
            Assert.NotNull(result.Data.EstadoCuenta);
            Assert.Equal(3210.50m, result.Data.EstadoCuenta!.SaldoActual);

            Assert.Equal(2, handler.Requests.Count);
            var requestApi = handler.Requests[0];
            Assert.Contains("idProducto=20", requestApi.RequestUri);
            Assert.Contains("idProceso=30", requestApi.RequestUri);
            Assert.Contains("idOfertaSeleccionada=10", requestApi.RequestUri);
            Assert.Contains("tipoInscripcion=ONLINE", requestApi.RequestUri);
            Assert.Contains("\"idTurno\":1", requestApi.Body);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenInteresOfertaMissing_ReturnsConflict()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupEncuesta(123, EncuestaDefinitiva(123));

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo
                .Setup(r => r.GetProcesoPorInteresActivoOferta(123, 20, 10))
                .Returns((Proceso)null);
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var result = await _service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_14", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WhenOfertaNoCoincideConEncuesta_ReturnsConflict()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 21, 40, 1);
            SetupEncuesta(123, EncuestaDefinitiva(123));

            var result = await _service.ConfirmarPreInscripcion(123, new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_15", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
        }

        [Fact]
        public void RegistrarInteresProducto_CreaInteresNuevoYPersistenciaRelacionada()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
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
            var encuesta = new EncuestaIniAdmision { IdEncuestaIni = 77, CodigoPersona = 123 };
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(encuesta);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(false);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_3100))
                .Returns(900)
                .Returns(901);

            var fechaAntes = DateTime.Now;
            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });
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
            Assert.Equal(20, encuesta.IdProceso);
            Assert.Equal(40, encuesta.IdComienzo);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaInteresXSeleccionEnSitio(
                _uowMock.Object,
                It.Is<TivenosAltaInteresRequest>(r =>
                    r.CodigoPersona == 123 &&
                    r.IdProducto == 10 &&
                    r.IdProceso == 20 &&
                    r.Operacion.TipoProcesoLlamador == "Alta" &&
                    r.Operacion.Disparador == "AltaInteresProducto" &&
                    r.Operacion.OrigenLlamador == null),
                It.IsAny<int>(),
                nameof(InscripcionesService.RegistrarInteresProducto)), Times.Once);
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

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(true);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);

            _tivenosEnvioServiceMock
                .Setup(s => s.EncolarAltaInteresXSeleccionEnSitio(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<TivenosAltaInteresRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Throws(new InvalidOperationException("No se pudo encolar Tivenos."));

            Assert.Throws<InvalidOperationException>(() =>
                _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 }));

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

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("GEN_IP_10", result.ErrorCode);
            _uowMock.Verify(u => u.Rollback(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public void RegistrarInteresProducto_ConInteresExistente_ReseteaYActualiza()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
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

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(false);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_3100))
                .Returns(900)
                .Returns(901);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.True(result.Success);
            Assert.Equal(4m, interesProductoProducto10.IdGradoInteres);
            Assert.Equal(0m, interesProductoProducto10.IdGradoInteresAnt);
            Assert.Equal(0m, interesProductoProducto11.IdGradoInteres);
            Assert.Equal(4m, interesProductoProducto11.IdGradoInteresAnt);
            Assert.Equal(5m, interesProductoProducto12.IdGradoInteres);
            interesProductoRepo.Verify(r => r.Update(It.IsAny<InteresProducto>()), Times.AtLeast(2));
            interesProductoRepo.Verify(r => r.Update(interesProductoProducto12), Times.Never);
            interesProductoOfertaRepo.Verify(r => r.Add(It.IsAny<InteresProductoOferta>()), Times.Never);
            intereRepo.Verify(r => r.Add(It.IsAny<Intere>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaInteresXSeleccionEnSitio(
                _uowMock.Object,
                It.Is<TivenosAltaInteresRequest>(r =>
                    r.CodigoPersona == 123 &&
                    r.IdProducto == 10 &&
                    r.IdProceso == 20 &&
                    r.Operacion.TipoProcesoLlamador == "Modificar" &&
                    r.Operacion.Disparador == "ActualizarInteres" &&
                    r.Operacion.OrigenLlamador == null),
                It.IsAny<int>(),
                nameof(InscripcionesService.RegistrarInteresProducto)), Times.Once);
        }

        [Fact]
        public void RegistrarInteresProducto_ConAccionExistente_NoDuplicaActividadNiAccion()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
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

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(true);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);

            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.True(result.Success);
            actividadRepo.Verify(r => r.Add(It.IsAny<Actividad>()), Times.Never);
            accionRepo.Verify(r => r.Add(It.IsAny<Accion>()), Times.Never);
        }

        [Fact]
        public void RegistrarInteresProducto_ConInscripcionPrevia_DevuelveConflict()
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.ExistePersona(123)).Returns(true);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.EsProductoValidoParaInteres(10)).Returns(true);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.TieneProcesoHabilitadoPorProducto(10, 20)).Returns(true);
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.TieneInscripcionPreviaAProducto(123, 10)).Returns(true);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

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

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

            Assert.False(result.Success);
            Assert.Equal(409, result.HttpCode);
            Assert.Equal("GEN_IP_05", result.ErrorCode);
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

            var result = _service.GuardarEncuestaInicial(123, new GuardarEncuestaInicialRequest());

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal(900, encuestaAgregada!.IdEncuestaIni);
            Assert.Equal("TEMPORAL", encuestaAgregada.EstadoEncuestaIniAdmision);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
            _uowMock.Verify(u => u.Personas.Update(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_ParcialSgiConTrabajaActualmente_ActualizaPersonaNormalizado()
        {
            var persona = new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "DE",
                Documento = "123",
                TipoPersona = "SGI"
            };
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
            _dbConnectionContextMock
                .Setup(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION))
                .Returns(900);

            var result = _service.GuardarEncuestaInicial(123, new GuardarEncuestaInicialRequest
            {
                TrabajaActualmente = true
            });

            Assert.True(result.Success);
            personaRepo.Verify(r => r.Update(It.Is<Persona>(p => p.CodigoPersona == 123 && p.TrabajaActualmente == "S")), Times.Once);
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

            var result = _service.GuardarEncuestaInicial(123, new GuardarEncuestaInicialRequest
            {
                OpcionesMotivosSeleccionados = []
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

            var result = _service.GuardarEncuestaInicial(123, new GuardarEncuestaInicialRequest());

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.GetByKey(It.IsAny<long>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<TivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarModificacionDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<TivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaSinBachillerato_InsertaYEncolaAlta()
        {
            SetupEncuestaDefinitivaParaGuardar(null, out var bachilleratoRepo);

            var result = _service.GuardarEncuestaInicial(123, RequestEncuestaDefinitiva(ultimoAnioSexto: 5, codigoTitulo: null));

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.Add(It.Is<BachilleratoPersona>(b =>
                b.CodigoPersona == 123 &&
                b.CodigoInstitucion == 50 &&
                b.AnioBachillerPer == "5" &&
                b.CodigoOrientacion == 1304 &&
                b.ActualizacionBachillerPer == FechaBase)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaDatosBachillerato(
                _uowMock.Object,
                It.Is<TivenosBachilleratoRequest>(r => r.CodigoPersona == 123 && r.CodigoOrientacion == 1304),
                777,
                nameof(InscripcionesService.GuardarEncuestaInicial)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarModificacionDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<TivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaSgiSinTrabajaActualmente_QuedaTemporal()
        {
            SetupEncuestaDefinitivaParaGuardar(null, out var bachilleratoRepo);

            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = "DE",
                Documento = "123",
                TipoPersona = "SGI"
            });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);

            EncuestaIniAdmision? encuestaAgregada = null;
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            encuestaRepo.Setup(r => r.GetByPersonaProductoComienzo(123, 10, 30)).Returns((EncuestaIniAdmision)null);
            encuestaRepo.Setup(r => r.Add(It.IsAny<EncuestaIniAdmision>()))
                .Callback<EncuestaIniAdmision>(e => encuestaAgregada = e);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.GuardarEncuestaInicial(123, RequestEncuestaDefinitiva());

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal("TEMPORAL", encuestaAgregada!.EstadoEncuestaIniAdmision);
            bachilleratoRepo.Verify(r => r.GetByKey(It.IsAny<long>()), Times.Never);
            personaRepo.Verify(r => r.Update(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaConBachilleratoDistinto_ActualizaYEncolaModificacion()
        {
            var existente = new BachilleratoPersona
            {
                CodigoPersona = 123,
                CodigoInstitucion = 40,
                AnioBachillerPer = "5",
                CodigoOrientacion = 1304,
                UsuarioIngreso = string.Empty,
                FechaIngreso = FechaBase.AddDays(-1)
            };
            SetupEncuestaDefinitivaParaGuardar(existente, out var bachilleratoRepo);

            var result = _service.GuardarEncuestaInicial(123, RequestEncuestaDefinitiva());

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.Update(It.Is<BachilleratoPersona>(b =>
                b.CodigoPersona == 123 &&
                b.CodigoInstitucion == 50 &&
                b.AnioBachillerPer == "6" &&
                b.CodigoOrientacion == 1300 &&
                b.ActualizacionBachillerPer == FechaBase)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarModificacionDatosBachillerato(
                _uowMock.Object,
                It.Is<TivenosBachilleratoRequest>(r => r.CodigoPersona == 123 && r.CodigoOrientacion == 1300),
                777,
                nameof(InscripcionesService.GuardarEncuestaInicial)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<TivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaConBachilleratoIgual_NoDuplicaEnvio()
        {
            var existente = new BachilleratoPersona
            {
                CodigoPersona = 123,
                CodigoInstitucion = 50,
                AnioBachillerPer = "6",
                CodigoOrientacion = 1300,
                UsuarioIngreso = string.Empty,
                FechaIngreso = FechaBase.AddDays(-1)
            };
            SetupEncuestaDefinitivaParaGuardar(existente, out var bachilleratoRepo);

            var result = _service.GuardarEncuestaInicial(123, RequestEncuestaDefinitiva());

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.Update(It.IsAny<BachilleratoPersona>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<TivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarModificacionDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<TivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
        }

        private void SetupOfertaConfirmacion(
            long idOferta,
            long idProducto,
            long idComienzo,
            long idTurno,
            string nombreExtenso = "Analista Programador",
            string nombre = "AP",
            string nombreComienzo = "Marzo 2026")
        {
            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo
                .Setup(r => r.GetByKeyWithRelated(idOferta))
                .Returns(OfertaValida(idOferta, idProducto, idComienzo, idTurno, nombreExtenso, nombre, nombreComienzo));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);
        }

        private void SetupOfertaParaRegistro(long idOferta, long idProducto, long idComienzo, long idTurno = 1)
        {
            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo
                .Setup(r => r.GetByKeyWithRelated(idOferta))
                .Returns(OfertaValida(idOferta, idProducto, idComienzo, idTurno));
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);
        }

        private void SetupOfertaValidaParaRegistro(
            long idOferta = 30,
            long idProducto = 10,
            long idProceso = 20,
            long idComienzo = 40,
            long idTurno = 1)
        {
            SetupOfertaParaRegistro(idOferta, idProducto, idComienzo, idTurno);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo
                .Setup(r => r.GetByKeyWithRelated(idProceso, idComienzo))
                .Returns(new ProcesoComienzo { IdProceso = idProceso, IdComienzo = idComienzo });
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);
        }

        private void SetupInteresActivoOferta(long codigoPersona, long idProducto, long idOferta, long idProceso)
        {
            var interesProductoOfertaRepo = new Mock<IInteresProductoOfertaRepository>();
            interesProductoOfertaRepo
                .Setup(r => r.GetProcesoPorInteresActivoOferta(codigoPersona, idProducto, idOferta))
                .Returns(new Proceso { IdProceso = idProceso, HabilitadoInteresSitio = "SI" });
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesProductoOfertaRepo.Object);
        }

        private void SetupEncuestaDefinitivaParaGuardar(
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

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetComienzoActivoPorProcesoOProducto(10, 20)).Returns(30);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var procesoRepo = new Mock<IProcesoRepository>();
            procesoRepo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns(new List<Proceso>
            {
                new() { IdProceso = 20 }
            });
            _uowMock.Setup(u => u.Procesos).Returns(procesoRepo.Object);

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
                AnioBachiller(4),
                AnioBachiller(5),
                AnioBachiller(6)
            });
            anioRepo.Setup(r => r.GetByKey(6)).Returns(AnioBachiller(6));
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var motivoOpcionesRepo = new Mock<BusinessLogic.IDevartRepositories.IMotivoOpcionesAdmisionRepository>();
            motivoOpcionesRepo.Setup(r => r.GetByKey(1)).Returns(new MotivoOpcionesAdmision { IdMotivo = 1 });
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

        private static GuardarEncuestaInicialRequest RequestEncuestaDefinitiva(
            long ultimoAnioSexto = 6,
            long? codigoTitulo = 1300)
        {
            return new GuardarEncuestaInicialRequest
            {
                IdProducto = 10,
                IdProceso = 20,
                CodigoTitulo = codigoTitulo,
                UltimoAnioSexto = ultimoAnioSexto,
                InstruccionPadre = 1,
                InstruccionMadre = 1,
                DecisionCarrera = 2,
                DecisionUniversidad = 2,
                InfoOtrasUniversidadesAntes = "NO",
                CompartidoCon = 1,
                CodigoInstitucionBac = 50,
                InformarEncuesta = "NO",
                UltimoAnioSecundaria = 1,
                TieneEducacionSuperior = false,
                NivelDecision = 1,
                AsesoramientoOrt = false,
                VistaSitioWebOrt = false,
                VistaInstalacionesOrt = false,
                PublicidadOrt = false,
                OpcionesMotivosSeleccionados =
                [
                    new EncuestaMotivoRequest { IdMotivo = 1 }
                ]
            };
        }

        private static AnioBachiller AnioBachiller(long cantAnios)
        {
            return new AnioBachiller
            {
                IdAnioBachiller = cantAnios,
                CantAniosAnioBachiller = cantAnios,
                NombreAnioBachiller = cantAnios.ToString(),
                UsuarioIngreso = string.Empty,
                FechaIngreso = FechaBase,
                HoraIngreso = "10:30:00"
            };
        }

        private static Oferta OfertaValida(
            long idOferta,
            long idProducto,
            long idComienzo,
            long idTurno,
            string nombreExtenso = "Analista Programador",
            string nombre = "AP",
            string nombreComienzo = "Marzo 2026")
        {
            return new Oferta
            {
                IdOferta = idOferta,
                IdTurno = idTurno,
                InscripcionesAbiertasOferta = "SI",
                Turno = new Turno { IdTurno = idTurno, NombreTurno = "Nocturno" },
                Supraoferta = new Supraoferta
                {
                    IdComienzo = idComienzo,
                    EstadoSupraoferta = "D",
                    Comienzo = new Comienzo { IdComienzo = idComienzo, NombreComienzo = nombreComienzo },
                    Paquete = new Paquete
                    {
                        IdProducto = idProducto,
                        Producto = new Producto
                        {
                            IdProducto = idProducto,
                            NombreProducto = nombre,
                            NombreExtensoProducto = nombreExtenso
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

        private void SetupPersona(Persona? persona)
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(123)).Returns(persona);
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
        }

        private void SetupPersona(long codigoPersona)
        {
            var personaRepo = new Mock<IPersonaRepository>();
            personaRepo.Setup(r => r.GetByKey(codigoPersona)).Returns(new Persona
            {
                CodigoPersona = codigoPersona,
                FechaVtoDocumentoPersona = DateTime.Today.AddYears(1)
            });
            _uowMock.Setup(u => u.Personas).Returns(personaRepo.Object);
        }

        private void SetupEncuesta(long codigoPersona, EncuestaIniAdmision encuesta)
        {
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(codigoPersona)).Returns(encuesta);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
        }

        private void SetupEncuestaIni(long codigoPersona, EncuestaIni encuesta)
        {
            var encuestaRepo = new Mock<IEncuestaIniRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(codigoPersona)).Returns(encuesta);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaRepo.Object);
        }

        private void SetupDocumentosValidos(long codigoPersona)
        {
            var imagenRepo = new Mock<IImagenTemporalRepository>();
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(codigoPersona, 1))
                .Returns(new ImagenTemporal
                {
                    CodigoPersona = codigoPersona,
                    TipoImagen = "1",
                    BlobImagen = [1],
                    FechaVtoDocumentoPersona = DateTime.Today.AddYears(1)
                });
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(codigoPersona, 2))
                .Returns(new ImagenTemporal
                {
                    CodigoPersona = codigoPersona,
                    TipoImagen = "1",
                    BlobImagen = [1],
                    FechaVtoDocumentoPersona = DateTime.Today.AddYears(1)
                });
            _uowMock.Setup(u => u.ImagenTemporals).Returns(imagenRepo.Object);
        }

        private void SetupDocumentosDefinitivosValidos(long codigoPersona)
        {
            var temporalRepo = new Mock<IImagenTemporalRepository>();
            temporalRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(codigoPersona, It.IsAny<int>()))
                .Returns((ImagenTemporal)null);
            _uowMock.Setup(u => u.ImagenTemporals).Returns(temporalRepo.Object);

            var imagenRepo = new Mock<IImagenRepository>();
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(codigoPersona, 1))
                .Returns(new Imagen
                {
                    CodigoPersona = codigoPersona,
                    TipoImagen = "1",
                    BlobImagen = [1]
                });
            imagenRepo
                .Setup(r => r.GetDocumentoByPersonaAndTipo(codigoPersona, 2))
                .Returns(new Imagen
                {
                    CodigoPersona = codigoPersona,
                    TipoImagen = "1",
                    BlobImagen = [1]
                });
            _uowMock.Setup(u => u.Imagens).Returns(imagenRepo.Object);
        }

        private static EncuestaIniAdmision EncuestaDefinitiva(long codigoPersona)
        {
            return new EncuestaIniAdmision
            {
                CodigoPersona = codigoPersona,
                EstadoEncuestaIniAdmision = "DEFINITIVO",
                IdProducto = 20,
                IdProceso = 30,
                IdComienzo = 40,
                FechaVtoAdmision = DateTime.Today.AddDays(10)
            };
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

        private InscripcionesService CrearServiceConApi(HttpMessageHandler handler)
        {
            var apiClient = new InscripcionesyPagosApiClient(
                new HttpClient(handler) { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<InscripcionesyPagosApiClient>.Instance);

            return new InscripcionesService(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _generalServiceMock.Object,
                _tivenosEnvioServiceMock.Object,
                apiClient);
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
            bool existeEncuestaIni = false,
            bool existeEncuestaCompleta = false)
        {
            var frescoRepo = new Mock<IVdEsFrescoAdmisionRepository>();
            frescoRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(existeFresco);
            _uowMock.Setup(u => u.VdEsFrescoAdmisions).Returns(frescoRepo.Object);

            var encuestaIniRepo = new Mock<IEncuestaIniRepository>();
            encuestaIniRepo.Setup(r => r.ExistePorDocumento("DE", "123")).Returns(existeEncuestaIni);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaIniRepo.Object);

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.ExisteCompletaPorDocumento("DE", "123")).Returns(existeEncuestaCompleta);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);
        }

        private void SetupFresco(string? estado, decimal idProducto = 10m, decimal idProceso = 20m, decimal idInscripto = 0m)
        {
            var fresco1y2Repo = new Mock<IVdInscripcionesFresco1y2Repository>();
            fresco1y2Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>()))
                .Returns(estado == null
                    ? new List<VdInscripcionesFresco1y2>()
                    : new List<VdInscripcionesFresco1y2>
                    {
                        new() { IdProducto = idProducto, IdProceso = idProceso, IdInscripto = idInscripto, EstadoInscripcion = estado }
                    });
            _uowMock.Setup(u => u.VdInscripcionesFresco1y2s).Returns(fresco1y2Repo.Object);

            var fresco3y4Repo = new Mock<IVdInscripcionesFresco3y4Repository>();
            fresco3y4Repo
                .Setup(r => r.GetInscripcionesFrescoHabilitadas(It.IsAny<long>()))
                .Returns(new List<VdInscripcionesFresco3y4>());
            _uowMock.Setup(u => u.VdInscripcionesFresco3y4s).Returns(fresco3y4Repo.Object);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenEnProceso_ReturnsOfertaSeleccionada()
        {
            SetupFresco(global::AppLogic.Constants.InscripcionesConstants.EstadoInscripcion.EnProceso);
            var oferta = new Oferta
            {
                IdOferta = 99,
                IdTurno = 5,
                Turno = new Turno { IdTurno = 5, NombreTurno = "Matutino" },
                Supraoferta = new Supraoferta
                {
                    Comienzo = new Comienzo { IdComienzo = 7, NombreComienzo = "Marzo 2026" },
                    Paquete = new Paquete
                    {
                        Producto = new Producto { IdProducto = 10, NombreWebProducto = "Licenciatura en Diseño Gráfico" }
                    }
                }
            };
            var interesRepo = new Mock<IInteresProductoOfertaRepository>();
            interesRepo.Setup(r => r.GetOfertaSeleccionada(123, 10, 20)).Returns(oferta);
            _uowMock.Setup(u => u.InteresProductoOfertas).Returns(interesRepo.Object);

            var result = await _service.ObtenerDetalleInscripcion(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("En proceso", result.Data!.Estado);
            Assert.NotNull(result.Data.Detalle);
            Assert.Equal(99, result.Data.Detalle!.IdOferta);
            Assert.Equal(10, result.Data.Detalle!.IdProducto);
            Assert.Equal("Licenciatura en Diseño Gráfico", result.Data.Detalle.Carrera);
            Assert.Equal("Marzo 2026", result.Data.Detalle.Comienzo);
            Assert.Equal("Matutino", result.Data.Detalle.Turno);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenALaEspera_ReturnsEstadoSinDetalle()
        {
            SetupFresco(global::AppLogic.Constants.InscripcionesConstants.EstadoInscripcion.ALaEspera);

            var result = await _service.ObtenerDetalleInscripcion(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("A la espera", result.Data!.Estado);
            Assert.Null(result.Data.Detalle);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenPagoPendiente_ReturnsDetallePago()
        {
            SetupFresco(global::AppLogic.Constants.InscripcionesConstants.EstadoInscripcion.PagoPendiente, idInscripto: 555m);

            var inscripto = new Inscripto
            {
                IdInscripto = 555,
                CodigoPersona = 123,
                IdOferta = 99,
                FechaVtoInscr = new DateTime(2026, 7, 1),
                Oferta = new Oferta
                {
                    IdOferta = 99,
                    IdTurno = 5,
                    Turno = new Turno { IdTurno = 5, NombreTurno = "Matutino" },
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 7, NombreComienzo = "Marzo 2026" },
                        Paquete = new Paquete { Producto = new Producto { IdProducto = 10, NombreWebProducto = "Analista programador" } }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(inscripto);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var service = CrearServiceConApi(new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
                """
                { "seniaMinima": 1500.50 }
                """)));

            var result = await service.ObtenerDetalleInscripcion(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("Pago pendiente", result.Data!.Estado);
            Assert.NotNull(result.Data.PagoPendiente);
            Assert.Equal(555, result.Data.PagoPendiente!.IdInscripcion);
            Assert.Equal(1500.50m, result.Data.PagoPendiente.Senia);
            Assert.Equal(new DateTime(2026, 7, 1), result.Data.PagoPendiente.FechaVencimientoPago);
            Assert.Equal("Analista programador", result.Data.PagoPendiente.Resumen.Carrera);
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenConfirmada_ReturnsDatosYMaterias()
        {
            SetupFresco(global::AppLogic.Constants.InscripcionesConstants.EstadoInscripcion.Confirmada, idInscripto: 555m);

            var inscripto = new Inscripto
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
                                NombreWebProducto = "Licenciatura en Diseño Gráfico",
                                NombreCoordAcadProducto = "María Rodríguez",
                                EmailCoordAcadProducto = "maria.rodriguez@ort.edu.uy"
                            }
                        }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(inscripto);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var ofertaRepo = new Mock<IOfertaRepository>();
            ofertaRepo.Setup(r => r.GetMateriasPorOferta(99)).Returns(new List<Materia>
            {
                new() { IdMateria = 1, NombreMateria = "Arte y estética I" },
                new() { IdMateria = 2, NombreMateria = "Fotografía y edición de video" }
            });
            _uowMock.Setup(u => u.Ofertas).Returns(ofertaRepo.Object);

            var result = await _service.ObtenerDetalleInscripcion(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("Confirmada", result.Data!.Estado);
            Assert.NotNull(result.Data.Confirmada);
            Assert.Equal(123, result.Data.Confirmada!.NumeroEstudiante);
            Assert.Equal("Licenciatura en Diseño Gráfico", result.Data.Confirmada.Resumen.Carrera);
            Assert.Equal("María Rodríguez", result.Data.Confirmada.CoordinadorAcademico!.Nombre);
            Assert.Equal(2, result.Data.Confirmada.MateriasPrimerSemestre.Count);
            Assert.Contains(result.Data.Confirmada.MateriasPrimerSemestre, m => m.Nombre == "Arte y estética I");
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenNoInscripcion_ReturnsNotFound()
        {
            SetupFresco(null);

            var result = await _service.ObtenerDetalleInscripcion(123, 10, 20);

            Assert.False(result.Success);
            Assert.Equal("INS_DET_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }
    }
}
