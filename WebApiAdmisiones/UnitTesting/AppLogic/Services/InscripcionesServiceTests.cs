using System.Collections.Generic;
using AppLogic.DTOs;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class InscripcionesServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly InscripcionesService _service;

        public InscripcionesServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _service = new InscripcionesService(_uowFactoryMock.Object);
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
        public void ObtenerProductosVigentesConInteres_ReturnsMappedItems()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetProductosVigentesConInteres(123)).Returns(new List<Producto>
            {
                new Producto
                {
                    IdProducto = 10,
                    NombreProducto = "Producto A",
                    NombreExtensoProducto = "Producto Extenso A",
                    IdNivelProducto = 2,
                    AliasProducto = "PA",
                    InscribibleProducto = "SI",
                    IntermedioProducto = "NO",
                    VisibleAdmisionesProducto = "SI",
                    ProcesoProductos = new List<ProcesoProducto>
                    {
                        new ProcesoProducto
                        {
                            IdProceso = 7,
                            Proceso = new Proceso { IdProceso = 7, NombreProceso = "Proceso A" }
                        }
                    }
                }
            });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = _service.ObtenerProductosVigentesConInteres(123);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(10, item.IdProducto);
            Assert.Equal("Producto A", item.NombreProducto);
            Assert.Equal(7, item.IdProceso);
            Assert.Equal("Proceso A", item.NombreProceso);
        }

        [Fact]
        public void ObtenerProductosConInteresActivo_ReturnsMappedItems()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetProductosConInteresActivo(123)).Returns(new List<Producto>
            {
                new Producto
                {
                    IdProducto = 10,
                    NombreProducto = "Producto A",
                    NombreExtensoProducto = "Producto Extenso A",
                    IdNivelProducto = 2,
                    ProcesoProductos = new List<ProcesoProducto>
                    {
                        new ProcesoProducto
                        {
                            IdProceso = 7,
                            Proceso = new Proceso { IdProceso = 7, NombreProceso = "Proceso A" }
                        }
                    }
                }
            });
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = _service.ObtenerProductosConInteresActivo(123);

            Assert.True(result.Success);
            var list = new List<DtoProductoAdmisiones>(result.Data!);
            Assert.Single(list);
            Assert.Equal(10, list[0].IdProducto);
            Assert.Equal(7, list[0].IdProceso);
            Assert.Equal("Proceso A", list[0].NombreProceso);
        }

        [Fact]
        public void ObtenerInscripcionesPendientes_AttachesRelatedInscripcion()
        {
            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.GetInscripcionesPendientes(123)).Returns(
            [
                new InstanciaWorkflow
                {
                    IdInstanciaWorkflow = 100,
                    IdProceso = 30,
                    FechaInicialInstanciaWf = new DateTime(2026, 1, 10)
                }
            ]);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var instWorkflowInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instWorkflowInscripcionRepo
                .Setup(r => r.GetByInstanciaIds(It.Is<IEnumerable<decimal>>(ids => ids.Count() == 1 && ids.First() == 100m)))
                .Returns(
                [
                    new InstWorkflowInscripcion
                    {
                        IdInstanciaWorkflow = 100,
                        IdProducto = 50,
                        IdTurno = 2,
                        IdComienzo = 3,
                        UsuarioIngreso = "USR",
                        FechaIngreso = DateTime.Today,
                        HoraIngreso = "10:00:00"
                    }
                ]);
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instWorkflowInscripcionRepo.Object);

            var result = _service.ObtenerInscripcionesPendientes(123);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(100m, item.IdInstanciaWorkflow);
            Assert.NotNull(item.InstWorkflowInscripcion);
            Assert.Equal(50m, item.InstWorkflowInscripcion.IdProducto);
        }

        [Fact]
        public void ObtenerInscripcionesRealizadas_ReturnsDistinctItemsByProduct()
        {
            var repo = new Mock<IInscriptoRepository>();
            repo.Setup(r => r.GetInscripcionesRealizadas(123)).Returns(
            [
                new Inscripto
                {
                    FechaInscr = new DateTime(2026, 2, 1),
                    Oferta = new Oferta
                    {
                        Turno = new Turno { NombreTurno = "Nocturno" },
                        Supraoferta = new Supraoferta
                        {
                            Comienzo = new Comienzo { NombreComienzo = "Marzo" },
                            Paquete = new Paquete
                            {
                                Producto = new Producto
                                {
                                    IdProducto = 10,
                                    NombreExtensoProducto = "Producto A"
                                }
                            }
                        }
                    }
                },
                new Inscripto
                {
                    FechaInscr = new DateTime(2026, 1, 10),
                    Oferta = new Oferta
                    {
                        Turno = new Turno { NombreTurno = "Manana" },
                        Supraoferta = new Supraoferta
                        {
                            Comienzo = new Comienzo { NombreComienzo = "Febrero" },
                            Paquete = new Paquete
                            {
                                Producto = new Producto
                                {
                                    IdProducto = 10,
                                    NombreExtensoProducto = "Producto A"
                                }
                            }
                        }
                    }
                }
            ]);
            _uowMock.Setup(u => u.Inscriptos).Returns(repo.Object);

            var result = _service.ObtenerInscripcionesRealizadas(123);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(10, item.IdProducto);
            Assert.Equal(new DateTime(2026, 1, 10), item.FechaInscripcion);
            Assert.Equal("Febrero", item.NombreComienzo);
            Assert.Equal("Manana", item.NombreTurno);
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
    }
}
