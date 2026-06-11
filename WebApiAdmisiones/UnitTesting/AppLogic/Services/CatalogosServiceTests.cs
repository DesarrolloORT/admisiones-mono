using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Configuration;
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

    }
}
