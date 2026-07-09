using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Dtos.Inscripciones;
using AppLogic.Dtos.Tivenos;
using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Inscripciones;
using AppLogic.IServices.Tivenos;
using AppLogic.Services.Inscripciones;
using AppLogic.Services.Inscripciones.Encuesta;
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
                .Setup(s => s.CalcularFechaVencimientoAdmisiones(It.IsAny<IUnitOfWork>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(global::Utilities.OperationResult<DateTime>.Ok(FechaBase.AddDays(5), nameof(IGeneralService.CalcularFechaVencimientoAdmisiones)));
            var encuestaIniRepo = new Mock<IEncuestaIniRepository>();
            encuestaIniRepo
                .Setup(r => r.GetByPersona(It.IsAny<long>()))
                .Returns((EncuestaIni)null);
            _uowMock.Setup(u => u.EncuestaInis).Returns(encuestaIniRepo.Object);
            _tivenosEnvioServiceMock
                .Setup(s => s.EncolarAltaInteresXSeleccionEnSitio(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosAltaInteresRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Returns(global::Utilities.OperationResult<bool>.Ok(true, nameof(ITivenosEnvioService.EncolarAltaInteresXSeleccionEnSitio)));
            _tivenosEnvioServiceMock
                .Setup(s => s.EncolarAltaDatosBachillerato(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosBachilleratoRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Returns(global::Utilities.OperationResult<bool>.Ok(true, nameof(ITivenosEnvioService.EncolarAltaDatosBachillerato)));
            _tivenosEnvioServiceMock
                .Setup(s => s.EncolarModificacionDatosBachillerato(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosBachilleratoRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Returns(global::Utilities.OperationResult<bool>.Ok(true, nameof(ITivenosEnvioService.EncolarModificacionDatosBachillerato)));
            var apiClient = new InscripcionesyPagosApiClient(
                new HttpClient { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<InscripcionesyPagosApiClient>.Instance);
            _service = new InscripcionesService(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _tivenosEnvioServiceMock.Object,
                apiClient,
                CrearEncuestaInicialServiceReal());
        }

        // Construye la implementación real de EncuestaInicialService con los mismos mocks,
        // para preservar la cobertura profunda de encuesta que ya ejercitan estos tests
        // (ahora vía la dependencia inyectada en lugar del antiguo `new` interno).
        private EncuestaInicialService CrearEncuestaInicialServiceReal() =>
            new(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _generalServiceMock.Object,
                _tivenosEnvioServiceMock.Object);

        private static readonly DateTime FechaBase = new(2026, 5, 27, 10, 30, 0);

        [Fact]
        public void ObtenerEncuestaInicial_DelegaEnEncuestaInicialService()
        {
            var encuestaMock = new Mock<IEncuestaInicialService>();
            var esperado = global::Utilities.OperationResult<DtoObtenerEncuestaInicialResponse>.Ok(
                new DtoObtenerEncuestaInicialResponse { TieneDerechoEncuesta = true },
                nameof(IEncuestaInicialService.ObtenerEncuestaInicial));
            encuestaMock.Setup(s => s.ObtenerEncuestaInicial(123)).Returns(esperado);
            var service = CrearServiceConEncuesta(encuestaMock.Object);

            var result = service.ObtenerEncuestaInicial(123);

            Assert.Same(esperado, result);
            encuestaMock.Verify(s => s.ObtenerEncuestaInicial(123), Times.Once);
        }

        [Fact]
        public void GuardarEncuestaInicial_DelegaEnEncuestaInicialService()
        {
            var encuestaMock = new Mock<IEncuestaInicialService>();
            var request = new DtoGuardarEncuestaInicialRequest();
            var esperado = global::Utilities.OperationResult<DtoGuardarEncuestaInicialResponse>.Ok(
                new DtoGuardarEncuestaInicialResponse(),
                nameof(IEncuestaInicialService.GuardarEncuestaInicial));
            encuestaMock.Setup(s => s.GuardarEncuestaInicial(123, request)).Returns(esperado);
            var service = CrearServiceConEncuesta(encuestaMock.Object);

            var result = service.GuardarEncuestaInicial(123, request);

            Assert.Same(esperado, result);
            encuestaMock.Verify(s => s.GuardarEncuestaInicial(123, request), Times.Once);
        }

        private InscripcionesService CrearServiceConEncuesta(IEncuestaInicialService encuestaInicialService)
        {
            var apiClient = new InscripcionesyPagosApiClient(
                new HttpClient { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<InscripcionesyPagosApiClient>.Instance);
            return new InscripcionesService(
                _uowFactoryMock.Object,
                _dbConnectionContextMock.Object,
                _tivenosEnvioServiceMock.Object,
                apiClient,
                encuestaInicialService);
        }

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
        public async Task ConfirmarPreInscripcion_WhenReglamentoNotAcceptedAndNoPriorAcceptance_ReturnsBadRequest()
        {
            SetupPersona(123);
            SetupOfertaConfirmacion(10, 20, 40, 1);
            SetupInteresActivoOferta(123, 20, 10, 30);
            SetupEncuesta(123, EncuestaDefinitiva(123));
            SetupDocumentosValidos(123);

            var aceptacionRepo = new Mock<IAceptacionReglamentoEstRepository>();
            aceptacionRepo
                .Setup(r => r.GetByPersonaProductoComienzo(123, 20, 40))
                .Returns((AceptacionReglamentoEst)null);
            aceptacionRepo
                .Setup(r => r.GetByPersona(123))
                .Returns((AceptacionReglamentoEst)null);
            _uowMock.Setup(u => u.AceptacionReglamentoEsts).Returns(aceptacionRepo.Object);

            var result = await _service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
            {
                AceptoReglamento = false,
                IdOfertaSeleccionada = 10
            });

            Assert.False(result.Success);
            Assert.Equal("INS_CPI_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
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
            SetupEncuesta(123, EncuestaDefinitiva(123));
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

            var result = await service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
            {
                AceptoReglamento = false,
                IdOfertaSeleccionada = 10
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.NotNull(aceptacionAgregada);
            Assert.Equal(1000, aceptacionAgregada!.IdAceptacionReglamentoEst);
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

            var result = await _service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
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

            var result = await _service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
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
                  "fechaVencimientoPago": "2026-07-01T00:00:00",
                  "carritos": [
                    {
                      "idCarrito": "123|20|1|40|77",
                      "senia": 2500
                    }
                  ],
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
                  }
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

            var result = await service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.Equal(77, result.Data.IdInscripcion);
            Assert.Equal(2500, result.Data.Senia);
            Assert.Equal(10, result.Data.Resumen.IdOferta);
            Assert.Equal("Analista Programador", result.Data.Resumen.Carrera);
            Assert.NotNull(result.Data.EstadoCuenta);
            Assert.Equal(3210.50m, result.Data.EstadoCuenta!.SaldoActual);
            Assert.NotNull(aceptacionAgregada);
            Assert.Equal(999, aceptacionAgregada!.IdAceptacionReglamentoEst);
            var requestApi = Assert.Single(handler.Requests);
            Assert.Contains("tipoInscripcion=ONLINE", requestApi.RequestUri);
            Assert.Contains("idProducto=20", requestApi.RequestUri);
            Assert.Contains("idProceso=30", requestApi.RequestUri);
            Assert.Contains("idOfertaSeleccionada=10", requestApi.RequestUri);
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
                  "idInscripcion": 78,
                  "estadoCuenta": {
                    "saldoActual": 3210.50
                  }
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

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(78)).Returns(new Inscripto { IdInscripto = 78 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
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
        public async Task ConfirmarPreInscripcion_WhenEstadoCuentaMissing_ReturnsConfirmationWithoutEstadoCuenta()
        {
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "confirmada": true,
                  "idInscripcion": 79
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

            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetByKey(79)).Returns(new Inscripto { IdInscripto = 79 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            });

            Assert.True(result.Success);
            Assert.True(result.Data!.Confirmada);
            Assert.Equal(79, result.Data.IdInscripcion);
            Assert.Null(result.Data.EstadoCuenta);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("ConfirmarPreInscripcion", request.RequestUri);
            Assert.DoesNotContain("Pagos/CtaCte", request.RequestUri);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithEncuestaIniHistorica_ConfirmsWithoutDefinitiveEncuestaAdmision()
        {
            AceptacionReglamentoEst? aceptacionAgregada = null;
            var handler = ConfirmacionConEstadoCuentaHandler(
                """
                {
                  "success": true,
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

            var result = await service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
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

            var requestApi = Assert.Single(handler.Requests);
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

            var result = await _service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
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

            var result = await _service.ConfirmarPreInscripcion(123, new DtoConfirmarPreInscripcionRequest
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

            var fechaAntes = DateTime.Now;
            var result = _service.RegistrarInteresProducto(123, new DtoInteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });
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
                It.Is<DtoTivenosAltaInteresRequest>(r =>
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

            _tivenosEnvioServiceMock
                .Setup(s => s.EncolarAltaInteresXSeleccionEnSitio(
                    It.IsAny<IUnitOfWork>(),
                    It.IsAny<DtoTivenosAltaInteresRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Throws(new InvalidOperationException("No se pudo encolar Tivenos."));

            Assert.Throws<InvalidOperationException>(() =>
                _service.RegistrarInteresProducto(123, new DtoInteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 }));

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

            var result = _service.RegistrarInteresProducto(123, new DtoInteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

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

            var result = _service.RegistrarInteresProducto(123, new DtoInteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

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
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaInteresXSeleccionEnSitio(
                _uowMock.Object,
                It.Is<DtoTivenosAltaInteresRequest>(r =>
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

            var result = _service.RegistrarInteresProducto(123, new DtoInteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

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

            var result = _service.RegistrarInteresProducto(123, new DtoInteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 });

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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest());

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

            var result = _service.ObtenerEncuestaInicial(123);

            Assert.True(result.Success);
            Assert.True(result.Data!.TieneDerechoEncuesta);
            Assert.Null(result.Data.Encuesta);
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
                Documento = "123",
                TrabajaActualmente = "S",
                TipoJornada = 2
            });

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.ExisteCompletaPorDocumento("DE", "123")).Returns(false);
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision
            {
                IdEncuestaIni = 900,
                CodigoPersona = 123,
                IdProducto = 1981,
                IdProceso = 110,
                EstadoEncuestaIniAdmision = "DEFINITIVO",
                CursaSecundariaActualmenteEncuestaIni = "SI",
                VecesSextoEncuestaIni = "2",
                TieneEducacionSuperiorEncuestaIni = "SE",
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

            var result = _service.ObtenerEncuestaInicial(123);

            Assert.True(result.Success);
            var encuesta = result.Data!.Encuesta!;
            Assert.Equal(900, encuesta.IdEncuestaIni);
            Assert.Equal("DEFINITIVO", encuesta.Estado);
            Assert.Equal(1981, encuesta.CarreraId);
            Assert.Equal(110, encuesta.ProcesoId);
            Assert.True(encuesta.CursaSecundariaActualmente);
            Assert.True(encuesta.RecursaAnioBachillerato);
            Assert.Equal(2, encuesta.VecesRecursaAnioBachillerato);
            Assert.Equal(2, encuesta.EstadoEducacionSuperiorPreviaId);
            Assert.Equal(2, encuesta.ApoyoDecisionId);
            Assert.Equal([55], encuesta.UniversidadConsideradaIds);
            Assert.Equal([7], encuesta.MotivoEleccionOrtIds);
            Assert.Equal([8], encuesta.PublicidadOrtIds);
            Assert.True(encuesta.TrabajaActualmente);
            Assert.Equal(2, encuesta.TipoJornadaId);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_SinDerecho_CortaAntesDeValidarRequest()
        {
            SetupPersonaValida();
            SetupReposDerechoEncuesta(existeFresco: true);

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                NivelDecisionId = 99
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                EstadoEducacionSuperiorPreviaId = 1,
                UniversidadEducacionSuperiorIds = []
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                CarreraId = 10,
                CursaSecundariaActualmente = true,
                AnioBachillerato = 10
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                UbicacionUltimoAnioSecundariaId = 2,
                CursaSecundariaActualmente = true,
                AnioBachillerato = 12
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                SeInformoEnOtrasUniversidades = true,
                UniversidadConsideradaIds = [0],
                UniversidadConsideradaOtros = [" Otra universidad "],
                EstadoEducacionSuperiorPreviaId = 1,
                UniversidadEducacionSuperiorIds = [0],
                UniversidadEducacionSuperiorOtros = [" Otra superior "]
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                EstadoEducacionSuperiorPreviaId = 2,
                UniversidadEducacionSuperiorIds = [0],
                UniversidadEducacionSuperiorOtros = ["Otra superior"]
            });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("INS_EI_25", result.ErrorCode);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Never);
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                SeInformoEnOtrasUniversidades = true,
                UniversidadConsideradaIds = [55],
                UniversidadConsideradaOtros = ["Otra universidad"]
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                CursaSecundariaActualmente = true,
                AnioBachillerato = 6
            });

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal("12", encuestaAgregada!.UltimoAnioSextoEncuestaIni);
        }

        [Fact]
        public void GuardarEncuestaInicial_NoCursaSecundaria_LimpiaBachillerato()
        {
            SetupPersonaValida();

            var encuesta = new EncuestaIniAdmision
            {
                IdEncuestaIni = 10,
                CodigoPersona = 123,
                AniosInstruccionEncuestaIni = "12",
                UltimoAnioSextoEncuestaIni = "12",
                CodigoTitulo = 1300
            };
            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(encuesta);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                CursaSecundariaActualmente = false,
                AnioBachillerato = 12,
                OrientacionBachilleratoId = 1300
            });

            Assert.True(result.Success);
            Assert.Equal("NO", encuesta.CursaSecundariaActualmenteEncuestaIni);
            Assert.Null(encuesta.AniosInstruccionEncuestaIni);
            Assert.Null(encuesta.UltimoAnioSextoEncuestaIni);
            Assert.Null(encuesta.CodigoTitulo);
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                TrabajaActualmente = true,
                TipoJornadaId = 1
            });

            Assert.True(result.Success);
            personaRepo.As<IRepository<Persona>>()
                .Verify(r => r.Update(It.Is<Persona>(p => p.CodigoPersona == 123 && p.TrabajaActualmente == "S")), Times.Once);
        }

        [Fact]
        public void GuardarEncuestaInicial_ParcialSgiConTipoJornadaSinTrabajaActualmente_NoTocaPersona()
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
                .Returns(901);

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                TipoJornadaId = 2
            });

            Assert.True(result.Success);
            personaRepo.As<IRepository<Persona>>()
                .Verify(r => r.Update(It.IsAny<Persona>()), Times.Never);
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest
            {
                MotivoEleccionOrtIds = []
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

            var result = _service.GuardarEncuestaInicial(123, new DtoGuardarEncuestaInicialRequest());

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.GetByKey(It.IsAny<long>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarModificacionDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaSinBachillerato_InsertaYEncolaAlta()
        {
            SetupEncuestaDefinitivaParaGuardar(null, out var bachilleratoRepo);

            var result = _service.GuardarEncuestaInicial(123, RequestEncuestaDefinitiva(ultimoAnioSexto: 11, codigoTitulo: null));

            Assert.True(result.Success);
            bachilleratoRepo.Verify(r => r.Add(It.Is<BachilleratoPersona>(b =>
                b.CodigoPersona == 123 &&
                b.CodigoInstitucion == 50 &&
                b.AnioBachillerPer == "5" &&
                b.CodigoOrientacion == 1304 &&
                b.ActualizacionBachillerPer == FechaBase)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaDatosBachillerato(
                _uowMock.Object,
                It.Is<DtoTivenosBachilleratoRequest>(r => r.CodigoPersona == 123 && r.CodigoOrientacion == 1304),
                777,
                nameof(InscripcionesService.GuardarEncuestaInicial)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarModificacionDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaSinTrabajaActualmente_QuedaTemporal()
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

            var request = RequestEncuestaDefinitiva();
            request.TrabajaActualmente = null;

            var result = _service.GuardarEncuestaInicial(123, request);

            Assert.True(result.Success);
            Assert.NotNull(encuestaAgregada);
            Assert.Equal("TEMPORAL", encuestaAgregada!.EstadoEncuestaIniAdmision);
            Assert.Equal("TEMPORAL", result.Data!.Estado);
            Assert.Contains("trabajaActualmente", result.Data.CamposPendientes);
            bachilleratoRepo.Verify(r => r.GetByKey(123), Times.Never);
            personaRepo.Verify(r => r.Update(It.IsAny<Persona>()), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaSinCursaSecundaria_QuedaTemporal()
        {
            SetupEncuestaDefinitivaParaGuardar(null, out var bachilleratoRepo);

            var request = RequestEncuestaDefinitiva();
            request.CursaSecundariaActualmente = null;

            var result = _service.GuardarEncuestaInicial(123, request);

            Assert.True(result.Success);
            Assert.Equal("TEMPORAL", result.Data!.Estado);
            Assert.Contains("cursaSecundariaActualmente", result.Data.CamposPendientes);
            bachilleratoRepo.Verify(r => r.GetByKey(123), Times.Never);
        }

        [Fact]
        public void GuardarEncuestaInicial_DefinitivaNoCursaSecundaria_NoExigeBachillerato()
        {
            SetupEncuestaDefinitivaParaGuardar(null, out var bachilleratoRepo);

            var request = RequestEncuestaDefinitiva();
            request.CursaSecundariaActualmente = false;
            request.AnioBachillerato = null;
            request.OrientacionBachilleratoId = null;

            var result = _service.GuardarEncuestaInicial(123, request);

            Assert.True(result.Success);
            Assert.Equal("DEFINITIVO", result.Data!.Estado);
            Assert.DoesNotContain("anioBachillerato", result.Data.CamposPendientes);
            Assert.DoesNotContain("orientacionBachilleratoId", result.Data.CamposPendientes);
            bachilleratoRepo.Verify(r => r.GetByKey(123), Times.Never);
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
                It.Is<DtoTivenosBachilleratoRequest>(r => r.CodigoPersona == 123 && r.CodigoOrientacion == 1300),
                777,
                nameof(InscripcionesService.GuardarEncuestaInicial)), Times.Once);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarAltaDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
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
                It.IsAny<DtoTivenosBachilleratoRequest>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
            _tivenosEnvioServiceMock.Verify(s => s.EncolarModificacionDatosBachillerato(
                It.IsAny<IUnitOfWork>(),
                It.IsAny<DtoTivenosBachilleratoRequest>(),
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

        private static DtoGuardarEncuestaInicialRequest RequestEncuestaDefinitiva(
            long ultimoAnioSexto = 12,
            long? codigoTitulo = 1300)
        {
            return new DtoGuardarEncuestaInicialRequest
            {
                CarreraId = 10,
                ProcesoId = 20,
                CursaSecundariaActualmente = true,
                OrientacionBachilleratoId = codigoTitulo,
                AnioBachillerato = ultimoAnioSexto,
                NivelFormacionPadreTutorId = 1,
                NivelFormacionMadreTutorId = 1,
                AnioDecisionCarreraId = 2,
                AnioDecisionOrtId = 2,
                SeInformoEnOtrasUniversidades = false,
                ApoyoDecisionId = 1,
                InstitucionSecundariaId = 50,
                UbicacionUltimoAnioSecundariaId = 1,
                EstadoEducacionSuperiorPreviaId = 3,
                NivelDecisionId = 1,
                TuvoAsesoramientoOrt = false,
                VisitoSitioWebOrt = false,
                VisitoInstalacionesOrt = false,
                RecuerdaPublicidadOrt = false,
                TrabajaActualmente = false,
                MotivoEleccionOrtIds = [1]
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
                _tivenosEnvioServiceMock.Object,
                apiClient,
                CrearEncuestaInicialServiceReal());
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
                        Producto = new Producto { IdProducto = 10, NombreWebProducto = "Licenciatura en DiseÃ±o GrÃ¡fico" }
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
            Assert.Equal("Licenciatura en DiseÃ±o GrÃ¡fico", result.Data.Detalle.Carrera);
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

            var result = await service.ObtenerDetalleInscripcion(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("Pago pendiente", result.Data!.Estado);
            Assert.NotNull(result.Data.PagoPendiente);
            Assert.True(result.Data.PagoPendiente!.Confirmada);
            Assert.Equal(555, result.Data.PagoPendiente!.IdInscripcion);
            Assert.Equal(1500.50m, result.Data.PagoPendiente.Senia);
            Assert.Equal(3210.50m, result.Data.PagoPendiente.EstadoCuenta!.SaldoActual);
            Assert.Equal(new DateTime(2026, 7, 1), result.Data.PagoPendiente.FechaVencimientoPago);
            Assert.Equal("Analista programador", result.Data.PagoPendiente.Resumen.Carrera);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("Pagos/Carritos?idInscripcion=555", request.RequestUri);
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
                                NombreWebProducto = "Licenciatura en DiseÃ±o GrÃ¡fico",
                                NombreCoordAcadProducto = "No usar",
                                EmailCoordAcadProducto = "no.usar@ort.edu.uy"
                            }
                        }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(inscripto);
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

            var result = await _service.ObtenerDetalleInscripcion(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("Confirmada", result.Data!.Estado);
            Assert.NotNull(result.Data.Confirmada);
            Assert.Equal(123, result.Data.Confirmada!.NumeroEstudiante);
            Assert.Equal("Licenciatura en DiseÃ±o GrÃ¡fico", result.Data.Confirmada.Resumen.Carrera);
            Assert.Equal("MarÃ­a RodrÃ­guez", result.Data.Confirmada.CoordinadorAcademico!.Nombre);
            Assert.Equal("maria.rodriguez@ort.edu.uy", result.Data.Confirmada.CoordinadorAcademico.Email);
            Assert.Equal("Juan PÃ©rez", result.Data.Confirmada.CoordinadorCursos!.Nombre);
            Assert.Equal("juan.perez@ort.edu.uy", result.Data.Confirmada.CoordinadorCursos.Email);
            Assert.Equal(2, result.Data.Confirmada.MateriasPrimerSemestre.Count);
            Assert.Contains(result.Data.Confirmada.MateriasPrimerSemestre, m => m.Nombre == "Arte y estÃ©tica I");
        }

        [Fact]
        public async Task ObtenerDetalleInscripcion_WhenConfirmadaAndCoordinadoresIguales_MuestraSoloAcademico()
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
                            Producto = new Producto { IdProducto = 10, NombreWebProducto = "Licenciatura en DiseÃ±o GrÃ¡fico" }
                        }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(inscripto);
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

            var result = await _service.ObtenerDetalleInscripcion(123, 10, 20);

            Assert.True(result.Success);
            Assert.Equal("MarÃ­a RodrÃ­guez", result.Data!.Confirmada!.CoordinadorAcademico!.Nombre);
            Assert.Null(result.Data.Confirmada.CoordinadorCursos);
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

        [Fact]
        public async Task ObtenerUrlFactura_WithValidData_PostsAllCarritosAndReturnsUrl()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, "\"https://pagos.test/factura\""));
            var service = CrearServiceConApi(handler);

            var result = await service.ObtenerUrlFactura(123, new DtoObtenerUrlFacturaRequest
            {
                IdInscripto = 555,
                TipoPago = "SISTARBANC",
                IdBancoSistarbanc = "001"
            });

            Assert.True(result.Success);
            Assert.Equal("https://pagos.test/factura", result.Data);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("UrlCrearFactura?tipoPago=SISTARBANC&idInscripcion=555&banco=001", request.RequestUri);
            Assert.Equal(string.Empty, request.Body);
        }

        [Fact]
        public async Task ObtenerUrlFactura_WhenSistarbancWithoutBank_ReturnsBadRequest()
        {
            var result = await _service.ObtenerUrlFactura(123, new DtoObtenerUrlFacturaRequest
            {
                IdInscripto = 555,
                TipoPago = "SISTARBANC"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_UF_03", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ObtenerUrlFactura_WhenInscriptoDoesNotBelongToPersona_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await _service.ObtenerUrlFactura(123, new DtoObtenerUrlFacturaRequest
            {
                IdInscripto = 555,
                TipoPago = "BANRED"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_UF_04", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task ObtenerUrlFactura_WhenNoCarritos_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.BadRequest, "No hay carritos para la inscripcion."));
            var service = CrearServiceConApi(handler);

            var result = await service.ObtenerUrlFactura(123, new DtoObtenerUrlFacturaRequest
            {
                IdInscripto = 555,
                TipoPago = "BANRED"
            });

            Assert.False(result.Success);
            Assert.Equal("URL_CREAR_FACTURA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task Pagar_WithCuentaPersonal_ReturnsPagoConfirmadoConDetalle()
        {
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
                            Producto = new Producto { IdProducto = 10, NombreWebProducto = "Licenciatura en DiseÃ±o GrÃ¡fico" }
                        }
                    }
                }
            };
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(inscripto);
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
            var service = CrearServiceConApi(handler);

            var result = await service.Pagar(123, new DtoPagarRequest { IdInscripto = 555, TipoPago = "CUENTA_PERSONAL" });

            Assert.True(result.Success);
            Assert.Equal("PAGO_CONFIRMADO", result.Data!.Resultado);
            Assert.Single(result.Data.Mensajes);
            Assert.NotNull(result.Data.Confirmada);
            Assert.Equal(123, result.Data.Confirmada!.NumeroEstudiante);
            Assert.Equal("Licenciatura en DiseÃ±o GrÃ¡fico", result.Data.Confirmada.Resumen.Carrera);
            Assert.Equal("MarÃ­a RodrÃ­guez", result.Data.Confirmada.CoordinadorAcademico!.Nombre);
            Assert.Single(result.Data.Confirmada.MateriasPrimerSemestre);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("Pagos/Carritos/Pagar?tipoPago=PAGO_CUENTA_CORRIENTE&idInscripcion=555", request.RequestUri);
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
            var service = CrearServiceConApi(handler);

            var result = await service.Pagar(123, new DtoPagarRequest { IdInscripto = 555, TipoPago = "CUENTA_PERSONAL" });

            Assert.True(result.Success);
            Assert.Equal("PAGO_CONFIRMADO", result.Data!.Resultado);
            Assert.NotNull(result.Data.Confirmada);
            Assert.Equal(123, result.Data.Confirmada!.NumeroEstudiante);
            Assert.Null(result.Data.Confirmada.CoordinadorAcademico);
            Assert.Empty(result.Data.Confirmada.MateriasPrimerSemestre);
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

            var result = await _service.Pagar(123, new DtoPagarRequest { IdInscripto = 555, TipoPago = " abitab " });

            Assert.True(result.Success);
            Assert.Equal("METODO_GUARDADO", result.Data!.Resultado);
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
            var service = CrearServiceConApi(handler);

            var result = await service.Pagar(123, new DtoPagarRequest { IdInscripto = 555, TipoPago = "BANRED" });

            Assert.True(result.Success);
            Assert.Equal("URL_GENERADA", result.Data!.Resultado);
            Assert.Equal("https://pagos.test/factura", result.Data.UrlPago);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("UrlCrearFactura?tipoPago=BANRED&idInscripcion=555&banco=", request.RequestUri);
        }

        [Fact]
        public async Task Pagar_WithInvalidTipoPago_ReturnsBadRequest()
        {
            var result = await _service.Pagar(123, new DtoPagarRequest { IdInscripto = 555, TipoPago = "OTRO" });

            Assert.False(result.Success);
            Assert.Equal("INS_PAG_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _uowFactoryMock.Verify(f => f.Create(), Times.Never);
        }

        [Fact]
        public async Task PagarCuentaPersonal_WithValidData_PostsAllCarritosAndReturnsMessages()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK,
                """
                [
                  { "clave": "123|10|1|7|555", "valor": "Tu pago con Cuenta Personal se realizó exitosamente." },
                  { "clave": "123|10|1|7|556", "valor": "Tu pago con Cuenta Personal se realizó exitosamente." }
                ]
                """));
            var service = CrearServiceConApi(handler);

            var result = await service.PagarCuentaPersonal(123, new DtoPagarCuentaPersonalRequest { IdInscripto = 555 });

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal("123|10|1|7|555", result.Data[0].Clave);
            var request = Assert.Single(handler.Requests);
            Assert.Contains("Pagos/Carritos/Pagar?tipoPago=PAGO_CUENTA_CORRIENTE&idInscripcion=555", request.RequestUri);
            Assert.Equal(string.Empty, request.Body);
        }

        [Fact]
        public async Task PagarCuentaPersonal_WhenInscriptoDoesNotBelongToPersona_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = await _service.PagarCuentaPersonal(123, new DtoPagarCuentaPersonalRequest { IdInscripto = 555 });

            Assert.False(result.Success);
            Assert.Equal("INS_PC_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public async Task PagarCuentaPersonal_WhenNoCarritos_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.BadRequest, "No hay carritos para la inscripcion."));
            var service = CrearServiceConApi(handler);

            var result = await service.PagarCuentaPersonal(123, new DtoPagarCuentaPersonalRequest { IdInscripto = 555 });

            Assert.False(result.Success);
            Assert.Equal("PAGAR_CARRITOS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task PagarCuentaPersonal_WhenLegacyRejects_ReturnsFailure()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo
                .Setup(r => r.GetDetalleByKey(555, 123))
                .Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, "saldo insuficiente"));
            var service = CrearServiceConApi(handler);

            var result = await service.PagarCuentaPersonal(123, new DtoPagarCuentaPersonalRequest { IdInscripto = 555 });

            Assert.False(result.Success);
            Assert.Equal("PAGAR_CARRITOS_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Contains("saldo insuficiente", result.Message);
        }

        [Fact]
        public void GuardarMetodoPago_WithValidData_AddsAndSaves()
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

            var result = _service.GuardarMetodoPago(123, new DtoGuardarMetodoPagoRequest
            {
                IdInscripto = 555,
                MetodoPago = "ABITAB"
            });

            Assert.True(result.Success);
            Assert.True(result.Data);
            Assert.NotNull(agregado);
            Assert.Equal(555, agregado!.IdInscripto);
            Assert.Equal("ABITAB", agregado.MetodoPagoSeniaMinima);
            _uowMock.Verify(u => u.Save(), Times.Once);
        }

        [Fact]
        public void GuardarMetodoPago_NormalizesMetodoPago()
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

            var result = _service.GuardarMetodoPago(123, new DtoGuardarMetodoPagoRequest
            {
                IdInscripto = 555,
                MetodoPago = " paganza "
            });

            Assert.True(result.Success);
            Assert.Equal("PAGANZA", agregado!.MetodoPagoSeniaMinima);
        }

        [Fact]
        public void GuardarMetodoPago_WhenMetodoPagoInvalid_ReturnsBadRequest()
        {
            var result = _service.GuardarMetodoPago(123, new DtoGuardarMetodoPagoRequest
            {
                IdInscripto = 555,
                MetodoPago = "TARJETA"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_MP_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            _uowFactoryMock.Verify(f => f.Create(), Times.Never);
        }

        [Fact]
        public void GuardarMetodoPago_WhenInscriptoDoesNotBelongToPersona_ReturnsNotFound()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var result = _service.GuardarMetodoPago(123, new DtoGuardarMetodoPagoRequest
            {
                IdInscripto = 555,
                MetodoPago = "ABITAB"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_MP_03", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }

        [Fact]
        public void GuardarMetodoPago_WhenAlreadyExists_ReturnsConflict()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetDetalleByKey(555, 123)).Returns(new Inscripto { IdInscripto = 555, CodigoPersona = 123 });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);
            var seniaRepo = new Mock<IInscriptoSeniaMinimumRepository>();
            seniaRepo.Setup(r => r.GetByKey(555)).Returns(new InscriptoSeniaMinimum { IdInscripto = 555, MetodoPagoSeniaMinima = "ABITAB" });
            _uowMock.Setup(u => u.InscriptoSeniaMinima).Returns(seniaRepo.Object);

            var result = _service.GuardarMetodoPago(123, new DtoGuardarMetodoPagoRequest
            {
                IdInscripto = 555,
                MetodoPago = "PAGANZA"
            });

            Assert.False(result.Success);
            Assert.Equal("INS_MP_04", result.ErrorCode);
            Assert.Equal(409, result.HttpCode);
            seniaRepo.Verify(r => r.Add(It.IsAny<InscriptoSeniaMinimum>()), Times.Never);
            _uowMock.Verify(u => u.Save(), Times.Never);
        }
    }
}

