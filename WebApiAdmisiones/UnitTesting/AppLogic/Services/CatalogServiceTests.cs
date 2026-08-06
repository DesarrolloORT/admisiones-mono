using AppLogic.Catalogs.Dtos;
using System.Net;
using System.Text;
using AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;
using AppLogic.Integrations.EnrollmentsAndPayments.Services;
using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;
using Xunit;
using AppLogic.Catalogs.Services;

namespace UnitTesting.AppLogic.Services
{
    public class CatalogServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly CatalogService _service;

        public CatalogServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new CatalogService(_uowFactoryMock.Object, Mock.Of<IEnrollmentsAndPaymentsApiClient>());

            SetupEncuestaInicialCatalogos();
        }

        private void SetupEncuestaInicialCatalogos()
        {
            var motivoRepo = new Mock<BusinessLogic.IDevartRepositories.IMotivoOpcionesAdmisionRepository>();
            motivoRepo.Setup(r => r.GetAll()).Returns(new List<MotivoOpcionesAdmision>());
            _uowMock.Setup(u => u.MotivoOpcionesAdmisions).Returns(motivoRepo.Object);

            var publicidadRepo = new Mock<BusinessLogic.IDevartRepositories.IPublicidadOpcionesAdmisionRepository>();
            publicidadRepo.Setup(r => r.GetAll()).Returns(new List<PublicidadOpcionesAdmision>());
            _uowMock.Setup(u => u.PublicidadOpcionesAdmisions).Returns(publicidadRepo.Object);

            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAllWithRelated()).Returns(new List<AnioBachiller>());
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetUniversidades()).Returns(new List<Empresa>());
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudades_ReturnsSlimPaisesEstadosCiudades()
        {
            var paisRepo = new Mock<IPaisRepository>();
            paisRepo.Setup(r => r.GetPaisesConEstadosYCiudades()).Returns(new List<Pais>
            {
                new Pais
                {
                    CodigoPais = 1,
                    Nombre = "Uruguay",
                    Estado =
                    [
                        new Estado
                        {
                            CodigoPais = 1,
                            CodigoEstado = 10,
                            Nombre = "Montevideo",
                            Ciudad =
                            [
                                new Ciudad
                                {
                                    CodigoPais = 1,
                                    CodigoEstado = 10,
                                    CodigoCiudad = 100,
                                    Nombre = "Montevideo"
                                }
                            ]
                        }
                    ]
                }
            });
            _uowMock.Setup(u => u.Paises).Returns(paisRepo.Object);

            var result = await _service.GetCountriesStatesCitiesAsync();

            Assert.True(result.Success);
            Assert.Equal("GetCountriesStatesCitiesAsync", result.Method);
            var country = Assert.Single(result.Data!);
            Assert.Equal(1, country.CountryId);
            Assert.Equal("Uruguay", country.Name);
            Assert.NotNull(country.States);
            var estado = Assert.Single(country.States);
            Assert.Equal(1, estado.CountryId);
            Assert.Equal(10, estado.StateId);
            Assert.Equal("Montevideo", estado.Name);
            Assert.NotNull(country.States[0].Cities);
            var city = Assert.Single(country.States[0].Cities);
            Assert.Equal(1, city.CountryId);
            Assert.Equal(10, city.StateId);
            Assert.Equal(100, city.CityId);
            Assert.Equal("Montevideo", city.Name);
            Assert.Null(typeof(CountryStateCityResponse).GetProperty("DgiPais"));
            Assert.Null(typeof(StateWithCitiesResponse).GetProperty("Pai"));
            Assert.Null(typeof(StateWithCitiesResponse).GetProperty("SolicitudAltas"));
            Assert.Null(typeof(CityResponse).GetProperty("Empresas"));
            Assert.Null(typeof(CityResponse).GetProperty("Personas"));
        }

        [Fact]
        public async Task ObtenerEncuestaInicial_ReturnsAllStaticCatalogs()
        {
            var result = await _service.GetInitialSurveyCatalogsAsync();

            Assert.True(result.Success);
            Assert.Equal("GetInitialSurveyCatalogsAsync", result.Method);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Education.YesNoOptions.Count);
            Assert.Equal(2, result.Data.Education.LastSecondaryYearLocations.Count);
            Assert.Equal(3, result.Data.Education.PreviousHigherEducationOptions.Count);
            Assert.Equal(7, result.Data.Education.EducationLevels.Count);
            Assert.Equal(4, result.Data.AcademicDecision.UpperSecondaryYears.Count);
            Assert.Equal(5, result.Data.AcademicDecision.DecisionSupports.Count);
            Assert.Equal(2, result.Data.AcademicDecision.DecisionLevels.Count);
            Assert.Equal(5, result.Data.OrtExperience.Ratings.Count);
        }

        [Fact]
        public async Task ObtenerEncuestaInicial_UsesEmsLabelsForSecondaryDecisionOptions()
        {
            var result = await _service.GetInitialSurveyCatalogsAsync();

            var decisionCarrera = result.Data!.AcademicDecision.UpperSecondaryYears.ToList();
            Assert.Collection(
                decisionCarrera,
                item =>
                {
                    Assert.Equal(2, item.Value);
                    Assert.Equal("1\u00b0 EMS (4\u00b0 a\u00f1o)", item.Label);
                },
                item =>
                {
                    Assert.Equal(3, item.Value);
                    Assert.Equal("2\u00b0 EMS (5\u00b0 a\u00f1o)", item.Label);
                },
                item =>
                {
                    Assert.Equal(4, item.Value);
                    Assert.Equal("3\u00b0 EMS (6\u00b0 a\u00f1o)", item.Label);
                },
                item =>
                {
                    Assert.Equal(0, item.Value);
                    Assert.Equal("Otro", item.Label);
                });
        }

        [Fact]
        public async Task ObtenerEncuestaInicial_IncludesDynamicCatalogsBySection()
        {
            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetAllWithRelated()).Returns(
            [
                new AnioBachiller
                {
                    IdAnioBachiller = 6,
                    CantAniosAnioBachiller = 12,
                    NombreAnioBachiller = "6 anio",
                    Titulos =
                    [
                        new Titulo
                        {
                            CodigoTitulo = 20,
                            Nombre = "Ingenieria",
                            OrientacionTitulo = "Fisico Matematica",
                            OrientacionNewTitulo = "Fisico-Matematica"
                        }
                    ]
                }
            ]);
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var empresaRepo = new Mock<IEmpresaRepository>();
            empresaRepo.Setup(r => r.GetUniversidades()).Returns(
            [
                new Empresa { CodigoEmpresa = 30, Nombre = "Universidad ejemplo" }
            ]);
            _uowMock.Setup(u => u.Empresas).Returns(empresaRepo.Object);

            var result = await _service.GetInitialSurveyCatalogsAsync();

            var year = Assert.Single(result.Data!.Education.HighSchoolYears);
            Assert.Equal(12, year.Value);
            Assert.Equal("6 anio", year.Label);
            var orientacion = Assert.Single(year.Tracks);
            Assert.Equal(20, orientacion.Value);
            Assert.Equal("Ingenieria", orientacion.Label);

            Assert.Equal(2, result.Data.Education.Universities.Count);
            var universidadEducacion = result.Data.Education.Universities[0];
            var universidadDecision = result.Data.AcademicDecision.Universities[0];
            Assert.Equal(30, universidadEducacion.Value);
            Assert.Equal("Universidad ejemplo", universidadEducacion.Label);
            Assert.Equal(universidadEducacion.Value, universidadDecision.Value);
            Assert.Equal(universidadEducacion.Label, universidadDecision.Label);

            var otroEducacion = result.Data.Education.Universities[1];
            Assert.Equal(0, otroEducacion.Value);
            Assert.Equal("Otro", otroEducacion.Label);
        }

        [Fact]
        public void ObtenerComienzos_ReturnsMappedItems()
        {
            var repo = new Mock<IVdProcesosDisponibles1y2Repository>();
            repo.Setup(r => r.GetProcesosDisponibles(10)).Returns(
            [
                new VdProcesosDisponibles1y2 { IdProceso = 20, NombreProceso = "Marzo" }
            ]);
            _uowMock.Setup(u => u.VdProcesosDisponibles1y2s).Returns(repo.Object);

            var result = _service.GetIntakes(10);

            Assert.True(result.Success);
            Assert.Equal(nameof(CatalogService.GetIntakes), result.Method);
            var item = Assert.Single(result.Data!);
            Assert.Equal(20, item.AdmissionProcessId);
            Assert.Equal("Marzo", item.AdmissionProcessName);
        }

        [Fact]
        public void ObtenerCarreras_Nivel1o2_FiltraPorNivelYNoConsultaVista3y4()
        {
            var repo = new Mock<IVdProductosDisponibles1y2Repository>();
            repo.Setup(r => r.GetProductosDisponibles(99, 1)).Returns(
            [
                new VdProductosDisponibles1y2
                {
                    IdProducto = 11,
                    NombreWebProducto = "Comunicacion",
                    IdNivelProducto = 1,
                    NombreNivelProducto = "Tecnico",
                    IdEscuela = 7,
                    NombreExtensoEscuela = "Facultad de Comunicacion",
                    OrdenListadoEscuela = 1,
                    OrdenListadoNivelProducto = 1
                }
            ]);
            _uowMock.Setup(u => u.VdProductosDisponibles1y2s).Returns(repo.Object);

            var vistaRepo = new Mock<IVdOfertasDisponibles3y4Repository>();
            _uowMock.Setup(u => u.VdOfertasDisponibles3y4s).Returns(vistaRepo.Object);

            var result = _service.GetDegreePrograms(99, AcademicOffer.UniversityDegree);

            Assert.True(result.Success);
            Assert.Equal(nameof(CatalogService.GetDegreePrograms), result.Method);

            var nivel = Assert.Single(result.Data!);
            Assert.Equal(1, nivel.ProductLevelId);
            Assert.Equal("Tecnico", nivel.ProductLevelName);
            var escuela = Assert.Single(nivel.Schools);
            Assert.Equal(7, escuela.SchoolId);
            Assert.Equal("Facultad de Comunicacion", escuela.SchoolName);
            var product = Assert.Single(escuela.Products!);
            Assert.Equal(11, product.ProductId);
            Assert.Equal("Comunicacion", product.ProductName);
            Assert.Null(escuela.Seminars);

            vistaRepo.Verify(r => r.GetProductosDisponibles(), Times.Never);
        }

        [Fact]
        public void ObtenerCarreras_Nivel3o4_DevuelveAmbosNivelesCombinados()
        {
            var vistaRepo = new Mock<IVdOfertasDisponibles3y4Repository>();
            vistaRepo.Setup(r => r.GetProductosDisponibles()).Returns(
            [
                new VdOfertasDisponibles3y4
                {
                    IdProducto = 50,
                    NombreWebProducto = "MBA",
                    IdNivelProducto = 3,
                    NombreNivelProducto = "Postgrado",
                    IdEscuela = 7,
                    NombreExtensoEscuela = "Facultad de Administracion"
                },
                new VdOfertasDisponibles3y4
                {
                    IdProducto = 50,
                    NombreWebProducto = "MBA duplicado",
                    IdNivelProducto = 3,
                    NombreNivelProducto = "Postgrado",
                    IdEscuela = 7,
                    NombreExtensoEscuela = "Facultad de Administracion"
                },
                new VdOfertasDisponibles3y4
                {
                    IdProducto = 51,
                    NombreWebProducto = "MBA con seminario",
                    IdNivelProducto = 3,
                    NombreNivelProducto = "Postgrado",
                    IdEscuela = 7,
                    NombreExtensoEscuela = "Facultad de Administracion",
                    ConSeminarios = "SI"
                },
                new VdOfertasDisponibles3y4
                {
                    IdProducto = 60,
                    NombreWebProducto = "Curso corto",
                    IdNivelProducto = 4,
                    NombreNivelProducto = "Actualizacion",
                    IdEscuela = 9,
                    NombreExtensoEscuela = "Facultad de Negocios"
                }
            ]);
            _uowMock.Setup(u => u.VdOfertasDisponibles3y4s).Returns(vistaRepo.Object);

            var repo = new Mock<IVdProductosDisponibles1y2Repository>();
            _uowMock.Setup(u => u.VdProductosDisponibles1y2s).Returns(repo.Object);

            var result = _service.GetDegreePrograms(99, AcademicOffer.ProfessionalUpdate);

            Assert.True(result.Success);
            var niveles = result.Data!.ToList();
            Assert.Collection(
                niveles,
                nivel =>
                {
                    Assert.Equal(3, nivel.ProductLevelId);
                    var escuela = Assert.Single(nivel.Schools);
                    Assert.Equal("Facultad de Administracion", escuela.SchoolName);
                    Assert.Null(escuela.Products);
                    Assert.Collection(
                        escuela.Seminars!,
                        sinSeminario =>
                        {
                            Assert.False(sinSeminario.HasSeminar);
                            var product = Assert.Single(sinSeminario.Products);
                            Assert.Equal(50, product.ProductId);
                            Assert.Equal("MBA", product.ProductName);
                        },
                        conSeminario =>
                        {
                            Assert.True(conSeminario.HasSeminar);
                            var product = Assert.Single(conSeminario.Products);
                            Assert.Equal(51, product.ProductId);
                        });
                },
                nivel =>
                {
                    Assert.Equal(4, nivel.ProductLevelId);
                    var escuela = Assert.Single(nivel.Schools);
                    var sinSeminario = Assert.Single(escuela.Seminars!);
                    Assert.False(sinSeminario.HasSeminar);
                    var product = Assert.Single(sinSeminario.Products);
                    Assert.Equal(60, product.ProductId);
                });

            repo.Verify(r => r.GetProductosDisponibles(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public void ObtenerCarreras_PropuestaAcademicaInvalida_DevuelveBadRequest()
        {
            var result = _service.GetDegreePrograms(99, (AcademicOffer)99);

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ObtenerTurnos_Nivel1o2_UsesInscripcionesYPagosApi()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto
            {
                IdProducto = 10,
                IdNivelProducto = 2,
                NombreProducto = "ATI",
                NombreExtensoProducto = "Analista en TI"
            });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                [
                  {
                    "idOferta": 57319,
                    "horarioReferencia": "Lunes 19:00",
                    "idTurno": 1,
                    "nombreTurno": "Nocturno"
                  }
                ]
                """));
            var service = new CatalogService(_uowFactoryMock.Object, CrearInscripcionesClient(handler));

            var result = await service.GetShifts(99, 10, 20);

            Assert.True(result.Success);
            var offering = Assert.Single(result.Data!);
            Assert.Equal(57319, offering.OfferingId);
            Assert.Equal(1, offering.Shift.ShiftId);
            Assert.Equal("Nocturno", offering.Shift.ShiftName);
            Assert.Equal("Lunes 19:00", offering.ReferenceSchedule);

            var request = Assert.Single(handler.Requests);
            Assert.Contains("OfertasParaInscripcionAdmisionesConProceso?idProducto=10&idProceso=20", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerTurnos_Nivel3o4_UsesVistaAndDoesNotCallApi()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(10)).Returns(new Producto
            {
                IdProducto = 10,
                IdNivelProducto = 3,
                NombreProducto = "POS",
                NombreExtensoProducto = "Postgrado"
            });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var ofertasRepo = new Mock<IVdOfertasDisponibles3y4Repository>();
            ofertasRepo
                .Setup(r => r.GetOfertasDisponibles(10, 99))
                .Returns(
                [
                    new VdOfertasDisponibles3y4 { IdProducto = 10, IdComienzo = 30, IdOferta = 100, IdTurno = 1, IdMateria = 1 },
                    new VdOfertasDisponibles3y4 { IdProducto = 10, IdComienzo = 30, IdOferta = 100, IdTurno = 1, IdMateria = 99 },
                    new VdOfertasDisponibles3y4 { IdProducto = 10, IdComienzo = 30, IdOferta = 101, IdTurno = 2, IdMateria = 2 }
                ]);
            _uowMock.Setup(u => u.VdOfertasDisponibles3y4s).Returns(ofertasRepo.Object);

            var turnoRepo = new Mock<ITurnoRepository>();
            turnoRepo
                .Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>()))
                .Returns(
                [
                    new Turno { IdTurno = 1, NombreTurno = "Matutino" },
                    new Turno { IdTurno = 2, NombreTurno = "Nocturno" }
                ]);
            _uowMock.Setup(u => u.Turnos).Returns(turnoRepo.Object);

            var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("No debe llamar la API"));
            var service = new CatalogService(_uowFactoryMock.Object, CrearInscripcionesClient(handler));

            var result = await service.GetShifts(99, 10, 20);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal(100, result.Data[0].OfferingId);
            Assert.Equal(1, result.Data[0].Shift.ShiftId);
            Assert.Equal("Matutino", result.Data[0].Shift.ShiftName);
            Assert.Null(result.Data[0].ReferenceSchedule);
            Assert.Equal(101, result.Data[1].OfferingId);
            Assert.Equal(2, result.Data[1].Shift.ShiftId);
            Assert.Equal("Nocturno", result.Data[1].Shift.ShiftName);
            Assert.Empty(handler.Requests);
            _uowMock.Verify(u => u.ProcesoComienzos, Times.Never);
        }

        [Fact]
        public async Task ObtenerTurnos_WhenProductoDoesNotExist_ReturnsFailureWithoutCallingApi()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKey(99)).Returns((Producto)null);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("No debe llamar la API"));
            var service = new CatalogService(_uowFactoryMock.Object, CrearInscripcionesClient(handler));

            var result = await service.GetShifts(99, 99, 20);

            Assert.False(result.Success);
            Assert.Equal("CAT_TURNOS_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public void ObtenerInstituciones_ReturnsMappedItems()
        {
            var repo = new Mock<IEmpresaRepository>();
            repo.Setup(r => r.GetInstituciones(1, 2)).Returns(
            [
                new Empresa
                {
                    CodigoEmpresa = 100,
                    Nombre = "Instituto Ejemplo",
                    UsuarioUltimaActualizacion = "USR",
                    FechaUltimaActualizacion = DateTime.Today,
                    HoraUltimaActualizacion = "10:00:00",
                    UsuarioIngreso = "USR",
                    FechaIngreso = DateTime.Today,
                    HoraIngreso = "10:00:00",
                    CodigoTipoEmpresa = 9
                }
            ]);
            _uowMock.Setup(u => u.Empresas).Returns(repo.Object);

            var result = _service.GetInstitutions(1, 2);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(100, item.Id);
            Assert.Equal("Instituto Ejemplo", item.Name);
        }

        private static EnrollmentsAndPaymentsApiClient CrearInscripcionesClient(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://internal.test/")
            };

            return new EnrollmentsAndPaymentsApiClient(
                httpClient,
                NullLogger<EnrollmentsAndPaymentsApiClient>.Instance);
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
    }
}
