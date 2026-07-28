using AppLogic.Catalogos.Dtos;
using System.Net;
using System.Text;
using AppLogic.ApiClients.Interfaces;
using AppLogic.ApiClients.Services;
using AppLogic.DevartDTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;
using Xunit;
using AppLogic.Catalogos.Services;

namespace UnitTesting.AppLogic.Services
{
    public class CatalogosServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly CatalogosService _service;

        public CatalogosServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new CatalogosService(_uowFactoryMock.Object, Mock.Of<IInscripcionesyPagosApiClient>());

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

            var result = await _service.ObtenerPaisesEstadosCiudadesAsync();

            Assert.True(result.Success);
            Assert.Equal("ObtenerPaisesEstadosCiudades", result.Method);
            var pais = Assert.Single(result.Data!);
            Assert.Equal(1, pais.CodigoPais);
            Assert.Equal("Uruguay", pais.Nombre);
            Assert.NotNull(pais.Estado);
            var estado = Assert.Single(pais.Estado);
            Assert.Equal(1, estado.CodigoPais);
            Assert.Equal(10, estado.CodigoEstado);
            Assert.Equal("Montevideo", estado.Nombre);
            Assert.NotNull(pais.Estado[0].Ciudad);
            var ciudad = Assert.Single(pais.Estado[0].Ciudad);
            Assert.Equal(1, ciudad.CodigoPais);
            Assert.Equal(10, ciudad.CodigoEstado);
            Assert.Equal(100, ciudad.CodigoCiudad);
            Assert.Equal("Montevideo", ciudad.Nombre);
            Assert.Null(typeof(DtoPaisEstadoCiudadResponse).GetProperty("DgiPais"));
            Assert.Null(typeof(DtoEstadoCiudadResponse).GetProperty("Pai"));
            Assert.Null(typeof(DtoEstadoCiudadResponse).GetProperty("SolicitudAltas"));
            Assert.Null(typeof(DtoCiudadResponse).GetProperty("Empresas"));
            Assert.Null(typeof(DtoCiudadResponse).GetProperty("Personas"));
        }

        [Fact]
        public void ObtenerEncuestaInicial_ReturnsAllStaticCatalogs()
        {
            var result = _service.ObtenerEncuestaInicial();

            Assert.True(result.Success);
            Assert.Equal(nameof(CatalogosService.ObtenerEncuestaInicial), result.Method);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Educacion.OpcionesSiNo.Count);
            Assert.Equal(2, result.Data.Educacion.UbicacionesUltimoAnioSecundaria.Count);
            Assert.Equal(3, result.Data.Educacion.EstadosEducacionSuperiorPrevia.Count);
            Assert.Equal(7, result.Data.Educacion.NivelesFormacionTutores.Count);
            Assert.Equal(4, result.Data.DecisionAcademica.AniosEducacionMediaSuperior.Count);
            Assert.Equal(5, result.Data.DecisionAcademica.ApoyosDecision.Count);
            Assert.Equal(2, result.Data.DecisionAcademica.NivelesDecision.Count);
            Assert.Equal(5, result.Data.ExperienciaOrt.Valoraciones.Count);
        }

        [Fact]
        public void ObtenerEncuestaInicial_UsesEmsLabelsForSecondaryDecisionOptions()
        {
            var result = _service.ObtenerEncuestaInicial();

            var decisionCarrera = result.Data!.DecisionAcademica.AniosEducacionMediaSuperior.ToList();
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
        public void ObtenerEncuestaInicial_IncludesDynamicCatalogsBySection()
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

            var result = _service.ObtenerEncuestaInicial();

            var anio = Assert.Single(result.Data!.Educacion.AniosBachillerato);
            Assert.Equal(12, anio.Value);
            Assert.Equal("6 anio", anio.Label);
            var orientacion = Assert.Single(anio.Orientaciones);
            Assert.Equal(20, orientacion.Value);
            Assert.Equal("Ingenieria", orientacion.Label);

            Assert.Equal(2, result.Data.Educacion.Universidades.Count);
            var universidadEducacion = result.Data.Educacion.Universidades[0];
            var universidadDecision = result.Data.DecisionAcademica.Universidades[0];
            Assert.Equal(30, universidadEducacion.Value);
            Assert.Equal("Universidad ejemplo", universidadEducacion.Label);
            Assert.Equal(universidadEducacion.Value, universidadDecision.Value);
            Assert.Equal(universidadEducacion.Label, universidadDecision.Label);

            var otroEducacion = result.Data.Educacion.Universidades[1];
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

            var result = _service.ObtenerComienzos(10);

            Assert.True(result.Success);
            Assert.Equal(nameof(CatalogosService.ObtenerComienzos), result.Method);
            var item = Assert.Single(result.Data!);
            Assert.Equal(20, item.IdProceso);
            Assert.Equal("Marzo", item.NombreProceso);
        }

        [Fact]
        public void ObtenerCarreras_ReturnsProductsGroupedByLevelAndSchool()
        {
            var repo = new Mock<IVdProductosDisponibles1y2Repository>();
            repo.Setup(r => r.GetProductosDisponibles(99)).Returns(
            [
                new VdProductosDisponibles1y2
                {
                    IdProducto = 10,
                    NombreWebProducto = "ATI",
                    IdNivelProducto = 2,
                    NombreNivelProducto = "Carrera",
                    IdEscuela = 8,
                    NombreExtensoEscuela = "Facultad de Ingenieria",
                    OrdenListadoEscuela = 2,
                    OrdenListadoNivelProducto = 2
                },
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
                }
            ]);
            _uowMock.Setup(u => u.VdOfertasDisponibles3y4s).Returns(vistaRepo.Object);

            var result = _service.ObtenerCarreras(99);

            Assert.True(result.Success);
            Assert.Equal(nameof(CatalogosService.ObtenerCarreras), result.Method);

            var niveles = result.Data!.ToList();
            Assert.Collection(
                niveles,
                nivel =>
                {
                    Assert.Equal(1, nivel.IdNivelProducto);
                    Assert.Equal("Tecnico", nivel.NombreNivelProducto);
                    var escuela = Assert.Single(nivel.Escuelas);
                    Assert.Equal(7, escuela.IdEscuela);
                    Assert.Equal("Facultad de Comunicacion", escuela.NombreEscuela);
                    var producto = Assert.Single(escuela.Productos);
                    Assert.Equal(11, producto.IdProducto);
                    Assert.Equal("Comunicacion", producto.NombreProducto);
                },
                nivel =>
                {
                    Assert.Equal(2, nivel.IdNivelProducto);
                    var escuela = Assert.Single(nivel.Escuelas);
                    Assert.Equal(8, escuela.IdEscuela);
                    var producto = Assert.Single(escuela.Productos);
                    Assert.Equal(10, producto.IdProducto);
                },
                nivel =>
                {
                    Assert.Equal(3, nivel.IdNivelProducto);
                    var escuela = Assert.Single(nivel.Escuelas);
                    Assert.Equal("Facultad de Administracion", escuela.NombreEscuela);
                    var producto = Assert.Single(escuela.Productos);
                    Assert.Equal(50, producto.IdProducto);
                    Assert.Equal("MBA", producto.NombreProducto);
                });
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
            var service = new CatalogosService(_uowFactoryMock.Object, CrearInscripcionesClient(handler));

            var result = await service.ObtenerTurnos(10, 20);

            Assert.True(result.Success);
            var oferta = Assert.Single(result.Data!);
            Assert.Equal(57319, oferta.IdOferta);
            Assert.Equal(1, oferta.Turno.IdTurno);
            Assert.Equal("Nocturno", oferta.Turno.NombreTurno);
            Assert.Equal("Lunes 19:00", oferta.HorarioReferencia);

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
                .Setup(r => r.GetOfertasDisponibles(10))
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
            var service = new CatalogosService(_uowFactoryMock.Object, CrearInscripcionesClient(handler));

            var result = await service.ObtenerTurnos(10, 20);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal(100, result.Data[0].IdOferta);
            Assert.Equal(1, result.Data[0].Turno.IdTurno);
            Assert.Equal("Matutino", result.Data[0].Turno.NombreTurno);
            Assert.Null(result.Data[0].HorarioReferencia);
            Assert.Equal(101, result.Data[1].IdOferta);
            Assert.Equal(2, result.Data[1].Turno.IdTurno);
            Assert.Equal("Nocturno", result.Data[1].Turno.NombreTurno);
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
            var service = new CatalogosService(_uowFactoryMock.Object, CrearInscripcionesClient(handler));

            var result = await service.ObtenerTurnos(99, 20);

            Assert.False(result.Success);
            Assert.Equal("CAT_TURNOS_02", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public void ObtenerFondosDeBecaPorProducto_ReturnsMappedItems()
        {
            var repo = new Mock<ITipoDescuentoRepository>();
            repo.Setup(r => r.GetFondosDeBecaVigentesPorProducto(10)).Returns(
            [
                new TipoDescuento
                {
                    IdTipoDescuento = 3,
                    NombreTipoDescuento = "Fondo A",
                    EsFijoTipoDescuento = "NO",
                    AliasTipoDescuento = "FA",
                    ReimputableTipoDescuento = "SI"
                }
            ]);
            _uowMock.Setup(u => u.TipoDescuentos).Returns(repo.Object);

            var result = _service.ObtenerFondosDeBecaPorProducto(10);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(3, item.IdTipoDescuento);
            Assert.Equal("Fondo A", item.NombreTipoDescuento);
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

            var result = _service.ObtenerInstituciones(1, 2);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(100, item.CodigoEmpresa);
            Assert.Equal("Instituto Ejemplo", item.Nombre);
        }

        private static InscripcionesyPagosApiClient CrearInscripcionesClient(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://internal.test/")
            };

            return new InscripcionesyPagosApiClient(
                httpClient,
                NullLogger<InscripcionesyPagosApiClient>.Instance);
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
