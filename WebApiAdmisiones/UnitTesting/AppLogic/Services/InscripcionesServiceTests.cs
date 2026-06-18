using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices.Catalogos;
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
        private readonly InscripcionesService _service;

        public InscripcionesServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _generalServiceMock = new Mock<IGeneralService>();
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
            var apiClient = new InscripcionesyPagosApiClient(
                new HttpClient { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<InscripcionesyPagosApiClient>.Instance);
            _service = new InscripcionesService(_uowFactoryMock.Object, _dbConnectionContextMock.Object, _generalServiceMock.Object, apiClient);
        }

        private static readonly DateTime FechaBase = new(2026, 5, 27, 10, 30, 0);

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
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
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
                """));
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
            Assert.NotNull(aceptacionAgregada);
            Assert.Equal(999, aceptacionAgregada!.IdAceptacionReglamentoEst);
            var requestApi = Assert.Single(handler.Requests);
            Assert.Contains("tipoInscripcion=ONLINE", requestApi.RequestUri);
            Assert.Contains("idProducto=20", requestApi.RequestUri);
            Assert.Contains("idProceso=30", requestApi.RequestUri);
            Assert.Contains("idOfertaSeleccionada=10", requestApi.RequestUri);
            Assert.Contains("\"idTurno\":1", requestApi.Body);
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithDefinitiveDocuments_Confirms()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                {
                  "confirmada": true,
                  "idInscripcion": 78
                }
                """));
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
        }

        [Fact]
        public async Task ConfirmarPreInscripcion_WithEncuestaIniHistorica_ConfirmsWithoutDefinitiveEncuestaAdmision()
        {
            AceptacionReglamentoEst? aceptacionAgregada = null;
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                {
                  "success": true,
                  "seniaInscripcion": 1500
                }
                """));
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
        public void ObtenerUltimaInscripcionActiva_NotFound_ReturnsFailed()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.GetUltimaInscripcionActiva(123)).Returns((Inscripto)null);
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.ObtenerUltimaInscripcionActiva(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_UI_01", result.ErrorCode);
            Assert.Equal(204, result.HttpCode);
        }

        [Fact]
        public void ObtenerUltimaInscripcionActiva_ReturnsMappedDto()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.GetUltimaInscripcionActiva(123)).Returns(new Inscripto
            {
                IdInscripto = 9,
                Oferta = new Oferta
                {
                    Supraoferta = new Supraoferta
                    {
                        Comienzo = new Comienzo { IdComienzo = 5, NombreComienzo = "Abril" },
                        Paquete = new Paquete
                        {
                            Producto = new Producto
                            {
                                IdProducto = 7,
                                NombreProducto = "ATI",
                                NombreExtensoProducto = "Analista en TI"
                            }
                        }
                    }
                }
            });
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.ObtenerUltimaInscripcionActiva(123);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(9, result.Data.IdInscripto);
            Assert.Equal(7, result.Data.IdProducto);
            Assert.Equal("ATI", result.Data.NombreProducto);
            Assert.Equal("Analista en TI", result.Data.NombreExtensoProducto);
            Assert.Equal(5, result.Data.IdComienzo);
            Assert.Equal("Abril", result.Data.NombreComienzo);
        }

        [Fact]
        public void ObtenerUltimaInscripcionActiva_ConOfertaNula_UsaValoresPorDefecto()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.GetUltimaInscripcionActiva(123)).Returns(new Inscripto
            {
                IdInscripto = 9,
                Oferta = null
            });
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.ObtenerUltimaInscripcionActiva(123);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data.IdProducto);
            Assert.Equal(0, result.Data.IdComienzo);
            Assert.Null(result.Data.NombreProducto);
            Assert.Null(result.Data.NombreComienzo);
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
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Once);
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
        public void TieneInscripcionActivaParaProceso_ReturnsRepositoryValue()
        {
            var repo = new Mock<IVdEsFrescoAdmisionRepository>();
            repo.Setup(r => r.TieneInscripcionActivaParaProceso(1, 2, 3)).Returns(true);
            _uowMock.Setup(u => u.VdEsFrescoAdmisions).Returns(repo.Object);

            var result = _service.TieneInscripcionActivaParaProceso(1, 2, 3);

            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public void TieneInscripcionAdmisiones_ReturnsRepositoryValue()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.TieneInscripcionAdmisiones(1, 2, 3)).Returns(false);
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.TieneInscripcionAdmisiones(1, 2, 3);

            Assert.True(result.Success);
            Assert.False(result.Data);
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

        private InscripcionesService CrearServiceConApi(HttpMessageHandler handler)
        {
            var apiClient = new InscripcionesyPagosApiClient(
                new HttpClient(handler) { BaseAddress = new Uri("https://internal.test/") },
                NullLogger<InscripcionesyPagosApiClient>.Instance);

            return new InscripcionesService(_uowFactoryMock.Object, _dbConnectionContextMock.Object, _generalServiceMock.Object, apiClient);
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
    }
}
