using System.Collections.Generic;
using AppLogic.DTOs;
using AppLogic.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Moq;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class InscripcionesServiceTests
    {
        private readonly Mock<IUnitOfWorkFactory> _uowFactoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDbConnectionContext> _dbConnectionContextMock;
        private readonly InscripcionesService _service;

        public InscripcionesServiceTests()
        {
            _uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            _uowMock = new Mock<IUnitOfWork>();
            _dbConnectionContextMock = new Mock<IDbConnectionContext>();
            _uowFactoryMock.Setup(f => f.Create()).Returns(_uowMock.Object);
            _dbConnectionContextMock
                .Setup(d => d.CurrentDateTime())
                .Returns(FechaBase);
            _service = new InscripcionesService(_uowFactoryMock.Object, _dbConnectionContextMock.Object);
        }

        private static readonly DateTime FechaBase = new(2026, 5, 27, 10, 30, 0);

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

            var intereRepo = new Mock<BusinessLogic.IDevartRepositories.IIntereRepository>();
            intereRepo
                .Setup(r => r.GetProcesoPorInteresActivo(123, 10))
                .Returns(new Proceso { IdProceso = 109, NombreProceso = "Marzo 2026" });
            _uowMock.Setup(u => u.Interes).Returns(intereRepo.Object);

            var result = _service.ObtenerProductosConInteresActivo(123);

            Assert.True(result.Success);
            var list = new List<DtoProductoAdmisiones>(result.Data!);
            Assert.Single(list);
            Assert.Equal(10, list[0].IdProducto);
            Assert.Equal(109, list[0].IdProceso);
            Assert.Equal("Marzo 2026", list[0].NombreProceso);
        }

        [Fact]
        public void ObtenerProductosVigentesConInteres_SinProceso_MapeaProcesoCero()
        {
            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetProductosVigentesConInteres(123)).Returns(
            [
                new Producto
                {
                    IdProducto = 10,
                    NombreProducto = "Producto A",
                    NombreExtensoProducto = "Producto Extenso A",
                    IdNivelProducto = 2,
                    ProcesoProductos = new List<ProcesoProducto>()
                }
            ]);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var result = _service.ObtenerProductosVigentesConInteres(123);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!);
            Assert.Equal(0, item.IdProceso);
            Assert.Null(item.NombreProceso);
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
            var encuesta = new EncuestaIniAdmision { IdEncuestaIni = 77, CodigoPersona = 123 };
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(encuesta);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetComienzoActivoPorProcesoOProducto(10, 20)).Returns(30);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(false);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_3100))
                .Returns(900)
                .Returns(901);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20 });

            Assert.True(result.Success);
            intereRepo.Verify(r => r.Add(It.Is<Intere>(i =>
                i.CodigoPersona == 123 &&
                i.IdProceso == 20 &&
                i.IdFormaContacto == 7m)), Times.Once);
            interesProductoRepo.Verify(r => r.Add(It.Is<InteresProducto>(i =>
                i.IdInteres == 500m &&
                i.IdProducto == 10 &&
                i.IdGradoInteres == 4m &&
                i.FechaInteresProd == FechaBase)), Times.Once);
            personaAdmiteRepo.Verify(r => r.Add(It.Is<PersonaAdmite>(p => p.CodigoPersona == 123)), Times.Once);
            Assert.Equal(20, encuesta.IdProceso);
            Assert.Equal(30, encuesta.IdComienzo);
            actividadRepo.Verify(r => r.Add(It.Is<Actividad>(a =>
                a.IdProceso == 20 &&
                a.IdTipoAccion == 109m &&
                a.FechaGeneradorActividad == FechaBase &&
                a.FechaRealizadoActividad == FechaBase)), Times.Once);
            accionRepo.Verify(r => r.Add(It.Is<Accion>(a =>
                a.CodigoPersona == 123 &&
                a.IdActividad == 900m &&
                a.FechaRealizadoAccion == FechaBase &&
                a.IdAccionResultado == 1m)), Times.Once);
            _uowMock.Verify(u => u.BeginTransaction(), Times.Once);
            _uowMock.Verify(u => u.Save(), Times.Never);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public void RegistrarInteresProducto_ConEncuestaSinComienzoActivo_DevuelveErrorYRollback()
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

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns(new EncuestaIniAdmision { IdEncuestaIni = 77, CodigoPersona = 123 });
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            procesoComienzoRepo.Setup(r => r.GetComienzoActivoPorProcesoOProducto(10, 20)).Returns((long?)null);
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20 });

            Assert.False(result.Success);
            Assert.Equal(400, result.HttpCode);
            Assert.Equal("GEN_IP_06", result.ErrorCode);
            _uowMock.Verify(u => u.Rollback(), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Never);
            actividadRepo.Verify(r => r.Add(It.IsAny<Actividad>()), Times.Never);
            accionRepo.Verify(r => r.Add(It.IsAny<Accion>()), Times.Never);
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

            var encuestaRepo = new Mock<IEncuestaIniAdmisionRepository>();
            encuestaRepo.Setup(r => r.GetByPersona(123)).Returns((EncuestaIniAdmision)null);
            _uowMock.Setup(u => u.EncuestaIniAdmisions).Returns(encuestaRepo.Object);

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(false);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);
            _dbConnectionContextMock
                .SetupSequence(d => d.NextId(DbConnectionContext.DbConnectionContextType.TO_3100))
                .Returns(900)
                .Returns(901);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20 });

            Assert.True(result.Success);
            Assert.Equal(4m, interesProductoProducto10.IdGradoInteres);
            Assert.Equal(0m, interesProductoProducto10.IdGradoInteresAnt);
            Assert.Equal(0m, interesProductoProducto11.IdGradoInteres);
            Assert.Equal(4m, interesProductoProducto11.IdGradoInteresAnt);
            Assert.Equal(5m, interesProductoProducto12.IdGradoInteres);
            interesProductoRepo.Verify(r => r.Update(It.IsAny<InteresProducto>()), Times.AtLeast(2));
            interesProductoRepo.Verify(r => r.Update(interesProductoProducto12), Times.Never);
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

            var procesoComienzoRepo = new Mock<IProcesoComienzoRepository>();
            _uowMock.Setup(u => u.ProcesoComienzos).Returns(procesoComienzoRepo.Object);

            var actividadRepo = new Mock<BusinessLogic.IDevartRepositories.IActividadRepository>();
            _uowMock.Setup(u => u.Actividads).Returns(actividadRepo.Object);

            var accionRepo = new Mock<BusinessLogic.IDevartRepositories.IAccionRepository>();
            accionRepo.Setup(r => r.ExisteAccionParaProcesoPersona(123, 20)).Returns(true);
            _uowMock.Setup(u => u.Accions).Returns(accionRepo.Object);

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20 });

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

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20 });

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

            var result = _service.RegistrarInteresProducto(123, new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20 });

            Assert.False(result.Success);
            Assert.Equal(409, result.HttpCode);
            Assert.Equal("GEN_IP_05", result.ErrorCode);
        }

        [Fact]
        public void ObtenerMisInscripciones_ReturnsConfirmadasPendientesYCanceladas()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetInscripcionesRealizadas(123)).Returns(
            [
                new Inscripto
                {
                    Oferta = new Oferta
                    {
                        Turno = new Turno { IdTurno = 3, NombreTurno = "Nocturno" },
                        Supraoferta = new Supraoferta
                        {
                            Comienzo = new Comienzo { IdComienzo = 4, NombreComienzo = "Marzo" },
                            Paquete = new Paquete
                            {
                                Producto = new Producto
                                {
                                    IdProducto = 5,
                                    NombreExtensoProducto = "Analista en Tecnologias de la Informacion"
                                }
                            }
                        }
                    }
                }
            ]);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.GetInscripcionesPendientes(123)).Returns(
            [
                new InstanciaWorkflow { IdInstanciaWorkflow = 100 }
            ]);
            workflowRepo.Setup(r => r.GetInscripcionesCanceladas(123)).Returns(
            [
                new InstanciaWorkflow { IdInstanciaWorkflow = 200 }
            ]);
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var instWorkflowInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instWorkflowInscripcionRepo
                .Setup(r => r.GetByInstanciaIds(It.Is<IEnumerable<decimal>>(ids => ids.Contains(100m))))
                .Returns(
                [
                    new InstWorkflowInscripcion
                    {
                        IdInstanciaWorkflow = 100,
                        IdProducto = 50,
                        IdComienzo = 60,
                        IdTurno = 70
                    }
                ]);
            instWorkflowInscripcionRepo
                .Setup(r => r.GetByInstanciaIds(It.Is<IEnumerable<decimal>>(ids => ids.Contains(200m))))
                .Returns(
                [
                    new InstWorkflowInscripcion
                    {
                        IdInstanciaWorkflow = 200,
                        IdProducto = 51,
                        IdComienzo = 61,
                        IdTurno = 71
                    }
                ]);
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instWorkflowInscripcionRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(
            [
                new Producto { IdProducto = 50, NombreExtensoProducto = "Licenciatura en Sistemas" },
                new Producto { IdProducto = 51, NombreExtensoProducto = "Analista Programador" }
            ]);
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var comienzoRepo = new Mock<IComienzoRepository>();
            comienzoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(
            [
                new Comienzo { IdComienzo = 60, NombreComienzo = "Agosto" },
                new Comienzo { IdComienzo = 61, NombreComienzo = "Octubre" }
            ]);
            _uowMock.Setup(u => u.Comienzos).Returns(comienzoRepo.Object);

            var turnoRepo = new Mock<ITurnoRepository>();
            turnoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(
            [
                new Turno { IdTurno = 70, NombreTurno = "Matutino" },
                new Turno { IdTurno = 71, NombreTurno = "Vespertino" }
            ]);
            _uowMock.Setup(u => u.Turnos).Returns(turnoRepo.Object);

            var result = _service.ObtenerMisInscripciones(123);

            Assert.True(result.Success);
            var items = result.Data!.ToList();
            Assert.Equal(3, items.Count);

            Assert.Equal("Confirmada", items[0].Estado);
            Assert.Equal(5, items[0].IdProducto);
            Assert.Equal("Analista en Tecnologias de la Informacion", items[0].NombreProducto);
            Assert.Equal(4, items[0].IdComienzo);
            Assert.Equal("Marzo", items[0].NombreComienzo);
            Assert.Equal(3, items[0].IdTurno);
            Assert.Equal("Nocturno", items[0].NombreTurno);

            Assert.Equal("Pendiente", items[1].Estado);
            Assert.Equal(50, items[1].IdProducto);
            Assert.Equal("Licenciatura en Sistemas", items[1].NombreProducto);
            Assert.Equal(60, items[1].IdComienzo);
            Assert.Equal("Agosto", items[1].NombreComienzo);
            Assert.Equal(70, items[1].IdTurno);
            Assert.Equal("Matutino", items[1].NombreTurno);

            Assert.Equal("Cancelada", items[2].Estado);
            Assert.Equal(51, items[2].IdProducto);
            Assert.Equal("Analista Programador", items[2].NombreProducto);
            Assert.Equal(61, items[2].IdComienzo);
            Assert.Equal("Octubre", items[2].NombreComienzo);
            Assert.Equal(71, items[2].IdTurno);
            Assert.Equal("Vespertino", items[2].NombreTurno);
        }

        [Fact]
        public void ObtenerMisInscripciones_ConDatosFaltantes_UsaValoresPorDefecto()
        {
            var inscriptoRepo = new Mock<IInscriptoRepository>();
            inscriptoRepo.Setup(r => r.GetInscripcionesRealizadas(123)).Returns(
            [
                new Inscripto { Oferta = null }
            ]);
            _uowMock.Setup(u => u.Inscriptos).Returns(inscriptoRepo.Object);

            var workflowRepo = new Mock<IInstanciaWorkflowRepository>();
            workflowRepo.Setup(r => r.GetInscripcionesPendientes(123)).Returns(
            [
                new InstanciaWorkflow { IdInstanciaWorkflow = 100 }
            ]);
            workflowRepo.Setup(r => r.GetInscripcionesCanceladas(123)).Returns(new List<InstanciaWorkflow>());
            _uowMock.Setup(u => u.InstanciaWorkflows).Returns(workflowRepo.Object);

            var instWorkflowInscripcionRepo = new Mock<IInstWorkflowInscripcionRepository>();
            instWorkflowInscripcionRepo
                .Setup(r => r.GetByInstanciaIds(It.IsAny<IEnumerable<decimal>>()))
                .Returns(new List<InstWorkflowInscripcion>());
            _uowMock.Setup(u => u.InstWorkflowInscripcions).Returns(instWorkflowInscripcionRepo.Object);

            var productoRepo = new Mock<IProductoRepository>();
            productoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Producto>());
            _uowMock.Setup(u => u.Productos).Returns(productoRepo.Object);

            var comienzoRepo = new Mock<IComienzoRepository>();
            comienzoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Comienzo>());
            _uowMock.Setup(u => u.Comienzos).Returns(comienzoRepo.Object);

            var turnoRepo = new Mock<ITurnoRepository>();
            turnoRepo.Setup(r => r.GetByKeys(It.IsAny<IEnumerable<long>>())).Returns(new List<Turno>());
            _uowMock.Setup(u => u.Turnos).Returns(turnoRepo.Object);

            var result = _service.ObtenerMisInscripciones(123);

            Assert.True(result.Success);
            var items = result.Data!.ToList();
            Assert.Equal(2, items.Count);
            Assert.All(items, item =>
            {
                Assert.Equal(0, item.IdProducto);
                Assert.Null(item.NombreProducto);
                Assert.Equal(0, item.IdComienzo);
                Assert.Null(item.NombreComienzo);
                Assert.Equal(0, item.IdTurno);
                Assert.Null(item.NombreTurno);
            });
        }

        [Fact]
        public void DtoInscripcionHome_ExponeSoloCamposRequeridos()
        {
            var propiedades = typeof(DtoInscripcionHome)
                .GetProperties()
                .Select(p => p.Name)
                .OrderBy(name => name)
                .ToList();

            Assert.Equal(
            [
                nameof(DtoInscripcionHome.Estado),
                nameof(DtoInscripcionHome.IdComienzo),
                nameof(DtoInscripcionHome.IdProducto),
                nameof(DtoInscripcionHome.IdTurno),
                nameof(DtoInscripcionHome.NombreComienzo),
                nameof(DtoInscripcionHome.NombreProducto),
                nameof(DtoInscripcionHome.NombreTurno)
            ], propiedades);
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
        public void TieneDerechoAEncuestaInicial_PersonaInexistente_ReturnsNotFound()
        {
            SetupPersona(null);

            var result = _service.TieneDerechoAEncuestaInicial(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_TEI_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void TieneDerechoAEncuestaInicial_DocumentoInvalido_ReturnsBadRequest()
        {
            SetupPersona(new Persona
            {
                CodigoPersona = 123,
                TipoDocumento = string.Empty,
                Documento = "123"
            });

            var result = _service.TieneDerechoAEncuestaInicial(123);

            Assert.False(result.Success);
            Assert.Equal("GEN_TEI_02", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public void TieneDerechoAEncuestaInicial_ExisteEnFrescos_ReturnsFalse()
        {
            SetupPersonaValida();
            SetupReposDerechoEncuesta(existeFresco: true);

            var result = _service.TieneDerechoAEncuestaInicial(123);

            Assert.True(result.Success);
            Assert.False(result.Data);
        }

        [Fact]
        public void TieneDerechoAEncuestaInicial_ExisteEnEncuestaIni_ReturnsFalse()
        {
            SetupPersonaValida();
            SetupReposDerechoEncuesta(existeEncuestaIni: true);

            var result = _service.TieneDerechoAEncuestaInicial(123);

            Assert.True(result.Success);
            Assert.False(result.Data);
        }

        [Fact]
        public void TieneDerechoAEncuestaInicial_ExisteEncuestaCompleta_ReturnsFalse()
        {
            SetupPersonaValida();
            SetupReposDerechoEncuesta(existeEncuestaCompleta: true);

            var result = _service.TieneDerechoAEncuestaInicial(123);

            Assert.True(result.Success);
            Assert.False(result.Data);
        }

        [Fact]
        public void TieneDerechoAEncuestaInicial_NoExisteEnNingunaFuente_ReturnsTrue()
        {
            SetupPersonaValida();
            SetupReposDerechoEncuesta();

            var result = _service.TieneDerechoAEncuestaInicial(123);

            Assert.True(result.Success);
            Assert.True(result.Data);
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
