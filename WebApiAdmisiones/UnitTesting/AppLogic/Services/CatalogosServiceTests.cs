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
        public void ObtenerPaises_ReturnsSortedPaises()
        {
            var paisRepo = new Mock<IPaisRepository>();
            paisRepo.Setup(r => r.GetPaisesOrdenados()).Returns(new List<Pais>
            {
                new Pais { CodigoPais = 1, Nombre = "Uruguay" },
                new Pais { CodigoPais = 2, Nombre = "Argentina" }
            });
            _uowMock.Setup(u => u.Paises).Returns(paisRepo.Object);

            var result = _service.ObtenerPaises();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            var list = new List<DtoPaisDevart>(result.Data);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].CodigoPais);
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
            productoRepo.Setup(r => r.GetProductosConInteresActivo(123)).Returns(new List<Producto>());
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = _service.ObtenerProductosBeca(123);

            Assert.True(result.Success);
            Assert.Single(result.Data!);
            Assert.Equal(10, new List<DtoProductoBeca>(result.Data!)[0].IdProducto);
        }
    }
}
