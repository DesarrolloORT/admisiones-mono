using System.Net;
using System.Text;
using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;
using Xunit;
using AppLogic.Services.Catalogos;

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
            _service = new CatalogosService(_uowFactoryMock.Object);
        }

        [Fact]
        public void ObtenerPaisesEstadosCiudades_ReturnsSlimPaisesEstadosCiudades()
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

            var result = _service.ObtenerPaisesEstadosCiudades();

            Assert.True(result.Success);
            Assert.Equal(nameof(CatalogosService.ObtenerPaisesEstadosCiudades), result.Method);
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
            Assert.Equal(4, result.Data.NivelConocimiento.Count);
            Assert.Equal(4, result.Data.DecisionCarrera.Count);
            Assert.Equal(5, result.Data.CompartidoCon.Count);
            Assert.Equal(7, result.Data.FormacionTutores.Count);
            Assert.Equal(3, result.Data.EstadoEducacionSuperior.Count);
            Assert.Equal(4, result.Data.DecisionUniversidad.Count);
            Assert.Equal(8, result.Data.AniosAprobadosEducacionSuperior.Count);
        }

        [Fact]
        public void ObtenerEncuestaInicial_UsesEmsLabelsForSecondaryDecisionOptions()
        {
            var result = _service.ObtenerEncuestaInicial();

            var decisionCarrera = result.Data!.DecisionCarrera.ToList();
            Assert.Collection(
                decisionCarrera,
                item =>
                {
                    Assert.Equal(2, item.Value);
                    Assert.Equal("1° EMS (4° año)", item.Label);
                },
                item =>
                {
                    Assert.Equal(3, item.Value);
                    Assert.Equal("2° EMS (5° año)", item.Label);
                },
                item =>
                {
                    Assert.Equal(4, item.Value);
                    Assert.Equal("3° EMS (6° año)", item.Label);
                },
                item =>
                {
                    Assert.Equal(0, item.Value);
                    Assert.Equal("Otro", item.Label);
                });

            Assert.Equal(decisionCarrera.Select(x => x.Value), result.Data.DecisionUniversidad.Select(x => x.Value));
            Assert.Equal(decisionCarrera.Select(x => x.Label), result.Data.DecisionUniversidad.Select(x => x.Label));
        }

        [Fact]
        public void ObtenerPais_PaisNotFound_ReturnsFailed()
        {
            var paisRepo = new Mock<IPaisRepository>();
            paisRepo.Setup(r => r.GetPaisConEstadosYCiudades(It.IsAny<long>())).Returns((Pais)null);
            _uowMock.Setup(u => u.Paises).Returns(paisRepo.Object);

            var result = _service.ObtenerPais(99);

            Assert.False(result.Success);
            Assert.Equal("FDP_GPAC_01", result.ErrorCode);
        }

        [Fact]
        public void ObtenerPais_WhenFound_SortsEstadosAndCiudades()
        {
            var paisRepo = new Mock<IPaisRepository>();
            paisRepo.Setup(r => r.GetPaisConEstadosYCiudades(54)).Returns(new Pais
            {
                CodigoPais = 54,
                Nombre = "Argentina",
                Estado =
                [
                    new Estado
                    {
                        CodigoPais = 54,
                        CodigoEstado = 2,
                        Nombre = "Buenos Aires",
                        Ciudad =
                        [
                            new Ciudad { CodigoPais = 54, CodigoEstado = 2, CodigoCiudad = 2, Nombre = "Zarate" },
                            new Ciudad { CodigoPais = 54, CodigoEstado = 2, CodigoCiudad = 1, Nombre = "Avellaneda" }
                        ]
                    },
                    new Estado
                    {
                        CodigoPais = 54,
                        CodigoEstado = 1,
                        Nombre = "Cordoba",
                        Ciudad =
                        [
                            new Ciudad { CodigoPais = 54, CodigoEstado = 1, CodigoCiudad = 2, Nombre = "Villa Carlos Paz" },
                            new Ciudad { CodigoPais = 54, CodigoEstado = 1, CodigoCiudad = 1, Nombre = "Cordoba Capital" }
                        ]
                    }
                ]
            });
            _uowMock.Setup(u => u.Paises).Returns(paisRepo.Object);

            var result = _service.ObtenerPais(54);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Buenos Aires", result.Data.Estado[0].Nombre);
            Assert.Equal("Cordoba", result.Data.Estado[1].Nombre);
            Assert.Equal("Avellaneda", result.Data.Estado[0].Ciudad[0].Nombre);
            Assert.Equal("Zarate", result.Data.Estado[0].Ciudad[1].Nombre);
            Assert.Equal("Cordoba Capital", result.Data.Estado[1].Ciudad[0].Nombre);
            Assert.Equal("Villa Carlos Paz", result.Data.Estado[1].Ciudad[1].Nombre);
        }

        [Fact]
        public void ObtenerTipoDocumentos_ReturnsMappedItems()
        {
            var repo = new Mock<IAcaTipoDocumentoRepository>();
            repo.Setup(r => r.GetAll()).Returns(
            [
                new AcaTipoDocumento { CodTipoDocumento = 1, Descripcion = "Cedula", DescrTd = "CI" },
                new AcaTipoDocumento { CodTipoDocumento = 2, Descripcion = "Pasaporte", DescrTd = "PA" }
            ]);
            _uowMock.Setup(u => u.AcaTipoDocumentos).Returns(repo.Object);

            var result = _service.ObtenerTipoDocumentos();

            Assert.True(result.Success);
            var list = result.Data!.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal((decimal)1, list[0].CodTipoDocumento);
            Assert.Equal("Cedula", list[0].Descripcion);
        }

        [Fact]
        public void ObtenerComienzos_ReturnsMappedItems()
        {
            var repo = new Mock<IProcesoRepository>();
            repo.Setup(r => r.GetProcesosHabilitadosPorProducto(10)).Returns(
            [
                new Proceso { IdProceso = 20, NombreProceso = "Marzo" }
            ]);
            _uowMock.Setup(u => u.Procesos).Returns(repo.Object);

            var result = _service.ObtenerComienzos(10);

            Assert.True(result.Success);
            Assert.Equal(nameof(CatalogosService.ObtenerComienzos), result.Method);
            var item = Assert.Single(result.Data!);
            Assert.Equal(20, item.IdProceso);
            Assert.Equal("Marzo", item.NombreProceso);
        }

        [Fact]
        public void ObtenerCarreras_ReturnsMappedItems()
        {
            var repo = new Mock<IProductoRepository>();
            repo.Setup(r => r.GetProductosVigentes()).Returns(
            [
                new Producto
                {
                    IdProducto = 10,
                    NombreProducto = "ATI",
                    NombreWebProducto = "ATI",
                    NombreExtensoProducto = "Analista en TI",
                    IdNivelProducto = 2,
                    NivelProducto = new NivelProducto { IdNivelProducto = 2, NombreNivelProducto = "Carrera" }
                }
            ]);
            _uowMock.Setup(u => u.Productos).Returns(repo.Object);

            var result = _service.ObtenerCarreras();

            Assert.True(result.Success);
            Assert.Equal(nameof(CatalogosService.ObtenerCarreras), result.Method);
            var item = Assert.Single(result.Data!);
            Assert.Equal(10, item.IdProducto);
            Assert.Equal("ATI", item.NombreProducto);
            Assert.Equal(2, item.IdNivelProducto);
            Assert.Equal("Carrera", item.NombreNivelProducto);
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
        public async Task ObtenerBancos_WithSuccess_ReturnsMappedResponse()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """
                {
                  "bancos": [
                    {
                      "idBanco": 10,
                      "nombreBanco": "Banco Uno",
                      "codigo": "B1",
                      "activo": true
                    }
                  ],
                  "totalCount": 1
                }
                """));
            var service = new CatalogosService(_uowFactoryMock.Object, CrearInscripcionesClient(handler));

            var result = await service.ObtenerBancos();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data!.TotalCount);
            var banco = Assert.Single(result.Data.Bancos);
            Assert.Equal(10, banco.IdBanco);
            Assert.Equal("Banco Uno", banco.NombreBanco);
            Assert.Equal("B1", banco.Codigo);
            Assert.True(banco.Activo);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("ORTSecure/Pagos/Bancos", request.RequestUri);
        }

        [Fact]
        public async Task ObtenerBancos_WhenApiRejects_ReturnsFailure()
        {
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, "error bancos"));
            var service = new CatalogosService(_uowFactoryMock.Object, CrearInscripcionesClient(handler));

            var result = await service.ObtenerBancos();

            Assert.False(result.Success);
            Assert.Equal("BANCOS_GET_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Contains("error bancos", result.Message);
        }

        [Fact]
        public void ObtenerMotivosEleccion_ReturnsMappedItems()
        {
            var repo = new Mock<BusinessLogic.IDevartRepositories.IMotivoOpcionesAdmisionRepository>();
            repo.Setup(r => r.GetAll()).Returns(
            [
                new MotivoOpcionesAdmision
                {
                    IdMotivo = 5,
                    NombreMotivo = "Prestigio",
                    FechaIngreso = DateTime.Today,
                    HoraIngreso = "10:00:00",
                    UsuarioIngreso = "USR"
                }
            ]);
            _uowMock.Setup(u => u.MotivoOpcionesAdmisions).Returns(repo.Object);

            var result = _service.ObtenerMotivosEleccion();

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(5, item.IdMotivo);
            Assert.Equal("Prestigio", item.NombreMotivo);
        }

        [Fact]
        public void ObtenerPublicidadesEleccion_ReturnsMappedItems()
        {
            var repo = new Mock<BusinessLogic.IDevartRepositories.IPublicidadOpcionesAdmisionRepository>();
            repo.Setup(r => r.GetAll()).Returns(new List<PublicidadOpcionesAdmision>
            {
                new PublicidadOpcionesAdmision
                {
                    IdPublicidad = 7,
                    NombrePublicidad = "Redes",
                    FechaIngreso = DateTime.Today,
                    HoraIngreso = "10:00:00",
                    UsuarioIngreso = "USR"
                }
            });
            _uowMock.Setup(u => u.PublicidadOpcionesAdmisions).Returns(repo.Object);

            var result = _service.ObtenerPublicidadesEleccion();

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(7, item.IdPublicidad);
            Assert.Equal("Redes", item.NombrePublicidad);
        }

        [Fact]
        public void ObtenerBachilleratos_ReturnsMappedItems()
        {
            var repo = new Mock<ITituloRepository>();
            repo.Setup(r => r.GetBachilleratosPorAnio(6)).Returns(
            [
                new Titulo
                {
                    CodigoTitulo = 10,
                    Nombre = "Informatica",
                    UsuarioIngreso = "USR",
                    FechaIngreso = DateTime.Today,
                    HoraIngreso = "10:00:00",
                    Bachillerato = "SI"
                }
            ]);
            _uowMock.Setup(u => u.Titulos).Returns(repo.Object);

            var result = _service.ObtenerBachilleratos(6);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(10, item.CodigoTitulo);
            Assert.Equal("Informatica", item.Nombre);
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
        public void ObtenerAnioBachiller_NotFound_ReturnsFailed()
        {
            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetWithRelated(10)).Returns((AnioBachiller)null);
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var result = _service.ObtenerAnioBachiller(10);

            Assert.False(result.Success);
            Assert.Equal("GEN_ANB_01", result.ErrorCode);
            Assert.Equal(204, result.HttpCode);
        }

        [Fact]
        public void ObtenerAnioBachiller_ReturnsDto()
        {
            var anioRepo = new Mock<IAnioBachillerRepository>();
            anioRepo.Setup(r => r.GetWithRelated(10)).Returns(new AnioBachiller
            {
                IdAnioBachiller = 10,
                NombreAnioBachiller = "Sexto",
                UsuarioIngreso = "USR",
                FechaIngreso = DateTime.Today,
                HoraIngreso = "10:00:00"
            });
            _uowMock.Setup(u => u.AnioBachillers).Returns(anioRepo.Object);

            var result = _service.ObtenerAnioBachiller(10);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal((decimal)10, result.Data.IdAnioBachiller);
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

        [Fact]
        public void ObtenerUniversidades_ReturnsMappedItems()
        {
            var repo = new Mock<IEmpresaRepository>();
            repo.Setup(r => r.GetUniversidades()).Returns(
            [
                new Empresa
                {
                    CodigoEmpresa = 200,
                    Nombre = "Universidad Ejemplo",
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

            var result = _service.ObtenerUniversidades();

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(200, item.CodigoEmpresa);
            Assert.Equal("Universidad Ejemplo", item.Nombre);
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
