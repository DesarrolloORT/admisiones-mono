using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using Xunit;

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
            repo.Setup(r => r.GetProductosVigentesParaRegistro()).Returns(
            [
                new Producto
                {
                    IdProducto = 10,
                    NombreProducto = "ATI",
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

        [Fact]
        public void ObtenerProductosBeca_ReturnsDistinctItemsByProduct()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetInscripcionesRealizadas(123)).Returns(new List<Inscripto>
            {
                new Inscripto
                {
                    FechaInscr = new System.DateTime(2024, 1, 1),
                    Oferta = new Oferta
                    {
                        Turno = new Turno { NombreTurno = "Matutino" },
                        Supraoferta = new Supraoferta
                        {
                            Comienzo = new Comienzo
                            {
                                NombreComienzo = "Marzo",
                                ProcesoComienzos = new List<ProcesoComienzo> { new ProcesoComienzo { IdProceso = 8 } }
                            },
                            Paquete = new Paquete
                            {
                                Producto = new Producto
                                {
                                    IdProducto = 10,
                                    IdNivelProducto = 2,
                                    NombreExtensoProducto = "Producto A"
                                }
                            }
                        }
                    }
                }
            });
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.GetInscripcionesPendientes(123)).Returns(new List<InstanciaWorkflow>());
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var instWorkflowInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instWorkflowInscripcionRepo.Setup(r => r.GetByInstanciaIds(It.IsAny<IEnumerable<decimal>>())).Returns(new List<InstWorkflowInscripcion>());
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instWorkflowInscripcionRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Producto>());
            productoRepo.Setup(r => r.GetProductosConInteresActivo(123)).Returns(new List<Producto>());
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var comienzoRepo = new Mock<BusinessLogic.IDevartRepositories.IComienzoRepository>();
            comienzoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Comienzo>());
            _uowMock.Setup(u => u.Comienzos).Returns(comienzoRepo.Object);

            var turnoRepo = new Mock<ITurnoRepository>();
            turnoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Turno>());
            _uowMock.Setup(u => u.Turnos).Returns(turnoRepo.Object);

            var result = _service.ObtenerProductosBeca(123);

            Assert.True(result.Success);
            Assert.Single(result.Data!);
            Assert.Equal(10, new List<DtoProductoBeca>(result.Data!)[0].IdProducto);
        }

        [Fact]
        public void ObtenerProductosBeca_RealizadaConOfertaNula_UsaValoresPorDefecto()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetInscripcionesRealizadas(123)).Returns(
            [
                new Inscripto
                {
                    FechaInscr = null,
                    Oferta = null
                }
            ]);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.GetInscripcionesPendientes(123)).Returns(new List<InstanciaWorkflow>());
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var instWorkflowInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instWorkflowInscripcionRepo.Setup(r => r.GetByInstanciaIds(It.IsAny<IEnumerable<decimal>>())).Returns(new List<InstWorkflowInscripcion>());
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instWorkflowInscripcionRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Producto>());
            productoRepo.Setup(r => r.GetProductosConInteresActivo(123)).Returns(new List<Producto>());
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var comienzoRepo = new Mock<BusinessLogic.IDevartRepositories.IComienzoRepository>();
            comienzoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Comienzo>());
            _uowMock.Setup(u => u.Comienzos).Returns(comienzoRepo.Object);

            var turnoRepo = new Mock<ITurnoRepository>();
            turnoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Turno>());
            _uowMock.Setup(u => u.Turnos).Returns(turnoRepo.Object);

            var result = _service.ObtenerProductosBeca(123);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(0, item.IdProducto);
            Assert.Equal(0, item.IdNivelProducto);
            Assert.Null(item.NombreProducto);
            Assert.Null(item.NombreComienzo);
            Assert.Null(item.NombreTurno);
        }

        [Fact]
        public void ObtenerProductosBeca_CubrePendientesEInteresesConValoresFaltantes()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetInscripcionesRealizadas(123)).Returns(new List<Inscripto>());
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.GetInscripcionesPendientes(123)).Returns(
            [
                new InstanciaWorkflow { IdInstanciaWorkflow = 1, IdProceso = 5, FechaInicialInstanciaWf = new DateTime(2026, 1, 1) },
                new InstanciaWorkflow { IdInstanciaWorkflow = 2, IdProceso = 6, FechaInicialInstanciaWf = new DateTime(2026, 1, 2) },
                new InstanciaWorkflow { IdInstanciaWorkflow = 3, IdProceso = 7, FechaInicialInstanciaWf = new DateTime(2026, 1, 3) }
            ]);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var instWorkflowInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instWorkflowInscripcionRepo.Setup(r => r.GetByInstanciaIds(It.IsAny<IEnumerable<decimal>>())).Returns(
            [
                new InstWorkflowInscripcion { IdInstanciaWorkflow = 2, IdProducto = null },
                new InstWorkflowInscripcion { IdInstanciaWorkflow = 3, IdProducto = 20, IdComienzo = null, IdTurno = null }
            ]);
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instWorkflowInscripcionRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(
            [
                new Producto { IdProducto = 20, IdNivelProducto = 4, NombreExtensoProducto = "Producto pendiente" }
            ]);
            productoRepo.Setup(r => r.GetProductosConInteresActivo(123)).Returns(
            [
                new Producto
                {
                    IdProducto = 30,
                    IdNivelProducto = 2,
                    NombreExtensoProducto = "Producto interes",
                    ProcesoProductos = null
                }
            ]);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var comienzoRepo = new Mock<BusinessLogic.IDevartRepositories.IComienzoRepository>();
            comienzoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Comienzo>());
            _uowMock.Setup(u => u.Comienzos).Returns(comienzoRepo.Object);

            var turnoRepo = new Mock<ITurnoRepository>();
            turnoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Turno>());
            _uowMock.Setup(u => u.Turnos).Returns(turnoRepo.Object);

            var result = _service.ObtenerProductosBeca(123);

            Assert.True(result.Success);
            var list = result.Data!.OrderBy(x => x.IdProducto).ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal(20, list[0].IdProducto);
            Assert.Null(list[0].NombreComienzo);
            Assert.Null(list[0].NombreTurno);
            Assert.Equal(30, list[1].IdProducto);
            Assert.Equal(0, list[1].IdProceso);
        }

        [Fact]
        public void ObtenerProductosBeca_PendientesConIdsRepetidos_UsaCargaBatchYMantieneResultado()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetInscripcionesRealizadas(123)).Returns(new List<Inscripto>());
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.GetInscripcionesPendientes(123)).Returns(
            [
                new InstanciaWorkflow { IdInstanciaWorkflow = 1, IdProceso = 5, FechaInicialInstanciaWf = new DateTime(2026, 1, 2) },
                new InstanciaWorkflow { IdInstanciaWorkflow = 2, IdProceso = 5, FechaInicialInstanciaWf = new DateTime(2026, 1, 1) }
            ]);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var instWorkflowInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instWorkflowInscripcionRepo.Setup(r => r.GetByInstanciaIds(It.IsAny<IEnumerable<decimal>>())).Returns(
            [
                new InstWorkflowInscripcion { IdInstanciaWorkflow = 1, IdProducto = 20, IdComienzo = 30, IdTurno = 40 },
                new InstWorkflowInscripcion { IdInstanciaWorkflow = 2, IdProducto = 20, IdComienzo = 30, IdTurno = 40 }
            ]);
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instWorkflowInscripcionRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKeys(It.Is<IEnumerable<long>>(ids => ids.Single() == 20))).Returns(
            [
                new Producto { IdProducto = 20, IdNivelProducto = 4, NombreExtensoProducto = "Producto pendiente" }
            ]);
            productoRepo.Setup(r => r.GetProductosConInteresActivo(123)).Returns(new List<Producto>());
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var comienzoRepo = new Mock<BusinessLogic.IDevartRepositories.IComienzoRepository>();
            comienzoRepo.Setup(r => r.GetByKeys(It.Is<IEnumerable<long>>(ids => ids.Single() == 30))).Returns(
            [
                new Comienzo { IdComienzo = 30, NombreComienzo = "Marzo" }
            ]);
            _uowMock.Setup(u => u.Comienzos).Returns(comienzoRepo.Object);

            var turnoRepo = new Mock<ITurnoRepository>();
            turnoRepo.Setup(r => r.GetByKeys(It.Is<IEnumerable<long>>(ids => ids.Single() == 40))).Returns(
            [
                new Turno { IdTurno = 40, NombreTurno = "Matutino" }
            ]);
            _uowMock.Setup(u => u.Turnos).Returns(turnoRepo.Object);

            var result = _service.ObtenerProductosBeca(123);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(20, item.IdProducto);
            Assert.Equal("Producto pendiente", item.NombreProducto);
            Assert.Equal("Marzo", item.NombreComienzo);
            Assert.Equal("Matutino", item.NombreTurno);
            Assert.Equal(new DateTime(2026, 1, 1), item.FechaInscripcion);

            productoRepo.Verify(r => r.GetByKeys(It.IsAny<IEnumerable<long>>()), Times.Once);
            productoRepo.Verify(r => r.GetByKey(It.IsAny<long>()), Times.Never);
            comienzoRepo.Verify(r => r.GetByKeys(It.IsAny<IEnumerable<long>>()), Times.Once);
            turnoRepo.Verify(r => r.GetByKeys(It.IsAny<IEnumerable<long>>()), Times.Once);
        }

        [Fact]
        public void ObtenerProductosBeca_PendientesConRelacionFaltanteMantieneDefaults()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetInscripcionesRealizadas(123)).Returns(new List<Inscripto>());
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.GetInscripcionesPendientes(123)).Returns(
            [
                new InstanciaWorkflow { IdInstanciaWorkflow = 1, IdProceso = 5, FechaInicialInstanciaWf = new DateTime(2026, 1, 2) }
            ]);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var instWorkflowInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instWorkflowInscripcionRepo.Setup(r => r.GetByInstanciaIds(It.IsAny<IEnumerable<decimal>>())).Returns(
            [
                new InstWorkflowInscripcion { IdInstanciaWorkflow = 1, IdProducto = 20, IdComienzo = 30, IdTurno = 40 }
            ]);
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instWorkflowInscripcionRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Producto>());
            productoRepo.Setup(r => r.GetProductosConInteresActivo(123)).Returns(new List<Producto>());
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var comienzoRepo = new Mock<BusinessLogic.IDevartRepositories.IComienzoRepository>();
            comienzoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Comienzo>());
            _uowMock.Setup(u => u.Comienzos).Returns(comienzoRepo.Object);

            var turnoRepo = new Mock<ITurnoRepository>();
            turnoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Turno>());
            _uowMock.Setup(u => u.Turnos).Returns(turnoRepo.Object);

            var result = _service.ObtenerProductosBeca(123);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(20, item.IdProducto);
            Assert.Equal(0, item.IdNivelProducto);
            Assert.Null(item.NombreProducto);
            Assert.Null(item.NombreComienzo);
            Assert.Null(item.NombreTurno);
            Assert.Equal(5, item.IdProceso);
        }
    }
}
